namespace Infrastructure.Seeding;

public sealed class SeedOptions
{
    public string? AdministratorUsername { get; init; }

    public string? AdministratorPassword { get; init; }

    public bool IncludeDemoData { get; init; }
}
