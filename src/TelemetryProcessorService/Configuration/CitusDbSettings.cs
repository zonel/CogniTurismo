namespace TelemetryProcessorService.Configuration
{
    public class CitusDbSettings
    {
        public string ConnectionString { get; set; }
        public string SchemaName { get; set; } = "public";
        public string TableName { get; set; } = "telemetry_data";
    }
}