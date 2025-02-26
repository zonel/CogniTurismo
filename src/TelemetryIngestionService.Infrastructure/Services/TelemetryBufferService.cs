using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryIngestionService.Domain.Models;
using TelemetryIngestionService.Infrastructure.Configuration;
using TelemetryIngestionService.Infrastructure.Repositories;

namespace TelemetryIngestionService.Infrastructure.Services
{
    public class TelemetryBufferService(
        IServiceProvider serviceProvider,
        IOptions<BufferSettings> bufferSettings,
        ILogger<TelemetryBufferService> logger)
        : BackgroundService, ITelemetryBufferService
    {
        private readonly ConcurrentBag<TelemetryData> _telemetryBuffer = new();
        private readonly BufferSettings _bufferSettings = bufferSettings.Value;
        private readonly SemaphoreSlim _flushSemaphore = new(1, 1);
        private DateTime _lastFlushTime = DateTime.UtcNow;

        public void AddTelemetryData(TelemetryData data)
        {
            _telemetryBuffer.Add(data);
            
            // Trigger immediate flush if buffer threshold is reached
            if (_telemetryBuffer.Count >= _bufferSettings.BatchSize)
            {
                _ = FlushBufferAsync();
            }
        }
        
        private async Task FlushBufferAsync()
        {
            // Use semaphore to prevent concurrent flushes
            if (!await _flushSemaphore.WaitAsync(0))
            {
                return; // Another flush is in progress
            }
            
            try
            {
                var records = new List<TelemetryData>();
                
                // Extract records from the buffer
                while (_telemetryBuffer.TryTake(out var item) && records.Count < _bufferSettings.MaxBatchSize)
                {
                    records.Add(item);
                }
                
                if (records.Count == 0)
                    return;
                
                logger.LogInformation($"Flushing {records.Count} telemetry records to database");
                
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                try
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var telemetryRepository = scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();
                        
                        // Use COPY command for maximum insert performance when batch size is large
                        if (records.Count > _bufferSettings.CopyThreshold)
                        {
                            await telemetryRepository.BulkInsertWithCopyAsync(records);
                        }
                        else
                        {
                            await telemetryRepository.BulkInsertTelemetryDataAsync(records);
                        }
                    }
                    
                    stopwatch.Stop();
                    logger.LogInformation($"Successfully inserted {records.Count} records in {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error flushing telemetry buffer: {ex.Message}");
                    
                    // If insertion fails, put records back into the buffer for retry
                    foreach (var record in records)
                    {
                        _telemetryBuffer.Add(record);
                    }
                }
                
                _lastFlushTime = DateTime.UtcNow;
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Periodically flush the buffer even if it's not full
                var timeSinceLastFlush = DateTime.UtcNow - _lastFlushTime;
                if (_telemetryBuffer.Count > 0 && timeSinceLastFlush >= TimeSpan.FromMilliseconds(_bufferSettings.FlushIntervalMs))
                {
                    await FlushBufferAsync();
                }
                
                // Wait before checking again
                await Task.Delay(_bufferSettings.CheckIntervalMs, stoppingToken);
            }
            
            // Final flush when shutting down
            if (_telemetryBuffer.Count > 0)
            {
                await FlushBufferAsync();
            }
        }
    }
}