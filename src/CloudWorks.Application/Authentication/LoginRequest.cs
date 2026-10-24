using System.ComponentModel.DataAnnotations;

namespace Application.Authentication;

public sealed record LoginRequest([Required] string Username, [Required] string Password);
