using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnalyticsService.Models;

[Table("telemetry_data")]
public class TelemetryData
{
    [Column("id")]
    public Guid Id { get; set; }

    [Column("vehicle_id")]
    [Required]
    public string VehicleId { get; set; } = null!;

    [Column("latitude")]
    [Required]
    public double Latitude { get; set; }

    [Column("longitude")]
    [Required]
    public double Longitude { get; set; }

    [Column("speed")]
    [Required]
    public double Speed { get; set; }

    [Column("battery_percentage")]
    [Required]
    public double BatteryPercentage { get; set; }

    [Column("battery_temperature")]
    [Required]
    public double BatteryTemperature { get; set; }

    [Column("recorded_at")]
    [Required]
    public DateTime RecordedAt { get; set; }
}