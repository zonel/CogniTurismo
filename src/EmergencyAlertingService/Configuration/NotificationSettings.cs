namespace EmergencyAlertingService.Configuration
{
    public class NotificationSettings
    {
        public bool EnableSms { get; set; }
        public bool EnableEmail { get; set; }
        public bool EnableLogs { get; set; }
        public string LogLevel { get; set; }
    }
}