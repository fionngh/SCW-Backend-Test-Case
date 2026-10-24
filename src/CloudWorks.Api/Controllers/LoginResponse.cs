namespace Api.Controllers;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
