using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventoryManagement.Tests.IntegrationTests.Common;

internal sealed class PostgresTestDatabase : IAsyncDisposable
{
    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=admin123";
    private int _isCreated;

    public PostgresTestDatabase()
    {
        DatabaseName = $"inventory_test_{Guid.NewGuid():N}";

        var adminConnectionString = Environment.GetEnvironmentVariable(
                "INVENTORY_TEST_POSTGRES_ADMIN_CONNECTION")
            ?? DefaultAdminConnectionString;

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString);
        AdminConnectionString = builder.ConnectionString;
        builder.Database = DatabaseName;
        ConnectionString = builder.ConnectionString;
    }

    public string DatabaseName { get; }
    public string ConnectionString { get; }

    private string AdminConnectionString { get; }

    public async Task CreateAsync()
    {
        if (Interlocked.Exchange(ref _isCreated, 1) == 1)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"""CREATE DATABASE "{DatabaseName}";""";
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            _isCreated = 0;
            throw;
        }
    }

    public DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using var terminateCommand = connection.CreateCommand();
        terminateCommand.CommandText =
            """
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = @databaseName;
            """;
        terminateCommand.Parameters.AddWithValue("databaseName", DatabaseName);
        await terminateCommand.ExecuteNonQueryAsync();

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"""DROP DATABASE IF EXISTS "{DatabaseName}";""";
        await dropCommand.ExecuteNonQueryAsync();
    }
}
