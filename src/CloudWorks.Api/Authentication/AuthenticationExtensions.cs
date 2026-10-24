using System.Text;
using System.Threading.RateLimiting;
using Application.Authentication;
using Application.Common;
using Application.Users;
using Domain.Permissions.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.Authentication;

internal static class AuthenticationExtensions
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration("Jwt")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IAuthorizationHandler, AdministratorHandler>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (bearer, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;
                    bearer.MapInboundClaims = false;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.SigningKey)
                        ),
                        ClockSkew = TimeSpan.FromSeconds(30),
                    };
                    bearer.Events = new JwtBearerEvents { OnTokenValidated = RejectInactiveUser };
                }
            );

        // Secure by default: an endpoint is public only when it says [AllowAnonymous].
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(
                nameof(PermissionType.Administer),
                policy => policy.AddRequirements(new AdministratorRequirement())
            );

        return services;
    }

    public static IServiceCollection AddLoginRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var attemptsPerMinute = configuration.GetValue("RateLimiting:LoginAttemptsPerMinute", 5);

        return services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(
                LoginRateLimitPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = attemptsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                        }
                    )
            );
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context
                    .HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .TryWriteAsync(
                        new ProblemDetailsContext
                        {
                            HttpContext = context.HttpContext,
                            ProblemDetails = new ProblemDetails
                            {
                                Status = StatusCodes.Status429TooManyRequests,
                                Title = "Too many requests",
                                Detail = "Too many login attempts. Try again later.",
                            },
                        }
                    );
            };
        });
    }

    private static async Task RejectInactiveUser(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var user = Guid.TryParse(subject, out var userId)
            ? await users.FindByIdAsync(userId, context.HttpContext.RequestAborted)
            : null;

        if (user is not { IsActive: true })
        {
            context.Fail("The user does not exist or is deactivated.");
        }
    }
}
