using System.Net;
using System.Net.Http.Json;
using Xunit;
using WorkoutTracker.Tests.Infrastructure;

namespace WorkoutTracker.Tests.Api;

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
    public async Task GetMuscles_FirstMuscleIsBack()
    {
        var response = await _client.GetAsync("/api/muscles");
        var muscles = await response.Content.ReadFromJsonAsync<List<MuscleDto>>();

        Assert.NotNull(muscles);
        Assert.Equal("Adductors", muscles[0].Name);
    }

    private sealed record MuscleDto(Guid MuscleId, string Name);
}
