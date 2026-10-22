using Domain.Common;
using Domain.Permissions;
using Domain.Permissions.Enums;

namespace UnitTests.Domain;

public class PermissionTests
{
    private static readonly Guid GrantingUserId = Guid.CreateVersion7();

    [Fact]
    public void Create_OpenDoorOnDoor_CreatesActivePermission()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();

        var permission = Permission.Create(
            user,
            GrantingUserId,
            PermissionType.OpenDoor,
            door,
            TestData.Now
        );

        Assert.Equal(user.Id, permission.UserId);
        Assert.Equal(PermissionType.OpenDoor, permission.Type);
        Assert.Equal(door.Id, permission.DoorId);
        Assert.Equal(GrantingUserId, permission.GrantedByUserId);
        Assert.Equal(TestData.Now, permission.GrantedAt);
        Assert.True(permission.IsActive);
    }

    [Fact]
    public void Create_ViewEventsWithoutDoor_IsSystemWide()
    {
        var permission = Permission.Create(
            TestData.CreateUser(),
            GrantingUserId,
            PermissionType.ViewEvents,
            null,
            TestData.Now
        );

        Assert.Null(permission.DoorId);
    }

    [Fact]
    public void Create_OpenDoorWithoutDoor_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            Permission.Create(
                TestData.CreateUser(),
                GrantingUserId,
                PermissionType.OpenDoor,
                null,
                TestData.Now
            )
        );

    [Fact]
    public void Create_AdministerOnDoor_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            Permission.Create(
                TestData.CreateUser(),
                GrantingUserId,
                PermissionType.Administer,
                TestData.CreateDoor(),
                TestData.Now
            )
        );

    [Fact]
    public void Create_DeactivatedUser_Throws()
    {
        var user = TestData.CreateUser();
        user.Deactivate();

        Assert.Throws<DomainRuleException>(() =>
            Permission.Create(user, GrantingUserId, PermissionType.ViewEvents, null, TestData.Now)
        );
    }

    [Fact]
    public void Create_DeactivatedDoor_Throws()
    {
        var door = TestData.CreateDoor();
        door.Deactivate();

        Assert.Throws<DomainRuleException>(() =>
            Permission.Create(
                TestData.CreateUser(),
                GrantingUserId,
                PermissionType.OpenDoor,
                door,
                TestData.Now
            )
        );
    }

    [Fact]
    public void Revoke_ActivePermission_RecordsRevocationAndDeactivates()
    {
        var permission = Permission.Create(
            TestData.CreateUser(),
            GrantingUserId,
            PermissionType.ViewEvents,
            null,
            TestData.Now
        );
        var revokingUserId = Guid.CreateVersion7();

        permission.Revoke(revokingUserId, TestData.Now.AddHours(1));

        Assert.False(permission.IsActive);
        Assert.Equal(revokingUserId, permission.RevokedByUserId);
        Assert.Equal(TestData.Now.AddHours(1), permission.RevokedAt);
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsAndKeepsOriginalRevocation()
    {
        var permission = Permission.Create(
            TestData.CreateUser(),
            GrantingUserId,
            PermissionType.ViewEvents,
            null,
            TestData.Now
        );
        var firstRevokerId = Guid.CreateVersion7();
        permission.Revoke(firstRevokerId, TestData.Now.AddHours(1));

        Assert.Throws<DomainRuleException>(() =>
            permission.Revoke(Guid.CreateVersion7(), TestData.Now.AddHours(2))
        );

        Assert.Equal(firstRevokerId, permission.RevokedByUserId);
        Assert.Equal(TestData.Now.AddHours(1), permission.RevokedAt);
    }

    [Fact]
    public void Revoke_BeforePermissionTime_ThrowsAndStaysActive()
    {
        var permission = Permission.Create(
            TestData.CreateUser(),
            GrantingUserId,
            PermissionType.ViewEvents,
            null,
            TestData.Now
        );

        Assert.Throws<DomainRuleException>(() =>
            permission.Revoke(Guid.CreateVersion7(), TestData.Now.AddMinutes(-1))
        );

        Assert.True(permission.IsActive);
    }
}
