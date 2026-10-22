using Domain.Doors;
using Domain.Permissions;
using Domain.Permissions.Enums;
using Domain.Users;

namespace Domain.Access;

public sealed class ExplicitPermissionRule : IDoorAccessRule
{
    public bool Permits(User user, Door door, IEnumerable<Permission> userPermissions)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(door);
        ArgumentNullException.ThrowIfNull(userPermissions);

        return userPermissions.Any(permission =>
            permission.IsActive
            && permission.UserId == user.Id
            && permission.Type == PermissionType.OpenDoor
            && permission.DoorId == door.Id
        );
    }
}
