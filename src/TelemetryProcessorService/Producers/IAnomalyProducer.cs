using TelemetryProcessorService.Models;

namespace TelemetryProcessorService.Producers;

public interface IAnomalyProducer
{
    Task PublishAnomalyAsync(TelemetryData data, AnomalyType type, double value, AnomalySeverity severity);
}