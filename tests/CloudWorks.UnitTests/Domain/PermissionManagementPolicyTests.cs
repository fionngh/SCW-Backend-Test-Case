using Domain.Permissions;

namespace UnitTests.Domain;

public class PermissionManagementPolicyTests
{
    [Fact]
    public void CanManagePermissions_AdministratorManagingAnotherUser_IsAllowed()
    {
        var administrator = TestData.CreateUser();
        var permission = TestData.CreateSystemAdministratorPermission(administrator);

        Assert.True(
            PermissionManagementPolicy.CanManagePermissions(
                administrator,
                [permission],
                Guid.CreateVersion7()
            )
        );
    }

    [Fact]
    public void CanManagePermissions_UserWithoutAdministerPermission_IsDenied()
    {
        var user = TestData.CreateUser();

        Assert.False(
            PermissionManagementPolicy.CanManagePermissions(user, [], Guid.CreateVersion7())
        );
    }

    [Fact]
    public void CanManagePermissions_AdministratorManagingOwnPermissions_IsDenied()
    {
        var administrator = TestData.CreateUser();
        var permission = TestData.CreateSystemAdministratorPermission(administrator);

        Assert.False(
            PermissionManagementPolicy.CanManagePermissions(
                administrator,
                [permission],
                administrator.Id
            )
        );
    }

    [Fact]
    public void CanManagePermissions_RevokedAdministratorPermission_IsDenied()
    {
        var administrator = TestData.CreateUser();
        var permission = TestData.CreateSystemAdministratorPermission(administrator);
        permission.Revoke(Guid.CreateVersion7(), TestData.Now.AddHours(1));

        Assert.False(
            PermissionManagementPolicy.CanManagePermissions(
                administrator,
                [permission],
                Guid.CreateVersion7()
            )
        );
    }

    [Fact]
    public void CanManagePermissions_AdministerPermissionBelongingToAnotherUser_IsDenied()
    {
        var user = TestData.CreateUser();
        var permissionOfAnotherUser = TestData.CreateSystemAdministratorPermission(
            TestData.CreateUser()
        );

        Assert.False(
            PermissionManagementPolicy.CanManagePermissions(
                user,
                [permissionOfAnotherUser],
                Guid.CreateVersion7()
            )
        );
    }

    [Fact]
    public void CanManagePermissions_InactiveAdministrator_IsDenied()
    {
        var administrator = TestData.CreateUser();
        var permission = TestData.CreateSystemAdministratorPermission(administrator);
        administrator.Deactivate();

        Assert.False(
            PermissionManagementPolicy.CanManagePermissions(
                administrator,
                [permission],
                Guid.CreateVersion7()
            )
        );
    }
}
