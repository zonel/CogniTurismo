using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;
using TelemetryProcessorService.Producers;

namespace TelemetryProcessorService.Consumers
{
    public class TelemetryDataConsumer(
        ILogger<TelemetryDataConsumer> logger,
        IAnomalyProducer anomalyProducer,
        IOptions<CitusDbSettings> dbOptions,
        IOptions<AnomalyDetectionSettings> anomalyOptions)
        : IConsumer<TelemetryData>
    {
        private readonly ILogger<TelemetryDataConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAnomalyProducer _anomalyProducer = anomalyProducer ?? throw new ArgumentNullException(nameof(anomalyProducer));
        private readonly CitusDbSettings _dbSettings = dbOptions?.Value ?? throw new ArgumentNullException(nameof(dbOptions));
        private readonly AnomalyDetectionSettings _anomalySettings = anomalyOptions?.Value ?? throw new ArgumentNullException(nameof(anomalyOptions));

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
                
                var severity = CalculateSpeedSeverity(data.Speed);
                await _anomalyProducer.PublishAnomalyAsync(data, AnomalyType.Speed, data.Speed, severity);
            }
        }

        private async Task CheckBatteryPercentageAnomalyAsync(TelemetryData data)
        {
            if (data.BatteryPercentage < _anomalySettings.BatteryLevelThresholdLow)
            {
                _logger.LogWarning("Battery percentage anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.BatteryPercentage);
                
                var severity = CalculateBatteryPercentageSeverity(data.BatteryPercentage);
                await _anomalyProducer.PublishAnomalyAsync(data, AnomalyType.BatteryPercentage, data.BatteryPercentage, severity);
            }
        }

        private async Task CheckBatteryTemperatureAnomalyAsync(TelemetryData data)
        {
            if (data.BatteryTemperature > _anomalySettings.BatteryTemperatureThresholdHigh)
            {
                _logger.LogWarning("Battery temperature anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.BatteryTemperature);
                
                var severity = CalculateBatteryTemperatureSeverity(data.BatteryTemperature);
                await _anomalyProducer.PublishAnomalyAsync(data, AnomalyType.BatteryTemperature, data.BatteryTemperature, severity);
            }
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