using Application.Common;
using Application.Permissions;
using Domain.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Api.Authentication;

internal sealed class AdministratorHandler(
    ICurrentUser currentUser,
    IPermissionRepository permissions,
    IHttpContextAccessor httpContextAccessor
) : AuthorizationHandler<AdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdministratorRequirement requirement
    )
    {
        var userId = currentUser.UserId;
        var activePermissions = await permissions.GetActiveForUserAsync(
            userId,
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None
        );

        if (PermissionManagementPolicy.IsAdministrator(userId, activePermissions))
        {
            context.Succeed(requirement);
        }
    }
}
