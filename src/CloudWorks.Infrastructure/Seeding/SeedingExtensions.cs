using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Seeding;

public static class SeedingExtensions
{
    public static async Task SeedDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = services.CreateAsyncScope();
        await scope
            .ServiceProvider.GetRequiredService<DatabaseSeeder>()
            .SeedAsync(cancellationToken);
    }
}
