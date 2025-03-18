using EmergencyAlertingService.Models;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService.Services.CommandServices
{
    public class LowBatteryCommandService(ILogger<LowBatteryCommandService> logger) : ICommandService
    {
        public bool CanHandle(int alertType)
        {
            return alertType == 5;
        }

        public Task ExecuteCommandAsync(Alert alert)
        {
            if (alert.Severity >= 3)
            {
                logger.LogWarning("COMMAND EXECUTED: ENTER POWER SAVING MODE for {VehicleId} due to battery issue", 
                    alert.VehicleId);
            }
            else
            {
                logger.LogWarning("COMMAND EXECUTED: NOTIFY DRIVER TO CHARGE SOON for {VehicleId}", 
                    alert.VehicleId);
            }
            
            return Task.CompletedTask;
        }
    }
}