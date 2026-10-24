using System.Net;
using System.Text.Json;

namespace IntegrationTests;

internal static class ProblemDetailsReader
{
    public static async Task<JsonElement> ReadAsync(
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
