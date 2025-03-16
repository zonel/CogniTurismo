namespace TelemetryProcessorService.Configuration
{
    public class CitusDbSettings
    {
        public string ConnectionString { get; set; }
        public string SchemaName { get; set; } = "telemetry";
        public string TableName { get; set; } = "device_data";
    }
}