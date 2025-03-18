using EmergencyAlertingService.Models;

namespace EmergencyAlertingService.Services.CommandServices
{
    public interface ICommandService
    {
        Task ExecuteCommandAsync(Alert alert);
        bool CanHandle(int alertType);
    }
}