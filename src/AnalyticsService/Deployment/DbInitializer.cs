using AnalyticsService.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalyticsService.Deployment;

public class DbInitializer
{
    public static async Task InitializeDbAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var context = services.GetRequiredService<AnalyticsDbContext>();
            var logger = services.GetRequiredService<ILogger<DbInitializer>>();
            
            await InitializeAsync(context, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database");
        }
    }

    private static async Task InitializeAsync(AnalyticsDbContext context, ILogger<DbInitializer> logger)
    {
        logger.LogInformation("Checking materialized views existence");
        
        var createMaterializedViewsQuery = await File.ReadAllTextAsync("Deployment/CreateMaterializedViews.sql");
        var queries = createMaterializedViewsQuery.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var query in queries)
        {
            if (string.IsNullOrWhiteSpace(query))
                continue;
                
            try
            {
                var trimmedQuery = query.Trim();
                if (!string.IsNullOrEmpty(trimmedQuery))
                {
                    await context.Database.ExecuteSqlRawAsync(trimmedQuery);
                    logger.LogInformation("Executed query successfully");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Query execution warning (may already exist): {Message}", ex.Message);
            }
        }
        
        logger.LogInformation("Database initialization completed");
    }
}