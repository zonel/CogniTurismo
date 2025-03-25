using AnalyticsService.Data;
using AnalyticsService.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Dynamic;

namespace AnalyticsService.Repositories;

public class AnalyticsRepository(AnalyticsDbContext dbContext)
{
    public async Task<VehicleBatteryStatistics?> GetVehicleBatteryStatisticsAsync(string vehicleId)
    {
        return await dbContext.VehicleBatteryStatistics
            .Where(v => v.VehicleId == vehicleId)
            .FirstOrDefaultAsync();
    }

    public async Task<VehicleUsageStatistics?> GetVehicleUsageStatisticsAsync(string vehicleId)
    {
        return await dbContext.VehicleUsageStatistics
            .Where(v => v.VehicleId == vehicleId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<HourlyTelemetryAggregate>> GetVehicleHourlyStatsAsync(string vehicleId, DateTime startDate, DateTime endDate)
    {
        return await dbContext.HourlyTelemetryAggregates
            .Where(h => h.VehicleId == vehicleId && h.HourStart >= startDate && h.HourStart <= endDate)
            .OrderBy(h => h.HourStart)
            .ToListAsync();
    }

    public async Task<List<VehicleBatteryStatistics>> GetAllVehicleBatteryStatisticsAsync()
    {
        return await dbContext.VehicleBatteryStatistics
            .OrderBy(v => v.VehicleId)
            .ToListAsync();
    }

    public async Task<List<VehicleUsageStatistics>> GetAllVehicleUsageStatisticsAsync() 
    {
        return await dbContext.VehicleUsageStatistics
            .OrderBy(v => v.VehicleId)
            .ToListAsync();
    }

    public async Task<List<VehicleUsageStatistics>> GetActiveVehiclesAsync(int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        
        return await dbContext.VehicleUsageStatistics
            .Where(v => v.LastActive >= cutoffTime)
            .OrderByDescending(v => v.LastActive)
            .ToListAsync();
    }

    public async Task<List<dynamic>> ExecuteRawSqlAsync(string sql, params NpgsqlParameter[] parameters)
    {
        var result = new List<dynamic>();
        
        using var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString());
        await connection.OpenAsync();
        
        using var command = new NpgsqlCommand(sql, connection);
        
        if (parameters != null && parameters.Length > 0)
        {
            command.Parameters.AddRange(parameters);
        }
        
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var expandoObject = new ExpandoObject() as IDictionary<string, object>;
            
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var columnValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                expandoObject.Add(columnName, columnValue);
            }
            
            result.Add(expandoObject);
        }
        
        return result;
    }

    public async Task<int> RefreshMaterializedViewAsync(string viewName)
    {
        var sql = $"REFRESH MATERIALIZED VIEW {viewName}";
        return await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}