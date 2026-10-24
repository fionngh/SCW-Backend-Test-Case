using Application.Common;
using Domain.Permissions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTests.Authentication;

[ApiController]
[Route("test-auth")]
public sealed class AuthProbeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("me")]
    public Guid Me() => currentUser.UserId;

    [HttpGet("administrator")]
    [Authorize(nameof(PermissionType.Administer))]
    public Guid Administrator() => currentUser.UserId;
}
