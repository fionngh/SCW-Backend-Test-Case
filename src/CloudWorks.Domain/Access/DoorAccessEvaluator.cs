using Domain.Access.Enums;
using Domain.Doors;
using Domain.Permissions;
using Domain.Users;

namespace Domain.Access;

public sealed class DoorAccessEvaluator(IEnumerable<IDoorAccessRule> rules)
{
    private readonly IDoorAccessRule[] _rules = [.. rules];

    public AccessFailureReason? GetDenialReason(
        User user,
        Door door,
        IEnumerable<Permission> userPermissions
    )
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(door);
        ArgumentNullException.ThrowIfNull(userPermissions);

        if (!user.IsActive)
        {
            return AccessFailureReason.UserInactive;
        }

        if (!door.IsActive)
        {
            return AccessFailureReason.DoorInactive;
        }

        var permissions =
            userPermissions as IReadOnlyCollection<Permission> ?? [.. userPermissions];

        return _rules.Any(rule => rule.Permits(user, door, permissions))
            ? null
            : AccessFailureReason.NoPermission;
    }
}
