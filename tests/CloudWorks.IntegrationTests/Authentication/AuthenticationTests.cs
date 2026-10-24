using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Application.Authentication;
using Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace IntegrationTests.Authentication;

public sealed class AuthenticationTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private const string EmployeePassword = "Employee-password-1";

    private readonly PostgresDatabaseFixture _database;
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public AuthenticationTests(PostgresDatabaseFixture database)
    {
        _database = database;
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessTokenValidForAnHour()
    {
        var response = await Login("admin", ApiFactory.AdministratorPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        var lifetime = body.GetProperty("expiresAt").GetDateTimeOffset() - DateTimeOffset.UtcNow;
        Assert.InRange(lifetime, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var response = await Login("admin", "not-the-password");

        var problem = await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
        Assert.Equal("Invalid username or password.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_UnknownUser_GivesTheSameAnswerAsAWrongPassword()
    {
        var response = await Login("nobody", "not-the-password");

        var problem = await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
        Assert.Equal("Invalid username or password.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_DeactivatedUser_ReturnsUnauthorized()
    {
        var username = await CreateEmployee(isActive: false);

        var response = await Login(username, EmployeePassword);

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_MissingPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username = "admin" }
        );

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Request_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        var response = await _client.GetAsync("/test-auth/me");

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ForUnknownRouteWithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/does-not-exist");

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ForUnknownRouteWithToken_ReturnsNotFoundProblemDetails()
    {
        var token = await LoginForToken("admin", ApiFactory.AdministratorPassword);

        var response = await Get("/does-not-exist", token);

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithValidToken_IdentifiesTheCaller()
    {
        var token = await LoginForToken("admin", ApiFactory.AdministratorPassword);

        var response = await Get("/test-auth/me", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var context = _database.CreateContext();
        var administrator = await context.Users.SingleAsync(user => user.Username == "admin");
        Assert.Equal(administrator.Id, await response.Content.ReadFromJsonAsync<Guid>());
    }

    [Fact]
    public async Task Request_WithTokenSignedByAnotherKey_ReturnsUnauthorized()
    {
        var token = new JsonWebTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
                ]),
                Issuer = "cloudworks",
                Audience = "cloudworks-clients",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("a-different-signing-key-of-32-bytes!!")
                    ),
                    SecurityAlgorithms.HmacSha256
                ),
            }
        );

        var response = await Get("/test-auth/me", token);

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithTokenOfUserDeactivatedAfterLogin_ReturnsUnauthorized()
    {
        var username = await CreateEmployee();
        var token = await LoginForToken(username, EmployeePassword);
        await Deactivate(username);

        var response = await Get("/test-auth/me", token);

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdministratorEndpoint_AsAdministrator_ReturnsOk()
    {
        var token = await LoginForToken("admin", ApiFactory.AdministratorPassword);

        var response = await Get("/test-auth/administrator", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdministratorEndpoint_AsEmployee_ReturnsForbiddenProblemDetails()
    {
        var username = await CreateEmployee();
        var token = await LoginForToken(username, EmployeePassword);

        var response = await Get("/test-auth/administrator", token);

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_TooManyAttempts_ReturnsTooManyRequestsProblemDetails()
    {
        using var limitedFactory = new ApiFactory(
            _database.ConnectionString,
            loginAttemptsPerMinute: 3
        );
        using var client = limitedFactory.CreateClient();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new { username = "admin", password = "wrong" }
            );
        }
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username = "admin", password = "wrong" }
        );

        await ProblemDetailsReader.ReadAsync(response, HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [InlineData(ApiFactory.AllowedOrigin, true)]
    [InlineData("https://other.example.com", false)]
    public async Task Preflight_FromOrigin_IsAllowedOnlyForConfiguredOrigins(
        string origin,
        bool expectedAllowed
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            expectedAllowed,
            response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowed)
                && allowed.Contains(origin)
        );
    }

    private Task<HttpResponseMessage> Login(string username, string password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });

    private async Task<string> LoginForToken(string username, string password)
    {
        var response = await Login(username, password);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> Get(string url, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<string> CreateEmployee(bool isActive = true)
    {
        var username = $"user.{Guid.NewGuid():N}";
        var hasher = _factory.Services.GetRequiredService<IPasswordHasher>();
        await using var context = _database.CreateContext();
        var office = await context.Offices.FirstAsync();
        var user = User.Create(office.Id, username, "Test Employee", hasher.Hash(EmployeePassword));
        if (!isActive)
        {
            user.Deactivate();
        }
        context.Add(user);
        await context.SaveChangesAsync();
        return username;
    }

    private async Task Deactivate(string username)
    {
        await using var context = _database.CreateContext();
        var user = await context.Users.SingleAsync(user => user.Username == username);
        user.Deactivate();
        await context.SaveChangesAsync();
    }
}
