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
            && IsAdministrator(actor.Id, actorPermissions);
    }

    public static bool IsAdministrator(Guid userId, IEnumerable<Permission> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        return permissions.Any(permission =>
            permission.IsActive
            && permission.UserId == userId
            && permission.Type == PermissionType.Administer
        );
    }
}
