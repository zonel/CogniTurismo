using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyAlertingService
{
    public class Worker(ILogger<Worker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Emergency Alerting Service started at: {time}", DateTimeOffset.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}