namespace Application.Authentication;

public interface ITokenIssuer
{
    AccessToken Issue(Guid userId);
}
