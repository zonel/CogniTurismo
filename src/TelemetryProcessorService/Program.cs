using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelemetryProcessorService;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Models;
using Confluent.Kafka;

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
    
    services.AddTransient<TelemetryDataConsumer>();
    
    services.AddMassTransit(x =>
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.ConfigureEndpoints(context);
        });
        
        x.AddRider(rider =>
        {
            var settings = massTransitSection.Get<MassTransitSettings>();
            if (settings == null)
            {
                throw new InvalidOperationException("MassTransit settings could not be loaded.");
            }
            
            rider.AddConsumer<TelemetryDataConsumer>();
            
            rider.UsingKafka((context, kafkaConfig) =>
            {
                kafkaConfig.Host(settings.Host ?? "localhost:29092");
                kafkaConfig.SecurityProtocol = SecurityProtocol.Plaintext;
                
                kafkaConfig.TopicEndpoint<TelemetryData>(
                    settings.Topic ?? "telemetry-topic", 
                    settings.ConsumerGroup ?? "telemetry-processor-group",
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
            });
            
            rider.AddProducer<AnomalyEvent>(
                settings.AnomalyTopic ?? "telemetry-anomaly"
            );
        });
    });
    
    services.AddHostedService<Worker>();
});

var app = builder.Build();
await app.RunAsync();

public class TopicNameProvider
{
    private readonly IConfiguration _configuration;
    
    public TopicNameProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var massTransitSettings = _configuration.GetSection("MassTransit").Get<MassTransitSettings>();
        AnomalyTopic = massTransitSettings?.AnomalyTopic ?? "telemetry-anomaly";
    }
    
    public string AnomalyTopic { get; }
}