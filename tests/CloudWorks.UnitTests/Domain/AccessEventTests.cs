using Domain.Access;
using Domain.Access.Enums;
using Domain.Common;

namespace UnitTests.Domain;

public class AccessEventTests
{
    [Theory]
    [InlineData(AccessOutcome.Simulated)]
    [InlineData(AccessOutcome.Accepted)]
    [InlineData(AccessOutcome.Confirmed)]
    public void Record_SuccessfulOutcome_SnapshotsDoorAndUserNames(AccessOutcome outcome)
    {
        var user = TestData.CreateUser();
        var door = TestData.CreateDoor();

        var accessEvent = AccessEvent.Record(door, user, outcome, null, TestData.Now);

        Assert.Equal(door.Id, accessEvent.DoorId);
        Assert.Equal(door.Name, accessEvent.DoorName);
        Assert.Equal(user.Id, accessEvent.UserId);
        Assert.Equal(user.DisplayName, accessEvent.UserDisplayName);
        Assert.Equal(outcome, accessEvent.Outcome);
        Assert.Null(accessEvent.Reason);
        Assert.Equal(TestData.Now, accessEvent.OccurredAt);
    }

    [Fact]
    public void Record_RenamedDoorAfterwards_KeepsOriginalDoorName()
    {
        var door = TestData.CreateDoor();
        var accessEvent = AccessEvent.Record(
            door,
            TestData.CreateUser(),
            AccessOutcome.Confirmed,
            null,
            TestData.Now
        );

        door.Rename("Archive");

        Assert.Equal("Storage room", accessEvent.DoorName);
    }

    [Fact]
    public void Record_DeniedWithNoPermission_StoresReason()
    {
        var accessEvent = AccessEvent.Record(
            TestData.CreateDoor(),
            TestData.CreateUser(),
            AccessOutcome.Denied,
            AccessFailureReason.NoPermission,
            TestData.Now
        );

        Assert.Equal(AccessOutcome.Denied, accessEvent.Outcome);
        Assert.Equal(AccessFailureReason.NoPermission, accessEvent.Reason);
    }

    [Fact]
    public void Record_DeniedForUnknownUser_HasNoUser()
    {
        var accessEvent = AccessEvent.Record(
            TestData.CreateDoor(),
            null,
            AccessOutcome.Denied,
            AccessFailureReason.UnknownUser,
            TestData.Now
        );

        Assert.Null(accessEvent.UserId);
        Assert.Null(accessEvent.UserDisplayName);
    }

    [Fact]
    public void Record_FailedWithDeviceError_Succeeds()
    {
        var accessEvent = AccessEvent.Record(
            TestData.CreateDoor(),
            TestData.CreateUser(),
            AccessOutcome.Failed,
            AccessFailureReason.DeviceError,
            TestData.Now
        );

        Assert.Equal(AccessOutcome.Failed, accessEvent.Outcome);
    }

    [Fact]
    public void Record_TimedOutWithDeviceTimeout_Succeeds()
    {
        var accessEvent = AccessEvent.Record(
            TestData.CreateDoor(),
            TestData.CreateUser(),
            AccessOutcome.TimedOut,
            AccessFailureReason.DeviceTimeout,
            TestData.Now
        );

        Assert.Equal(AccessOutcome.TimedOut, accessEvent.Outcome);
    }

    [Theory]
    [InlineData(AccessOutcome.Denied, null)]
    [InlineData(AccessOutcome.Denied, AccessFailureReason.DeviceError)]
    [InlineData(AccessOutcome.Failed, null)]
    [InlineData(AccessOutcome.Failed, AccessFailureReason.NoPermission)]
    [InlineData(AccessOutcome.TimedOut, AccessFailureReason.DeviceError)]
    [InlineData(AccessOutcome.Confirmed, AccessFailureReason.NoPermission)]
    [InlineData(AccessOutcome.Simulated, AccessFailureReason.DeviceTimeout)]
    public void Record_InconsistentOutcomeAndReason_Throws(
        AccessOutcome outcome,
        AccessFailureReason? reason
    ) =>
        Assert.Throws<DomainRuleException>(() =>
            AccessEvent.Record(
                TestData.CreateDoor(),
                TestData.CreateUser(),
                outcome,
                reason,
                TestData.Now
            )
        );

    [Fact]
    public void Record_UnknownUserReasonWithUser_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            AccessEvent.Record(
                TestData.CreateDoor(),
                TestData.CreateUser(),
                AccessOutcome.Denied,
                AccessFailureReason.UnknownUser,
                TestData.Now
            )
        );

    [Fact]
    public void Record_SuccessWithoutUser_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            AccessEvent.Record(
                TestData.CreateDoor(),
                null,
                AccessOutcome.Confirmed,
                null,
                TestData.Now
            )
        );

    [Fact]
    public void Record_NonUtcTime_Throws() =>
        Assert.Throws<DomainRuleException>(() =>
            AccessEvent.Record(
                TestData.CreateDoor(),
                TestData.CreateUser(),
                AccessOutcome.Confirmed,
                null,
                new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.FromHours(2))
            )
        );
}
