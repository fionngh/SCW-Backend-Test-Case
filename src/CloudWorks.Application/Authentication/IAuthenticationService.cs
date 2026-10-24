namespace Application.Authentication;

public interface IAuthenticationService
{
    Task<AccessToken> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
