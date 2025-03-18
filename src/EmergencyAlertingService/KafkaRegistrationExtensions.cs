using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using EmergencyAlertingService.Configuration;
using EmergencyAlertingService.Consumers;
using EmergencyAlertingService.Models;

namespace EmergencyAlertingService
{
    public static class KafkaRegistrationExtensions
    {
        public static IServiceCollection AddKafkaMassTransit(this IServiceCollection services, MassTransitSettings settings)
        {
            if (settings == null)
            {
                throw new InvalidOperationException("MassTransit settings could not be loaded.");
            }
            
            var kafkaHost = settings.Host ?? "localhost:29092";
            var alertTopic = settings.AlertTopic ?? "telemetry-anomaly";
            var consumerGroup = settings.ConsumerGroup ?? "emergency-alerting-group";
            
            services.AddMassTransit(x =>
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
                
                x.AddRider(rider =>
                {
                    rider.AddConsumer<AlertConsumer>();
                    
                    rider.UsingKafka((context, kafkaConfig) =>
                    {
                        kafkaConfig.Host(kafkaHost);
                        kafkaConfig.SecurityProtocol = SecurityProtocol.Plaintext;
                        kafkaConfig.ClientId = "emergency-alerting-client";
                        
                        kafkaConfig.TopicEndpoint<Alert>(
                            alertTopic,
                            consumerGroup,
                            e =>
                            {
                                e.ConfigureConsumer<AlertConsumer>(context);
                                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                                e.CreateIfMissing(t =>
                                {
                                    t.NumPartitions = 1;
                                    t.ReplicationFactor = 1;
                                });
                            });
                    });
                });
            });
            
            return services;
        }
    }
}