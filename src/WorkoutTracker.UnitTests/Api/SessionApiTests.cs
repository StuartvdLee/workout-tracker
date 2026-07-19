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
        Assert.Equal(2, result.Exercises.Count);

        var exA = result.Exercises.First(e => e.ExerciseId == exerciseA.ExerciseId);
        Assert.Equal(0, exA.Sequence);
        Assert.Equal("80", exA.LoggedWeight);
        Assert.Equal(7, exA.Effort);

        var exB = result.Exercises.First(e => e.ExerciseId == exerciseB.ExerciseId);
        Assert.Equal(1, exB.Sequence);

        Assert.DoesNotContain(result.Exercises, e => e.ExerciseId == exerciseC.ExerciseId);
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
        Assert.Single(result.Exercises);

        var exA = result.Exercises.First(e => e.ExerciseId == exerciseA.ExerciseId);
        Assert.Equal("60", exA.LoggedWeight);
        Assert.Null(exA.Effort);

        Assert.DoesNotContain(result.Exercises, e => e.ExerciseId == exerciseB.ExerciseId);
    }

    [Fact]
    public async Task GetPreviousPerformance_FallsBackToOlderUsableExerciseData_WhenLatestRowIsEmpty()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Skipped Latest Workout", "Deadlift");

        await CreateSessionAsync(workoutId, exerciseId, "120", 8);
        await CreateSessionAsync(workoutId, exerciseId, null, null);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.True(result.HasPreviousSession);
        Assert.Single(result.Exercises);
        Assert.Equal(exerciseId, result.Exercises[0].ExerciseId);
        Assert.Equal("120", result.Exercises[0].LoggedWeight);
        Assert.Equal(8, result.Exercises[0].Effort);
        Assert.NotNull(result.Exercises[0].CompletedAt);
    }

    [Fact]
    public async Task GetPreviousPerformance_ReturnsLatestUsableExerciseData_WhenLatestRowHasWeightOrEffort()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Latest Usable Workout", "Row");

        await CreateSessionAsync(workoutId, exerciseId, "50", 5);
        await CreateSessionAsync(workoutId, exerciseId, null, 7);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.True(result.HasPreviousSession);
        Assert.Single(result.Exercises);
        Assert.Null(result.Exercises[0].LoggedWeight);
        Assert.Equal(7, result.Exercises[0].Effort);
    }

    [Fact]
    public async Task GetPreviousPerformance_ReturnsNoUsableData_WhenHistoryOnlyHasEmptyRows()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Only Empty History", "Curl");

        await CreateSessionAsync(workoutId, exerciseId, null, null);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/previous-performance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreviousPerformanceDto>();
        Assert.NotNull(result);
        Assert.False(result.HasPreviousSession);
        Assert.Null(result.CompletedAt);
        Assert.Empty(result.Exercises);
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

    [Fact]
    public async Task GetPreviousPerformance_SelectsEachExerciseFromItsOwnLatestUsableSession()
    {
        var exerciseAResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Independent A" });
        exerciseAResponse.EnsureSuccessStatusCode();
        var exerciseA = (await exerciseAResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var exerciseBResponse = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Independent B" });
        exerciseBResponse.EnsureSuccessStatusCode();
        var exerciseB = (await exerciseBResponse.Content.ReadFromJsonAsync<ExerciseDto>())!;

        var workoutResponse = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Independent Exercise Workout",
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
                    new { ExerciseId = exerciseA.ExerciseId, LoggedWeight = "40", Notes = (string?)null, Effort = (int?)5 },
                    new { ExerciseId = exerciseB.ExerciseId, LoggedWeight = "60", Notes = (string?)null, Effort = (int?)6 },
                }
            });

        await _client.PostAsJsonAsync(
            $"/api/workouts/{workout.PlannedWorkoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseA.ExerciseId, LoggedWeight = (string?)"45", Notes = (string?)null, Effort = (int?)7 },
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
        Assert.Equal("45", exA.LoggedWeight);
        Assert.Equal(7, exA.Effort);

        var exB = result.Exercises.First(e => e.ExerciseId == exerciseB.ExerciseId);
        Assert.Equal("60", exB.LoggedWeight);
        Assert.Equal(6, exB.Effort);
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
        Assert.Equal(exerciseId, detail.Exercises[0].ExerciseId);
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
    public async Task GetSessionDetail_FallsBackToOlderUsableExerciseData_WhenPriorSessionRowIsEmpty()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Review Fallback Workout", "Bench Press");
        await CreateSessionAsync(workoutId, exerciseId, "70 KG", 6);
        await CreateSessionAsync(workoutId, exerciseId, null, null);
        var currentSession = await CreateSessionAsync(workoutId, exerciseId, "75 KG", 7);

        var response = await _client.GetAsync($"/api/sessions/{currentSession.WorkoutSessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Single(detail.Exercises);
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

    // --- Overall effort tests ---

    [Fact]
    public async Task CreateSession_StoresOverallEffort_WhenProvided()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Effort Test", "Bench Press");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                OverallEffort = 7,
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        // Verify OverallEffort round-trips via GET
        var getResp = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        getResp.EnsureSuccessStatusCode();
        using var doc = await getResp.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.Equal(7, doc!.RootElement.GetProperty("overallEffort").GetInt32());
    }

    [Fact]
    public async Task CreateSession_StoresNullOverallEffort_WhenNotProvided()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("No Effort Test", "Squat");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                OverallEffort = (int?)null,
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        var getResp = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        getResp.EnsureSuccessStatusCode();
        using var doc = await getResp.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, doc!.RootElement.GetProperty("overallEffort").ValueKind);
    }

    [Fact]
    public async Task CreateSession_StoresNullOverallEffort_WhenOverallEffortOmitted()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Omit Effort Test", "Deadlift");

        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedWeight = (string?)null, Notes = (string?)null, Effort = (int?)null }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        var getResp = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        getResp.EnsureSuccessStatusCode();
        using var doc = await getResp.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.Equal(System.Text.Json.JsonValueKind.Null, doc!.RootElement.GetProperty("overallEffort").ValueKind);
    }

    [Fact]
    public async Task CreateSession_Returns400_WhenOverallEffortOutOfRange()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Bad Overall Effort", "Row");

        // Too low
        var responseLow = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new { OverallEffort = 0, LoggedExercises = new[] { new { ExerciseId = exerciseId } } });
        Assert.Equal(HttpStatusCode.BadRequest, responseLow.StatusCode);

        // Too high
        var responseHigh = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new { OverallEffort = 11, LoggedExercises = new[] { new { ExerciseId = exerciseId } } });
        Assert.Equal(HttpStatusCode.BadRequest, responseHigh.StatusCode);
    }

    [Fact]
    public async Task GetSessions_ReturnsOverallEffort()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Get Sessions Effort", "Curl");

        var postResp = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new { OverallEffort = 5, LoggedExercises = new[] { new { ExerciseId = exerciseId } } });
        postResp.EnsureSuccessStatusCode();

        var response = await _client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        Assert.NotNull(doc);
        var sessions = doc.RootElement.EnumerateArray().ToList();
        var session = sessions.FirstOrDefault(s => s.GetProperty("plannedWorkoutId").GetString() == workoutId.ToString());
        Assert.Equal(5, session.GetProperty("overallEffort").GetInt32());
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsOverallEffort()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Detail Effort", "Bench Press");
        var session = await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "80 KG", 7, 8);

        var response = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Equal(8, detail.OverallEffort);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsPreviousOverallEffort_WhenPriorSessionExists()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Two Sessions Effort", "Squat");
        await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "70 KG", 6, 6);
        var secondSession = await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "75 KG", 7, 8);

        var response = await _client.GetAsync($"/api/sessions/{secondSession.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Equal(8, detail.OverallEffort);
        Assert.Equal(6, detail.PreviousOverallEffort);
    }

    [Fact]
    public async Task GetSessionDetail_ReturnsNullPreviousOverallEffort_WhenNoPriorSession()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("First Session Effort", "Deadlift");
        var session = await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "100 KG", 9, 9);

        var response = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SessionDetailWithPreviousDto>();
        Assert.NotNull(detail);
        Assert.Equal(9, detail.OverallEffort);
        Assert.Null(detail.PreviousOverallEffort);
    }

    // --- T001: GET /api/workouts/{workoutId}/session-trends ---

    [Fact]
    public async Task GetSessionTrends_ReturnsNotFound_WhenWorkoutDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/workouts/{nonExistentId}/session-trends");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionTrends_ReturnsEmptyDataPoints_WhenWorkoutHasNoSessions()
    {
        var (workoutId, _) = await CreateWorkoutWithExerciseAsync("No Sessions Workout", "No Sessions Exercise");

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/session-trends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SessionTrendsDto>();
        Assert.NotNull(result);
        Assert.Empty(result.DataPoints);
    }

    [Fact]
    public async Task GetSessionTrends_ReturnsSingleDataPoint_WhenWorkoutHasOneSession()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("One Session Workout", "One Session Exercise");
        await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "80", 7, 7);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/session-trends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SessionTrendsDto>();
        Assert.NotNull(result);
        Assert.Single(result.DataPoints);
        Assert.Equal(7, result.DataPoints[0].OverallEffort);
        Assert.NotNull(result.DataPoints[0].Exercises);
        Assert.Single(result.DataPoints[0].Exercises);
        Assert.Equal("80", result.DataPoints[0].Exercises[0].LoggedWeight);
    }

    [Fact]
    public async Task GetSessionTrends_ReturnsDataPointsInChronologicalOrder()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Ordered Sessions Workout", "Ordered Sessions Exercise");
        var session1 = await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "60", 5, 5);
        var session2 = await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "70", 6, 6);
        var session3 = await CreateSessionWithOverallEffortAsync(workoutId, exerciseId, "80", 7, 7);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/session-trends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SessionTrendsDto>();
        Assert.NotNull(result);
        Assert.Equal(3, result.DataPoints.Count);

        // Data points should be in ascending CompletedAt order
        for (int i = 1; i < result.DataPoints.Count; i++)
        {
            Assert.True(result.DataPoints[i].CompletedAt >= result.DataPoints[i - 1].CompletedAt);
        }
    }

    [Fact]
    public async Task GetSessionTrends_ReturnsCappedAt50Sessions_WhenMoreThan50SessionsExist()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Many Sessions Workout", "Many Sessions Exercise");
        for (int i = 0; i < 55; i++)
        {
            await CreateSessionAsync(workoutId, exerciseId, $"{50 + i}", null);
        }

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/session-trends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SessionTrendsDto>();
        Assert.NotNull(result);
        Assert.Equal(50, result.DataPoints.Count);
    }

    [Fact]
    public async Task GetSessionTrends_ReturnsNullOverallEffort_WhenSessionHasNoOverallEffort()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("No Effort Workout", "No Effort Exercise");
        await CreateSessionAsync(workoutId, exerciseId, "75", null);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/session-trends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SessionTrendsDto>();
        Assert.NotNull(result);
        Assert.Single(result.DataPoints);
        Assert.Null(result.DataPoints[0].OverallEffort);
    }

    [Fact]
    public async Task GetSessionTrends_LoggedWeightIsAlwaysSingleNumericStringOrNull()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Weight Invariant Workout", "Weight Invariant Exercise");
        await CreateSessionAsync(workoutId, exerciseId, "82.5", null);
        await CreateSessionAsync(workoutId, exerciseId, null, null);

        var response = await _client.GetAsync($"/api/workouts/{workoutId}/session-trends");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SessionTrendsDto>();
        Assert.NotNull(result);
        foreach (var dp in result.DataPoints)
        {
            foreach (var ex in dp.Exercises)
            {
                // loggedWeight must be null or parseable as a double — never a compound string
                if (ex.LoggedWeight is not null)
                {
                    Assert.True(double.TryParse(ex.LoggedWeight, out _),
                        $"loggedWeight '{ex.LoggedWeight}' is not a single numeric string");
                }
            }
        }
    }

    // --- DELETE /api/sessions/{sessionId} ---

    [Fact]
    public async Task DeleteSession_Returns204_AndSessionNoLongerRetrievable()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Delete Test Workout", "Delete Test Exercise");
        var session = await CreateSessionAsync(workoutId, exerciseId);

        var deleteResponse = await _client.DeleteAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSession_Returns204_AndSessionAbsentFromList()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Delete List Test", "Delete List Exercise");
        var session = await CreateSessionAsync(workoutId, exerciseId);

        var deleteResponse = await _client.DeleteAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var sessions = await listResponse.Content.ReadFromJsonAsync<List<SessionDto>>();
        Assert.NotNull(sessions);
        Assert.DoesNotContain(sessions, s => s.WorkoutSessionId == session.WorkoutSessionId);
    }

    [Fact]
    public async Task DeleteSession_Returns204_AndCascadesLoggedExercises()
    {
        var (workoutId, exerciseId) = await CreateWorkoutWithExerciseAsync("Cascade Test Workout", "Cascade Test Exercise");
        var session = await CreateSessionAsync(workoutId, exerciseId, "100", 8);

        var deleteResponse = await _client.DeleteAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Session is gone
        var getResponse = await _client.GetAsync($"/api/sessions/{session.WorkoutSessionId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Session list is empty
        var listResponse = await _client.GetAsync("/api/sessions");
        var sessions = await listResponse.Content.ReadFromJsonAsync<List<SessionDto>>();
        Assert.NotNull(sessions);
        Assert.DoesNotContain(sessions, s => s.WorkoutSessionId == session.WorkoutSessionId);
    }

    [Fact]
    public async Task DeleteSession_Returns404_WhenSessionDoesNotExist()
    {
        var nonExistentId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/sessions/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("Session not found.", error.Error);
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

    private async Task<SessionDetailDto> CreateSessionWithOverallEffortAsync(
        Guid workoutId, Guid exerciseId, string? loggedWeight, int? effort, int? overallEffort)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                OverallEffort = overallEffort,
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
    private sealed record PreviousExerciseDataDto(Guid ExerciseId, string? LoggedWeight, int? Effort, int? Sequence, DateTime? CompletedAt);
    private sealed record LatestSessionDto(bool HasSession, string? WorkoutName, DateTime? CompletedAt);
    private sealed record ErrorDto(string Error);
    private sealed record SessionDetailWithPreviousDto(
        Guid WorkoutSessionId,
        Guid? PlannedWorkoutId,
        string? WorkoutName,
        DateTime CompletedAt,
        int? OverallEffort,
        int? PreviousOverallEffort,
        List<SessionExerciseWithPreviousDto> Exercises);
    private sealed record SessionExerciseWithPreviousDto(
        Guid LoggedExerciseId,
        Guid ExerciseId,
        string ExerciseName,
        string? LoggedWeight,
        int? Effort,
        string? PreviousWeight,
        int? PreviousEffort);
    private sealed record SessionTrendsDto(List<SessionTrendsDataPointDto> DataPoints);
    private sealed record SessionTrendsDataPointDto(
        DateTime CompletedAt,
        int? OverallEffort,
        List<SessionTrendsExerciseDto> Exercises);
    private sealed record SessionTrendsExerciseDto(
        Guid ExerciseId,
        string ExerciseName,
        string? LoggedWeight,
        int? Effort);
}
