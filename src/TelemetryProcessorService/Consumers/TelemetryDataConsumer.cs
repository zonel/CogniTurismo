using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;
using TelemetryProcessorService.Services;
using System;
using System.Threading.Tasks;
using TelemetryProcessorService.Persistence;

namespace TelemetryProcessorService.Consumers
{
    public class TelemetryDataConsumer(
        ILogger<TelemetryDataConsumer> logger,
        IAnomalyDetectionService anomalyDetectionService,
        ITelemetryDataStorageService storageService)
        : IConsumer<TelemetryData>
    {
        private readonly ILogger<TelemetryDataConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAnomalyDetectionService _anomalyDetectionService = anomalyDetectionService ?? throw new ArgumentNullException(nameof(anomalyDetectionService));
        private readonly ITelemetryDataStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

        public async Task Consume(ConsumeContext<TelemetryData> context)
        {
            try
            {
                await ProcessTelemetryDataAsync(context);

                _logger.LogInformation("Successfully processed telemetry data with ID: {Id}", context.Message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry data: {Message}", ex.Message);
                throw;
            }
        }

        private async Task ProcessTelemetryDataAsync(ConsumeContext<TelemetryData> context)
        {
            var telemetryData = context.Message;
            _logger.LogDebug("Processing telemetry data with ID: {Id}", telemetryData.Id);
            await _storageService.StoreTelemetryDataAsync(telemetryData);
            _logger.LogDebug("Telemetry data processing completed for ID: {Id}", telemetryData.Id);
            await _anomalyDetectionService.CheckForAnomaliesAsync(telemetryData);
        }
    }
}
