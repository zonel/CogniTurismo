using AnalyticsService.Dtos;
using AnalyticsService.Repositories;
using Npgsql;

namespace AnalyticsService.Services;

public class AnalyticsService(AnalyticsRepository repository)
{
    public async Task<VehicleBatteryStatisticsDto?> GetVehicleBatteryStatisticsAsync(string vehicleId)
    {
        var result = await repository.GetVehicleBatteryStatisticsAsync(vehicleId);
        
        return result == null ? null : new VehicleBatteryStatisticsDto(
            result.VehicleId,
            result.AvgBatteryPercentage,
            result.MinBatteryPercentage,
            result.MaxBatteryPercentage,
            result.AvgBatteryTemperature,
            result.LastUpdated
        );
    }

    public async Task<VehicleUsageStatisticsDto?> GetVehicleUsageStatisticsAsync(string vehicleId)
    {
        var result = await repository.GetVehicleUsageStatisticsAsync(vehicleId);
        
        return result == null ? null : new VehicleUsageStatisticsDto(
            result.VehicleId,
            result.TotalDistanceKm,
            result.AvgSpeed,
            result.MaxSpeed,
            result.LastLocationLat,
            result.LastLocationLon,
            result.LastActive,
            result.LastUpdated
        );
    }

    public async Task<List<HourlyTelemetryAggregateDto>> GetVehicleHourlyStatsAsync(string vehicleId, DateTime startDate, DateTime endDate)
    {
        var results = await repository.GetVehicleHourlyStatsAsync(vehicleId, startDate, endDate);
        
        return results.Select(r => new HourlyTelemetryAggregateDto(
            r.HourStart,
            r.VehicleId,
            r.DataPoints,
            r.AvgSpeed,
            r.AvgBatteryPercentage,
            r.AvgBatteryTemperature,
            r.DistanceTraveledKm,
            r.LastUpdated
        )).ToList();
    }

    public async Task<List<VehicleBatteryStatisticsDto>> GetAllVehicleBatteryStatisticsAsync()
    {
        var results = await repository.GetAllVehicleBatteryStatisticsAsync();
        
        return results.Select(r => new VehicleBatteryStatisticsDto(
            r.VehicleId,
            r.AvgBatteryPercentage,
            r.MinBatteryPercentage,
            r.MaxBatteryPercentage,
            r.AvgBatteryTemperature,
            r.LastUpdated
        )).ToList();
    }

    public async Task<List<VehicleUsageStatisticsDto>> GetAllVehicleUsageStatisticsAsync()
    {
        var results = await repository.GetAllVehicleUsageStatisticsAsync();
        
        return results.Select(r => new VehicleUsageStatisticsDto(
            r.VehicleId,
            r.TotalDistanceKm,
            r.AvgSpeed,
            r.MaxSpeed,
            r.LastLocationLat,
            r.LastLocationLon,
            r.LastActive,
            r.LastUpdated
        )).ToList();
    }

    public async Task<List<VehicleUsageStatisticsDto>> GetActiveVehiclesAsync(int hours = 24)
    {
        var results = await repository.GetActiveVehiclesAsync(hours);
        
        return results.Select(r => new VehicleUsageStatisticsDto(
            r.VehicleId,
            r.TotalDistanceKm,
            r.AvgSpeed,
            r.MaxSpeed,
            r.LastLocationLat,
            r.LastLocationLon,
            r.LastActive,
            r.LastUpdated
        )).ToList();
    }

    public async Task<List<VehicleNearbyDto>> FindVehiclesNearLocationAsync(double latitude, double longitude, double radiusKm)
    {
        var sql = "SELECT * FROM find_vehicles_near_location(@lat, @lon, @radius)";
        
        var parameters = new[]
        {
            new NpgsqlParameter("@lat", latitude),
            new NpgsqlParameter("@lon", longitude),
            new NpgsqlParameter("@radius", radiusKm)
        };
        
        var results = await repository.ExecuteRawSqlAsync(sql, parameters);
        
        return results.Select(r => new VehicleNearbyDto(
            r.vehicle_id,
            r.latitude,
            r.longitude,
            r.distance_km,
            r.recorded_at
        )).ToList();
    }

    public async Task<bool> RefreshMaterializedViewsAsync()
    {
        try
        {
            await repository.RefreshMaterializedViewAsync("mv_vehicle_battery_statistics");
            await repository.RefreshMaterializedViewAsync("mv_vehicle_usage_statistics");
            await repository.RefreshMaterializedViewAsync("mv_hourly_telemetry_aggregates");
            return true;
        }
        catch
        {
            return false;
        }
    }
}