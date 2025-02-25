using MQTTnet;
using System.Text;
using System.Text.Json;
using MQTTnet.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using TelemetryIngestionService.Domain.Models;
using TelemetryIngestionService.Domain.Services;
using TelemetryIngestionService.Infrastructure.Configuration;

namespace TelemetryIngestionService.Infrastructure.Messaging;

public class MqttClientService
{
    private readonly IMqttClient _mqttClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly MqttSettings _mqttSettings;

    public MqttClientService(IServiceProvider serviceProvider, IOptions<MqttSettings> mqttSettings)
    {
        _serviceProvider = serviceProvider;
        _mqttSettings = mqttSettings.Value;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient()!;
    }

    public async Task StartAsync()
    {
        var options = new MqttClientOptionsBuilder()
            .WithClientId(_mqttSettings.ClientId)!
            .WithTcpServer(_mqttSettings.BrokerAddress, _mqttSettings.Port)!
            .WithCleanSession(_mqttSettings.CleanSession)!
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
        _mqttClient.ConnectedAsync += HandleConnected;
        _mqttClient.DisconnectedAsync += async _ => await HandleDisconnected(options!)!;

        await _mqttClient.ConnectAsync(options!)!;
    }

    private async Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine("✅ received message");
        using var scope = _serviceProvider.CreateScope();
        var telemetryService = scope.ServiceProvider.GetRequiredService<ITelemetryService>();

        var payload = Encoding.UTF8.GetString(e.ApplicationMessage!.PayloadSegment);
        try
        {
            var telemetryData = JsonSerializer.Deserialize<TelemetryData>(payload);
            if (telemetryData != null)
            {
                await telemetryService.ProcessTelemetryData(telemetryData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process MQTT message: {ex.Message}");
        }
    }

    private async Task HandleConnected(MqttClientConnectedEventArgs _)
    {
        await _mqttClient.SubscribeAsync(_mqttSettings.Topic)!;
        Console.WriteLine($"Subscribed to MQTT topic: {_mqttSettings.Topic}");
    }

    private async Task HandleDisconnected(MqttClientOptions options)
    {
        Console.WriteLine("MQTT Disconnected, retrying in 5 seconds...");
        await Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            await _mqttClient.ConnectAsync(options)!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reconnection failed: {ex.Message}");
        }
    }
}
