using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<MqttSettings>(context.Configuration.GetSection("MqttSettings"));
        services.AddSingleton<TrafficSimulatorService.TrafficSimulatorService>();
    })
    .Build();

var simulator = builder.Services.GetRequiredService<TrafficSimulatorService.TrafficSimulatorService>();
await simulator.StartAsync();