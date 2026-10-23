using Domain.Common;
using Domain.Doors;
using Domain.Doors.Enums;

namespace UnitTests.Domain;

public class DoorTests
{
    private static readonly Guid OfficeId = Guid.CreateVersion7();

    [Fact]
    public void Create_ValidArguments_CreatesActiveDoor()
    {
        var door = Door.Create(OfficeId, "  Main entrance ", DoorAccessPolicy.OfficeMembers);

        Assert.Equal("Main entrance", door.Name);
        Assert.Equal(OfficeId, door.OfficeId);
        Assert.Equal(DoorAccessPolicy.OfficeMembers, door.AccessPolicy);
        Assert.True(door.IsActive);
    }

    [Fact]
    public void Create_EmptyOfficeId_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            Door.Create(Guid.Empty, "Main entrance", DoorAccessPolicy.OfficeMembers)
        );

    [Fact]
    public void Create_UndefinedAccessPolicy_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            Door.Create(OfficeId, "Main entrance", (DoorAccessPolicy)99)
        );

    [Fact]
    public void ChangeAccessPolicy_ValidPolicy_UpdatesPolicy()
    {
        var door = Door.Create(OfficeId, "Storage room", DoorAccessPolicy.OfficeMembers);

        door.ChangeAccessPolicy(DoorAccessPolicy.ExplicitPermissionOnly);

        Assert.Equal(DoorAccessPolicy.ExplicitPermissionOnly, door.AccessPolicy);
    }

    [Fact]
    public void Deactivate_ActiveDoor_MarksDoorInactive()
    {
        var door = Door.Create(OfficeId, "Main entrance", DoorAccessPolicy.OfficeMembers);

        door.Deactivate();

        Assert.False(door.IsActive);
    }
}
