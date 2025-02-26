using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelemetryIngestionService.Infrastructure.Configuration;
using TelemetryIngestionService.Infrastructure.Messaging;
using TelemetryIngestionService.Infrastructure.Repositories;
using TelemetryIngestionService.Infrastructure.Services;

namespace TelemetryIngestionService.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MqttSettings>(configuration.GetSection("MqttSettings"));
        services.Configure<DatabaseSettings>(configuration.GetSection("ConnectionStrings"));
        services.Configure<BufferSettings>(configuration.GetSection("BufferSettings"));

        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        
        services.AddSingleton<ITelemetryBufferService, TelemetryBufferService>();
        services.AddHostedService(provider => (TelemetryBufferService)provider.GetRequiredService<ITelemetryBufferService>());
        
        services.AddSingleton<MqttClientService>();
            
        return services;
    }
}