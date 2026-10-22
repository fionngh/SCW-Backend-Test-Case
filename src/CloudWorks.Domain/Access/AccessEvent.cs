using Domain.Access.Enums;
using Domain.Common;
using Domain.Doors;
using Domain.Users;

namespace Domain.Access;

public sealed class AccessEvent
{
    private AccessEvent(
        Guid id,
        DateTimeOffset occurredAt,
        Door door,
        User? user,
        AccessOutcome outcome,
        AccessFailureReason? reason
    )
    {
        Id = id;
        OccurredAt = occurredAt;
        DoorId = door.Id;
        DoorName = door.Name;
        UserId = user?.Id;
        UserDisplayName = user?.DisplayName;
        Outcome = outcome;
        Reason = reason;
    }

    public Guid Id { get; }

    public DateTimeOffset OccurredAt { get; }

    public Guid DoorId { get; }

    public string DoorName { get; }

    public Guid? UserId { get; }

    public string? UserDisplayName { get; }

    public AccessOutcome Outcome { get; }

    public AccessFailureReason? Reason { get; }

    public static AccessEvent Record(
        Door door,
        User? user,
        AccessOutcome outcome,
        AccessFailureReason? reason,
        DateTimeOffset occurredAt
    )
    {
        ArgumentNullException.ThrowIfNull(door);
        Guard.RequireDefined(outcome, "Access outcome");

        if (!IsConsistent(outcome, reason))
        {
            throw new DomainRuleException(
                $"Outcome {outcome} cannot have reason {reason?.ToString() ?? "none"}."
            );
        }

        if ((user is null) != (reason == AccessFailureReason.UnknownUser))
        {
            throw new DomainRuleException(
                "An event has no user exactly when the reason is UnknownUser."
            );
        }

        return new(
            Guid.CreateVersion7(),
            Guard.RequireUtc(occurredAt, "Event time"),
            door,
            user,
            outcome,
            reason
        );
    }

    private static bool IsConsistent(AccessOutcome outcome, AccessFailureReason? reason) =>
        outcome switch
        {
            AccessOutcome.Denied => reason
                is AccessFailureReason.UnknownUser
                    or AccessFailureReason.UserInactive
                    or AccessFailureReason.DoorInactive
                    or AccessFailureReason.NoPermission,
            AccessOutcome.Failed => reason == AccessFailureReason.DeviceError,
            AccessOutcome.TimedOut => reason == AccessFailureReason.DeviceTimeout,
            _ => reason is null,
        };
}
