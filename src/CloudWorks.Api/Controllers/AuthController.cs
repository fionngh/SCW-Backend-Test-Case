using Api.Authentication;
using Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthenticationService authentication) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthenticationExtensions.LoginRateLimitPolicy)]
    public async Task<LoginResponse> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var token = await authentication.LoginAsync(request, cancellationToken);

        return new LoginResponse(token.Value, token.ExpiresAt);
    }
}
