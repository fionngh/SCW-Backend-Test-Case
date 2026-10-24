using Domain.Permissions;

namespace Application.Permissions;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    );
}
