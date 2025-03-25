using AnalyticsService.Repositories;

namespace AnalyticsService.Services;

public class MaterializedViewRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaterializedViewRefreshService> _logger;
    private readonly TimeSpan _refreshInterval;

    public MaterializedViewRefreshService(
        IServiceProvider serviceProvider,
        ILogger<MaterializedViewRefreshService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        var intervalMinutes = configuration.GetValue<int>("MaterializedViewRefreshIntervalMinutes");
        _refreshInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshMaterializedViewsAsync();
            await Task.Delay(_refreshInterval, stoppingToken);
        }
    }

    private async Task RefreshMaterializedViewsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AnalyticsRepository>();
        
        try
        {
            await repository.RefreshMaterializedViewAsync("mv_vehicle_battery_statistics");
            await repository.RefreshMaterializedViewAsync("mv_vehicle_usage_statistics");
            await repository.RefreshMaterializedViewAsync("mv_hourly_telemetry_aggregates");
            
            _logger.LogInformation("Materialized views refreshed successfully at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh materialized views");
        }
    }
}