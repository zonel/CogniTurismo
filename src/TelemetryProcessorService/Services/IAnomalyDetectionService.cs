using TelemetryProcessorService.Models;

namespace TelemetryProcessorService.Services;

public interface IAnomalyDetectionService
{
    Task CheckForAnomaliesAsync(TelemetryData data);
}