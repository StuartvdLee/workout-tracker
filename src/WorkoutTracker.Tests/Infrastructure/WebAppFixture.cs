using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace WorkoutTracker.Tests.Infrastructure;

public class WebAppFixture : WebApplicationFactory<Program>
{
    private IHost? _host;

    public string BaseUrl { get; }

    /// <summary>
    /// Mock workout types served by the test server at /api/workout-types.
    /// </summary>
    public static readonly IReadOnlyList<MockWorkoutType> WorkoutTypes =
    [
        new("d0f1a2b3-c4d5-6e7f-8a9b-0c1d2e3f4a5b", "Legs"),
        new("a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d", "Pull"),
        new("b2c3d4e5-f6a7-8b9c-0d1e-2f3a4b5c6d7e", "Push"),
    ];

    /// <summary>
    /// Mock muscles served by the test server at /api/muscles.
    /// Pre-sorted alphabetically by name to match DbContext seed data.
    /// </summary>
    public static readonly IReadOnlyList<MockMuscle> Muscles =
    [
        new("a1000000-0000-0000-0000-000000000001", "Back"),
        new("a1000000-0000-0000-0000-000000000002", "Biceps"),
        new("a1000000-0000-0000-0000-000000000003", "Calves"),
        new("a1000000-0000-0000-0000-000000000004", "Chest"),
        new("a1000000-0000-0000-0000-000000000005", "Core"),
        new("a1000000-0000-0000-0000-000000000006", "Forearms"),
        new("a1000000-0000-0000-0000-000000000007", "Glutes"),
        new("a1000000-0000-0000-0000-000000000008", "Hamstrings"),
        new("a1000000-0000-0000-0000-000000000009", "Quads"),
        new("a1000000-0000-0000-0000-00000000000a", "Shoulders"),
        new("a1000000-0000-0000-0000-00000000000b", "Triceps"),
    ];

    private static readonly Lock _exercisesLock = new();
    private static readonly List<MockExercise> _exercises = [];

    public static void ResetExercises()
    {
        lock (_exercisesLock)
        {
            _exercises.Clear();
        }
    }

    public WebAppFixture()
    {
        var port = Random.Shared.Next(5100, 5999);
        BaseUrl = $"http://localhost:{port}";

        // Force creation of the server
        _ = Server;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = base.CreateHost(builder);

        // Resolve the Web project source directory for static files
        var webProjectDir = FindWebProjectDir();

        // Start a real Kestrel host so Playwright can connect via HTTP
        var webAppBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = webProjectDir,
            WebRootPath = Path.Combine(webProjectDir, "wwwroot"),
        });
        webAppBuilder.WebHost.UseUrls(BaseUrl);
        webAppBuilder.Environment.EnvironmentName = "Development";

        var app = webAppBuilder.Build();

        // Mock API endpoint for workout types
        app.MapGet("/api/workout-types", () => Results.Ok(WorkoutTypes));

        // Mock API endpoint for muscles
        app.MapGet("/api/muscles", () => Results.Ok(Muscles));

        // Mock API endpoint to list exercises
        app.MapGet("/api/exercises", () =>
        {
            List<MockExercise> snapshot;
            lock (_exercisesLock)
            {
                snapshot = [.. _exercises];
            }

            var result = snapshot
                .Select(e => new
                {
                    e.ExerciseId,
                    e.Name,
                    Muscles = e.MuscleIds
                        .Select(mid => Muscles.FirstOrDefault(m =>
                            string.Equals(m.MuscleId, mid, StringComparison.OrdinalIgnoreCase)))
                        .Where(m => m is not null)
                        .Select(m => new MockExerciseMuscle(m!.MuscleId, m.Name))
                        .ToList(),
                })
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Results.Ok(result);
        });

        // Mock API endpoint to create an exercise
        app.MapPost("/api/exercises", async (HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<ExerciseRequest>();
            var name = body?.Name?.Trim() ?? "";

            // Special case: simulate server error
            if (name == "__MOCK_SERVER_ERROR")
            {
                return Results.Json(
                    new { error = "An unexpected error occurred. Please try again." },
                    statusCode: 500);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "Exercise name is required." }, statusCode: 400);
            }

            if (name.Length > 150)
            {
                return Results.Json(
                    new { error = "Exercise name must be 150 characters or fewer." },
                    statusCode: 400);
            }

            lock (_exercisesLock)
            {
                if (_exercises.Any(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.Json(
                        new { error = "An exercise with this name already exists." },
                        statusCode: 400);
                }

                var muscleIds = body?.MuscleIds ?? [];
                if (muscleIds.Any(mid => !Muscles.Any(m =>
                    string.Equals(m.MuscleId, mid, StringComparison.OrdinalIgnoreCase))))
                {
                    return Results.Json(
                        new { error = "One or more selected muscles are invalid." },
                        statusCode: 400);
                }

                var exercise = new MockExercise(Guid.NewGuid().ToString(), name, [.. muscleIds]);
                _exercises.Add(exercise);

                var muscles = exercise.MuscleIds
                    .Select(mid => Muscles.FirstOrDefault(m =>
                        string.Equals(m.MuscleId, mid, StringComparison.OrdinalIgnoreCase)))
                    .Where(m => m is not null)
                    .Select(m => new MockExerciseMuscle(m!.MuscleId, m.Name))
                    .ToList();

                return Results.Json(
                    new { exercise.ExerciseId, exercise.Name, Muscles = muscles },
                    statusCode: 201);
            }
        });

        // Mock API endpoint to update an exercise
        app.MapPut("/api/exercises/{exerciseId}", async (string exerciseId, HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<ExerciseRequest>();
            var name = body?.Name?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "Exercise name is required." }, statusCode: 400);
            }

            if (name.Length > 150)
            {
                return Results.Json(
                    new { error = "Exercise name must be 150 characters or fewer." },
                    statusCode: 400);
            }

            lock (_exercisesLock)
            {
                var exercise = _exercises.FirstOrDefault(e =>
                    string.Equals(e.ExerciseId, exerciseId, StringComparison.OrdinalIgnoreCase));

                if (exercise is null)
                {
                    return Results.Json(new { error = "Exercise not found." }, statusCode: 404);
                }

                if (_exercises.Any(e =>
                    !string.Equals(e.ExerciseId, exerciseId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.Json(
                        new { error = "An exercise with this name already exists." },
                        statusCode: 400);
                }

                var muscleIds = body?.MuscleIds ?? [];
                if (muscleIds.Any(mid => !Muscles.Any(m =>
                    string.Equals(m.MuscleId, mid, StringComparison.OrdinalIgnoreCase))))
                {
                    return Results.Json(
                        new { error = "One or more selected muscles are invalid." },
                        statusCode: 400);
                }

                exercise.Name = name;
                exercise.MuscleIds = [.. muscleIds];

                var muscles = exercise.MuscleIds
                    .Select(mid => Muscles.FirstOrDefault(m =>
                        string.Equals(m.MuscleId, mid, StringComparison.OrdinalIgnoreCase)))
                    .Where(m => m is not null)
                    .Select(m => new MockExerciseMuscle(m!.MuscleId, m.Name))
                    .ToList();

                return Results.Ok(new { exercise.ExerciseId, exercise.Name, Muscles = muscles });
            }
        });

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        _host = app;
        _host.Start();

        return testHost;
    }

    private static string FindWebProjectDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "WorkoutTracker.Web");
            if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
            {
                return candidate;
            }

            var srcCandidate = Path.Combine(dir, "src", "WorkoutTracker.Web");
            if (Directory.Exists(Path.Combine(srcCandidate, "wwwroot")))
            {
                return srcCandidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find WorkoutTracker.Web project directory");
    }

    protected override void Dispose(bool disposing)
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        base.Dispose(disposing);
    }

    private sealed class ExerciseRequest
    {
        public string Name { get; set; } = "";
        public string[]? MuscleIds { get; set; }
    }
}

public record MockWorkoutType(string WorkoutTypeId, string Name);

public record MockMuscle(string MuscleId, string Name);

public record MockExerciseMuscle(string MuscleId, string Name);

public record MockExercise(string ExerciseId, string Name, List<string> MuscleIds)
{
    public string Name { get; set; } = Name;
    public List<string> MuscleIds { get; set; } = MuscleIds;
}
