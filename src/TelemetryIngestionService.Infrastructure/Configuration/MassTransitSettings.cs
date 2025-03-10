namespace TelemetryIngestionService.Infrastructure.Configuration
{
    public class MassTransitSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "password";
        public string VirtualHost { get; set; } = "/";
        public ushort PrefetchCount { get; set; } = 100;
        public int ConcurrencyLimit { get; set; } = 10;
        public int RetryIntervalSeconds { get; set; } = 5;
        public int RetryCount { get; set; } = 3;
        public bool UseMessageRetry { get; set; } = true;
        public bool UseCircuitBreaker { get; set; } = true;
        public int CircuitBreakerThreshold { get; set; } = 15;
        public int CircuitBreakerTrackingPeriod { get; set; } = 60;
        public int CircuitBreakerActiveThreshold { get; set; } = 10;
        public int CircuitBreakerResetInterval { get; set; } = 300;
    }
}