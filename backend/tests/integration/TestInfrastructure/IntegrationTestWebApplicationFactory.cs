using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Backend.Tests.Integration.TestInfrastructure;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object InitLock = new();
    private static bool _initialized;
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = ResolveConnectionString();

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            EnsureDatabaseInitialized(services, connectionString);
        });
    }

    private static string ResolveConnectionString()
    {
        return Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
               ?? "Host=localhost;Port=5432;Database=workout_tracker_integration;Username=postgres;Password=postgres";
    }

    private static void EnsureDatabaseInitialized(IServiceCollection services, string connectionString)
    {
        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }

            EnsureDatabaseExists(connectionString);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerDbContext>();
            dbContext.Database.EnsureCreated();

            if (!dbContext.Users.Any(user => user.Id == DefaultUserId))
            {
                dbContext.Users.Add(new UserEntity
                {
                    Id = DefaultUserId,
                    Email = "integration-user@local.test",
                    DisplayName = "Integration User",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                dbContext.SaveChanges();
            }

            _initialized = true;
        }
    }

    private static void EnsureDatabaseExists(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = string.IsNullOrWhiteSpace(builder.Database) ? "workout_tracker_integration" : builder.Database;
        var maintenanceBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres"
        };

        using var maintenanceConnection = new NpgsqlConnection(maintenanceBuilder.ConnectionString);
        maintenanceConnection.Open();

        using var existsCommand = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @databaseName", maintenanceConnection);
        existsCommand.Parameters.AddWithValue("databaseName", databaseName);

        var exists = existsCommand.ExecuteScalar() is not null;
        if (exists)
        {
            return;
        }

        var safeDatabaseName = databaseName.Replace("\"", "\"\"");
        using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{safeDatabaseName}\"", maintenanceConnection);
        createCommand.ExecuteNonQuery();
    }
}
