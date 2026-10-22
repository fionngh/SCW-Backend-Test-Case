using Domain.Access;
using Domain.Access.Enums;
using Domain.Doors;
using Domain.Doors.Enums;
using Domain.Permissions;
using Domain.Permissions.Enums;
using Domain.Users;

namespace UnitTests.Domain;

public class DoorAccessEvaluatorTests
{
    private static readonly DoorAccessEvaluator Evaluator = new([
        new OfficeMembershipRule(),
        new ExplicitPermissionRule(),
    ]);

    [Fact]
    public void Evaluate_OfficeMemberOnOfficeMembersDoor_IsAllowedWithoutPermission()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor(DoorAccessPolicy.OfficeMembers);

        var denialReason = Evaluator.GetDenialReason(user, door, []);

        Assert.Null(denialReason);
    }

    [Fact]
    public void Evaluate_UserFromOtherOfficeOnOfficeMembersDoor_IsDenied()
    {
        var user = TestData.CreateUser(officeId: Guid.CreateVersion7());
        var door = TestData.CreateDoor(DoorAccessPolicy.OfficeMembers);

        var denialReason = Evaluator.GetDenialReason(user, door, []);

        Assert.Equal(AccessFailureReason.NoPermission, denialReason);
    }

    [Fact]
    public void Evaluate_OfficeMemberOnExplicitPermissionOnlyDoorWithoutPermission_IsDenied()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor(DoorAccessPolicy.ExplicitPermissionOnly);

        var denialReason = Evaluator.GetDenialReason(user, door, []);

        Assert.Equal(AccessFailureReason.NoPermission, denialReason);
    }

    [Fact]
    public void Evaluate_ExplicitPermissionOnExplicitPermissionOnlyDoor_IsAllowed()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor(DoorAccessPolicy.ExplicitPermissionOnly);
        var permission = TestData.CreateOpenDoorPermission(user, door);

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Null(denialReason);
    }

    [Fact]
    public void Evaluate_ExplicitPermissionForUserInOtherOffice_IsAllowed()
    {
        var user = TestData.CreateUser(officeId: Guid.CreateVersion7());
        var door = TestData.CreateDoor(DoorAccessPolicy.ExplicitPermissionOnly);
        var permission = TestData.CreateOpenDoorPermission(user, door);

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Null(denialReason);
    }

    [Fact]
    public void Evaluate_RevokedPermission_IsDenied()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();
        var permission = TestData.CreateOpenDoorPermission(user, door);
        permission.Revoke(Guid.CreateVersion7(), TestData.Now.AddHours(1));

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Equal(AccessFailureReason.NoPermission, denialReason);
    }

    [Fact]
    public void Evaluate_PermissionForDifferentDoor_IsDenied()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();
        var permission = TestData.CreateOpenDoorPermission(user, TestData.CreateDoor());

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Equal(AccessFailureReason.NoPermission, denialReason);
    }

    [Fact]
    public void Evaluate_PermissionBelongingToAnotherUser_IsDenied()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();
        var permission = TestData.CreateOpenDoorPermission(TestData.CreateUser(), door);

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Equal(AccessFailureReason.NoPermission, denialReason);
    }

    [Fact]
    public void Evaluate_ViewEventsPermissionOnDoor_DoesNotAllowOpening()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();
        var permission = Permission.Create(
            user,
            Guid.CreateVersion7(),
            PermissionType.ViewEvents,
            door,
            TestData.Now
        );

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Equal(AccessFailureReason.NoPermission, denialReason);
    }

    [Fact]
    public void Evaluate_InactiveUserWithPermission_IsDeniedAsUserInactive()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();
        var permission = TestData.CreateOpenDoorPermission(user, door);
        user.Deactivate();

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Equal(AccessFailureReason.UserInactive, denialReason);
    }

    [Fact]
    public void Evaluate_InactiveDoorWithPermission_IsDeniedAsDoorInactive()
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();
        var permission = TestData.CreateOpenDoorPermission(user, door);
        door.Deactivate();

        var denialReason = Evaluator.GetDenialReason(user, door, [permission]);

        Assert.Equal(AccessFailureReason.DoorInactive, denialReason);
    }

    [Fact]
    public void Evaluate_AdditionalRulePermits_IsAllowed()
    {
        var evaluator = new DoorAccessEvaluator([new AlwaysPermitRule()]);

        var denialReason = evaluator.GetDenialReason(
            TestData.CreateUser(),
            TestData.CreateDoor(),
            []
        );

        Assert.Null(denialReason);
    }

    [Fact]
    public void Evaluate_AdditionalRuleWithInactiveUser_IsStillDenied()
    {
        var evaluator = new DoorAccessEvaluator([new AlwaysPermitRule()]);
        var user = TestData.CreateUser();
        user.Deactivate();

        var denialReason = evaluator.GetDenialReason(user, TestData.CreateDoor(), []);

        Assert.Equal(AccessFailureReason.UserInactive, denialReason);
    }

    private sealed class AlwaysPermitRule : IDoorAccessRule
    {
        public bool Permits(User user, Door door, IEnumerable<Permission> userPermissions) => true;
    }
}
