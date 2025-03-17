using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelemetryProcessorService;
using TelemetryProcessorService.Configuration;
using TelemetryProcessorService.Consumers;
using TelemetryProcessorService.Producers;
using TelemetryProcessorService.Services;

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
    services.AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>();
    
    services.AddTransient<TelemetryDataConsumer>();
    
    var massTransitSettings = massTransitSection.Get<MassTransitSettings>();

    services.AddKafkaMassTransit(massTransitSettings!);
    
    services.AddHostedService<Worker>();
});

var app = builder.Build();
await app.RunAsync();
