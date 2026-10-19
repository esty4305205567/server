using GameHub.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.Tests;

public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private const string EnvironmentVariable = "GAMEHUB_TEST_DB";

    private readonly string _adminConnectionString;
    private readonly string _databaseName = $"gamehub_test_{Guid.NewGuid():N}";

    public PostgresTestDatabase()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable(EnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Environment variable {EnvironmentVariable} is not set.");
    }

    public string ConnectionString =>
        new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName
        }.ConnectionString;

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await ExecuteAdminAsync($"CREATE DATABASE \"{_databaseName}\"");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)");
    }

    private async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}