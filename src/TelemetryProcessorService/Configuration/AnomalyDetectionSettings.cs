namespace TelemetryProcessorService.Configuration
{
    public class AnomalyDetectionSettings
    {
        public double BatteryTemperatureThresholdHigh { get; set; } = 45.0;
        public double BatteryLevelThresholdLow { get; set; } = 5.0;
        public int SignalStrengthThresholdLow { get; set; } = -90;
        public double MovementThresholdHigh { get; set; } = 10.0;
    }
}