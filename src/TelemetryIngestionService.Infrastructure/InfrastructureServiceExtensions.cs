using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelemetryIngestionService.Domain.Services;
using TelemetryIngestionService.Infrastructure.Configuration;
using TelemetryIngestionService.Infrastructure.Messaging;
using TelemetryIngestionService.Infrastructure.Persistence;

namespace TelemetryIngestionService.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TelemetryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSQL")));

        services.AddScoped<TelemetryRepository>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddSingleton<MqttClientService>();

        services.Configure<MqttSettings>(options => configuration.GetSection("MqttSettings").Bind(options));
            
        return services;
    }
}