using TelemetryIngestionService.Infrastructure;
using TelemetryIngestionService.Infrastructure.Messaging;
using TelemetryIngestionService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

var mqttService = app.Services.GetRequiredService<MqttClientService>();
await mqttService.StartAsync();

app.Run();