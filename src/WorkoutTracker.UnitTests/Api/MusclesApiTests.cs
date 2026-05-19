using System.Net;
using System.Net.Http.Json;
using Xunit;
using WorkoutTracker.UnitTests.Infrastructure;

namespace WorkoutTracker.UnitTests.Api;

[Collection("Api")]
public class MusclesApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ApiFixture _fixture;

    public MusclesApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDataAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetMuscles_Returns200WithAllMuscles()
    {
        var response = await _client.GetAsync("/api/muscles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var muscles = await response.Content.ReadFromJsonAsync<List<MuscleDto>>();
        Assert.NotNull(muscles);
        Assert.Equal(12, muscles.Count);
    }

    [Fact]
    public async Task GetMuscles_ReturnsMusclesInAlphabeticalOrder()
    {
        var response = await _client.GetAsync("/api/muscles");
        var muscles = await response.Content.ReadFromJsonAsync<List<MuscleDto>>();

        Assert.NotNull(muscles);
        var names = muscles.Select(m => m.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    [Fact]
    public async Task GetMuscles_FirstMuscleIsAdductors()
    {
        var response = await _client.GetAsync("/api/muscles");
        var muscles = await response.Content.ReadFromJsonAsync<List<MuscleDto>>();

        Assert.NotNull(muscles);
        Assert.Equal("Adductors", muscles[0].Name);
    }

    [Fact]
    public async Task PostMuscle_Returns201_WithValidName()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = "Hip Flexors" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<MuscleDto>();
        Assert.NotNull(data);
        Assert.Equal("Hip Flexors", data.Name);
        Assert.NotEqual(Guid.Empty, data.MuscleId);
    }

    [Fact]
    public async Task PostMuscle_CreatedMuscleAppearsInGetMusclesSortedAlphabetically()
    {
        await _client.PostAsJsonAsync("/api/muscles", new { name = "Hip Flexors" });

        var response = await _client.GetAsync("/api/muscles");
        var muscles = await response.Content.ReadFromJsonAsync<List<MuscleDto>>();
        Assert.NotNull(muscles);
        var names = muscles.Select(m => m.Name).ToList();
        Assert.Contains("Hip Flexors", names);
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }

    [Fact]
    public async Task PostMuscle_NameIsTrimmedBeforePersistence()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = "  Hip Flexors  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<MuscleDto>();
        Assert.NotNull(data);
        Assert.Equal("Hip Flexors", data.Name);
    }

    [Fact]
    public async Task PostMuscle_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle name is required.", data?.Error);
    }

    [Fact]
    public async Task PostMuscle_WhitespaceName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle name is required.", data?.Error);
    }

    [Fact]
    public async Task PostMuscle_NullName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle name is required.", data?.Error);
    }

    [Fact]
    public async Task PostMuscle_NameTooLong_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = new string('A', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle name must be 100 characters or fewer.", data?.Error);
    }

    [Fact]
    public async Task PostMuscle_DuplicateName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = "Chest" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("A muscle with this name already exists.", data?.Error);
    }

    [Fact]
    public async Task PostMuscle_DuplicateNameDifferentCase_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name = "biceps" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("A muscle with this name already exists.", data?.Error);
    }

    [Fact]
    public async Task UpdateMuscle_Returns200_WhenValidData()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = "Hip Adductors" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MuscleDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.MuscleId, updated.MuscleId);
        Assert.Equal("Hip Adductors", updated.Name);
    }

    [Fact]
    public async Task UpdateMuscle_TrimmedName_IsPersisted()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = "  Hip Adductors  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MuscleDto>();
        Assert.NotNull(updated);
        Assert.Equal("Hip Adductors", updated.Name);
    }

    [Fact]
    public async Task UpdateMuscle_EmptyName_Returns400()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle name is required.", data?.Error);
    }

    [Fact]
    public async Task UpdateMuscle_NameTooLong_Returns400()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = new string('A', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle name must be 100 characters or fewer.", data?.Error);
    }

    [Fact]
    public async Task UpdateMuscle_DuplicateName_Returns400()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = "Chest" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("A muscle with this name already exists.", data?.Error);
    }

    [Fact]
    public async Task UpdateMuscle_DuplicateNameDifferentCase_Returns400()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = "chest" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("A muscle with this name already exists.", data?.Error);
    }

    [Fact]
    public async Task UpdateMuscle_KeepingOwnName_Returns200()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.PutAsJsonAsync($"/api/muscles/{created.MuscleId}", new { name = "Hip Flexors" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMuscle_NotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"/api/muscles/{Guid.NewGuid()}", new { name = "Hip Flexors" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle not found.", data?.Error);
    }

    [Fact]
    public async Task DeleteMuscle_Returns204_WhenExists()
    {
        var created = await CreateMuscleAsync("Hip Flexors");

        var response = await _client.DeleteAsync($"/api/muscles/{created.MuscleId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMuscle_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/muscles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ErrorDto>();
        Assert.Equal("Muscle not found.", data?.Error);
    }

    [Fact]
    public async Task DeleteMuscle_RemovesItFromList()
    {
        var created = await CreateMuscleAsync("Hip Flexors");
        await _client.DeleteAsync($"/api/muscles/{created.MuscleId}");

        var response = await _client.GetAsync("/api/muscles");
        var muscles = await response.Content.ReadFromJsonAsync<List<MuscleDto>>();
        Assert.NotNull(muscles);
        Assert.DoesNotContain(muscles, m => m.MuscleId == created.MuscleId);
    }

    private async Task<MuscleDto> CreateMuscleAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/muscles", new { name });
        response.EnsureSuccessStatusCode();
        var muscle = await response.Content.ReadFromJsonAsync<MuscleDto>();
        return muscle!;
    }

    private sealed record MuscleDto(Guid MuscleId, string Name);
    private sealed record ErrorDto(string Error);
}
