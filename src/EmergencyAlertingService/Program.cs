using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EmergencyAlertingService;
using EmergencyAlertingService.Configuration;
using EmergencyAlertingService.Consumers;
using EmergencyAlertingService.Services;
using EmergencyAlertingService.Services.NotificationServices;
using EmergencyAlertingService.Services.CommandServices;

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
    
    var massTransitSection = configuration.GetSection("MassTransit");
    var notificationSection = configuration.GetSection("Notification");
    
    if (!massTransitSection.Exists() || !notificationSection.Exists())
    {
        throw new InvalidOperationException(
            "Required configuration sections are missing. Please check appsettings.json file.");
    }
    
    services.Configure<MassTransitSettings>(massTransitSection);
    services.Configure<NotificationSettings>(notificationSection);
    
    services.AddSingleton<AlertProcessingService>();
    
    services.AddSingleton<INotificationService, SmsNotificationService>();
    services.AddSingleton<INotificationService, EmailNotificationService>();
    services.AddSingleton<INotificationService, LogNotificationService>();
    
    services.AddSingleton<ICommandService, TemperatureCommandService>();
    services.AddSingleton<ICommandService, BatteryCommandService>();
    
    services.AddTransient<AlertConsumer>();
    
    var massTransitSettings = massTransitSection.Get<MassTransitSettings>();
    services.AddKafkaMassTransit(massTransitSettings!);
    
    services.AddHostedService<Worker>();
});

var app = builder.Build();
await app.RunAsync();