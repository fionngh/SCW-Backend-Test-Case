using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Users;

public sealed class User
{
    private User(Guid id, Guid officeId, string username, string displayName, string passwordHash)
    {
        Id = id;
        OfficeId = officeId;
        Username = username;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public Guid Id { get; }

    public Guid OfficeId { get; private set; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^\S+$", ErrorMessage = "Username must not contain spaces.")]
    public string Username { get; private set; }

    [Required]
    [StringLength(100)]
    public string DisplayName { get; private set; }

    public string PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public static User Create(
        Guid officeId,
        string? username,
        string? displayName,
        string? passwordHash
    ) =>
        new(
            Guid.CreateVersion7(),
            Guard.RequireId(officeId, "Office"),
            NormalizeUsername(username),
            Guard.RequireText<User>(displayName, nameof(DisplayName)),
            RequirePasswordHash(passwordHash)
        );

    public void UpdateProfile(string? username, string? displayName)
    {
        var normalizedUsername = NormalizeUsername(username);
        var validDisplayName = Guard.RequireText<User>(displayName, nameof(DisplayName));

        Username = normalizedUsername;
        DisplayName = validDisplayName;
    }

    public void ChangeOffice(Guid officeId) => OfficeId = Guard.RequireId(officeId, "Office");

    public void ChangePasswordHash(string? passwordHash) =>
        PasswordHash = RequirePasswordHash(passwordHash);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static string NormalizeUsername(string? username) =>
        Guard.RequireText<User>(username, nameof(Username)).ToLowerInvariant();

    private static string RequirePasswordHash(string? passwordHash) =>
        string.IsNullOrWhiteSpace(passwordHash)
            ? throw new DomainRuleException("Password hash is required.")
            : passwordHash;
}
