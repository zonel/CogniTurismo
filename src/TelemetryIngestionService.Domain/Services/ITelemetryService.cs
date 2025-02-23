using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Domain.Services;

public interface ITelemetryService
{
    Task ProcessTelemetryData(TelemetryData data);
}