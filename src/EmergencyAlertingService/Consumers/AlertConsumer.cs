using EmergencyAlertingService.Models;
using EmergencyAlertingService.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService.Consumers
{
    public class AlertConsumer(
        ILogger<AlertConsumer> logger,
        AlertProcessingService alertProcessingService)
        : IConsumer<Alert>
    {
        private readonly ILogger<AlertConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly AlertProcessingService _alertProcessingService = alertProcessingService ?? throw new ArgumentNullException(nameof(alertProcessingService));

        public async Task Consume(ConsumeContext<Alert> context)
        {
            try
            {
                var alert = context.Message;
                _logger.LogInformation("Received alert: {AlertType} for vehicle {VehicleId}", alert.Type, alert.VehicleId);
                
                await _alertProcessingService.ProcessAlertAsync(alert);
                
                _logger.LogInformation("Alert processed successfully: {Id}", alert.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming alert: {Message}", ex.Message);
                throw;
            }
        }
    }
}