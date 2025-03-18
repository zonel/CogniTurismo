namespace EmergencyAlertingService.Configuration
{
    public class MassTransitSettings
    {
        public string Host { get; set; }
        public string AlertTopic { get; set; }
        public string ConsumerGroup { get; set; }
    }
}