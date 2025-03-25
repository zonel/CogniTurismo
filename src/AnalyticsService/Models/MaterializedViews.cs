using System.ComponentModel.DataAnnotations.Schema;

namespace AnalyticsService.Models;

[Table("mv_vehicle_battery_statistics")]
public class VehicleBatteryStatistics
{
    [Column("vehicle_id")]
    public string VehicleId { get; set; } = null!;

    [Column("avg_battery_percentage")]
    public double AvgBatteryPercentage { get; set; }

    [Column("min_battery_percentage")]
    public double MinBatteryPercentage { get; set; }

    [Column("max_battery_percentage")]
    public double MaxBatteryPercentage { get; set; }
    
    [Column("avg_battery_temperature")]
    public double AvgBatteryTemperature { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; }
}

[Table("mv_vehicle_usage_statistics")]
public class VehicleUsageStatistics
{
    [Column("vehicle_id")]
    public string VehicleId { get; set; } = null!;

    [Column("total_distance_km")]
    public double TotalDistanceKm { get; set; }

    [Column("avg_speed")]
    public double AvgSpeed { get; set; }

    [Column("max_speed")]
    public double MaxSpeed { get; set; }

    [Column("last_location_lat")]
    public double LastLocationLat { get; set; }

    [Column("last_location_lon")]
    public double LastLocationLon { get; set; }

    [Column("last_active")]
    public DateTime LastActive { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; }
}

[Table("mv_hourly_telemetry_aggregates")]
public class HourlyTelemetryAggregate
{
    [Column("hour_start")]
    public DateTime HourStart { get; set; }

    [Column("vehicle_id")]
    public string VehicleId { get; set; } = null!;

    [Column("data_points")]
    public int DataPoints { get; set; }

    [Column("avg_speed")]
    public double AvgSpeed { get; set; }

    [Column("avg_battery_percentage")]
    public double AvgBatteryPercentage { get; set; }

    [Column("avg_battery_temperature")]
    public double AvgBatteryTemperature { get; set; }

    [Column("distance_traveled_km")]
    public double DistanceTraveledKm { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; }
}