using Infrastructure.Database.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IntegrationTests;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string ConnectionStringVariable = "CLOUDWORKS_TEST_POSTGRES";

    private readonly NpgsqlConnectionStringBuilder _serverConnection = new(
        Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? throw new InvalidOperationException(
                $"Set the {ConnectionStringVariable} environment variable to a PostgreSQL connection string "
                    + "whose role can create and drop databases."
            )
    );

    private readonly string _databaseName = $"cloudworks_test_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        await ExecuteOnServerAsync($"CREATE DATABASE {_databaseName}");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await ExecuteOnServerAsync($"DROP DATABASE {_databaseName} WITH (FORCE)");
    }

    public string ConnectionString =>
        new NpgsqlConnectionStringBuilder(_serverConnection.ConnectionString)
        {
            Database = _databaseName,
        }.ConnectionString;

    public CloudWorksDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CloudWorksDbContext>().UseNpgsql(ConnectionString).Options);

    private async Task ExecuteOnServerAsync(string commandText)
    {
        await using var connection = new NpgsqlConnection(_serverConnection.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync();
    }
}
