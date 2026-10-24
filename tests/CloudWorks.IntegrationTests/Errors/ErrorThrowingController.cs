using Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTests.Errors;

[ApiController]
[AllowAnonymous]
[Route("test-errors")]
public sealed class ErrorThrowingController : ControllerBase
{
    [HttpGet("{kind}")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "MVC actions must be instance methods."
    )]
    public IActionResult Throw(string kind) =>
        throw kind switch
        {
            "domain-rule" => new DomainRuleException("Name is required."),
            "not-found" => new NotFoundException("Door was not found."),
            "conflict" => new ConflictException("Username is already taken."),
            "forbidden" => new ForbiddenException(
                "Administrators cannot change their own permissions."
            ),
            _ => new InvalidOperationException("secret connection detail"),
        };
}
