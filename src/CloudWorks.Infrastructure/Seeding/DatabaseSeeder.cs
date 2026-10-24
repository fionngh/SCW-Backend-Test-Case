using Application.Authentication;
using Domain.Doors;
using Domain.Doors.Enums;
using Domain.Offices;
using Domain.Permissions;
using Domain.Permissions.Enums;
using Domain.Users;
using Infrastructure.Database.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Seeding;

internal sealed partial class DatabaseSeeder(
    CloudWorksDbContext database,
    IPasswordHasher passwordHasher,
    IHostEnvironment environment,
    IOptions<SeedOptions> options,
    TimeProvider timeProvider,
    ILogger<DatabaseSeeder> logger
)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;
        var now = timeProvider.GetUtcNow();

        await using var transaction = await database.Database.BeginTransactionAsync(
            cancellationToken
        );

        // Serialises concurrent starts of several instances; released when the transaction ends.
        await database.Database.ExecuteSqlAsync(
            $"select pg_advisory_xact_lock(6283001)",
            cancellationToken
        );

        var office = await database.Offices.SingleOrDefaultAsync(
            office => office.Name == "Head Office",
            cancellationToken
        );
        if (office is null)
        {
            office = Office.Create("Head Office");
            database.Add(office);
        }

        var administratorId = await database
            .Permissions.Where(permission =>
                permission.Type == PermissionType.Administer && permission.RevokedAt == null
            )
            .Select(permission => (Guid?)permission.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (administratorId is null)
        {
            if (
                string.IsNullOrWhiteSpace(seed.AdministratorUsername)
                || string.IsNullOrWhiteSpace(seed.AdministratorPassword)
            )
            {
                throw new InvalidOperationException(
                    "No administrator exists. Set Seed:AdministratorUsername and Seed:AdministratorPassword to create the first one."
                );
            }

            var administrator = AddUser(
                office,
                seed.AdministratorUsername,
                "Administrator",
                seed.AdministratorPassword
            );
            database.Add(
                Permission.Create(
                    administrator,
                    administrator.Id,
                    PermissionType.Administer,
                    null,
                    now
                )
            );
            administratorId = administrator.Id;
            LogAdministratorCreated(logger, administrator.Username);
        }

        if (
            seed.IncludeDemoData
            && environment.IsDevelopment()
            && !await database.Doors.AnyAsync(cancellationToken)
        )
        {
            if (string.IsNullOrWhiteSpace(seed.AdministratorPassword))
            {
                throw new InvalidOperationException(
                    "Demo data uses the administrator password. Set Seed:AdministratorPassword."
                );
            }

            var storageRoom = Door.Create(
                office.Id,
                "Storage room",
                DoorAccessPolicy.ExplicitPermissionOnly
            );
            var officeManager = AddUser(
                office,
                "office.manager",
                "Office Manager",
                seed.AdministratorPassword
            );
            database.Add(Door.Create(office.Id, "Main entrance", DoorAccessPolicy.OfficeMembers));
            database.Add(storageRoom);
            AddUser(office, "employee", "Employee", seed.AdministratorPassword);
            database.Add(
                Permission.Create(
                    officeManager,
                    administratorId.Value,
                    PermissionType.OpenDoor,
                    storageRoom,
                    now
                )
            );
            database.Add(
                Permission.Create(
                    officeManager,
                    administratorId.Value,
                    PermissionType.ViewEvents,
                    null,
                    now
                )
            );
            LogDemoDataCreated(logger);
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private User AddUser(Office office, string username, string displayName, string password)
    {
        var user = User.Create(office.Id, username, displayName, passwordHasher.Hash(password));
        database.Add(user);
        return user;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created administrator {Username}")]
    private static partial void LogAdministratorCreated(ILogger logger, string username);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created demo data")]
    private static partial void LogDemoDataCreated(ILogger logger);
}
