using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;
using TelemetryProcessorService.Producers;

namespace TelemetryProcessorService.Services
{
    public class AnomalyDetectionService(
        ILogger<AnomalyDetectionService> logger,
        IAnomalyProducer anomalyProducer,
        IOptions<AnomalyDetectionSettings> anomalyOptions)
        : IAnomalyDetectionService
    {
        private readonly ILogger<AnomalyDetectionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAnomalyProducer _anomalyProducer = anomalyProducer ?? throw new ArgumentNullException(nameof(anomalyProducer));
        private readonly AnomalyDetectionSettings _anomalySettings = anomalyOptions?.Value ?? throw new ArgumentNullException(nameof(anomalyOptions));

        public async Task CheckForAnomaliesAsync(TelemetryData data)
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
            var threshold = _anomalySettings.MovementThresholdHigh;
            if (speed > threshold * 2) return AnomalySeverity.High;
            return speed > threshold * 1.5 ? AnomalySeverity.Medium : AnomalySeverity.Low;
        }

        private AnomalySeverity CalculateBatteryPercentageSeverity(double percentage)
        {
            var threshold = _anomalySettings.BatteryLevelThresholdLow;
            if (percentage < threshold / 2) return AnomalySeverity.High;
            return percentage < threshold ? AnomalySeverity.Medium : AnomalySeverity.Low;
        }

        private AnomalySeverity CalculateBatteryTemperatureSeverity(double temperature)
        {
            var threshold = _anomalySettings.BatteryTemperatureThresholdHigh;
            if (temperature > threshold * 1.5) return AnomalySeverity.High;
            return temperature > threshold ? AnomalySeverity.Medium : AnomalySeverity.Low;
        }
    }
}
