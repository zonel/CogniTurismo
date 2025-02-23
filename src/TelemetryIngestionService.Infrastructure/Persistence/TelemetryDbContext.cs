using Microsoft.EntityFrameworkCore;
using TelemetryIngestionService.Domain.Models;

namespace TelemetryIngestionService.Infrastructure.Persistence;

public class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryData> Telemetry { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryData>()
            .HasIndex(t => t.Timestamp);
    }
}