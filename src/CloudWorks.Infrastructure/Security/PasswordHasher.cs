using Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(new object(), passwordHash, password)
            is not PasswordVerificationResult.Failed;
}
