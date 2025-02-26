using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Repositories;

public interface ITelemetryRepository
{
    Task BulkInsertTelemetryDataAsync(IEnumerable<TelemetryData> telemetryBatch);
    Task<ulong> BulkInsertWithCopyAsync(IEnumerable<TelemetryData> telemetryBatch);
}