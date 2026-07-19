using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTracker.Infrastructure.Data;

namespace WorkoutTracker.E2ETests.Infrastructure;

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

    private static readonly MockMuscle[] _seedMuscles =
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

    private static readonly Lock _musclesLock = new();
    private static List<MockMuscle> _muscles = [.. _seedMuscles];

    /// <summary>
    /// Pre-sorted alphabetically by name. Thread-safe snapshot.
    /// </summary>
    public static IReadOnlyList<MockMuscle> Muscles
    {
        get { lock (_musclesLock) { return [.. _muscles]; } }
    }

    public static void ResetMuscles()
    {
        lock (_musclesLock)
        {
            _muscles = [.. _seedMuscles];
        }
    }

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
    /// Seeds a planned workout with a stub exercise so the home page dropdown is populated.
    /// </summary>
    public static void SeedWorkout(string name)
    {
        lock (_workoutsLock)
        {
            var stubExercise = new MockPlannedWorkoutExercise(Guid.NewGuid().ToString(), null, null);
            _workouts.Add(new MockPlannedWorkout(Guid.NewGuid().ToString(), name, [stubExercise]));
        }
    }

    /// <summary>
    /// Provides a stub DB configuration so the test host starts without requiring a real PostgreSQL
    /// instance. The E2E tests use the separate mock Kestrel server (see CreateHost), not this host.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:workout-tracker-db", "Host=localhost;Database=unused;Username=unused;Password=unused");
        builder.UseSetting("OTEL_SDK_DISABLED", "true");

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
                    .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
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
        app.MapGet("/api/muscles", () =>
        {
            lock (_musclesLock) { return Results.Ok(_muscles.OrderBy(m => m.Name, StringComparer.Ordinal).ToList()); }
        });

        // Mock API endpoint to add a muscle
        app.MapPost("/api/muscles", async (HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<MuscleRequest>();
            var name = body?.Name?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return Results.Json(new { error = "Muscle name is required." }, statusCode: 400);

            if (name.Length > 100)
                return Results.Json(new { error = "Muscle name must be 100 characters or fewer." }, statusCode: 400);

            lock (_musclesLock)
            {
                if (_muscles.Any(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
                    return Results.Json(new { error = "A muscle with this name already exists." }, statusCode: 400);

                var muscle = new MockMuscle(Guid.NewGuid().ToString(), name);
                _muscles.Add(muscle);
                _muscles = [.. _muscles.OrderBy(m => m.Name, StringComparer.Ordinal)];
                return Results.Json(new { muscleId = muscle.MuscleId, name = muscle.Name }, statusCode: 201);
            }
        });

        // Mock API endpoint to update (rename) a muscle
        app.MapPatch("/api/muscles/{muscleId}", async (string muscleId, HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<MuscleRequest>();
            var name = body?.Name?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return Results.Json(new { error = "Muscle name is required." }, statusCode: 400);

            if (name.Length > 100)
                return Results.Json(new { error = "Muscle name must be 100 characters or fewer." }, statusCode: 400);

            lock (_musclesLock)
            {
                var muscle = _muscles.FirstOrDefault(m =>
                    string.Equals(m.MuscleId, muscleId, StringComparison.OrdinalIgnoreCase));

                if (muscle is null)
                    return Results.Json(new { error = "Muscle not found." }, statusCode: 404);

                if (_muscles.Any(m =>
                    !string.Equals(m.MuscleId, muscleId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)))
                    return Results.Json(new { error = "A muscle with this name already exists." }, statusCode: 400);

                var updated = muscle with { Name = name };
                var idx = _muscles.IndexOf(muscle);
                _muscles[idx] = updated;
                _muscles = [.. _muscles.OrderBy(m => m.Name, StringComparer.Ordinal)];
                return Results.Ok(new { muscleId = updated.MuscleId, name = updated.Name });
            }
        });

        // Mock API endpoint to delete a muscle
        app.MapDelete("/api/muscles/{muscleId}", (string muscleId) =>
        {
            lock (_musclesLock)
            {
                var muscle = _muscles.FirstOrDefault(m =>
                    string.Equals(m.MuscleId, muscleId, StringComparison.OrdinalIgnoreCase));

                if (muscle is null)
                    return Results.Json(new { error = "Muscle not found." }, statusCode: 404);

                _muscles.Remove(muscle);
                return Results.NoContent();
            }
        });

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

        // Mock API endpoint for previous performance (returns no previous session by default)
        app.MapGet("/api/workouts/{workoutId}/previous-performance", (string workoutId) =>
        {
            MockPlannedWorkout? workout;
            lock (_workoutsLock)
            {
                workout = _workouts.FirstOrDefault(w =>
                    string.Equals(w.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase));
            }

            if (workout is null)
            {
                return Results.Json(new { error = "Workout not found." }, statusCode: 404);
            }

            List<MockWorkoutSession> sessionSnapshot;
            lock (_sessionsLock)
            {
                sessionSnapshot = [.. _sessions];
            }

            var targetExerciseIds = workout.Exercises
                .Select(e => e.ExerciseId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selectedExercises = SelectLatestUsableExercises(
                sessionSnapshot
                    .Where(s => string.Equals(s.PlannedWorkoutId, workoutId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(s => s.CompletedAt)
                    .ThenByDescending(s => Guid.Parse(s.SessionId)),
                targetExerciseIds);

            if (selectedExercises.Count == 0)
            {
                return Results.Ok(new
                {
                    hasPreviousSession = false,
                    completedAt = (DateTime?)null,
                    exercises = Array.Empty<object>(),
                });
            }

            return Results.Ok(new
            {
                hasPreviousSession = true,
                completedAt = (DateTime?)selectedExercises.Values.Max(e => e.CompletedAt),
                exercises = selectedExercises.Values
                    .Select(e => new { e.ExerciseId, e.LoggedWeight, e.Effort, e.Sequence, e.CompletedAt })
                    .ToArray(),
            });
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
                .Select(e => new MockLoggedExercise(Guid.NewGuid().ToString(), e.ExerciseId, e.LoggedReps, e.LoggedWeight, e.Notes, e.Effort, e.Sequence))
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
                loggedExercises)
            {
                OverallEffort = body?.OverallEffort,
            };

            lock (_sessionsLock)
            {
                _sessions.Add(session);
            }

            return Results.Json(
                new
                {
                    WorkoutSessionId = session.SessionId,
                    session.PlannedWorkoutId,
                    session.WorkoutName,
                    session.CompletedAt,
                    session.OverallEffort,
                },
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
                    .Select(e => new MockLoggedExercise(Guid.NewGuid().ToString(), e.ExerciseId, e.LoggedReps, e.LoggedWeight, e.Notes, e.Effort, e.Sequence))
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
                    WorkoutSessionId = s.SessionId,
                    s.PlannedWorkoutId,
                    s.WorkoutName,
                    s.CompletedAt,
                    OverallEffort = s.OverallEffort,
                    LoggedExercises = s.LoggedExercises
                        .Select(le =>
                        {
                            var ex = exerciseSnapshot.FirstOrDefault(e =>
                                string.Equals(e.ExerciseId, le.ExerciseId, StringComparison.OrdinalIgnoreCase));
                            return new
                            {
                                le.LoggedExerciseId,
                                le.ExerciseId,
                                ExerciseName = ex?.Name ?? "",
                                le.LoggedWeight,
                                le.Notes,
                                le.Effort,
                            };
                        })
                        .ToList(),
                })
                .ToList();

            return Results.Ok(result);
        });

        // Mock API endpoint to get session detail with previous session comparison
        app.MapGet("/api/sessions/{sessionId}", (string sessionId) =>
        {
            MockWorkoutSession? session;
            lock (_sessionsLock)
            {
                session = _sessions.FirstOrDefault(s =>
                    string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));
            }

            if (session is null)
            {
                return Results.Json(new { error = "Session not found." }, statusCode: 404);
            }

            List<MockExercise> exerciseSnapshot;
            lock (_exercisesLock)
            {
                exerciseSnapshot = [.. _exercises];
            }

            List<MockWorkoutSession> sessionSnapshot;
            lock (_sessionsLock)
            {
                sessionSnapshot = [.. _sessions];
            }

            var currentSessionId = Guid.Parse(session.SessionId);
            var priorSessions = sessionSnapshot
                .Where(s =>
                    string.Equals(s.PlannedWorkoutId, session.PlannedWorkoutId, StringComparison.OrdinalIgnoreCase) &&
                    Guid.TryParse(s.SessionId, out var candidateSessionId) &&
                    (
                        s.CompletedAt < session.CompletedAt ||
                        (s.CompletedAt == session.CompletedAt &&
                         candidateSessionId.CompareTo(currentSessionId) < 0)
                    ))
                .OrderByDescending(s => s.CompletedAt)
                .ThenByDescending(s => Guid.Parse(s.SessionId))
                .ToList();

            var targetExerciseIds = session.LoggedExercises
                .Select(e => e.ExerciseId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var previousExercises = SelectLatestUsableExercises(priorSessions, targetExerciseIds);
            var priorSession = priorSessions.FirstOrDefault();

            return Results.Ok(new
            {
                WorkoutSessionId = session.SessionId,
                session.PlannedWorkoutId,
                session.WorkoutName,
                session.CompletedAt,
                OverallEffort = session.OverallEffort,
                PreviousOverallEffort = priorSession?.OverallEffort,
                Exercises = session.LoggedExercises.Select(le =>
                {
                    var ex = exerciseSnapshot.FirstOrDefault(e =>
                        string.Equals(e.ExerciseId, le.ExerciseId, StringComparison.OrdinalIgnoreCase));
                    previousExercises.TryGetValue(le.ExerciseId, out var prior);
                    return new
                    {
                        le.LoggedExerciseId,
                        ExerciseName = ex?.Name ?? "",
                        le.LoggedWeight,
                        le.Effort,
                        PreviousWeight = prior?.LoggedWeight,
                        PreviousEffort = prior?.Effort,
                    };
                }).ToList(),
            });
        });

        // Mock API endpoint to edit completed session values
        app.MapPut("/api/sessions/{sessionId}", async (string sessionId, HttpRequest request) =>
        {
            var body = await request.ReadFromJsonAsync<SessionRequest>();

            MockWorkoutSession? session;
            lock (_sessionsLock)
            {
                session = _sessions.FirstOrDefault(s =>
                    string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));

                if (session is null)
                {
                    return Results.Json(new { error = "Session not found." }, statusCode: 404);
                }

                if (body?.OverallEffort is not null && (body.OverallEffort < 1 || body.OverallEffort > 10))
                {
                    return Results.Json(new { error = "Overall effort must be between 1 and 10." }, statusCode: 400);
                }

                var edits = body?.LoggedExercises ?? [];
                if (edits.Any(e => e.LoggedWeight is { Length: > 100 }))
                {
                    return Results.Json(new { error = "Logged weight must not exceed 100 characters." }, statusCode: 400);
                }

                if (edits.Any(e => e.Effort is not null && (e.Effort < 1 || e.Effort > 10)))
                {
                    return Results.Json(new { error = "Effort must be between 1 and 10." }, statusCode: 400);
                }

                var requestedIds = edits.Select(e => e.LoggedExerciseId).ToArray();
                if (requestedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != requestedIds.Length)
                {
                    return Results.Json(new { error = "Each logged exercise may only be included once." }, statusCode: 400);
                }

                if (requestedIds.Any(id => !session.LoggedExercises.Any(le =>
                    string.Equals(le.LoggedExerciseId, id, StringComparison.OrdinalIgnoreCase))))
                {
                    return Results.Json(new { error = "One or more logged exercises are not part of this session." }, statusCode: 400);
                }

                session.OverallEffort = body?.OverallEffort;
                session.LoggedExercises = session.LoggedExercises
                    .Select(le =>
                    {
                        var edit = edits.FirstOrDefault(e =>
                            string.Equals(e.LoggedExerciseId, le.LoggedExerciseId, StringComparison.OrdinalIgnoreCase));
                        return edit is null
                            ? le
                            : le with
                            {
                                LoggedWeight = string.IsNullOrWhiteSpace(edit.LoggedWeight) ? null : edit.LoggedWeight,
                                Effort = edit.Effort,
                            };
                    })
                    .ToList();
            }

            return BuildSessionDetailResponse(session);
        });

        // Mock API endpoint to delete a session
        app.MapDelete("/api/sessions/{sessionId}", (string sessionId) =>
        {
            lock (_sessionsLock)
            {
                var session = _sessions.FirstOrDefault(s =>
                    string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));

                if (session is null)
                {
                    return Results.Json(new { error = "Session not found." }, statusCode: 404);
                }

                _sessions.Remove(session);
                return Results.NoContent();
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

    private static Dictionary<string, LatestMockExerciseComparison> SelectLatestUsableExercises(
        IEnumerable<MockWorkoutSession> sessions,
        HashSet<string> targetExerciseIds)
    {
        var selected = new Dictionary<string, LatestMockExerciseComparison>(StringComparer.OrdinalIgnoreCase);

        foreach (var session in sessions)
        {
            foreach (var loggedExercise in session.LoggedExercises)
            {
                if (!targetExerciseIds.Contains(loggedExercise.ExerciseId) ||
                    selected.ContainsKey(loggedExercise.ExerciseId) ||
                    !HasUsableComparisonData(loggedExercise.LoggedWeight, loggedExercise.Effort))
                {
                    continue;
                }

                selected[loggedExercise.ExerciseId] = new LatestMockExerciseComparison(
                    loggedExercise.ExerciseId,
                    loggedExercise.LoggedWeight,
                    loggedExercise.Effort,
                    loggedExercise.Sequence,
                    session.CompletedAt);
            }

            if (selected.Count == targetExerciseIds.Count)
            {
                break;
            }
        }

        return selected;
    }

    private static bool HasUsableComparisonData(string? loggedWeight, int? effort) =>
        !string.IsNullOrWhiteSpace(loggedWeight) || effort is not null;

    private static IResult BuildSessionDetailResponse(MockWorkoutSession session)
    {
        List<MockExercise> exerciseSnapshot;
        lock (_exercisesLock)
        {
            exerciseSnapshot = [.. _exercises];
        }

        List<MockWorkoutSession> sessionSnapshot;
        lock (_sessionsLock)
        {
            sessionSnapshot = [.. _sessions];
        }

        var currentSessionId = Guid.Parse(session.SessionId);
        var priorSessions = sessionSnapshot
            .Where(s =>
                string.Equals(s.PlannedWorkoutId, session.PlannedWorkoutId, StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(s.SessionId, out var candidateSessionId) &&
                (
                    s.CompletedAt < session.CompletedAt ||
                    (s.CompletedAt == session.CompletedAt &&
                     candidateSessionId.CompareTo(currentSessionId) < 0)
                ))
            .OrderByDescending(s => s.CompletedAt)
            .ThenByDescending(s => Guid.Parse(s.SessionId))
            .ToList();

        var targetExerciseIds = session.LoggedExercises
            .Select(e => e.ExerciseId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previousExercises = SelectLatestUsableExercises(priorSessions, targetExerciseIds);
        var priorSession = priorSessions.FirstOrDefault();

        return Results.Ok(new
        {
            WorkoutSessionId = session.SessionId,
            session.PlannedWorkoutId,
            session.WorkoutName,
            session.CompletedAt,
            OverallEffort = session.OverallEffort,
            PreviousOverallEffort = priorSession?.OverallEffort,
            Exercises = session.LoggedExercises.Select(le =>
            {
                var ex = exerciseSnapshot.FirstOrDefault(e =>
                    string.Equals(e.ExerciseId, le.ExerciseId, StringComparison.OrdinalIgnoreCase));
                previousExercises.TryGetValue(le.ExerciseId, out var prior);
                return new
                {
                    le.LoggedExerciseId,
                    le.ExerciseId,
                    ExerciseName = ex?.Name ?? "",
                    le.LoggedWeight,
                    le.Effort,
                    PreviousWeight = prior?.LoggedWeight,
                    PreviousEffort = prior?.Effort,
                };
            }).ToList(),
        });
    }

    private sealed record LatestMockExerciseComparison(
        string ExerciseId,
        string? LoggedWeight,
        int? Effort,
        int? Sequence,
        DateTime CompletedAt);

    private sealed class ExerciseRequest
    {
        public string Name { get; set; } = "";
        public string[]? MuscleIds { get; set; }
    }

    private sealed class MuscleRequest
    {
        public string Name { get; set; } = "";
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
        public int? OverallEffort { get; set; }
        public SessionLoggedExerciseRequest[]? LoggedExercises { get; set; }
    }

    private sealed class SessionLoggedExerciseRequest
    {
        public string LoggedExerciseId { get; set; } = "";
        public string ExerciseId { get; set; } = "";
        public int? LoggedReps { get; set; }
        public string? LoggedWeight { get; set; }
        public string? Notes { get; set; }
        public int? Effort { get; set; }
        public int? Sequence { get; set; }
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

public record MockLoggedExercise(string LoggedExerciseId, string ExerciseId, int? LoggedReps, string? LoggedWeight, string? Notes, int? Effort, int? Sequence);

public record MockWorkoutSession(string SessionId, string PlannedWorkoutId, string WorkoutName, DateTime CompletedAt, List<MockLoggedExercise> LoggedExercises)
{
    public List<MockLoggedExercise> LoggedExercises { get; set; } = LoggedExercises;
    public int? OverallEffort { get; set; }
}
