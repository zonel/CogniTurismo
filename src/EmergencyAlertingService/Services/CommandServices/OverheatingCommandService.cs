using EmergencyAlertingService.Models;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService.Services.CommandServices
{
    public class OverheatingCommandService(ILogger<OverheatingCommandService> logger) : ICommandService
    {
        public bool CanHandle(int alertType)
        {
            return alertType == 1;
        }

        public Task ExecuteCommandAsync(Alert alert)
        {
            if (alert.Severity >= 3)
            {
                logger.LogWarning("COMMAND EXECUTED: STOP VEHICLE {VehicleId} due to overheating", 
                    alert.VehicleId);
            }
            else
            {
                logger.LogWarning("COMMAND EXECUTED: REDUCE POWER for {VehicleId} due to elevated temperature", 
                    alert.VehicleId);
            }
            
            return Task.CompletedTask;
        }
    }
}