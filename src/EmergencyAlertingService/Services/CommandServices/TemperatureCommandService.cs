using EmergencyAlertingService.Models;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService.Services.CommandServices
{
    public class TemperatureCommandService(ILogger<TemperatureCommandService> logger) : ICommandService
    {
        public bool CanHandle(int alertType)
        {
            return alertType == 1;
        }

        public Task ExecuteCommandAsync(Alert alert)
        {
            if (alert.Severity >= 3)
            {
                logger.LogWarning("COMMAND EXECUTED: STOP VEHICLE {VehicleId} due to overheating (Value: {Value})", 
                    alert.VehicleId, alert.Value);
            }
            else
            {
                logger.LogWarning("COMMAND EXECUTED: REDUCE POWER for {VehicleId} due to elevated temperature (Value: {Value})", 
                    alert.VehicleId, alert.Value);
            }
            
            return Task.CompletedTask;
        }
    }
}