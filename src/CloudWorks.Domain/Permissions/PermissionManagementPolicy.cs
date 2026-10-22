using Domain.Permissions.Enums;
using Domain.Users;

namespace Domain.Permissions;

public static class PermissionManagementPolicy
{
    public static bool CanManagePermissions(
        User actor,
        IEnumerable<Permission> actorPermissions,
        Guid targetUserId
    )
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(actorPermissions);

        return actor.IsActive
            && actor.Id != targetUserId
            && actorPermissions.Any(permission =>
                permission.IsActive
                && permission.UserId == actor.Id
                && permission.Type == PermissionType.Administer
            );
    }
}
