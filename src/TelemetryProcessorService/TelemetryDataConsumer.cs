using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;

namespace TelemetryProcessorService
{
    public class TelemetryDataConsumer : IConsumer<TelemetryData>
    {
        private readonly ILogger<TelemetryDataConsumer> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly TopicNameProvider _topicProvider;
        private readonly CitusDbSettings _dbSettings;
        private readonly AnomalyDetectionSettings _anomalySettings;
        private readonly MassTransitSettings _massTransitSettings;

        public TelemetryDataConsumer(
            ILogger<TelemetryDataConsumer> logger,
            IPublishEndpoint publishEndpoint,
            TopicNameProvider topicProvider,
            IOptions<CitusDbSettings> dbOptions,
            IOptions<AnomalyDetectionSettings> anomalyOptions,
            IOptions<MassTransitSettings> massTransitOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _topicProvider = topicProvider ?? throw new ArgumentNullException(nameof(topicProvider));
            _dbSettings = dbOptions?.Value ?? throw new ArgumentNullException(nameof(dbOptions));
            _anomalySettings = anomalyOptions?.Value ?? throw new ArgumentNullException(nameof(anomalyOptions));
            _massTransitSettings = massTransitOptions?.Value ?? throw new ArgumentNullException(nameof(massTransitOptions));
        }

        public async Task Consume(ConsumeContext<TelemetryData> context)
        {
            try
            {
                var telemetryData = context.Message;

                _logger.LogInformation("Received telemetry data with ID: {Id} from vehicle: {VehicleId} at {Time}", 
                    telemetryData.Id, telemetryData.VehicleId, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                await ProcessTelemetryDataAsync(telemetryData);
                await CheckForAnomaliesAsync(telemetryData);

                _logger.LogInformation("Successfully processed telemetry data with ID: {Id}", telemetryData.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry data: {Message}", ex.Message);
                throw;
            }
        }

        private async Task ProcessTelemetryDataAsync(TelemetryData data)
        {
            _logger.LogDebug("Processing telemetry data with ID: {Id}", data.Id);
            await StoreInDatabaseAsync(data);
            _logger.LogDebug("Telemetry data processing completed for ID: {Id}", data.Id);
        }

        private async Task StoreInDatabaseAsync(TelemetryData data)
        {
            // Simulate asynchronous database storing.
            await Task.Delay(50);
        }

        private async Task CheckForAnomaliesAsync(TelemetryData data)
        {
            await CheckBatteryPercentageAnomalyAsync(data);
            await CheckBatteryTemperatureAnomalyAsync(data);
            await CheckSpeedAnomalyAsync(data);
        }

        private async Task CheckSpeedAnomalyAsync(TelemetryData data)
        {
            if (data.Speed > _anomalySettings.MovementThresholdHigh)
            {
                _logger.LogWarning("Speed anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.Speed);
                await PublishAnomalyAsync(data, AnomalyType.Speed, data.Speed);
            }
        }

        private async Task CheckBatteryPercentageAnomalyAsync(TelemetryData data)
        {
            if (data.BatteryPercentage < _anomalySettings.BatteryLevelThresholdLow)
            {
                _logger.LogWarning("Battery percentage anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.BatteryPercentage);
                await PublishAnomalyAsync(data, AnomalyType.BatteryPercentage, data.BatteryPercentage);
            }
        }

        private async Task CheckBatteryTemperatureAnomalyAsync(TelemetryData data)
        {
            if (data.BatteryTemperature > _anomalySettings.BatteryTemperatureThresholdHigh)
            {
                _logger.LogWarning("Battery temperature anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.BatteryTemperature);
                await PublishAnomalyAsync(data, AnomalyType.BatteryTemperature, data.BatteryTemperature);
            }
        }

        private async Task PublishAnomalyAsync(TelemetryData data, AnomalyType type, double value)
        {
            var anomalyEvent = new AnomalyEvent
            {
                Id = Guid.NewGuid(),
                TelemetryId = data.Id,
                VehicleId = data.VehicleId,
                Type = type,
                Value = value,
                Severity = CalculateSeverity(type, value),
                DetectedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(anomalyEvent);

            _logger.LogInformation("Anomaly event published with ID: {Id}, Type: {Type}, Severity: {Severity} at {Time}", 
                anomalyEvent.Id, anomalyEvent.Type, anomalyEvent.Severity, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private AnomalySeverity CalculateSeverity(AnomalyType type, double value)
        {
            return type switch
            {
                AnomalyType.Speed => CalculateSpeedSeverity(value),
                AnomalyType.BatteryPercentage => CalculateBatteryPercentageSeverity(value),
                AnomalyType.BatteryTemperature => CalculateBatteryTemperatureSeverity(value),
                _ => AnomalySeverity.Low
            };
        }

        private AnomalySeverity CalculateSpeedSeverity(double speed)
        {
            double threshold = _anomalySettings.MovementThresholdHigh;

            if (speed > threshold * 2) return AnomalySeverity.High;
            if (speed > threshold * 1.5) return AnomalySeverity.Medium;
            return AnomalySeverity.Low;
        }

        private AnomalySeverity CalculateBatteryPercentageSeverity(double percentage)
        {
            double threshold = _anomalySettings.BatteryLevelThresholdLow;

            if (percentage < threshold / 2) return AnomalySeverity.High;
            if (percentage < threshold) return AnomalySeverity.Medium;
            return AnomalySeverity.Low;
        }

        private AnomalySeverity CalculateBatteryTemperatureSeverity(double temperature)
        {
            double threshold = _anomalySettings.BatteryTemperatureThresholdHigh;

            if (temperature > threshold * 1.5) return AnomalySeverity.High;
            if (temperature > threshold) return AnomalySeverity.Medium;
            return AnomalySeverity.Low;
        }
    }
}