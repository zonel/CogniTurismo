using MQTTnet;
using System.Text;
using System.Text.Json;
using MQTTnet.Client;
using Microsoft.Extensions.Options;
using TelemetryIngestionService.Domain;
using TelemetryIngestionService.Domain.Models;
using TelemetryIngestionService.Domain.Services;
using TelemetryIngestionService.Infrastructure.Configuration;

namespace TelemetryIngestionService.Infrastructure.Messaging;

public class MqttClientService
{
    private readonly IMqttClient _mqttClient;
    private readonly ITelemetryService _telemetryService;
    private readonly MqttSettings _mqttSettings;

    public MqttClientService(ITelemetryService telemetryService, IOptions<MqttSettings> mqttSettings)
    {
        _telemetryService = telemetryService;
        _mqttSettings = mqttSettings.Value;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient()!;

        var options = new MqttClientOptionsBuilder()
            .WithClientId(_mqttSettings.ClientId)!
            .WithTcpServer(_mqttSettings.BrokerAddress, _mqttSettings.Port)!
            .WithCleanSession(_mqttSettings.CleanSession)!
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage!.PayloadSegment);
            try
            {
                var telemetryData = JsonSerializer.Deserialize<TelemetryData>(payload);
                if (telemetryData != null)
                {
                    await _telemetryService.ProcessTelemetryData(telemetryData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process MQTT message: {ex.Message}");
            }
        };

        _mqttClient.ConnectedAsync += async _ =>
        {
            await _mqttClient.SubscribeAsync(_mqttSettings.Topic)!;
            Console.WriteLine($"Subscribed to MQTT topic: {_mqttSettings.Topic}");
        };

        _mqttClient.DisconnectedAsync += async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _mqttClient.ConnectAsync(options!)!;
        };

        _mqttClient.ConnectAsync(options!);
    }
}