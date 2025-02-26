namespace TelemetryIngestionService.Infrastructure.Configuration
{
    public class MqttSettings
    {
        public string BrokerAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 1883;
        public string ClientId { get; set; } = string.Empty;
        public bool CleanSession { get; set; } = true;
        public string Topic { get; set; } = string.Empty;
    }
}