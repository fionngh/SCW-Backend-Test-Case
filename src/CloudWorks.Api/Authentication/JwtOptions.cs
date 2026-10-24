using System.ComponentModel.DataAnnotations;

namespace Api.Authentication;

public sealed class JwtOptions
{
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = "";

    [Required]
    public string Issuer { get; init; } = "";

    [Required]
    public string Audience { get; init; } = "";

    [Range(1, 1440)]
    public int LifetimeMinutes { get; init; } = 60;
}
