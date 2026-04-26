using System.Net;
using System.Net.Http.Json;
using Xunit;
using WorkoutTracker.Tests.Infrastructure;

namespace WorkoutTracker.Tests.Api;

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
                    new { ExerciseId = exerciseId, LoggedReps = 10, LoggedWeight = "100kg", Notes = "Felt strong" }
                }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionDetailDto>();
        Assert.NotNull(session);
        Assert.Equal(workoutId, session.PlannedWorkoutId);
        Assert.Single(session.LoggedExercises);
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

    private async Task<SessionDetailDto> CreateSessionAsync(Guid workoutId, Guid exerciseId)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/workouts/{workoutId}/sessions",
            new
            {
                LoggedExercises = new[]
                {
                    new { ExerciseId = exerciseId, LoggedReps = 8, LoggedWeight = (string?)null, Notes = (string?)null }
                }
            });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionDetailDto>())!;
    }

    private sealed record ExerciseDto(Guid ExerciseId, string Name, List<object> Muscles);
    private sealed record WorkoutDto(Guid PlannedWorkoutId, string Name, int ExerciseCount);
    private sealed record SessionDto(Guid WorkoutSessionId, Guid? PlannedWorkoutId, string? WorkoutName);
    private sealed record SessionDetailDto(Guid WorkoutSessionId, Guid PlannedWorkoutId, string WorkoutName, List<object> LoggedExercises);
    private sealed record ErrorDto(string Error);
}
