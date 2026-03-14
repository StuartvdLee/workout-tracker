using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace WorkoutTracker.Tests.Infrastructure;

public class WebAppFixture : WebApplicationFactory<Program>
{
    private IHost? _host;

    public string BaseUrl { get; }

    public WebAppFixture()
    {
        var port = Random.Shared.Next(5100, 5999);
        BaseUrl = $"http://localhost:{port}";

        // Force creation of the server
        _ = Server;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = base.CreateHost(builder);

        // Resolve the Web project source directory for static files
        var webProjectDir = FindWebProjectDir();

        // Start a real Kestrel host so Playwright can connect via HTTP
        var webAppBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = webProjectDir,
            WebRootPath = Path.Combine(webProjectDir, "wwwroot"),
        });
        webAppBuilder.WebHost.UseUrls(BaseUrl);
        webAppBuilder.Environment.EnvironmentName = "Development";

        var app = webAppBuilder.Build();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        _host = app;
        _host.Start();

        return testHost;
    }

    private static string FindWebProjectDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "WorkoutTracker.Web");
            if (Directory.Exists(Path.Combine(candidate, "wwwroot")))
            {
                return candidate;
            }

            var srcCandidate = Path.Combine(dir, "src", "WorkoutTracker.Web");
            if (Directory.Exists(Path.Combine(srcCandidate, "wwwroot")))
            {
                return srcCandidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find WorkoutTracker.Web project directory");
    }

    protected override void Dispose(bool disposing)
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        base.Dispose(disposing);
    }
}
