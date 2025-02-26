using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using TelemetryIngestionService.Domain.Models;
using TelemetryIngestionService.Infrastructure.Configuration;

namespace TelemetryIngestionService.Infrastructure.Repositories
{
    public class TelemetryRepository(IOptions<DatabaseSettings> dbSettings) : ITelemetryRepository
    {
        private readonly string _connectionString = dbSettings.Value.ConnectionString;

        public async Task BulkInsertTelemetryDataAsync(IEnumerable<TelemetryData> telemetryBatch)
        {
            if (!telemetryBatch.Any())
                return;
                
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var transaction = await connection.BeginTransactionAsync();
            
            try
            {
                const string sql = @"
                    INSERT INTO telemetry_data 
                    (id, vehicle_id, latitude, longitude, speed, battery_percentage, battery_temperature, timestamp)
                    VALUES 
                    (@Id, @VehicleId, @Latitude, @Longitude, @Speed, @BatteryPercentage, @BatteryTemperature, @Timestamp)";

                await connection.ExecuteAsync(sql, telemetryBatch, transaction);
                
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        
        public async Task<ulong> BulkInsertWithCopyAsync(IEnumerable<TelemetryData> telemetryBatch)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(Guid));
            dataTable.Columns.Add("VehicleId", typeof(string));
            dataTable.Columns.Add("Latitude", typeof(double));
            dataTable.Columns.Add("Longitude", typeof(double));
            dataTable.Columns.Add("Speed", typeof(double));
            dataTable.Columns.Add("BatteryPercentage", typeof(double));
            dataTable.Columns.Add("BatteryTemperature", typeof(double));
            dataTable.Columns.Add("Timestamp", typeof(DateTime));

            foreach (var item in telemetryBatch)
            {
                dataTable.Rows.Add(
                    item.Id,
                    item.VehicleId,
                    item.Latitude,
                    item.Longitude,
                    item.Speed,
                    item.BatteryPercentage,
                    item.BatteryTemperature,
                    item.Timestamp
                );
            }
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var writer = connection.BeginBinaryImport("COPY telemetry_data (id, vehicle_id, latitude, longitude, speed, battery_percentage, battery_temperature, timestamp) FROM STDIN BINARY");
            
            foreach (var item in telemetryBatch)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(item.Id, NpgsqlTypes.NpgsqlDbType.Uuid);
                await writer.WriteAsync(item.VehicleId, NpgsqlTypes.NpgsqlDbType.Text);
                await writer.WriteAsync(item.Latitude, NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(item.Longitude, NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(item.Speed, NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(item.BatteryPercentage, NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(item.BatteryTemperature, NpgsqlTypes.NpgsqlDbType.Double);
                await writer.WriteAsync(item.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
            }
            
            return await writer.CompleteAsync();
        }
    }
}