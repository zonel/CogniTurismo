using MQTTnet;
using System.Text;
using System.Text.Json;
using MQTTnet.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelemetryIngestionService.Domain.Models;
using TelemetryIngestionService.Infrastructure.Configuration;
using TelemetryIngestionService.Infrastructure.Services;

namespace TelemetryIngestionService.Infrastructure.Messaging
{
    public class MqttClientService
    {
        private readonly IMqttClient _mqttClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly MqttSettings _mqttSettings;
        private readonly ILogger<MqttClientService> _logger;
        private int _messageCounter = 0;
        private DateTime _lastLogTime = DateTime.UtcNow;
        private const int LogIntervalSeconds = 5;
        
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };

        public MqttClientService(
            IServiceProvider serviceProvider, 
            IOptions<MqttSettings> mqttSettings,
            ILogger<MqttClientService> logger)
        {
            _serviceProvider = serviceProvider;
            _mqttSettings = mqttSettings.Value;
            _logger = logger;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient()!;
        }

        public async Task StartAsync()
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(_mqttSettings.ClientId)!
                .WithTcpServer(_mqttSettings.BrokerAddress, _mqttSettings.Port)!
                .WithCleanSession(_mqttSettings.CleanSession)!
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)!
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += HandleReceivedMessage;
            _mqttClient.ConnectedAsync += HandleConnected;
            _mqttClient.DisconnectedAsync += async _ => await HandleDisconnected(options!)!;

            try
            {
                _logger.LogInformation($"Connecting to MQTT broker at {_mqttSettings.BrokerAddress}:{_mqttSettings.Port}");
                await _mqttClient.ConnectAsync(options!)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to MQTT broker");
                throw;
            }
        }

        private async Task HandleReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            _messageCounter++;
            
            var now = DateTime.UtcNow;
            var elapsed = now - _lastLogTime;
            if (elapsed.TotalSeconds >= LogIntervalSeconds)
            {
                _logger.LogInformation($"Processing rate: {_messageCounter / elapsed.TotalSeconds:F2} messages/second");
                _messageCounter = 0;
                _lastLogTime = now;
            }

            // Use ValueTask to reduce allocations
            ValueTask ProcessMessageAsync()
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage!.PayloadSegment);
                
                try
                {
                    var telemetryData = JsonSerializer.Deserialize<TelemetryData>(payload, JsonOptions);
                    if (telemetryData != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var telemetryService = scope.ServiceProvider.GetRequiredService<ITelemetryService>();
                        return new ValueTask(telemetryService.ProcessTelemetryData(telemetryData));
                    }
                    
                    return new ValueTask();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize MQTT message: {Payload}", payload);
                    return new ValueTask();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message");
                    return new ValueTask();
                }
            }

            // Fire and forget to avoid waiting for processing
            // This maximizes MQTT message reception throughput
            _ = ProcessMessageAsync();
            
            // Complete the MQTT handler immediately to receive more messages
            await Task.CompletedTask;
        }

        private async Task HandleConnected(MqttClientConnectedEventArgs _)
        {
            try
            {
                var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(_mqttSettings.Topic)!
                    .Build();
                
                await _mqttClient.SubscribeAsync(topicFilter!)!;
                _logger.LogInformation($"Connected to MQTT broker and subscribed to topic: {_mqttSettings.Topic}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to MQTT topic");
            }
        }

        private async Task HandleDisconnected(MqttClientOptions options)
        {
            _logger.LogWarning("MQTT client disconnected, attempting to reconnect in 5 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(5));

            try
            {
                await _mqttClient.ConnectAsync(options)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to MQTT broker");
            }
        }
        
        public async Task StopAsync()
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync()!;
                _logger.LogInformation("MQTT client disconnected");
            }
        }
    }
}