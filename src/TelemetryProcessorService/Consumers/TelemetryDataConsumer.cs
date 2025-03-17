using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;
using TelemetryProcessorService.Services;

namespace TelemetryProcessorService.Consumers
{
    public class TelemetryDataConsumer(
        ILogger<TelemetryDataConsumer> logger,
        IAnomalyDetectionService anomalyDetectionService,
        IOptions<CitusDbSettings> dbOptions)
        : IConsumer<TelemetryData>
    {
        private readonly ILogger<TelemetryDataConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAnomalyDetectionService _anomalyDetectionService = anomalyDetectionService ?? throw new ArgumentNullException(nameof(anomalyDetectionService));
        private readonly CitusDbSettings _dbSettings = dbOptions?.Value ?? throw new ArgumentNullException(nameof(dbOptions));

        public async Task Consume(ConsumeContext<TelemetryData> context)
        {
            try
            {
                var telemetryData = context.Message;

                await ProcessTelemetryDataAsync(telemetryData);
                await _anomalyDetectionService.CheckForAnomaliesAsync(telemetryData);

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
    }
}
