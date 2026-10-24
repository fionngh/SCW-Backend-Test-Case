using Domain.Doors.Enums;
using Domain.Permissions.Enums;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Seeding;

public sealed class SeedingTests
{
    [Fact]
    public async Task Startup_OnEmptyDatabase_CreatesOneAdministratorInTheHeadOffice()
    {
        var database = new PostgresDatabaseFixture();
        await database.InitializeAsync();
        try
        {
            using var factory = new ApiFactory(database.ConnectionString);
            using var client = factory.CreateClient();
            await factory.Services.SeedDatabaseAsync();

            await using var context = database.CreateContext();
            var administrators = await context
                .Permissions.Where(permission => permission.Type == PermissionType.Administer)
                .ToListAsync();
            Assert.Single(administrators);
            Assert.Equal(administrators[0].UserId, administrators[0].GrantedByUserId);
            Assert.Single(await context.Users.ToListAsync());
            Assert.Equal("Head Office", (await context.Offices.SingleAsync()).Name);
            Assert.Empty(await context.Doors.ToListAsync());
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    [Fact]
    public async Task Startup_InDevelopmentWithDemoData_CreatesTheBriefsOfficeSetup()
    {
        var database = new PostgresDatabaseFixture();
        await database.InitializeAsync();
        try
        {
            using var factory = new ApiFactory(database.ConnectionString, includeDemoData: true);
            using var client = factory.CreateClient();
            await factory.Services.SeedDatabaseAsync();

            await using var context = database.CreateContext();
            var doors = await context.Doors.ToDictionaryAsync(door => door.Name);
            Assert.Equal(DoorAccessPolicy.OfficeMembers, doors["Main entrance"].AccessPolicy);
            Assert.Equal(
                DoorAccessPolicy.ExplicitPermissionOnly,
                doors["Storage room"].AccessPolicy
            );
            var officeManager = await context.Users.SingleAsync(user =>
                user.Username == "office.manager"
            );
            var managerPermissions = await context
                .Permissions.Where(permission => permission.UserId == officeManager.Id)
                .ToListAsync();
            Assert.Contains(
                managerPermissions,
                permission =>
                    permission.Type == PermissionType.OpenDoor
                    && permission.DoorId == doors["Storage room"].Id
            );
            Assert.Contains(
                managerPermissions,
                permission => permission.Type == PermissionType.ViewEvents
            );
            var employee = await context.Users.SingleAsync(user => user.Username == "employee");
            Assert.Empty(
                await context
                    .Permissions.Where(permission => permission.UserId == employee.Id)
                    .ToListAsync()
            );
            Assert.Equal(3, await context.Users.CountAsync());
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    [Fact]
    public async Task Startup_WithoutCredentialsOnEmptyDatabase_FailsWithAClearMessage()
    {
        var database = new PostgresDatabaseFixture();
        await database.InitializeAsync();
        try
        {
            using var factory = new ApiFactory(
                database.ConnectionString,
                administratorUsername: null
            );

            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("No administrator exists", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            await database.DisposeAsync();
        }
    }
}
