using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTracker.Infrastructure.Data;
using Xunit;

namespace WorkoutTracker.UnitTests.Infrastructure;

/// <summary>
/// Shared WebApplicationFactory for the API project. Creates one server per test collection run
/// and migrates the database once. Each test class resets relevant data in InitializeAsync.
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly CommandCountingInterceptor _commandCounter = new();

    public int ExecutedCommandCount => _commandCounter.Count;

    public void ResetCommandCount() => _commandCounter.Reset();

    /// <summary>
    /// Connection string for the test database. Override via TEST_DB_CONNECTION env var in CI.
    /// </summary>
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=workout_tracker_test;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use a non-Development environment so the app doesn't auto-run migrations or seed data
        builder.UseEnvironment("Test");

        // Inject the test database connection string so AddNpgsqlDbContext picks it up
        builder.UseSetting("ConnectionStrings:workout-tracker-db", ConnectionString);

        // Suppress OTLP exporter connection errors in tests
        builder.UseSetting("OTEL_SDK_DISABLED", "true");

        // Remove the Aspire-registered DbContext and replace with a plain Npgsql registration
        // so tests don't need the Aspire service bus running
        builder.ConfigureServices(services =>
        {
            // Remove all existing WorkoutTrackerDbContext registrations, options, and any
            // IDbContextOptionsConfiguration<WorkoutTrackerDbContext> that Aspire may register
            // to apply EnableRetryOnFailure — which breaks test-initiated transactions.
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(WorkoutTrackerDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<WorkoutTrackerDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(WorkoutTrackerDbContext)))
                .ToList();

            foreach (var d in descriptors)
                services.Remove(d);

            services.AddDbContext<WorkoutTrackerDbContext>(options =>
                options
                    .UseNpgsql(ConnectionString)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(_commandCounter));
        });
    }

    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Reset() => Interlocked.Exchange(ref _count, 0);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkoutTrackerDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }

    /// <summary>
    /// Deletes all application data (preserving seeded muscle data) between test runs.
    /// </summary>
    public async Task ResetDataAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkoutTrackerDbContext>();
        // Delete in FK-safe order
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.logged_exercises");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.workout_sessions");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.planned_workout_exercises");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.planned_workouts");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.exercise_muscles");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.exercises");
        await db.Database.ExecuteSqlRawAsync("DELETE FROM workout_tracker.muscles");
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO workout_tracker.muscles (muscle_id, name)
            VALUES
                ('a1000000-0000-0000-0000-00000000000c', 'Adductors'),
                ('a1000000-0000-0000-0000-000000000001', 'Back'),
                ('a1000000-0000-0000-0000-000000000002', 'Biceps'),
                ('a1000000-0000-0000-0000-000000000003', 'Calves'),
                ('a1000000-0000-0000-0000-000000000004', 'Chest'),
                ('a1000000-0000-0000-0000-000000000005', 'Core'),
                ('a1000000-0000-0000-0000-000000000006', 'Forearms'),
                ('a1000000-0000-0000-0000-000000000007', 'Glutes'),
                ('a1000000-0000-0000-0000-000000000008', 'Hamstrings'),
                ('a1000000-0000-0000-0000-000000000009', 'Quads'),
                ('a1000000-0000-0000-0000-00000000000a', 'Shoulders'),
                ('a1000000-0000-0000-0000-00000000000b', 'Triceps')
        ");
    }
}
