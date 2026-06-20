using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class HomeLandingPagePerformanceTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPagePerformanceTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    /// <summary>
    /// Lightweight performance smoke test for the home page.
    /// 
    /// Note: The product spec requires the page to be interactive within 3 seconds
    /// on a slow 3G connection. This test does not attempt to emulate slow 3G
    /// network conditions and instead runs against a local test server without
    /// network throttling. The relaxed 5-second threshold below is intended only
    /// to catch obvious regressions in the test environment, not to validate the
    /// full production performance requirement.
    /// </summary>
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

        // Smoke check: in the local test environment (no network throttling),
        // the page should load well under this relaxed 5-second budget. This
        // is not a strict verification of the 3-second slow-3G performance spec.
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
