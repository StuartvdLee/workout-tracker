using System.Net;
using System.Net.Http.Json;
using Xunit;
using WorkoutTracker.Tests.Infrastructure;

namespace WorkoutTracker.Tests.Api;

[Collection("Api")]
public class WorkoutApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFixture _fixture;

    public WorkoutApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // --- GET /api/workouts ---

    [Fact]
    public async Task GetWorkouts_ReturnsEmptyList_WhenNoWorkouts()
    {
        var response = await _client.GetAsync("/api/workouts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();
        Assert.NotNull(workouts);
        Assert.Empty(workouts);
    }

    [Fact]
    public async Task GetWorkouts_ReturnsList_AfterCreation()
    {
        var exercise = await CreateExerciseAsync("Push-up");
        await CreateWorkoutAsync("Morning Routine", exercise.ExerciseId);

        var response = await _client.GetAsync("/api/workouts");
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();

        Assert.NotNull(workouts);
        Assert.Single(workouts);
    }

    [Fact]
    public async Task GetWorkouts_ReturnsWorkoutsInAlphabeticalOrder()
    {
        var exercise = await CreateExerciseAsync("Squat");
        await CreateWorkoutAsync("Legs", exercise.ExerciseId);
        await CreateWorkoutAsync("Arms", exercise.ExerciseId);
        await CreateWorkoutAsync("Back", exercise.ExerciseId);

        var response = await _client.GetAsync("/api/workouts");
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();

        Assert.NotNull(workouts);
        var names = workouts.Select(w => w.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    [Fact]
    public async Task GetWorkouts_IncludesExerciseCount()
    {
        var ex1 = await CreateExerciseAsync("Squat");
        var ex2 = await CreateExerciseAsync("Lunge");
        await CreateWorkoutAsync("Legs", ex1.ExerciseId, ex2.ExerciseId);

        var response = await _client.GetAsync("/api/workouts");
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();

        Assert.NotNull(workouts);
        Assert.Equal(2, workouts[0].ExerciseCount);
    }

    // --- GET /api/workouts/{id} ---

    [Fact]
    public async Task GetWorkout_Returns200_WhenExists()
    {
        var exercise = await CreateExerciseAsync("Plank");
        var created = await CreateWorkoutAsync("Core", exercise.ExerciseId);

        var response = await _client.GetAsync($"/api/workouts/{created.PlannedWorkoutId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var workout = await response.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        Assert.NotNull(workout);
        Assert.Equal("Core", workout.Name);
    }

    [Fact]
    public async Task GetWorkout_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"/api/workouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout not found.", error?.Error);
    }

    // --- POST /api/workouts ---

    [Fact]
    public async Task CreateWorkout_Returns201_WhenValidData()
    {
        var exercise = await CreateExerciseAsync("Bench Press");

        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Chest Day",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var workout = await response.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        Assert.Equal("Chest Day", workout?.Name);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenNameIsEmpty()
    {
        var exercise = await CreateExerciseAsync("Squat");

        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout name is required.", error?.Error);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenNameIsWhitespace()
    {
        var exercise = await CreateExerciseAsync("Squat");

        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "   ",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout name is required.", error?.Error);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenNameExceeds150Characters()
    {
        var exercise = await CreateExerciseAsync("Squat");

        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = new string('A', 151),
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout name must be 150 characters or fewer.", error?.Error);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenNoExercises()
    {
        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Empty Workout",
            Exercises = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("At least one exercise is required.", error?.Error);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenDuplicateName()
    {
        var exercise = await CreateExerciseAsync("Row");
        await CreateWorkoutAsync("Back Day", exercise.ExerciseId);

        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Back Day",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("A workout with this name already exists.", error?.Error);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenDuplicateName_CaseInsensitive()
    {
        var exercise = await CreateExerciseAsync("Row");
        await CreateWorkoutAsync("Back Day", exercise.ExerciseId);

        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "BACK DAY",
            Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkout_Returns400_WhenInvalidExerciseId()
    {
        var response = await _client.PostAsJsonAsync("/api/workouts", new
        {
            Name = "Invalid Workout",
            Exercises = new[] { new { ExerciseId = Guid.NewGuid() } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("One or more selected exercises are invalid.", error?.Error);
    }

    // --- PUT /api/workouts/{id} ---

    [Fact]
    public async Task UpdateWorkout_Returns200_WhenValidData()
    {
        var exercise = await CreateExerciseAsync("Deadlift");
        var created = await CreateWorkoutAsync("Old Name", exercise.ExerciseId);

        var response = await _client.PutAsJsonAsync(
            $"/api/workouts/{created.PlannedWorkoutId}",
            new
            {
                Name = "New Name",
                Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WorkoutDetailDto>();
        Assert.Equal("New Name", updated?.Name);
    }

    [Fact]
    public async Task UpdateWorkout_Returns400_WhenNoExercises()
    {
        var exercise = await CreateExerciseAsync("Deadlift");
        var created = await CreateWorkoutAsync("Pull Day", exercise.ExerciseId);

        var response = await _client.PutAsJsonAsync(
            $"/api/workouts/{created.PlannedWorkoutId}",
            new
            {
                Name = "Pull Day",
                Exercises = Array.Empty<object>()
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("At least one exercise is required.", error?.Error);
    }

    [Fact]
    public async Task UpdateWorkout_Returns404_WhenNotFound()
    {
        var exercise = await CreateExerciseAsync("Hip Thrust");

        var response = await _client.PutAsJsonAsync(
            $"/api/workouts/{Guid.NewGuid()}",
            new
            {
                Name = "Any Name",
                Exercises = new[] { new { ExerciseId = exercise.ExerciseId } }
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout not found.", error?.Error);
    }

    // --- DELETE /api/workouts/{id} ---

    [Fact]
    public async Task DeleteWorkout_Returns204_WhenExists()
    {
        var exercise = await CreateExerciseAsync("Curl");
        var created = await CreateWorkoutAsync("Arms Day", exercise.ExerciseId);

        var response = await _client.DeleteAsync($"/api/workouts/{created.PlannedWorkoutId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkout_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync($"/api/workouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Workout not found.", error?.Error);
    }

    [Fact]
    public async Task DeleteWorkout_RemovesWorkoutFromList()
    {
        var exercise = await CreateExerciseAsync("Sit-up");
        var created = await CreateWorkoutAsync("Core Session", exercise.ExerciseId);
        await _client.DeleteAsync($"/api/workouts/{created.PlannedWorkoutId}");

        var response = await _client.GetAsync("/api/workouts");
        var workouts = await response.Content.ReadFromJsonAsync<List<WorkoutDto>>();

        Assert.NotNull(workouts);
        Assert.DoesNotContain(workouts, w => w.PlannedWorkoutId == created.PlannedWorkoutId);
    }

    // --- Helpers ---

    private async Task<ExerciseDto> CreateExerciseAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/exercises", new { Name = name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExerciseDto>())!;
    }

    private async Task<WorkoutDetailDto> CreateWorkoutAsync(string name, params Guid[] exerciseIds)
    {
        var exercises = exerciseIds.Select(id => new { ExerciseId = id }).ToArray();
        var response = await _client.PostAsJsonAsync("/api/workouts", new { Name = name, Exercises = exercises });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkoutDetailDto>())!;
    }

    private sealed record ExerciseDto(Guid ExerciseId, string Name, List<object> Muscles);
    private sealed record WorkoutDto(Guid PlannedWorkoutId, string Name, int ExerciseCount);
    private sealed record WorkoutDetailDto(Guid PlannedWorkoutId, string Name, int ExerciseCount, List<object> Exercises);
    private sealed record ErrorDto(string Error);
}
