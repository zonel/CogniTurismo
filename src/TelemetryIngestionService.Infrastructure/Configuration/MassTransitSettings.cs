namespace TelemetryIngestionService.Infrastructure.Configuration
{
    namespace TelemetryIngestionService.Infrastructure.Configuration
    {
        public class MassTransitSettings
        {
            public string Host { get; set; } = "localhost:9092";
        
            public string Topic { get; set; } = "telemetry-topic";
        }
    }
}