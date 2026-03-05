using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Backend.Tests.Integration.Api;

public sealed class SessionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private const string SessionsEndpoint = "/api/sessions";

    private sealed record CreateSessionRequest(string WorkoutType);

    private sealed record SessionResponse(Guid Id, string WorkoutType, DateTimeOffset StartedAt, DateTimeOffset? EndedAt);

    public SessionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateSession_WithWorkoutType_Succeeds()
    {
        // Arrange
        var workoutType = "Push";
        var request = new CreateSessionRequest(workoutType);

        // Act
        var response = await _client.PostAsJsonAsync(SessionsEndpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session!.Id);
        Assert.Equal(workoutType, session.WorkoutType);

        // Server should set timestamps; StartedAt should not be default
        Assert.NotEqual(default, session.StartedAt);
    }

    [Fact]
    public async Task CreateSession_WithoutWorkoutType_ReturnsValidationFailure()
    {
        // Arrange: workoutType omitted or empty to trigger validation
        var requestContent = new
        {
            WorkoutType = string.Empty
        };

        // Act
        var response = await _client.PostAsJsonAsync(SessionsEndpoint, requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Try to read a generic validation problem details structure
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsLike>();
        Assert.NotNull(problemDetails);
        Assert.NotNull(problemDetails!.Errors);
        Assert.Contains("workoutType", problemDetails.Errors.Keys);
        Assert.NotEmpty(problemDetails.Errors["workoutType"]);
    }

    private sealed class ValidationProblemDetailsLike
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
        public System.Collections.Generic.Dictionary<string, string[]> Errors { get; set; } =
            new System.Collections.Generic.Dictionary<string, string[]>();
    }
}
