using MassTransit;
using Microsoft.Extensions.Logging;
using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly IBus _bus;
        private readonly ILogger<TelemetryService> _logger;
        private readonly List<TelemetryData> _buffer = [];
        private readonly object _bufferLock = new();
        private readonly int _batchSize;
        
        public TelemetryService(
            IBus bus,
            ILogger<TelemetryService> logger,
            int batchSize = 100)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _batchSize = batchSize;
        }

        public async Task ProcessTelemetryData(TelemetryData data)
        {
            if (data == null)
                return;

            var shouldPublish = false;
            List<TelemetryData> itemsToPublish = null;

            lock (_bufferLock)
            {
                _buffer.Add(data);
                
                if (_buffer.Count >= _batchSize)
                {
                    itemsToPublish = new List<TelemetryData>(_buffer);
                    _buffer.Clear();
                    shouldPublish = true;
                }
            }

            if (shouldPublish && itemsToPublish is not null)
            {
                await PublishItemsAsync(itemsToPublish);
            }
        }

        private async Task PublishItemsAsync(List<TelemetryData> items)
        {
            try
            {
                foreach (var item in items)
                {
                    await _bus.Publish(item);
                }

                _logger.LogInformation("Published {count} individual telemetry items", items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish telemetry items");
                
                lock (_bufferLock)
                {
                    _buffer.AddRange(items);
                }
            }
        }
    }
}