using MemoryWiki.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemoryWiki.Infrastructure.Persistence;

public static class InfrastructureInitializer
{
    /// <summary>Applies EF migrations and ensures the object-store bucket exists. Idempotent.</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("InfrastructureInitializer");

        var db = sp.GetRequiredService<MemoryWikiDbContext>();
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(ct);
                logger.LogInformation("Database migrations applied.");
                break;
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning(ex, "Database not ready (attempt {Attempt}); retrying…", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }

        var storage = sp.GetRequiredService<IObjectStorage>();
        await storage.EnsureBucketAsync(ct);
        logger.LogInformation("Object storage bucket ensured.");
    }
}
