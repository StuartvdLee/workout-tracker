using System.Net;
using System.Net.Http.Json;
using Xunit;
using WorkoutTracker.UnitTests.Infrastructure;

namespace WorkoutTracker.UnitTests.Api;

[Collection("Api")]
public class ExerciseApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFixture _fixture;

    // Seeded muscle IDs from DbContext
    private static readonly Guid BackMuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000001");
    private static readonly Guid BicepsMuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000002");

    public ExerciseApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // --- GET /api/exercises ---

    [Fact]
    public async Task GetExercises_ReturnsEmptyList_WhenNoExercises()
    {
        var response = await _client.GetAsync("/api/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();
        Assert.NotNull(exercises);
        Assert.Empty(exercises);
    }

    [Fact]
    public async Task GetExercises_ReturnsList_AfterCreation()
    {
        await CreateExerciseAsync("Squat");
        await CreateExerciseAsync("Bench Press");

        var response = await _client.GetAsync("/api/exercises");
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();

        Assert.NotNull(exercises);
        Assert.Equal(2, exercises.Count);
    }

    [Fact]
    public async Task GetExercises_ReturnsExercisesInAlphabeticalOrder()
    {
        await CreateExerciseAsync("Squat");
        await CreateExerciseAsync("Bench Press");
        await CreateExerciseAsync("Deadlift");

        var response = await _client.GetAsync("/api/exercises");
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();

        Assert.NotNull(exercises);
        var names = exercises.Select(e => e.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    // --- POST /api/exercises ---

    [Fact]
    public async Task CreateExercise_Returns201_WhenValidName()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Lat Pulldown" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var exercise = await response.Content.ReadFromJsonAsync<ExerciseDto>();
        Assert.NotNull(exercise);
        Assert.Equal("Lat Pulldown", exercise.Name);
    }

    [Fact]
    public async Task CreateExercise_Returns201_WithMuscles()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new
        {
            Name = "Pull-up",
            MuscleIds = new[] { BackMuscleId, BicepsMuscleId }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var exercise = await response.Content.ReadFromJsonAsync<ExerciseDto>();
        Assert.NotNull(exercise);
        Assert.Equal(2, exercise.Muscles.Count);
    }

    [Fact]
    public async Task CreateExercise_Returns400_WhenNameIsEmpty()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Exercise name is required.", error?.Error);
    }

    [Fact]
    public async Task CreateExercise_Returns400_WhenNameIsWhitespace()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Exercise name is required.", error?.Error);
    }

    [Fact]
    public async Task CreateExercise_Returns400_WhenNameExceeds150Characters()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new
        {
            Name = new string('A', 151)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Exercise name must be 150 characters or fewer.", error?.Error);
    }

    [Fact]
    public async Task CreateExercise_Returns400_WhenDuplicateName()
    {
        await CreateExerciseAsync("Deadlift");

        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = "Deadlift" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("An exercise with this name already exists.", error?.Error);
    }

    [Fact]
    public async Task CreateExercise_Returns400_WhenDuplicateName_CaseInsensitive()
    {
        await CreateExerciseAsync("Deadlift");

        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = "DEADLIFT" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateExercise_Returns400_WhenInvalidMuscleId()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new
        {
            Name = "Mystery Exercise",
            MuscleIds = new[] { Guid.NewGuid() }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("One or more selected muscles are invalid.", error?.Error);
    }

    [Fact]
    public async Task CreateExercise_TrimmsWhitespacFromName()
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = "  Squat  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var exercise = await response.Content.ReadFromJsonAsync<ExerciseDto>();
        Assert.Equal("Squat", exercise?.Name);
    }

    // --- PUT /api/exercises/{id} ---

    [Fact]
    public async Task UpdateExercise_Returns200_WhenValidData()
    {
        var created = await CreateExerciseAsync("Old Name");

        var response = await _client.PutAsJsonAsync(
            $"/api/exercises/{created.ExerciseId}",
            new { Name = "New Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ExerciseDto>();
        Assert.Equal("New Name", updated?.Name);
    }

    [Fact]
    public async Task UpdateExercise_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateExerciseAsync("Bench Press");

        var response = await _client.PutAsJsonAsync(
            $"/api/exercises/{created.ExerciseId}",
            new { Name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExercise_Returns400_WhenNameExceeds150Characters()
    {
        var created = await CreateExerciseAsync("Short Name");

        var response = await _client.PutAsJsonAsync(
            $"/api/exercises/{created.ExerciseId}",
            new { Name = new string('X', 151) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExercise_Returns400_WhenDuplicateName()
    {
        var first = await CreateExerciseAsync("First Exercise");
        await CreateExerciseAsync("Second Exercise");

        var response = await _client.PutAsJsonAsync(
            $"/api/exercises/{first.ExerciseId}",
            new { Name = "Second Exercise" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExercise_Returns200_WhenKeepingOwnName()
    {
        var created = await CreateExerciseAsync("My Exercise");

        var response = await _client.PutAsJsonAsync(
            $"/api/exercises/{created.ExerciseId}",
            new { Name = "My Exercise" });

        // Updating with the same name is not a duplicate
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExercise_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/exercises/{Guid.NewGuid()}",
            new { Name = "Anything" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Exercise not found.", error?.Error);
    }

    // --- DELETE /api/exercises/{id} ---

    [Fact]
    public async Task DeleteExercise_Returns204_WhenExists()
    {
        var created = await CreateExerciseAsync("Leg Press");

        var response = await _client.DeleteAsync($"/api/exercises/{created.ExerciseId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExercise_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync($"/api/exercises/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Exercise not found.", error?.Error);
    }

    [Fact]
    public async Task DeleteExercise_RemovesExerciseFromList()
    {
        var created = await CreateExerciseAsync("Calf Raise");
        await _client.DeleteAsync($"/api/exercises/{created.ExerciseId}");

        var response = await _client.GetAsync("/api/exercises");
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseDto>>();

        Assert.NotNull(exercises);
        Assert.DoesNotContain(exercises, e => e.ExerciseId == created.ExerciseId);
    }

    // --- Helpers ---

    private async Task<ExerciseDto> CreateExerciseAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = name });
        response.EnsureSuccessStatusCode();
        var exercise = await response.Content.ReadFromJsonAsync<ExerciseDto>();
        return exercise!;
    }

    private sealed record ExerciseDto(
        Guid ExerciseId,
        string Name,
        List<MuscleDto> Muscles);

    private sealed record MuscleDto(Guid MuscleId, string Name);

    private sealed record ErrorDto(string Error);
}
