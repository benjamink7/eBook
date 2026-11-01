using eBook.Services.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eBook.Services.Seed;

public static class DbInitializer
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                await DbSeeder.SeedAsync(db, logger);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is SqlException or InvalidOperationException)
            {
                logger.LogWarning(ex, "Baza nije spremna (pokušaj {Attempt}/{MaxAttempts}). Ponovni pokušaj za {Delay} s.",
                    attempt, MaxAttempts, RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay);
            }
        }
    }
}
