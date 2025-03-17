using TelemetryProcessorService.Models;

namespace TelemetryProcessorService.Persistence;

public interface ITelemetryDataStorageService
{
    Task StoreTelemetryDataAsync(TelemetryData data);
}