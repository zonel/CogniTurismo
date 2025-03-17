namespace TelemetryProcessorService.Configuration
{
    public class AnomalyDetectionSettings
    {
        public double BatteryTemperatureThresholdHigh { get; set; } = 45.0;
        public double BatteryLevelThresholdLow { get; set; } = 2.0;
        public double MovementThresholdHigh { get; set; } = 115.0;
    }
}