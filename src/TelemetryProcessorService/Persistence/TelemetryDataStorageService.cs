using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;

namespace TelemetryProcessorService.Persistence
{
    public class TelemetryDataStorageService(
        IOptions<CitusDbSettings> dbOptions,
        ILogger<TelemetryDataStorageService> logger)
        : ITelemetryDataStorageService
    {
        private readonly CitusDbSettings _dbSettings = dbOptions?.Value ?? throw new ArgumentNullException(nameof(dbOptions));
        private readonly ILogger<TelemetryDataStorageService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task StoreTelemetryDataAsync(TelemetryData data)
        {
            var tableName = string.IsNullOrWhiteSpace(_dbSettings.SchemaName)
                ? _dbSettings.TableName
                : $"{_dbSettings.SchemaName}.{_dbSettings.TableName}";

            var sql = $@"
                INSERT INTO {tableName} 
                    (id, vehicle_id, latitude, longitude, speed, battery_percentage, battery_temperature, recorded_at)
                VALUES 
                    (@Id, @VehicleId, @Latitude, @Longitude, @Speed, @BatteryPercentage, @BatteryTemperature, @RecordedAt)
                ON CONFLICT (vehicle_id, recorded_at) DO NOTHING;
            ";

            try
            {
                await using var connection = new NpgsqlConnection(_dbSettings.ConnectionString);
                await connection.OpenAsync();

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    data.Id,
                    data.VehicleId,
                    data.Latitude,
                    data.Longitude,
                    data.Speed,
                    data.BatteryPercentage,
                    data.BatteryTemperature,
                    data.RecordedAt
                });

                _logger.LogInformation("Stored telemetry data with ID: {Id} in database. Rows affected: {RowsAffected}",
                    data.Id, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing telemetry data with ID: {Id}", data.Id);
                throw;
            }
        }
    }
}