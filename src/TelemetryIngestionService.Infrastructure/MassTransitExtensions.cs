using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelemetryIngestionService.Infrastructure.Configuration;
using TelemetryIngestionService.Infrastructure.Services;
using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure
{
    public static class MassTransitExtensions
    {
        public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MassTransitSettings>(configuration.GetSection("MassTransit"));
            
            services.AddSingleton<ITelemetryService, TelemetryService>();
            
            services.AddMassTransit(busConfig =>
            {
                busConfig.UsingRabbitMq((context, cfg) =>
                {
                    var settings = configuration.GetSection("MassTransit").Get<MassTransitSettings>() 
                        ?? new MassTransitSettings();
                    
                    cfg.Host(settings.Host, settings.VirtualHost, h =>
                    {
                        h.Username(settings.Username);
                        h.Password(settings.Password);
                        h.PublisherConfirmation = true;
                    });
                    
                    var exchangeName = "telemetry-direct-exchange";
                    var virtualHost = settings.VirtualHost == "/" ? "" : settings.VirtualHost;
                    
                    cfg.Message<TelemetryData>(m => 
                    {
                        m.SetEntityName(exchangeName);
                    });
                    
                    cfg.Send<TelemetryData>(s =>
                    {
                        s.UseRoutingKeyFormatter(_ => "telemetry-key");
                    });
                    
                    cfg.Publish<TelemetryData>(p =>
                    {
                        p.ExchangeType = "direct"; 
                        p.Durable = true;
                        p.AutoDelete = false;
                    });
                    
                    cfg.ReceiveEndpoint("telemetry-queue", e =>
                    {
                        e.Durable = true;
                        e.AutoDelete = false;
                        e.PurgeOnStartup = false;
                        
                        e.Bind(exchangeName, x =>
                        {
                            x.RoutingKey = "telemetry-key";
                            x.ExchangeType = "direct";
                        });
                        
                        e.ConfigureConsumeTopology = false;
                    });
                    
                    var host = settings.Host;
                    var uriString = $"rabbitmq://{host}/{virtualHost}/telemetry-queue";
                    EndpointConvention.Map<TelemetryData>(new Uri(uriString));
                });
            });
            
            return services;
        }
    }
}