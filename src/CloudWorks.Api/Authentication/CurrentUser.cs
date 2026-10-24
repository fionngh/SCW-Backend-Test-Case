using Application.Common;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Authentication;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.Parse(
            httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new InvalidOperationException("There is no authenticated user.")
        );
}
