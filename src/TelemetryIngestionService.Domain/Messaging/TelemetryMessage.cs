namespace TelemetryIngestionService.Domain.Messaging
{
    public class TelemetryMessage
    {
        public Guid Id { get; set; }
        public string VehicleId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double BatteryPercentage { get; set; }
        public double BatteryTemperature { get; set; }
        public DateTime RecordedAt { get; set; }
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    }
}