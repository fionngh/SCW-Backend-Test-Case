using Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Common;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, title) = exception switch
        {
            DomainRuleException => (StatusCodes.Status400BadRequest, "Validation failed"),
            AuthenticationFailedException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed"
            ),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        var isUnexpected = statusCode == StatusCodes.Status500InternalServerError;
        if (isUnexpected)
        {
            GlobalExceptionHandlerHelpers.LogUnhandledException(logger, exception);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = isUnexpected ? null : exception.Message,
                },
            }
        );
    }
}
