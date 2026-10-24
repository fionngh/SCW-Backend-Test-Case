using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Errors;

public class ErrorHandlingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureTestServices(services =>
                services
                    .AddControllers()
                    .AddApplicationPart(typeof(ErrorThrowingController).Assembly)
            );
        })
        .CreateClient();

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

        var problem = await ReadProblemDetails(response, expectedStatus);
        Assert.Equal(expectedTitle, problem.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, problem.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Request_ThrowingUnexpectedException_HidesTheErrorDetails()
    {
        var response = await _client.GetAsync("/test-errors/unexpected");

        var problem = await ReadProblemDetails(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.GetProperty("title").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.DoesNotContain("secret connection detail", problem.GetRawText());
    }

    [Fact]
    public async Task Request_ForUnknownRoute_ReturnsProblemDetails()
    {
        var response = await _client.GetAsync("/does-not-exist");

        var problem = await ReadProblemDetails(response, HttpStatusCode.NotFound);
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
    }

    private static async Task<JsonElement> ReadProblemDetails(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus
    )
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        return document.RootElement.Clone();
    }
}
