using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Services;

public interface ITelemetryService
{
    Task ProcessTelemetryData(TelemetryData data);
}