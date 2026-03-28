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

app.MapGet("/api/muscles", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");
    var response = await client.GetAsync("/api/muscles");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
});

app.MapGet("/api/exercises", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");
    var response = await client.GetAsync("/api/exercises");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
});

app.MapPost("/api/exercises", async (HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");
    var body = await new StreamReader(request.Body).ReadToEndAsync();
    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/api/exercises", content);
    var responseContent = await response.Content.ReadAsStringAsync();
    return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
});

app.MapPut("/api/exercises/{exerciseId}", async (string exerciseId, HttpRequest request, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");
    var body = await new StreamReader(request.Body).ReadToEndAsync();
    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
    var response = await client.PutAsync($"/api/exercises/{exerciseId}", content);
    var responseContent = await response.Content.ReadAsStringAsync();
    return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
// Expose Program class for WebApplicationFactory in tests
public partial class Program { }
