using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelemetryProcessorService;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;
using Confluent.Kafka;
using TelemetryProcessorService.Consumers;
using TelemetryProcessorService.Producers;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    var environment = hostingContext.HostingEnvironment.EnvironmentName;
    var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    
    if (!File.Exists(appSettingsPath))
    {
        throw new FileNotFoundException($"Required configuration file not found: {appSettingsPath}");
    }
    
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
          .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
    
    config.AddEnvironmentVariables();
    
    if (args != null)
    {
        config.AddCommandLine(args);
    }
});

builder.ConfigureServices((hostContext, services) =>
{
    var configuration = hostContext.Configuration;
    
    if (configuration == null)
    {
        throw new InvalidOperationException("Configuration is null. Cannot continue.");
    }
    
    var citusSection = configuration.GetSection("CitusDbSettings");
    var anomalySection = configuration.GetSection("AnomalyDetection");
    var massTransitSection = configuration.GetSection("MassTransit");
    
    if (!citusSection.Exists() || !anomalySection.Exists() || !massTransitSection.Exists())
    {
        throw new InvalidOperationException(
            "Required configuration sections are missing. Please check appsettings.json file.");
    }
    
    services.Configure<CitusDbSettings>(citusSection);
    services.Configure<AnomalyDetectionSettings>(anomalySection);
    services.Configure<MassTransitSettings>(massTransitSection);
    
    services.AddSingleton<TopicNameProvider>();
    services.AddSingleton<IAnomalyProducer, AnomalyProducer>();
    
    services.AddTransient<TelemetryDataConsumer>();
    
    var settings = massTransitSection.Get<MassTransitSettings>();
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
    
    services.AddHostedService<Worker>();
});

var app = builder.Build();
await app.RunAsync();