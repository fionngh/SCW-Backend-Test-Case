using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public sealed class ApiFactory(
    string connectionString,
    string? administratorUsername = "admin",
    bool includeDemoData = false,
    int loginAttemptsPerMinute = 100
) : WebApplicationFactory<Program>
{
    public const string AdministratorPassword = "Correct-horse-battery-1";

    public const string AllowedOrigin = "https://app.example.com";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(includeDemoData ? "Development" : "Production");

        // UseSetting is visible while Program.cs is still reading configuration.
        builder.UseSetting("ConnectionStrings:CloudWorks", connectionString);
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-at-least-32-bytes-long");
        builder.UseSetting("Seed:AdministratorUsername", administratorUsername);
        builder.UseSetting("Seed:AdministratorPassword", AdministratorPassword);
        builder.UseSetting("Seed:IncludeDemoData", includeDemoData.ToString());
        builder.UseSetting(
            "RateLimiting:LoginAttemptsPerMinute",
            loginAttemptsPerMinute.ToString(CultureInfo.InvariantCulture)
        );
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly)
        );
    }
}
