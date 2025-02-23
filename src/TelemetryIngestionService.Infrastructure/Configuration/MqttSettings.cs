namespace TelemetryIngestionService.Infrastructure.Configuration;

public class MqttSettings
{
    public string BrokerAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public bool CleanSession { get; set; }
}