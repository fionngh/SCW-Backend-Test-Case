using Domain.Access;
using Domain.Access.Enums;
using Domain.Doors;
using Domain.Doors.Enums;
using Domain.Offices;
using Domain.Permissions;
using Domain.Permissions.Enums;
using Domain.Users;
using Infrastructure.Database.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IntegrationTests.Database;

public class DatabaseTests(PostgresDatabaseFixture database)
    : IClassFixture<PostgresDatabaseFixture>
{
    private const string UniqueViolation = "23505";
    private const string RestrictViolation = "23001";

    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Database_AfterMigrations_MatchesTheModel()
    {
        await using var context = database.CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task SavedEntities_ReadBack_KeepAllValues()
    {
        var office = Office.Create("Dublin");
        var door = Door.Create(office.Id, "Server room", DoorAccessPolicy.ExplicitPermissionOnly);
        var user = User.Create(office.Id, UniqueUsername(), "Ada Lovelace", "hash");
        var permission = Permission.Create(user, user.Id, PermissionType.OpenDoor, door, Now);
        var confirmed = AccessEvent.Record(door, user, AccessOutcome.Confirmed, null, Now);
        var unknownUser = AccessEvent.Record(
            door,
            null,
            AccessOutcome.Denied,
            AccessFailureReason.UnknownUser,
            Now
        );
        await using (var context = database.CreateContext())
        {
            context.AddRange(office, door, user, permission, confirmed, unknownUser);
            await context.SaveChangesAsync();
        }

        await using var readContext = database.CreateContext();
        var savedDoor = await readContext.Doors.SingleAsync(d => d.Id == door.Id);
        var savedPermission = await readContext.Permissions.SingleAsync(p => p.Id == permission.Id);
        var savedConfirmed = await readContext.AccessEvents.SingleAsync(e => e.Id == confirmed.Id);
        var savedUnknownUser = await readContext.AccessEvents.SingleAsync(e =>
            e.Id == unknownUser.Id
        );

        Assert.Equal(DoorAccessPolicy.ExplicitPermissionOnly, savedDoor.AccessPolicy);
        Assert.Equal(PermissionType.OpenDoor, savedPermission.Type);
        Assert.Equal(door.Id, savedPermission.DoorId);
        Assert.Equal(Now, savedPermission.GrantedAt);
        Assert.True(savedPermission.IsActive);
        Assert.Equal("Server room", savedConfirmed.DoorName);
        Assert.Equal("Ada Lovelace", savedConfirmed.UserDisplayName);
        Assert.Equal(AccessOutcome.Confirmed, savedConfirmed.Outcome);
        Assert.Null(savedUnknownUser.UserId);
        Assert.Equal(AccessFailureReason.UnknownUser, savedUnknownUser.Reason);
    }

    [Fact]
    public async Task Users_WithTheSameUsername_AreRejected()
    {
        var office = Office.Create("Cork");
        var username = UniqueUsername();
        await using var context = database.CreateContext();
        context.Add(office);
        context.Add(User.Create(office.Id, username, "First", "hash"));
        context.Add(User.Create(office.Id, username, "Second", "hash"));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync()
        );

        Assert.Equal(UniqueViolation, SqlState(exception));
    }

    [Fact]
    public async Task Permissions_TwoActiveSystemWidePermissionsOfTheSameType_AreRejected()
    {
        var office = Office.Create("Galway");
        var user = User.Create(office.Id, UniqueUsername(), "Grace Hopper", "hash");
        await using var context = database.CreateContext();
        context.AddRange(office, user);
        context.Add(Permission.Create(user, user.Id, PermissionType.Administer, null, Now));
        context.Add(Permission.Create(user, user.Id, PermissionType.Administer, null, Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync()
        );

        Assert.Equal(UniqueViolation, SqlState(exception));
    }

    [Fact]
    public async Task Permissions_RevokedThenGrantedAgain_IsAllowed()
    {
        var office = Office.Create("Limerick");
        var user = User.Create(office.Id, UniqueUsername(), "Alan Turing", "hash");
        var first = Permission.Create(user, user.Id, PermissionType.ViewEvents, null, Now);
        first.Revoke(user.Id, Now.AddHours(1));
        await using var context = database.CreateContext();
        context.AddRange(office, user, first);
        context.Add(Permission.Create(user, user.Id, PermissionType.ViewEvents, null, Now));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Permissions_RevokedConcurrently_RejectsTheSecondSave()
    {
        var office = Office.Create("Waterford");
        var user = User.Create(office.Id, UniqueUsername(), "Edsger Dijkstra", "hash");
        var permission = Permission.Create(user, user.Id, PermissionType.ViewEvents, null, Now);
        await using (var setup = database.CreateContext())
        {
            setup.AddRange(office, user, permission);
            await setup.SaveChangesAsync();
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstCopy = await firstContext.Permissions.SingleAsync(p => p.Id == permission.Id);
        var secondCopy = await secondContext.Permissions.SingleAsync(p => p.Id == permission.Id);
        firstCopy.Revoke(user.Id, Now.AddHours(1));
        secondCopy.Revoke(user.Id, Now.AddHours(2));
        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            secondContext.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task Offices_WithDoors_CannotBeDeleted()
    {
        var office = Office.Create("Sligo");
        var door = Door.Create(office.Id, "Front door", DoorAccessPolicy.OfficeMembers);
        await using (var setup = database.CreateContext())
        {
            setup.AddRange(office, door);
            await setup.SaveChangesAsync();
        }

        await using var context = database.CreateContext();
        context.Remove(await context.Offices.SingleAsync(o => o.Id == office.Id));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync()
        );

        Assert.Equal(RestrictViolation, SqlState(exception));
    }

    [Theory]
    [InlineData("UPDATE audit.access_events SET outcome = 'Denied'")]
    [InlineData("DELETE FROM audit.access_events")]
    [InlineData("TRUNCATE audit.access_events")]
    public async Task AccessEvents_ChangedAfterRecording_AreRejected(string statement)
    {
        var office = Office.Create("Kilkenny");
        var door = Door.Create(office.Id, "Archive", DoorAccessPolicy.OfficeMembers);
        var user = User.Create(office.Id, UniqueUsername(), "Barbara Liskov", "hash");
        await using var context = database.CreateContext();
        context.AddRange(
            office,
            door,
            user,
            AccessEvent.Record(door, user, AccessOutcome.Confirmed, null, Now)
        );
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(statement)
        );

        Assert.Equal(RestrictViolation, exception.SqlState);
    }

    private static string UniqueUsername() => $"user.{Guid.NewGuid():N}";

    private static string? SqlState(DbUpdateException exception) =>
        (exception.InnerException as PostgresException)?.SqlState;
}
