using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Services;

public interface ITelemetryBufferService
{
    void AddTelemetryData(TelemetryData data);
}