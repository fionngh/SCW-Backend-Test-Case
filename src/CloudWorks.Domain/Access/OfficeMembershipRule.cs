using Domain.Doors;
using Domain.Doors.Enums;
using Domain.Permissions;
using Domain.Users;

namespace Domain.Access;

public sealed class OfficeMembershipRule : IDoorAccessRule
{
    public bool Permits(User user, Door door, IEnumerable<Permission> userPermissions)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(door);

        return door.AccessPolicy == DoorAccessPolicy.OfficeMembers
            && user.OfficeId == door.OfficeId;
    }
}
