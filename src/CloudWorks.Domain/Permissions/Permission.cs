using Domain.Common;
using Domain.Doors;
using Domain.Permissions.Enums;
using Domain.Users;

namespace Domain.Permissions;

public sealed class Permission
{
    private Permission(
        Guid id,
        Guid userId,
        PermissionType type,
        Guid? doorId,
        Guid grantedByUserId,
        DateTimeOffset grantedAt
    )
    {
        Id = id;
        UserId = userId;
        Type = type;
        DoorId = doorId;
        GrantedByUserId = grantedByUserId;
        GrantedAt = grantedAt;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public PermissionType Type { get; }

    public Guid? DoorId { get; }

    public Guid GrantedByUserId { get; }

    public DateTimeOffset GrantedAt { get; }

    public Guid? RevokedByUserId { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static Permission Create(
        User user,
        Guid grantedByUserId,
        PermissionType type,
        Door? door,
        DateTimeOffset grantedAt
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsActive)
        {
            throw new DomainRuleException("Permissions cannot be created for a deactivated user.");
        }

        Guard.RequireDefined(type, "Permission type");

        if (type == PermissionType.OpenDoor && door is null)
        {
            throw new DomainRuleException("An OpenDoor permission requires a door.");
        }

        if (type == PermissionType.Administer && door is not null)
        {
            throw new DomainRuleException(
                "An Administer permission applies to the whole system, not a door."
            );
        }

        if (door is { IsActive: false })
        {
            throw new DomainRuleException("Access cannot be granted on a deactivated door.");
        }

        return new(
            Guid.CreateVersion7(),
            user.Id,
            type,
            door?.Id,
            Guard.RequireId(grantedByUserId, "Granting user"),
            Guard.RequireUtc(grantedAt, "Granted time")
        );
    }

    public void Revoke(Guid revokedByUserId, DateTimeOffset revokedAt)
    {
        if (!IsActive)
        {
            throw new DomainRuleException("The permission has already been revoked.");
        }

        var revocationTime = Guard.RequireUtc(revokedAt, "Revocation time");

        if (revocationTime < GrantedAt)
        {
            throw new DomainRuleException("A permission cannot be revoked before it was granted.");
        }

        RevokedByUserId = Guard.RequireId(revokedByUserId, "Revoking user");
        RevokedAt = revocationTime;
    }
}
