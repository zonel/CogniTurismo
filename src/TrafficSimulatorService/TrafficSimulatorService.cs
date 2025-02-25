using MQTTnet;
using MQTTnet.Client;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class TrafficSimulatorService
{
    private readonly MqttSettings _settings;
    private readonly IMqttClient _mqttClient;
    private readonly Random _random = new();

    public TrafficSimulatorService(IOptions<MqttSettings> settings)
    {
        _settings = settings.Value;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
    }

    public async Task StartAsync()
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_settings.BrokerAddress, _settings.Port)!
            .WithCleanSession()!
            .Build();

        Console.WriteLine($"🚀 Connecting to MQTT {_settings.BrokerAddress}:{_settings.Port}...");
        await _mqttClient.ConnectAsync(options!)!;
        Console.WriteLine("✅ Connected!");

        while (true)
        {
            var telemetryData = GenerateTelemetryData();
            var payload = JsonSerializer.Serialize(telemetryData);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_settings.Topic)!
                .WithPayload(payload)!
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)!
                .Build();

            await _mqttClient.PublishAsync(message!)!;
            Console.WriteLine($"📤 Sent: {payload}");

            await Task.Delay(1000 / _settings.MessagesPerSecond);
        }
    }

    private object GenerateTelemetryData()
    {
        return new
        {
            vehicleId = $"Car_{_random.Next(1, 100)}",
            latitude = _random.NextDouble() * 180 - 90,
            longitude = _random.NextDouble() * 360 - 180,
            speed = _random.Next(0, 120),
            timestamp = DateTime.UtcNow.ToString("o")
        };
    }
}