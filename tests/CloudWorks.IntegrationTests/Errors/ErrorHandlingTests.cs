using System.Net;

namespace IntegrationTests.Errors;

public sealed class ErrorHandlingTests : IClassFixture<PostgresDatabaseFixture>, IDisposable
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ErrorHandlingTests(PostgresDatabaseFixture database)
    {
        _factory = new ApiFactory(database.ConnectionString);
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Theory]
    [InlineData("domain-rule", HttpStatusCode.BadRequest, "Validation failed", "Name is required.")]
    [InlineData("not-found", HttpStatusCode.NotFound, "Not found", "Door was not found.")]
    [InlineData("conflict", HttpStatusCode.Conflict, "Conflict", "Username is already taken.")]
    [InlineData(
        "forbidden",
        HttpStatusCode.Forbidden,
        "Forbidden",
        "Administrators cannot change their own permissions."
    )]
    public async Task Request_ThrowingKnownException_ReturnsMatchingProblemDetails(
        string kind,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedDetail
    )
    {
        var response = await _client.GetAsync($"/test-errors/{kind}");

        var problem = await ProblemDetailsReader.ReadAsync(response, expectedStatus);
        Assert.Equal(expectedTitle, problem.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, problem.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Request_ThrowingUnexpectedException_HidesTheErrorDetails()
    {
        var response = await _client.GetAsync("/test-errors/unexpected");

        var problem = await ProblemDetailsReader.ReadAsync(
            response,
            HttpStatusCode.InternalServerError
        );
        Assert.Equal("An unexpected error occurred", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.DoesNotContain("secret connection detail", problem.GetRawText());
    }
}
