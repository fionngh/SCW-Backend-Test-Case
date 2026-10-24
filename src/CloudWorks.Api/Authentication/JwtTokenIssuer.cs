using System.Security.Claims;
using System.Text;
using Application.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.Authentication;

internal sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
    : ITokenIssuer
{
    public AccessToken Issue(Guid userId)
    {
        var jwt = options.Value;
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(jwt.LifetimeMinutes);

        var token = new JsonWebTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                ]),
                Issuer = jwt.Issuer,
                Audience = jwt.Audience,
                Expires = expiresAt.UtcDateTime,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    SecurityAlgorithms.HmacSha256
                ),
            }
        );

        return new AccessToken(token, expiresAt);
    }
}
