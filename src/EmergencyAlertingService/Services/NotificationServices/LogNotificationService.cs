using EmergencyAlertingService.Models;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService.Services.NotificationServices
{
    public class LogNotificationService(ILogger<LogNotificationService> logger) : INotificationService
    {
        public Task SendNotificationAsync(Alert alert)
        {
            var severityText = GetSeverityText(alert.Severity);
            var alertTypeText = GetAlertTypeText(alert.Type);
            
            logger.LogInformation("LOG ALERT - Vehicle: {VehicleId}, Type: {AlertType}({TypeCode}), Severity: {Severity}({SeverityCode}), Value: {Value}, Time: {Timestamp}", 
                alert.VehicleId, alertTypeText, alert.Type, severityText, alert.Severity, alert.Value, alert.DetectedAt);
                
            return Task.CompletedTask;
        }
        
        private string GetSeverityText(int severity) => severity switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Critical",
            _ => $"Unknown({severity})"
        };
        
        private string GetAlertTypeText(int type) => type switch
        {
            1 => "TemperatureAnomaly",
            2 => "VibrationAnomaly",
            3 => "PressureAnomaly",
            4 => "SpeedAnomaly",
            5 => "BatteryAnomaly",
            _ => $"Unknown({type})"
        };
    }
}