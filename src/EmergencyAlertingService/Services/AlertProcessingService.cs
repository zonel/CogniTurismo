using EmergencyAlertingService.Models;
using EmergencyAlertingService.Services.CommandServices;
using EmergencyAlertingService.Services.NotificationServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmergencyAlertingService.Configuration;

namespace EmergencyAlertingService.Services
{
    public class AlertProcessingService(
        ILogger<AlertProcessingService> logger,
        IEnumerable<INotificationService> notificationServices,
        IEnumerable<ICommandService> commandServices,
        IOptions<NotificationSettings> notificationSettings)
    {
        private readonly ILogger<AlertProcessingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IEnumerable<INotificationService> _notificationServices = notificationServices ?? throw new ArgumentNullException(nameof(notificationServices));
        private readonly IEnumerable<ICommandService> _commandServices = commandServices ?? throw new ArgumentNullException(nameof(commandServices));
        private readonly NotificationSettings _notificationSettings = notificationSettings?.Value ?? throw new ArgumentNullException(nameof(notificationSettings));

        public async Task ProcessAlertAsync(Alert alert)
        {
            try
            {
                _logger.LogInformation("Processing alert: Type {AlertType} for vehicle {VehicleId}", alert.Type, alert.VehicleId);
                
                await SendNotificationsAsync(alert);
                await ExecuteCommandsAsync(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alert: {Message}", ex.Message);
                throw;
            }
        }

        private async Task SendNotificationsAsync(Alert alert)
        {
            var notificationTasks = _notificationServices
                .Select(service => SendNotificationIfEnabled(service, alert))
                .ToList();

            await Task.WhenAll(notificationTasks);
        }

        private Task SendNotificationIfEnabled(INotificationService service, Alert alert)
        {
            var serviceType = service.GetType();
            
            if ((serviceType == typeof(SmsNotificationService) && _notificationSettings.EnableSms) ||
                (serviceType == typeof(EmailNotificationService) && _notificationSettings.EnableEmail) ||
                (serviceType == typeof(LogNotificationService) && _notificationSettings.EnableLogs))
            {
                return service.SendNotificationAsync(alert);
            }
            
            return Task.CompletedTask;
        }

        private async Task ExecuteCommandsAsync(Alert alert)
        {
            var commandService = _commandServices.FirstOrDefault(cmd => cmd.CanHandle(alert.Type));
            
            if (commandService != null)
            {
                await commandService.ExecuteCommandAsync(alert);
            }
            else
            {
                _logger.LogWarning("No command handler found for alert type: {AlertType}", alert.Type);
            }
        }
    }
}