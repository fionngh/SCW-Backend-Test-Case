using Application.Permissions;
using Domain.Permissions;
using Infrastructure.Database.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database.Repositories;

public sealed class PermissionRepository(CloudWorksDbContext database) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetActiveForUserAsync(
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await database
            .Permissions.AsNoTracking()
            .Where(permission => permission.UserId == userId && permission.RevokedAt == null)
            .ToListAsync(cancellationToken);
}
