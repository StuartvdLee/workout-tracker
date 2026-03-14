using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class HomeLandingPagePerformanceTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPagePerformanceTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    [Fact]
    public async Task HomePage_LoadsWithinPerformanceBudget()
    {
        
        var page = await _playwright.Browser.NewPageAsync();
        var baseUrl = _webApp.BaseUrl;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await page.GotoAsync(baseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
        });
        stopwatch.Stop();

        // Page should load quickly in test environment (local server)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Page load took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");

        await page.CloseAsync();
    }

    [Fact]
    public async Task HomePage_NoExternalNetworkRequests()
    {
        
        var page = await _playwright.Browser.NewPageAsync();
        var baseUrl = _webApp.BaseUrl;
        var externalRequests = new List<string>();

        page.Request += (_, request) =>
        {
            var url = request.Url;
            if (!url.StartsWith(baseUrl, StringComparison.Ordinal))
            {
                externalRequests.Add(url);
            }
        };

        await page.GotoAsync(baseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Empty(externalRequests);

        await page.CloseAsync();
    }
}
