namespace TelemetryProcessorService.Configuration
{
    public class MassTransitSettings
    {
        public string Host { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "telemetry-topic";
        public string ConsumerGroup { get; set; } = "telemetry-processor-group";
        public string AnomalyTopic { get; set; } = "telemetry-anomaly";
    }
}