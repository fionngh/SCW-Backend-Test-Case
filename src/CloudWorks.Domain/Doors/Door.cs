using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Doors.Enums;

namespace Domain.Doors;

public sealed class Door
{
    private Door(Guid id, Guid officeId, string name, DoorAccessPolicy accessPolicy)
    {
        Id = id;
        OfficeId = officeId;
        Name = name;
        AccessPolicy = accessPolicy;
        IsActive = true;
    }

    public Guid Id { get; }

    public Guid OfficeId { get; }

    [Required]
    [StringLength(100)]
    public string Name { get; private set; }

    public DoorAccessPolicy AccessPolicy { get; private set; }

    public bool IsActive { get; private set; }

    public static Door Create(Guid officeId, string? name, DoorAccessPolicy accessPolicy) =>
        new(
            Guid.CreateVersion7(),
            Guard.RequireId(officeId, "Office"),
            Guard.RequireText<Door>(name, nameof(Name)),
            Guard.RequireDefined(accessPolicy, "Door access policy")
        );

    public void Rename(string? name) => Name = Guard.RequireText<Door>(name, nameof(Name));

    public void ChangeAccessPolicy(DoorAccessPolicy accessPolicy) =>
        AccessPolicy = Guard.RequireDefined(accessPolicy, "Door access policy");

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
