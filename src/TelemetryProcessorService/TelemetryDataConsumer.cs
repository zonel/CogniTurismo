using System;
using System.Threading;
using System.Threading.Tasks;
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

        public Task Consume(ConsumeContext<TelemetryData> context)
        {
            try
            {
                var telemetryData = context.Message;
                
                _logger.LogInformation("Received telemetry data with ID: {Id} from vehicle: {VehicleId} at {Time}", 
                    telemetryData.Id, telemetryData.VehicleId, "2025-03-16 22:19:27");
                
                ProcessTelemetryDataSync(telemetryData);
                CheckForAnomaliesSync(telemetryData);
                
                _logger.LogInformation("Successfully processed telemetry data with ID: {Id}", telemetryData.Id);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry data: {Message}", ex.Message);
                throw;
            }
        }

        private void ProcessTelemetryDataSync(TelemetryData data)
        {
            _logger.LogDebug("Processing telemetry data with ID: {Id}", data.Id);
            StoreInDatabaseSync(data);
            _logger.LogDebug("Telemetry data processing completed for ID: {Id}", data.Id);
        }

        private void StoreInDatabaseSync(TelemetryData data)
        {
            Thread.Sleep(50);
        }

        private void CheckForAnomaliesSync(TelemetryData data)
        {
            CheckBatteryPercentageAnomalySync(data);
            CheckBatteryTemperatureAnomalySync(data);
            CheckSpeedAnomalySync(data);
        }

        private void CheckSpeedAnomalySync(TelemetryData data)
        {
            if (data.Speed > _anomalySettings.MovementThresholdHigh)
            {
                _logger.LogWarning("Speed anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.Speed);
                    
                PublishAnomalySync(data, AnomalyType.Speed, data.Speed);
            }
        }

        private void CheckBatteryPercentageAnomalySync(TelemetryData data)
        {
            if (data.BatteryPercentage < _anomalySettings.BatteryLevelThresholdLow)
            {
                _logger.LogWarning("Battery percentage anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.BatteryPercentage);
                    
                PublishAnomalySync(data, AnomalyType.BatteryPercentage, data.BatteryPercentage);
            }
        }

        private void CheckBatteryTemperatureAnomalySync(TelemetryData data)
        {
            if (data.BatteryTemperature > _anomalySettings.BatteryTemperatureThresholdHigh)
            {
                _logger.LogWarning("Battery temperature anomaly detected for vehicle {VehicleId}. Value: {Value}", 
                    data.VehicleId, data.BatteryTemperature);
                    
                PublishAnomalySync(data, AnomalyType.BatteryTemperature, data.BatteryTemperature);
            }
        }

        private void PublishAnomalySync(TelemetryData data, AnomalyType type, double value)
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

            _publishEndpoint.Publish(anomalyEvent).GetAwaiter().GetResult();
            
            _logger.LogInformation("Anomaly event published with ID: {Id}, Type: {Type}, Severity: {Severity} at {Time}", 
                anomalyEvent.Id, anomalyEvent.Type, anomalyEvent.Severity, "2025-03-16 22:19:27");
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