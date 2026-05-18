using System.Net;
using System.Net.Http.Json;
using Xunit;
using WorkoutTracker.UnitTests.Infrastructure;

namespace WorkoutTracker.UnitTests.Api;

[Collection("Api")]
public class SessionApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFixture _fixture;

    public SessionApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // --- GET /api/sessions ---

    [Fact]
    public async Task GetSessions_ReturnsEmptyList_WhenNoSessions()
    {
        var response = await _client.GetAsync("/api/sessions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sessions = await response.Content.ReadFromJsonAsync<List<SessionDto>>();
        Assert.NotNull(sessions);
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task GetSessions_ReturnsSessions_AfterCreation()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Push Day", "Push-up");
        await CreateSessionAsync(workoutId, exerciseId);

        var response = await _client.GetAsync("/api/sessions");
        var sessions = await response.Content.ReadFromJsonAsync<List<SessionDto>>();

        Assert.NotNull(sessions);
        Assert.Single(sessions);
    }

    // --- POST /api/workouts/{id}/sessions ---

    [Fact]
    public async Task CreateSession_Returns201_WhenWorkoutExists()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Legs", "Squat");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "100", Notes = "Felt strong", Effort = 7 }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Equal(workoutId, session.PlannedWorkoutId);
        Assert.Single(session.LoggedExercises);
    }

    [Fact]
    public async Task CreateSession_DoesNotRequireReps()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Legs", "Deadlift");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "80", Notes = (string?)null, Effort = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_StoresEffort_WhenProvided()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Effort Day", "Bench Press");

        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "60", Notes = (string?)null, Effort = 8 }
                }
            });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var response = await _client.GetAsync("/api/sessions");
        var sessions = await response.Content.ReadFromJsonAsync<List<SessionWithDetailDto>>();
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        Assert.Equal(8, sessions[0].LoggedExercises[0].Effort);
    }

    [Fact]
    public async Task CreateSession_StoresNullEffort_WhenNotProvided()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Light Day", "Curl");

        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var session = await postResponse.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Single(session.LoggedExercises);
        Assert.Null(session.LoggedExercises[0].Effort);
    }

    [Fact]
    public async Task CreateSession_StoresNullEffort_WhenEffortOmittedFromPayload()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Light Day Omit", "Plank");

        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var session = await postResponse.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Single(session.LoggedExercises);
        Assert.Null(session.LoggedExercises[0].Effort);
    }

    [Fact]
    public async Task CreateSession_Returns400_WhenEffortOutOfRange()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Bad Effort", "Row");

        var responseTooHigh = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = 11 }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, responseTooHigh.StatusCode);
        var errorHigh = await responseTooHigh.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Effort must be between 1 and 10.", errorHigh?.Error);

        var responseTooLow = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = 0 }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, responseTooLow.StatusCode);
        var errorLow = await responseTooLow.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Effort must be between 1 and 10.", errorLow?.Error);
    }

    [Fact]
    public async Task CreateSession_Returns400_WhenWeightTooLong()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Long Weight", "Shrug");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = new string('x', 101), Notes = (string?)null, Effort = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Logged weight must not exceed 100 characters.", error?.Error);
    }

    [Fact]
    public async Task GetSessions_DoesNotReturnLoggedReps()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("No Reps", "Plank");
        await CreateSessionAsync(workoutId, exerciseId);

        var response = await _client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("loggedReps", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSessions_ReturnsEffortAndWeightWithoutReps()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Full Session", "Pull-up");
        await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "75", Notes = (string?)null, Effort = 6 }
                }
            });

        var response = await _client.GetAsync("/api/sessions");
        var sessions = await response.Content.ReadFromJsonAsync<List<SessionWithDetailDto>>();
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        var ex = sessions[0].LoggedExercises[0];
        Assert.Equal("75", ex.LoggedWeight);
        Assert.Equal(6, ex.Effort);
    }

    [Fact]
    public async Task CreateSession_Returns201_WithEmptyLoggedExercises()
    {
        var (workoutId, _) = await CreateWorkoutWithExerciseAsync("Quick Session", "Jumping Jack");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new { LoggedExercises = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_Returns404_WhenWorkoutNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{Guid.NewGuid()}/sessions",
            new { LoggedExercises = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout not found.", error?.Error);
    }

    [Fact]
    public async Task CreateSession_StoresWorkoutName()
    {
        var (workoutId, _) = await CreateWorkoutWithExerciseAsync("Test Workout", "Box Jump");

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new { LoggedExercises = Array.Empty<object>() });

        var sessionsResponse = await _client.GetAsync("/api/sessions");
        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<List<SessionDto>>();

        Assert.NotNull(sessions);
        Assert.Equal("Test Workout", sessions[0].WorkoutName);
    }

    // --- POST /api/workouts/{id}/sessions — Sequence field ---

    [Fact]
    public async Task CreateSession_StoresSequence_WhenProvided()
    {
        var exerciseAResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Sequence Exercise A" });
        exerciseAResponse.EnsureSuccessStatusCode();
        var exerciseA = (await exerciseAResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseBResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Sequence Exercise B" });
        exerciseBResponse.EnsureSuccessStatusCode();
        var exerciseB = (await exerciseBResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var workoutResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Sequence Workout",
            Exercises = new[]
            {
                new { ExerciseId = exerciseA.ExerciseId },
                new { ExerciseId = exerciseB.ExerciseId },
            }
        });
        workoutResponse.EnsureSuccessStatusCode();
        var workout = (await workoutResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        // Submit with reversed (shuffled) sequence: B first (0), A second (1)
        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workout.PlannedWorkoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseB.ExerciseId, LoggedWeight = (string?)null, Effort = (int?)null, Sequence = (int?)0 },
                    new { ExerciseId = exerciseA.ExerciseId, LoggedWeight = (string?)null, Effort = (int?)null, Sequence = (int?)1 },
                }
            });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var session = await postResponse.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Equal(2, session.LoggedExercises.Count);

        var loggedB = session.LoggedExercises.First(le => le.ExerciseId == exerciseB.ExerciseId);
        var loggedA = session.LoggedExercises.First(le => le.ExerciseId == exerciseA.ExerciseId);
        Assert.Equal(0, loggedB.Sequence);
        Assert.Equal(1, loggedA.Sequence);
    }

    [Fact]
    public async Task CreateSession_AcceptsNullSequence_ForBackwardCompatibility()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Null Sequence Workout", "Null Sequence Exercise");

        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Effort = (int?)null, Sequence = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var session = await postResponse.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Single(session.LoggedExercises);
        Assert.Null(session.LoggedExercises[0].Sequence);
    }

    [Fact]
    public async Task CreateSession_StoresNullSequence_WhenSequenceOmittedFromPayload()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Omitted Sequence Workout", "Omitted Sequence Exercise");

        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var session = await postResponse.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Single(session.LoggedExercises);
        Assert.Null(session.LoggedExercises[0].Sequence);
    }

    [Fact]
    public async Task CreateSession_WithShuffledSequence_DoesNotModifyWorkoutTemplateOrder()
    {
        var exerciseAResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Template Order A" });
        exerciseAResponse.EnsureSuccessStatusCode();
        var exerciseA = (await exerciseAResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseBResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Template Order B" });
        exerciseBResponse.EnsureSuccessStatusCode();
        var exerciseB = (await exerciseBResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseCResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Template Order C" });
        exerciseCResponse.EnsureSuccessStatusCode();
        var exerciseC = (await exerciseCResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        // Create workout with A→B→C order
        var workoutResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Template Immutability Workout",
            Exercises = new[]
            {
                new { ExerciseId = exerciseA.ExerciseId },
                new { ExerciseId = exerciseB.ExerciseId },
                new { ExerciseId = exerciseC.ExerciseId },
            }
        });
        workoutResponse.EnsureSuccessStatusCode();
        var workout = (await workoutResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        // Submit session with shuffled order: C first (0), A second (1), B third (2)
        var postResponse = await _client.PostAsJsonAsync(
            $"/api/workouts/{workout.PlannedWorkoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseC.ExerciseId, LoggedWeight = (string?)null, Sequence = (int?)0 },
                    new { ExerciseId = exerciseA.ExerciseId, LoggedWeight = (string?)null, Sequence = (int?)1 },
                    new { ExerciseId = exerciseB.ExerciseId, LoggedWeight = (string?)null, Sequence = (int?)2 },
                }
            });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        // Verify that GET /api/workouts/{id} still returns exercises in A→B→C template order
        var templateResponse = await _client.GetAsync($"/api/workouts/{workout.PlannedWorkoutId}");
        Assert.Equal(HttpStatusCode.OK, templateResponse.StatusCode);

        var templateWorkout = await templateResponse.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        Assert.NotNull(templateWorkout);
        Assert.Equal(3, templateWorkout.Exercises.Count);
        Assert.Equal(exerciseA.ExerciseId, templateWorkout.Exercises[0].ExerciseId);
        Assert.Equal(exerciseB.ExerciseId, templateWorkout.Exercises[1].ExerciseId);
        Assert.Equal(exerciseC.ExerciseId, templateWorkout.Exercises[2].ExerciseId);
    }

    // --- GET /api/workouts/{id}/previous-performance ---

    [Fact]
    public async Task GetPreviousPerformance_ReturnsNoSession_WhenNoSessionsExist()
    {
        var (workoutId, _) = await CreateWorkoutWithExerciseAsync("Prev Perf Empty", "Lunge");

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.False(result.HasPreviousSession);
        Assert.Empty(result.Exercises);
    }

    [Fact]
    public async Task GetPreviousPerformance_ReturnsSequence_FromLastSession()
    {
        var exerciseAResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Seq Exercise A" });
        exerciseAResponse.EnsureSuccessStatusCode();
        var exerciseA = (await exerciseAResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseBResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Seq Exercise B" });
        exerciseBResponse.EnsureSuccessStatusCode();
        var exerciseB = (await exerciseBResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseCResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Seq Exercise C" });
        exerciseCResponse.EnsureSuccessStatusCode();
        var exerciseC = (await exerciseCResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var workoutResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Sequence Test Workout",
            Exercises = new[]
            {
                new { ExerciseId = exerciseA.ExerciseId },
                new { ExerciseId = exerciseB.ExerciseId },
                new { ExerciseId = exerciseC.ExerciseId },
            }
        });
        workoutResponse.EnsureSuccessStatusCode();
        var workout = (await workoutResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workout.PlannedWorkoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseA.ExerciseId, LoggedWeight = (string?)"80", Notes = (string?)null, Effort = (int?)7, Sequence = (int?)0 },
                    new { ExerciseId = exerciseB.ExerciseId, LoggedWeight = (string?)"60", Notes = (string?)null, Effort = (int?)5, Sequence = (int?)1 },
                    new { ExerciseId = exerciseC.ExerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = (int?)null, Sequence = (int?)2 },
                }
            });

        var response = await _client.GetAsync($"/api/workouts/{workout.PlannedWorkoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.True(result.HasPreviousSession);
        Assert.Equal(3, result.Exercises.Count);

        var exA = result.Exercises.First(e => e.ExerciseId == exerciseA.ExerciseId);
        Assert.Equal(0, exA.Sequence);
        Assert.Equal("80", exA.LoggedWeight);
        Assert.Equal(7, exA.Effort);

        var exB = result.Exercises.First(e => e.ExerciseId == exerciseB.ExerciseId);
        Assert.Equal(1, exB.Sequence);

        var exC = result.Exercises.First(e => e.ExerciseId == exerciseC.ExerciseId);
        Assert.Equal(2, exC.Sequence);
        Assert.Null(exC.LoggedWeight);
        Assert.Null(exC.Effort);
    }

    [Fact]
    public async Task GetPreviousPerformance_ReturnsWeightAndEffort_FromLastSession()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Prev Perf Data", "Bench Press");

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "80", Notes = (string?)null, Effort = 7 }
                }
            });

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.True(result.HasPreviousSession);
        Assert.Single(result.Exercises);
        Assert.Equal("80", result.Exercises[0].LoggedWeight);
        Assert.Equal(7, result.Exercises[0].Effort);
        Assert.Equal(exerciseId, result.Exercises[0].ExerciseId);
        Assert.Null(result.Exercises[0].Sequence);
    }

    [Fact]
    public async Task GetPreviousPerformance_Returns404_WhenWorkoutNotFound()
    {
        var response = await _client.GetAsync($"/api/workouts/{Guid.NewGuid()}/previous-performance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout not found.", error?.Error);
    }

    [Fact]
    public async Task GetPreviousPerformance_HandlesPartialData_WhenFieldsAreNull()
    {
        var exerciseAResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Exercise A Partial" });
        exerciseAResponse.EnsureSuccessStatusCode();
        var exerciseA = (await exerciseAResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseBResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Exercise B Partial" });
        exerciseBResponse.EnsureSuccessStatusCode();
        var exerciseB = (await exerciseBResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var workoutResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Partial Data Workout",
            Exercises = new[]
            {
                new { ExerciseId = exerciseA.ExerciseId },
                new { ExerciseId = exerciseB.ExerciseId },
            }
        });
        workoutResponse.EnsureSuccessStatusCode();
        var workout = (await workoutResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workout.PlannedWorkoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseA.ExerciseId, LoggedWeight = (string?)"60", Notes = (string?)null, Effort = (int?)null },
                    new { ExerciseId = exerciseB.ExerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = (int?)null },
                }
            });

        var response = await _client.GetAsync($"/api/workouts/{workout.PlannedWorkoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.True(result.HasPreviousSession);
        Assert.Equal(2, result.Exercises.Count);

        var exA = result.Exercises.First(e => e.ExerciseId == exerciseA.ExerciseId);
        Assert.Equal("60", exA.LoggedWeight);
        Assert.Null(exA.Effort);

        var exB = result.Exercises.First(e => e.ExerciseId == exerciseB.ExerciseId);
        Assert.Null(exB.LoggedWeight);
        Assert.Null(exB.Effort);
    }

    [Fact]
    public async Task GetPreviousPerformance_ReturnsMostRecentSession_WhenMultipleSessionsExist()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Multi Session Workout", "Squat");

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "60", Notes = (string?)null, Effort = 5 }
                }
            });

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = "80", Notes = (string?)null, Effort = 7 }
                }
            });

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.True(result.HasPreviousSession);
        Assert.Equal("80", result.Exercises[0].LoggedWeight);
    }

    [Fact]
    public async Task GetPreviousPerformance_ReturnsOnlyDataFromSpecifiedWorkout()
    {
        var exerciseResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Shared Exercise Isolation" });
        exerciseResponse.EnsureSuccessStatusCode();
        var exercise = (await exerciseResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var workoutAResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Isolation Workout A",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });
        workoutAResponse.EnsureSuccessStatusCode();
        var workoutA = (await workoutAResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        var workoutBResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Isolation Workout B",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });
        workoutBResponse.EnsureSuccessStatusCode();
        var workoutB = (await workoutBResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutB.PlannedWorkoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exercise.ExerciseId, LoggedWeight = "100", Notes = (string?)null, Effort = 9 }
                }
            });

        // Workout A has no sessions — assert its previous-performance shows no data
        var response = await _client.GetAsync($"/api/workouts/{workoutA.PlannedWorkoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.False(result.HasPreviousSession);
        Assert.Empty(result.Exercises);
    }

    // --- GET /api/sessions/latest ---

    [Fact]
    public async Task GetLatestSession_ReturnsHasSessionFalse_WhenNoSessions()
    {
        var response = await _client.GetAsync("/api/sessions/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LatestSessionDto>();
        Assert.NotNull(result);
        Assert.False(result.HasSession);
    }

    [Fact]
    public async Task GetLatestSession_ReturnsWorkoutNameAndDate_AfterOneSession()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Push Day", "Push-up");
        await CreateSessionAsync(workoutId, exerciseId);

        var response = await _client.GetAsync("/api/sessions/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LatestSessionDto>();
        Assert.NotNull(result);
        Assert.True(result.HasSession);
        Assert.Equal("Push Day", result.WorkoutName);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task GetLatestSession_ReturnsMostRecentSession_WhenMultipleSessionsExist()
    {
        var (workoutIdA, exerciseIdA) = await CreateWorkoutWithExerciseAsync("Push Day", "Push-up");
        var (workoutIdB, exerciseIdB) = await CreateWorkoutWithExerciseAsync("Pull Day", "Pull-up");

        await CreateSessionAsync(workoutIdA, exerciseIdA);
        await CreateSessionAsync(workoutIdB, exerciseIdB);

        var response = await _client.GetAsync("/api/sessions/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LatestSessionDto>();
        Assert.NotNull(result);
        Assert.True(result.HasSession);
        Assert.Equal("Pull Day", result.WorkoutName);
    }

    [Fact]
    public async Task GetLatestSession_ReturnsHasSessionFalse_AfterReset()
    {
        var response = await _client.GetAsync("/api/sessions/latest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.NotNull(doc);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("hasSession").GetBoolean());
        Assert.False(root.TryGetProperty("workoutName", out _));
        Assert.False(root.TryGetProperty("completedAt", out _));
    }

    // --- GET /api/sessions/{sessionId} ---

    [Fact]
    public async Task GetSessionDetail_Returns404_WhenSessionNotFound()
    {
        var response = await _client.GetAsync($"/api/sessions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("Session not found.", error.Error);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsSessionData_WithExercises()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Push Day", "Bench Press");
        var session = await CreateSessionAsync(workoutId, exerciseId, "80 KG", 7);

        var response = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Equal("Push Day", detail.WorkoutName);
        Assert.NotEqual(default, detail.CompletedAt);
        Assert.Single(detail.Exercises);
        Assert.Equal("Bench Press", detail.Exercises[0].ExerciseName);
        Assert.Equal("80 KG", detail.Exercises[0].LoggedWeight);
        Assert.Equal(7, detail.Exercises[0].Effort);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsEmptyExercises_WhenNoneLogged()
    {
        var (workoutId, _) = await CreateWorkoutWithExerciseAsync("Rest Day", "Stretch");
        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new { LoggedExercises = Array.Empty<object>() });
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<SessionDetailDto>())!;

        var detailResponse = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await detailResponse.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Empty(detail.Exercises);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsPreviousData_WhenPriorSessionExists()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Push Day", "Bench Press");
        await CreateSessionAsync(workoutId, exerciseId, "70 KG", 6);
        var secondSession = await CreateSessionAsync(workoutId, exerciseId, "75 KG", 7);

        var response = await _client.GetAsync($"/api/sessions/{secondSession.WorkoutSessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Single(detail.Exercises);
        Assert.Equal("75 KG", detail.Exercises[0].LoggedWeight);
        Assert.Equal(7, detail.Exercises[0].Effort);
        Assert.Equal("70 KG", detail.Exercises[0].PreviousWeight);
        Assert.Equal(6, detail.Exercises[0].PreviousEffort);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsNullPreviousData_WhenNoPriorSession()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Push Day", "Bench Press");
        var session = await CreateSessionAsync(workoutId, exerciseId, "80 KG", 7);

        var response = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Single(detail.Exercises);
        Assert.Null(detail.Exercises[0].PreviousWeight);
        Assert.Null(detail.Exercises[0].PreviousEffort);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsNullPreviousFields_WhenExerciseMissingFromPriorSession()
    {
        // Create both exercises first
        var exRespA = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Bench Press" });
        exRespA.EnsureSuccessStatusCode();
        var exerciseIdA = (await exRespA.Content.ReadFromJsonAsync<ExerciseDto>())!.ExerciseId;

        var exRespB = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Overhead Press" });
        exRespB.EnsureSuccessStatusCode();
        var exerciseIdB = (await exRespB.Content.ReadFromJsonAsync<ExerciseDto>())!.ExerciseId;

        // Create workout with both exercises
        var workoutResp = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Push Day",
            Exercises = new[]
            {
                new { ExerciseId = exerciseIdA },
                new { ExerciseId = exerciseIdB },
            }
        });
        workoutResp.EnsureSuccessStatusCode();
        var workoutId = (await workoutResp.Content.ReadFromJsonAsync<WorkoutDto>())!.PlannedWorkoutId;

        // First session: only log exercise A
        await CreateSessionAsync(workoutId, exerciseIdA, "80 KG", 7);

        // Second session: log both exercises A and B
        var secondResp = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseIdA, LoggedWeight = "85 KG", Notes = (string?)null, Effort = (int?)8 },
                    new { ExerciseId = exerciseIdB, LoggedWeight = "60 KG", Notes = (string?)null, Effort = (int?)6 },
                }
            });
        secondResp.EnsureSuccessStatusCode();
        var secondSession = (await secondResp.Content.ReadFromJsonAsync<SessionDetailDto>())!;

        var response = await _client.GetAsync($"/api/sessions/{secondSession.WorkoutSessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Equal(2, detail.Exercises.Count);

        var benchPress = detail.Exercises.First(e => e.ExerciseName == "Bench Press");
        Assert.Equal("80 KG", benchPress.PreviousWeight);
        Assert.Equal(7, benchPress.PreviousEffort);

        var overheadPress = detail.Exercises.First(e => e.ExerciseName == "Overhead Press");
        Assert.Null(overheadPress.PreviousWeight);
        Assert.Null(overheadPress.PreviousEffort);
    }

    // --- Helpers ---

    private async Task<(Guid WorkoutId, Guid ExerciseId)> CreateWorkoutWithExerciseAsync(
        string workoutName, string exerciseName)
    {
        var exerciseResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = exerciseName });
        exerciseResponse.EnsureSuccessStatusCode();
        var exercise = (await exerciseResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var workoutResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = workoutName,
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });
        workoutResponse.EnsureSuccessStatusCode();
        var workout = (await workoutResponse.Content.ReadFromJsonAsync<WorkoutDto>())!;

        return (workout.PlannedWorkoutId, exercise.ExerciseId);
    }

    private async Task<SessionDetailDto> CreateSessionAsync(Guid workoutId, Guid exerciseId) =>
        await CreateSessionAsync(workoutId, exerciseId, null, null);

    private async Task<SessionDetailDto> CreateSessionAsync(
        Guid workoutId, Guid exerciseId, string? loggedWeight, int? effort)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = loggedWeight, Notes = (string?)null, Effort = effort }
                }
            });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionDetailDto>())!;
    }

    private sealed record ExerciseDto(Guid ExerciseId, string Name, List<object> Muscles);
    private sealed record WorkoutDto(Guid PlannedWorkoutId, string Name, int ExerciseCount);
    private sealed record WorkoutDetailDto(Guid PlannedWorkoutId, string Name, int ExerciseCount, List<WorkoutExerciseDto> Exercises);
    private sealed record WorkoutExerciseDto(Guid ExerciseId, string Name, string? TargetReps, string? TargetWeight);
    private sealed record SessionDto(Guid WorkoutSessionId, Guid? PlannedWorkoutId, string? WorkoutName);
    private sealed record SessionDetailDto(Guid WorkoutSessionId, Guid PlannedWorkoutId, string WorkoutName, List<SessionLoggedExerciseDto> LoggedExercises);
    private sealed record SessionLoggedExerciseDto(Guid LoggedExerciseId, Guid ExerciseId, string? LoggedWeight, string? Notes, int? Effort, int? Sequence);
    private sealed record SessionWithDetailDto(Guid WorkoutSessionId, Guid? PlannedWorkoutId, string? WorkoutName, List<SessionLoggedExerciseDto> LoggedExercises);
    private sealed record PreviousPerformanceDto(bool HasPreviousSession, DateTime? CompletedAt, List<PreviousExerciseDataDto> Exercises);
    private sealed record PreviousExerciseDataDto(Guid ExerciseId, string? LoggedWeight, int? Effort, int? Sequence);
    private sealed record LatestSessionDto(bool HasSession, string? WorkoutName, DateTime? CompletedAt);
    private sealed record ErrorDto(string Error);
    private sealed record SessionDetailWithPreviousDto(
        Guid WorkoutSessionId,
        Guid? PlannedWorkoutId,
        string? WorkoutName,
        DateTime CompletedAt,
        List<SessionExerciseWithPreviousDto> Exercises);
    private sealed record SessionExerciseWithPreviousDto(
        Guid LoggedExerciseId,
        string ExerciseName,
        string? LoggedWeight,
        int? Effort,
        string? PreviousWeight,
        int? PreviousEffort);
}
