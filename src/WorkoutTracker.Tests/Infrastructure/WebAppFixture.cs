using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTracker.Infrastructure.Data;

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
        new("a1000000-0000-0000-0000-00000000000c", "Adductors"),
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

    private static readonly Lock _workoutsLock = new();
    private static readonly List<MockPlannedWorkout> _workouts = [];
    private static readonly Lock _sessionsLock = new();
    private static readonly List<MockWorkoutSession> _sessions = [];

    public static void ResetExercises()
    {
        lock (_exercisesLock)
        {
            _exercises.Clear();
        }
    }

    public static void ResetWorkouts()
    {
        lock (_workoutsLock) { _workouts.Clear(); }
        lock (_sessionsLock) { _sessions.Clear(); }
    }

    /// <summary>
    /// Seeds Legs, Pull, and Push planned workouts for home page tests.
    /// Idempotent: existing entries with the same IDs are skipped, so the
    /// method is safe to call multiple times without a preceding ResetWorkouts().
    /// </summary>
    public static void SeedDefaultWorkouts()
    {
        var defaults = new[]
        {
            new MockPlannedWorkout("workout-legs-id", "Legs", []),
            new MockPlannedWorkout("workout-pull-id", "Pull", []),
            new MockPlannedWorkout("workout-push-id", "Push", []),
        };

        lock (_workoutsLock)
        {
            foreach (var workout in defaults)
            {
                if (!_workouts.Any(w => w.PlannedWorkoutId == workout.PlannedWorkoutId))
                    _workouts.Add(workout);
            }
        }
    }

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
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(WorkoutTrackerDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<WorkoutTrackerDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var d in descriptors)
                services.Remove(d);

            services.AddDbContext<WorkoutTrackerDbContext>(options =>
                options
                    .UseNpgsql(ConnectionString)
                    .UseSnakeCaseNamingConvention());
        });

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

        // Mock API endpoint to delete an exercise
        app.MapDelete("/api/exercises/{exerciseId}", (string exerciseId) =>
        {
            lock (_exercisesLock)
            {
                var exercise = _exercises.FirstOrDefault(e =>
                    string.Equals(e.ExerciseId, exerciseId, StringComparison.OrdinalIgnoreCase));

                if (exercise is null)
                {
                    return Results.Json(new { error = "Exercise not found." }, statusCode: 404);
                }

                _exercises.Remove(exercise);
                return Results.NoContent();
            }
        });

        // Mock API endpoint to list planned workouts
        app.MapGet("/api/workouts", () =>
        {
            List<MockPlannedWorkout> workoutSnapshot;
            lock (_workoutsLock)
            {
                workoutSnapshot = [.. _workouts];
            }

            List<MockExercise> exerciseSnapshot;
            lock (_exercisesLock)
            {
                exerciseSnapshot = [.. _exercises];
            }

            var result = workoutSnapshot
                .Select(w => new
                {
                    w.PlannedWorkoutId,
                    w.Name,
                    ExerciseCount = w.Exercises.Count,
                    Exercises = w.Exercises
                        .Select(we =>
                        {
                            var ex = exerciseSnapshot.FirstOrDefault(e =>
                                string.Equals(e.ExerciseId, we.ExerciseId, StringComparison.OrdinalIgnoreCase));
                            return new
                            {
                                we.ExerciseId,
                                Name = ex?.Name ?? "",
                                we.TargetReps,
                                we.TargetWeight,
                            };
                        })
                        .ToList(),
                })
                .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Results.Ok(result);
        });

        // Mock API endpoint to get a single planned workout
        app.MapGet("/api/workouts/{workoutId}", (string workoutId) =>
        {
            List<MockExercise> exerciseSnapshot;
            lock (_exercisesLock)
            {
                exerciseSnapshot = [.. _exercises];
            }

            lock (_workoutsLock)
            {
                var workout = _workouts.FirstOrDefault(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));

                if (workout is null)
                {
                    return Results.Json(new { error = "Workout not found." }, statusCode: 404);
                }

                var result = new
                {
                    workout.PlannedWorkoutId,
                    workout.Name,
                    ExerciseCount = workout.Exercises.Count,
                    Exercises = workout.Exercises
                        .Select(we =>
                        {
                            var ex = exerciseSnapshot.FirstOrDefault(e =>
                                string.Equals(e.ExerciseId, we.ExerciseId, StringComparison.OrdinalIgnoreCase));
                            return new
                            {
                                we.ExerciseId,
                                Name = ex?.Name ?? "",
                                we.TargetReps,
                                we.TargetWeight,
                            };
                        })
                        .ToList(),
                };

                return Results.Ok(result);
            }
        });

        // Mock API endpoint to create a planned workout
        app.MapPost("/api/workouts", async (HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<WorkoutRequest>();
            var name = body?.Name?.Trim() ?? "";

            if (name == "__MOCK_SERVER_ERROR")
            {
                return Results.Json(
                    new { error = "An unexpected error occurred. Please try again." },
                    statusCode: 500);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "Workout name is required." }, statusCode: 400);
            }

            if (name.Length > 150)
            {
                return Results.Json(
                    new { error = "Workout name must be 150 characters or fewer." },
                    statusCode: 400);
            }

            var exercises = body?.Exercises ?? [];
            if (exercises.Length == 0)
            {
                return Results.Json(
                    new { error = "At least one exercise is required." },
                    statusCode: 400);
            }

            lock (_workoutsLock)
            {
                if (_workouts.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.Json(
                        new { error = "A workout with this name already exists." },
                        statusCode: 400);
                }

                var workoutExercises = exercises
                    .Select(e => new MockPlannedWorkoutExercise(e.ExerciseId, e.TargetReps, e.TargetWeight))
                    .ToList();

                var workout = new MockPlannedWorkout(Guid.NewGuid().ToString(), name, workoutExercises);
                _workouts.Add(workout);

                return Results.Json(
                    new { workout.PlannedWorkoutId, workout.Name, ExerciseCount = workout.Exercises.Count },
                    statusCode: 201);
            }
        });

        // Mock API endpoint to update a planned workout
        app.MapPut("/api/workouts/{workoutId}", async (string workoutId, HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<WorkoutRequest>();
            var name = body?.Name?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Json(new { error = "Workout name is required." }, statusCode: 400);
            }

            if (name.Length > 150)
            {
                return Results.Json(
                    new { error = "Workout name must be 150 characters or fewer." },
                    statusCode: 400);
            }

            var exercises = body?.Exercises ?? [];
            if (exercises.Length == 0)
            {
                return Results.Json(
                    new { error = "At least one exercise is required." },
                    statusCode: 400);
            }

            lock (_workoutsLock)
            {
                var workout = _workouts.FirstOrDefault(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));

                if (workout is null)
                {
                    return Results.Json(new { error = "Workout not found." }, statusCode: 404);
                }

                if (_workouts.Any(w =>
                    !string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.Json(
                        new { error = "A workout with this name already exists." },
                        statusCode: 400);
                }

                workout.Name = name;
                workout.Exercises = exercises
                    .Select(e => new MockPlannedWorkoutExercise(e.ExerciseId, e.TargetReps, e.TargetWeight))
                    .ToList();

                return Results.Ok(new { workout.PlannedWorkoutId, workout.Name, ExerciseCount = workout.Exercises.Count });
            }
        });

        // Mock API endpoint to delete a planned workout
        app.MapDelete("/api/workouts/{workoutId}", (string workoutId) =>
        {
            lock (_workoutsLock)
            {
                var workout = _workouts.FirstOrDefault(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));

                if (workout is null)
                {
                    return Results.Json(new { error = "Workout not found." }, statusCode: 404);
                }

                _workouts.Remove(workout);
                return Results.NoContent();
            }
        });

        // Mock API endpoint to create a workout session
        app.MapPost("/api/workouts/{workoutId}/sessions", async (string workoutId, HttpRequest request) =>
        {
            lock (_workoutsLock)
            {
                var workout = _workouts.FirstOrDefault(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));

                if (workout is null)
                {
                    return Results.Json(new { error = "Workout not found." }, statusCode: 404);
                }
            }

            var body = await request.ReadFromJsonAsync<SessionRequest>();
            var loggedExercises = (body?.LoggedExercises ?? [])
                .Select(e => new MockLoggedExercise(e.ExerciseId, e.LoggedReps, e.LoggedWeight, e.Notes))
                .ToList();

            string workoutName;
            lock (_workoutsLock)
            {
                workoutName = _workouts.First(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase)).Name;
            }

            var session = new MockWorkoutSession(
                Guid.NewGuid().ToString(),
                workoutId,
                workoutName,
                DateTime.UtcNow,
                loggedExercises);

            lock (_sessionsLock)
            {
                _sessions.Add(session);
            }

            return Results.Json(
                new { session.SessionId, session.PlannedWorkoutId, session.WorkoutName, session.CompletedAt },
                statusCode: 201);
        });

        // Mock API endpoint to update a workout session
        app.MapPut("/api/workouts/{workoutId}/sessions/{sessionId}", async (string workoutId, string sessionId, HttpRequest request) =>
        {
            lock (_workoutsLock)
            {
                var workout = _workouts.FirstOrDefault(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));

                if (workout is null)
                {
                    return Results.Json(new { error = "Workout not found." }, statusCode: 404);
                }
            }

            var body = await request.ReadFromJsonAsync<SessionRequest>();

            lock (_sessionsLock)
            {
                var session = _sessions.FirstOrDefault(s =>
                    string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));

                if (session is null)
                {
                    return Results.Json(new { error = "Session not found." }, statusCode: 404);
                }

                session.LoggedExercises = (body?.LoggedExercises ?? [])
                    .Select(e => new MockLoggedExercise(e.ExerciseId, e.LoggedReps, e.LoggedWeight, e.Notes))
                    .ToList();

                return Results.Ok(new { session.SessionId, session.PlannedWorkoutId, session.WorkoutName, session.CompletedAt });
            }
        });

        // Mock API endpoint to list all sessions
        app.MapGet("/api/sessions", () =>
        {
            List<MockWorkoutSession> sessionSnapshot;
            lock (_sessionsLock)
            {
                sessionSnapshot = [.. _sessions];
            }

            List<MockExercise> exerciseSnapshot;
            lock (_exercisesLock)
            {
                exerciseSnapshot = [.. _exercises];
            }

            var result = sessionSnapshot
                .OrderByDescending(s => s.CompletedAt)
                .Select(s => new
                {
                    s.SessionId,
                    WorkoutId = s.PlannedWorkoutId,
                    s.WorkoutName,
                    s.CompletedAt,
                    Exercises = s.LoggedExercises
                        .Select(le =>
                        {
                            var ex = exerciseSnapshot.FirstOrDefault(e =>
                                string.Equals(e.ExerciseId, le.ExerciseId, StringComparison.OrdinalIgnoreCase));
                            return new
                            {
                                le.ExerciseId,
                                ExerciseName = ex?.Name ?? "",
                                le.LoggedReps,
                                le.LoggedWeight,
                                le.Notes,
                            };
                        })
                        .ToList(),
                })
                .ToList();

            return Results.Ok(result);
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

    private sealed class WorkoutRequest
    {
        public string Name { get; set; } = "";
        public WorkoutExerciseRequest[]? Exercises { get; set; }
    }

    private sealed class WorkoutExerciseRequest
    {
        public string ExerciseId { get; set; } = "";
        public string? TargetReps { get; set; }
        public string? TargetWeight { get; set; }
    }

    private sealed class SessionRequest
    {
        public SessionLoggedExerciseRequest[]? LoggedExercises { get; set; }
    }

    private sealed class SessionLoggedExerciseRequest
    {
        public string ExerciseId { get; set; } = "";
        public int? LoggedReps { get; set; }
        public string? LoggedWeight { get; set; }
        public string? Notes { get; set; }
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

public record MockPlannedWorkoutExercise(string ExerciseId, string? TargetReps, string? TargetWeight);

public record MockPlannedWorkout(string PlannedWorkoutId, string Name, List<MockPlannedWorkoutExercise> Exercises)
{
    public string Name { get; set; } = Name;
    public List<MockPlannedWorkoutExercise> Exercises { get; set; } = Exercises;
}

public record MockLoggedExercise(string ExerciseId, int? LoggedReps, string? LoggedWeight, string? Notes);

public record MockWorkoutSession(string SessionId, string PlannedWorkoutId, string WorkoutName, DateTime CompletedAt, List<MockLoggedExercise> LoggedExercises)
{
    public List<MockLoggedExercise> LoggedExercises { get; set; } = LoggedExercises;
}
