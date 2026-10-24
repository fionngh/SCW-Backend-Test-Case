using Application.Authentication;
using Application.Users;
using Common.Exceptions;
using Domain.Users;

namespace UnitTests.Authentication;

public class AuthenticationServiceTests
{
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly User _user;
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _user = User.Create(
            Guid.CreateVersion7(),
            "ada.lovelace",
            "Ada Lovelace",
            _passwordHasher.Hash("correct-password")
        );
        _service = new AuthenticationService(
            new FakeUserRepository(_user),
            _passwordHasher,
            new FakeTokenIssuer()
        );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_IssuesATokenForTheUser()
    {
        var token = await _service.LoginAsync(
            new LoginRequest("ada.lovelace", "correct-password"),
            CancellationToken.None
        );

        Assert.Equal($"token-for-{_user.Id}", token.Value);
    }

    [Fact]
    public async Task LoginAsync_UsernameWithOtherCaseAndSpaces_StillFindsTheUser()
    {
        var token = await _service.LoginAsync(
            new LoginRequest("  Ada.Lovelace ", "correct-password"),
            CancellationToken.None
        );

        Assert.Equal($"token-for-{_user.Id}", token.Value);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_Throws() =>
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _service.LoginAsync(
                new LoginRequest("ada.lovelace", "wrong-password"),
                CancellationToken.None
            )
        );

    [Fact]
    public async Task LoginAsync_UnknownUser_ThrowsAfterCheckingAPasswordAnyway()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _service.LoginAsync(
                new LoginRequest("nobody", "correct-password"),
                CancellationToken.None
            )
        );

        Assert.Equal(1, _passwordHasher.VerifyCount);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedUser_Throws()
    {
        _user.Deactivate();

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _service.LoginAsync(
                new LoginRequest("ada.lovelace", "correct-password"),
                CancellationToken.None
            )
        );
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(user => user.Id == id));

        public Task<User?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken
        ) => Task.FromResult(users.SingleOrDefault(user => user.Username == username));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public int VerifyCount { get; private set; }

        public string Hash(string password) => $"hash:{password}";

        public bool Verify(string passwordHash, string password)
        {
            VerifyCount++;
            return passwordHash == Hash(password);
        }
    }

    private sealed class FakeTokenIssuer : ITokenIssuer
    {
        public AccessToken Issue(Guid userId) =>
            new($"token-for-{userId}", DateTimeOffset.UnixEpoch);
    }
}
