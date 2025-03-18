using EmergencyAlertingService.Models;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService.Services.CommandServices
{
    public class BatteryCommandService(ILogger<BatteryCommandService> logger) : ICommandService
    {
        public bool CanHandle(int alertType)
        {
            return alertType == 5;
        }

        public Task ExecuteCommandAsync(Alert alert)
        {
            if (alert.Severity >= 3)
            {
                logger.LogWarning("COMMAND EXECUTED: ENTER POWER SAVING MODE for {VehicleId} due to critical battery issue (Value: {Value})", 
                    alert.VehicleId, alert.Value);
            }
            else
            {
                logger.LogWarning("COMMAND EXECUTED: NOTIFY DRIVER TO CHARGE SOON for {VehicleId} due to battery issue (Value: {Value})", 
                    alert.VehicleId, alert.Value);
            }
            
            return Task.CompletedTask;
        }
    }
}