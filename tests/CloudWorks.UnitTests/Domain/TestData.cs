using Domain.Doors;
using Domain.Doors.Enums;
using Domain.Permissions;
using Domain.Permissions.Enums;
using Domain.Users;

namespace UnitTests.Domain;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    public static readonly Guid OfficeId = Guid.CreateVersion7();

    public static User CreateUser(Guid? officeId = null) =>
        User.Create(officeId ?? OfficeId, $"user-{Guid.NewGuid():N}", "Ada Lovelace", "hash");

    public static Door CreateDoor(
        DoorAccessPolicy accessPolicy = DoorAccessPolicy.ExplicitPermissionOnly,
        Guid? officeId = null
    ) => Door.Create(officeId ?? OfficeId, "Storage room", accessPolicy);

    public static Permission CreateOpenDoorPermission(User user, Door door) =>
        Permission.Create(user, Guid.CreateVersion7(), PermissionType.OpenDoor, door, Now);

    public static Permission CreateSystemAdministratorPermission(User user) =>
        Permission.Create(user, Guid.CreateVersion7(), PermissionType.Administer, null, Now);
}
