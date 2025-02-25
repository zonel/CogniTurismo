public class MqttSettings
{
    public string BrokerAddress { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string Topic { get; set; } = "traffic/simulated";
    public int MessagesPerSecond { get; set; } = 10;
}