using System.ComponentModel.DataAnnotations;
using Common.Guards;

namespace Domain.Offices;

public sealed class Office
{
    private Office(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }

    [Required]
    [StringLength(100)]
    public string Name { get; private set; }

    public static Office Create(string? name) =>
        new(Guid.CreateVersion7(), Guard.RequireText<Office>(name, nameof(Name)));

    public void Rename(string? name) => Name = Guard.RequireText<Office>(name, nameof(Name));
}
