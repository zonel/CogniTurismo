using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Consumers;
using TelemetryProcessorService.Models;

namespace TelemetryProcessorService
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
            var telemetryTopic = settings.Topic ?? "telemetry-topic";
            var consumerGroup = settings.ConsumerGroup ?? "telemetry-processor-group";
            var anomalyTopic = settings.AnomalyTopic ?? "telemetry-anomaly";
            
            services.AddMassTransit(x =>
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
                
                x.AddRider(rider =>
                {
                    rider.AddConsumer<TelemetryDataConsumer>();
                    
                    rider.UsingKafka((context, kafkaConfig) =>
                    {
                        kafkaConfig.Host(kafkaHost);
                        kafkaConfig.SecurityProtocol = SecurityProtocol.Plaintext;
                        kafkaConfig.ClientId = "telemetry-processor-client";
                        
                        kafkaConfig.TopicEndpoint<TelemetryData>(
                            telemetryTopic,
                            consumerGroup,
                            e =>
                            {
                                e.ConfigureConsumer<TelemetryDataConsumer>(context);
                                e.AutoOffsetReset = AutoOffsetReset.Earliest;
                                e.CreateIfMissing(t =>
                                {
                                    t.NumPartitions = 1;
                                    t.ReplicationFactor = 1;
                                });
                            });
                        
                        kafkaConfig.TopicEndpoint<AnomalyEvent>(
                            anomalyTopic,
                            "anomaly-topic-consumer",
                            e =>
                            {
                                e.CreateIfMissing(t =>
                                {
                                    t.NumPartitions = 1;
                                    t.ReplicationFactor = 1;
                                });
                            });
                    });
                    
                    rider.AddProducer<AnomalyEvent>(anomalyTopic);
                });
            });
            
            return services;
        }
    }
}
