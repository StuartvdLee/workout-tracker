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

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
// Expose Program class for WebApplicationFactory in tests
public partial class Program { }
