using System;

namespace TelemetryProcessorService.Models
{
    public class AnomalyEvent
    {
        public Guid Id { get; set; }
        public Guid TelemetryId { get; set; }
        public string VehicleId { get; set; } = string.Empty;
        public AnomalyType Type { get; set; }
        public double Value { get; set; }
        public AnomalySeverity Severity { get; set; }
        public DateTime DetectedAt { get; set; }
    }
    
    public enum AnomalyType
    {
        Speed,
        BatteryPercentage,
        BatteryTemperature
    }
    
    public enum AnomalySeverity
    {
        Low,
        Medium,
        High
    }
}