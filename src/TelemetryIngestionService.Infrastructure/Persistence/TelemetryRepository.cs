using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Persistence;

public class TelemetryRepository(TelemetryDbContext context)
{
    public async Task AddAsync(TelemetryData data)
    {
        await context.Telemetry.AddAsync(data);
        await context.SaveChangesAsync();
    }
}