using EmergencyAlertingService.Models;

namespace EmergencyAlertingService.Services.NotificationServices
{
    public interface INotificationService
    {
        Task SendNotificationAsync(Alert alert);
    }
}