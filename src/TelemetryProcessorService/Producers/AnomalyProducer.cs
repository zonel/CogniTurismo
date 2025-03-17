using MassTransit;
using Microsoft.Extensions.Logging;
using TelemetryProcessorService.Models;

namespace TelemetryProcessorService.Producers
{
    public class AnomalyProducer(
        ILogger<AnomalyProducer> logger,
        ITopicProducer<AnomalyEvent> producer)
        : IAnomalyProducer
    {
        private readonly ILogger<AnomalyProducer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ITopicProducer<AnomalyEvent> _producer = producer ?? throw new ArgumentNullException(nameof(producer));

        public async Task PublishAnomalyAsync(TelemetryData data, AnomalyType type, double value, AnomalySeverity severity)
        {
            var anomalyEvent = new AnomalyEvent
            {
                Id = Guid.NewGuid(),
                TelemetryId = data.Id,
                VehicleId = data.VehicleId,
                Type = type,
                Value = value,
                Severity = severity,
                DetectedAt = DateTime.UtcNow
            };

            try
            {
                await _producer.Produce(anomalyEvent)!;
                _logger.LogInformation("Anomaly event published with ID: {Id}, Type: {Type}, Severity: {Severity} at {Time}", 
                    anomalyEvent.Id, anomalyEvent.Type, anomalyEvent.Severity, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to publish anomaly event with ID: {Id}", anomalyEvent.Id);
                throw;
            }
        }
    }
}