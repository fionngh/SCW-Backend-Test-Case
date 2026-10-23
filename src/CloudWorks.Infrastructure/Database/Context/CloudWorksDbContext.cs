using Domain.Access;
using Domain.Doors;
using Domain.Offices;
using Domain.Permissions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Context;

public sealed class CloudWorksDbContext(DbContextOptions<CloudWorksDbContext> options)
    : DbContext(options)
{
    public DbSet<Office> Offices => Set<Office>();

    public DbSet<Door> Doors => Set<Door>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudWorksDbContext).Assembly);
        modelBuilder.ApplySnakeCaseNames();
    }
}
