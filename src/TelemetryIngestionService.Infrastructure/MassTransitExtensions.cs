using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelemetryIngestionService.Infrastructure.Services;
using TelemetryIngestionService.Domain.Models;
using Confluent.Kafka;
using TelemetryIngestionService.Infrastructure.Configuration.TelemetryIngestionService.Infrastructure.Configuration;

namespace TelemetryIngestionService.Infrastructure
{
    public static class MassTransitExtensions
    {
        private const string TelemetryTopicName = "telemetry-topic";
        public static IServiceCollection AddMassTransitWithKafka(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MassTransitSettings>(configuration.GetSection("MassTransit"));
            
            services.AddScoped<ITelemetryService, TelemetryService>();
            
            var settings = configuration.GetSection("MassTransit").Get<MassTransitSettings>() 
                ?? new MassTransitSettings();
            
            services.AddMassTransit(busConfig =>
            {
                busConfig.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
                
                busConfig.AddRider(rider =>
                {
                    rider.AddProducer<TelemetryData>(
                        settings.Topic ?? TelemetryTopicName, 
                        (_, producerConfig) => 
                        {
                            producerConfig.EnableDeliveryReports = true;
                            producerConfig.MessageSendMaxRetries = 3;
                            producerConfig.SetValueSerializer(new JsonSerializer<TelemetryData>());
                        }
                    );

                    rider.UsingKafka((_, kafkaConfig) =>
                    {
                        kafkaConfig.Host(settings.Host ?? "localhost:29092");
                        
                        kafkaConfig.SecurityProtocol = SecurityProtocol.Plaintext;
                    });
                });
            });
            
            return services;
        }
    }
    
    public class JsonSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T data, SerializationContext context)
        {
            if (data == null) return null!;
            
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
}