using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Database.Context;

/// <summary>
/// Lets the EF command-line tools build the context without starting the application. Commands
/// that connect (update, drop) use the ConnectionStrings__CloudWorks environment variable; adding
/// a migration never connects, so it falls back to a placeholder.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CloudWorksDbContext>
{
    public CloudWorksDbContext CreateDbContext(string[] args) =>
        new(
            new DbContextOptionsBuilder<CloudWorksDbContext>()
                .UseNpgsql(
                    Environment.GetEnvironmentVariable("ConnectionStrings__CloudWorks")
                        ?? "Host=localhost;Database=cloudworks"
                )
                .Options
        );
}
