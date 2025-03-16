using MassTransit;
using Microsoft.Extensions.Logging;
using TelemetryIngestionService.Domain.Models;
using Microsoft.Extensions.Options;
using TelemetryIngestionService.Infrastructure.Configuration.TelemetryIngestionService.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TelemetryIngestionService.Infrastructure.Services
{
    public class TelemetryService(
        IServiceProvider serviceProvider,
        IOptions<MassTransitSettings> options,
        ILogger<TelemetryService> logger,
        int batchSize = 100)
        : ITelemetryService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        private readonly ILogger<TelemetryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly MassTransitSettings _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly List<TelemetryData> _buffer = [];
        private readonly object _bufferLock = new();

        public async Task ProcessTelemetryData(TelemetryData data)
        {
            if (data == null)
                return;

            var shouldPublish = false;
            List<TelemetryData> itemsToPublish = null!;

            lock (_bufferLock)
            {
                _buffer.Add(data);
                
                if (_buffer.Count >= batchSize)
                {
                    itemsToPublish = new List<TelemetryData>(_buffer);
                    _buffer.Clear();
                    shouldPublish = true;
                }
            }

            if (shouldPublish && itemsToPublish != null)
            {
                await PublishItemsAsync(itemsToPublish);
            }
        }
        
        public async Task FlushBufferAsync()
        {
            List<TelemetryData> itemsToPublish = null;
            
            lock (_bufferLock)
            {
                if (_buffer.Count > 0)
                {
                    itemsToPublish = new List<TelemetryData>(_buffer);
                    _buffer.Clear();
                }
            }
            
            if (itemsToPublish != null && itemsToPublish.Count > 0)
            {
                await PublishItemsAsync(itemsToPublish);
            }
        }

        private async Task PublishItemsAsync(List<TelemetryData> items)
        {
            try
            {
                _logger.LogInformation("Attempting to publish {count} telemetry items to Kafka topic: {topic}", 
                    items.Count, _settings.Topic);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var producer = scope.ServiceProvider.GetRequiredService<ITopicProducer<TelemetryData>>();
                    
                    foreach (var item in items)
                    {
                        await producer.Produce(item)!;
                    }
                }

                _logger.LogInformation("Successfully published {count} telemetry items to Kafka topic: {topic}", 
                    items.Count, _settings.Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish telemetry items to Kafka topic {topic}: {Message}", 
                    _settings.Topic, ex.Message);
                
                lock (_bufferLock)
                {
                    _buffer.AddRange(items);
                }
            }
        }
    }
}