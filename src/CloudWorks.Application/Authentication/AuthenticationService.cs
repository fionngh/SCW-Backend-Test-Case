using Application.Users;
using Common.Exceptions;

namespace Application.Authentication;

public sealed class AuthenticationService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer
) : IAuthenticationService
{
    // Verified when the username is unknown, so a missing user takes as long to reject as a wrong password.
    private readonly string _unknownUserPasswordHash = passwordHasher.Hash(
        Guid.NewGuid().ToString()
    );

    public async Task<AccessToken> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByUsernameAsync(
            request.Username.Trim().ToLowerInvariant(),
            cancellationToken
        );
        var passwordMatches = passwordHasher.Verify(
            user?.PasswordHash ?? _unknownUserPasswordHash,
            request.Password
        );

        if (user is null || !user.IsActive || !passwordMatches)
        {
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        return tokenIssuer.Issue(user.Id);
    }
}
