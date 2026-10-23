using Domain.Common;
using Domain.Users;

namespace UnitTests.Domain;

public class UserTests
{
    private static readonly Guid OfficeId = Guid.CreateVersion7();

    private static User CreateUser() =>
        User.Create(OfficeId, "ada.lovelace", "Ada Lovelace", "hash");

    [Fact]
    public void Create_ValidArguments_CreatesActiveUserWithLowercaseUsername()
    {
        var user = User.Create(OfficeId, "  Ada.Lovelace ", " Ada Lovelace ", "hash");

        Assert.Equal("ada.lovelace", user.Username);
        Assert.Equal("Ada Lovelace", user.DisplayName);
        Assert.Equal(OfficeId, user.OfficeId);
        Assert.True(user.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("ada lovelace")]
    public void Create_InvalidUsername_Throws(string? username) =>
        Assert.Throws<DomainRuleException>(() =>
            User.Create(OfficeId, username, "Ada Lovelace", "hash")
        );

    [Fact]
    public void Create_UsernameLongerThanLimit_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            User.Create(OfficeId, new string('a', 51), "Ada Lovelace", "hash")
        );

    [Fact]
    public void Create_EmptyOfficeId_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            User.Create(Guid.Empty, "ada.lovelace", "Ada Lovelace", "hash")
        );

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Create_MissingPasswordHash_Throws(string? passwordHash) =>
        Assert.Throws<DomainRuleException>(() =>
            User.Create(OfficeId, "ada.lovelace", "Ada Lovelace", passwordHash)
        );

    [Fact]
    public void UpdateProfile_ValidValues_ChangesUsernameAndDisplayName()
    {
        var user = CreateUser();

        user.UpdateProfile("grace.hopper", "Grace Hopper");

        Assert.Equal("grace.hopper", user.Username);
        Assert.Equal("Grace Hopper", user.DisplayName);
    }

    [Fact]
    public void UpdateProfile_InvalidDisplayName_ChangesNothing()
    {
        var user = CreateUser();

        Assert.Throws<DomainRuleException>(() => user.UpdateProfile("grace.hopper", " "));

        Assert.Equal("ada.lovelace", user.Username);
        Assert.Equal("Ada Lovelace", user.DisplayName);
    }

    [Fact]
    public void ChangeOffice_ValidOffice_UpdatesOffice()
    {
        var user = CreateUser();
        var otherOfficeId = Guid.CreateVersion7();

        user.ChangeOffice(otherOfficeId);

        Assert.Equal(otherOfficeId, user.OfficeId);
    }

    [Fact]
    public void Deactivate_ActiveUser_MarksUserInactive()
    {
        var user = CreateUser();

        user.Deactivate();

        Assert.False(user.IsActive);
    }
}
