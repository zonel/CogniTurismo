using TelemetryIngestionService.Domain.Models;
using TelemetryIngestionService.Domain.Services;
using TelemetryIngestionService.Infrastructure.Persistence;

namespace TelemetryIngestionService.Infrastructure.Messaging;

public class TelemetryService(TelemetryRepository repository) : ITelemetryService
{
    public async Task ProcessTelemetryData(TelemetryData data)
    {
        await repository.AddAsync(data);
    }
}