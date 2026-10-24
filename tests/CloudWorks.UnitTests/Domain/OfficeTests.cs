using Common.Exceptions;
using Domain.Offices;

namespace UnitTests.Domain;

public class OfficeTests
{
    [Fact]
    public void Create_ValidName_TrimsNameAndAssignsId()
    {
        var office = Office.Create("  Dublin  ");

        Assert.Equal("Dublin", office.Name);
        Assert.NotEqual(Guid.Empty, office.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_MissingName_Throws(string? name) =>
        Assert.Throws<DomainRuleException>(() => Office.Create(name));

    [Fact]
    public void Rename_InvalidName_KeepsExistingName()
    {
        var office = Office.Create("Dublin");

        Assert.Throws<DomainRuleException>(() => office.Rename(" "));

        Assert.Equal("Dublin", office.Name);
    }
}
