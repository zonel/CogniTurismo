using AnalyticsService.Data;
using AnalyticsService.Deployment;
using AnalyticsService.Middleware;
using AnalyticsService.Repositories;
using AnalyticsService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AnalyticsRepository>();
builder.Services.AddScoped<AnalyticsService.Services.AnalyticsService>();

builder.Services.AddHostedService<MaterializedViewRefreshService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionMiddleware();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await DbInitializer.InitializeDbAsync(app);

app.Run();