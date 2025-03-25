using AnalyticsService.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Data;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryData> TelemetryData { get; set; } = null!;
    public DbSet<VehicleBatteryStatistics> VehicleBatteryStatistics { get; set; } = null!;
    public DbSet<VehicleUsageStatistics> VehicleUsageStatistics { get; set; } = null!;
    public DbSet<HourlyTelemetryAggregate> HourlyTelemetryAggregates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryData>()
            .HasKey(t => new { t.VehicleId, t.Id });

        modelBuilder.Entity<VehicleBatteryStatistics>()
            .HasKey(v => v.VehicleId);
            
        modelBuilder.Entity<VehicleUsageStatistics>()
            .HasKey(v => v.VehicleId);
            
        modelBuilder.Entity<HourlyTelemetryAggregate>()
            .HasKey(h => new { h.HourStart, h.VehicleId });

        base.OnModelCreating(modelBuilder);
    }
}