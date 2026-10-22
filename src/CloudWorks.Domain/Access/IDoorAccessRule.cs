using Domain.Doors;
using Domain.Permissions;
using Domain.Users;

namespace Domain.Access;

public interface IDoorAccessRule
{
    bool Permits(User user, Door door, IEnumerable<Permission> userPermissions);
}
