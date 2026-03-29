var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("https+http://api");
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/workout-types", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");
    var response = await client.GetAsync("/api/workout-types");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
});

app.MapGet("/api/muscles", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/muscles");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "GET /api/muscles", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapGet("/api/exercises", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/exercises");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "GET /api/exercises", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapPost("/api/exercises", async (ILogger<Program> logger, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/exercises", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "POST /api/exercises", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapPut("/api/exercises/{exerciseId:guid}", async (Guid exerciseId, ILogger<Program> logger, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/exercises/{exerciseId}", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"PUT /api/exercises/{exerciseId}", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapDelete("/api/exercises/{exerciseId:guid}", async (Guid exerciseId, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.DeleteAsync($"/api/exercises/{exerciseId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return Results.NoContent();
        }
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"DELETE /api/exercises/{exerciseId}", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapGet("/api/workouts", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/workouts");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "GET /api/workouts", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapGet("/api/workouts/{workoutId:guid}", async (Guid workoutId, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.GetAsync($"/api/workouts/{workoutId}");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"GET /api/workouts/{workoutId}", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapPost("/api/workouts", async (ILogger<Program> logger, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/workouts", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "POST /api/workouts", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapPut("/api/workouts/{workoutId:guid}", async (Guid workoutId, ILogger<Program> logger, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PutAsync($"/api/workouts/{workoutId}", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"PUT /api/workouts/{workoutId}", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapDelete("/api/workouts/{workoutId:guid}", async (Guid workoutId, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.DeleteAsync($"/api/workouts/{workoutId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return Results.NoContent();
        }
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"DELETE /api/workouts/{workoutId}", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapPost("/api/workouts/{workoutId:guid}/sessions", async (Guid workoutId, ILogger<Program> logger, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"/api/workouts/{workoutId}/sessions", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"POST /api/workouts/{workoutId}/sessions", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.MapGet("/api/sessions", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/sessions");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "GET /api/sessions", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
// Expose Program class for WebApplicationFactory in tests
public partial class Program { }

internal static partial class WebProxyLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Error proxying {Route}")]
    public static partial void ProxyError(ILogger logger, string route, Exception ex);
}
