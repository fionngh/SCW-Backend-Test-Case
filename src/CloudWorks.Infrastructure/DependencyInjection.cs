using Application.Authentication;
using Application.Permissions;
using Application.Users;
using Infrastructure.Database.Context;
using Infrastructure.Database.Repositories;
using Infrastructure.Security;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<CloudWorksDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CloudWorks"))
        );

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddOptions<SeedOptions>().BindConfiguration("Seed");
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
