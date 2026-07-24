using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Shared.Data;

public static class MigrationsExtensions
{
    public static WebApplication MigrateDatabase<TDbContext>(this WebApplication app) where TDbContext : DbContext
    {
        var env = app.Services.GetRequiredService<IHostEnvironment>();
        var config = app.Services.GetRequiredService<IConfiguration>();

        bool runMigrations = config.GetValue<bool>("RunMigrations") || env.IsDevelopment();

        if (runMigrations)
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<TDbContext>();

            logger.LogInformation("Running database migrations for {DbContext}", typeof(TDbContext).Name);

            int retryCount = 0;
            const int maxRetries = 10;
            while (retryCount < maxRetries)
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

                try
                {
                    db.Database.Migrate();
                    break;
                }
                catch (Exception ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    logger.LogWarning(ex, "Migration attempt {RetryCount}/{MaxRetries} failed for {DbContext}. Retrying in 3 seconds...",
                        retryCount, maxRetries, typeof(TDbContext).Name);
                    Thread.Sleep(3000);
                }
            }

            logger.LogInformation("Database migrations completed for {DbContext}", typeof(TDbContext).Name);
        }
        else
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDbContext>>();

            try
            {
                if (db.Database.CanConnect())
                    logger.LogInformation("Database connection verified for {DbContext}", typeof(TDbContext).Name);
                else
                    logger.LogWarning("Cannot connect to database for {DbContext}", typeof(TDbContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database connection check failed for {DbContext}", typeof(TDbContext).Name);
            }
        }

        return app;
    }
}
