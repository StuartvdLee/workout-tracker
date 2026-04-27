using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class HomeLandingPageResponsiveTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPageResponsiveTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageWithViewportAsync(int width, int height)
    {
        WebAppFixture.ResetWorkouts();
        WebAppFixture.SeedWorkout("Legs");
        WebAppFixture.SeedWorkout("Pull");
        WebAppFixture.SeedWorkout("Push");
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    [Fact]
    public async Task MobileViewport_AllElementsVisible_NoHorizontalOverflow()
    {
        var page = await CreatePageWithViewportAsync(375, 667);

        await Expect(page.Locator("h1")).ToBeVisibleAsync();
        await Expect(page.Locator("#workout-select")).ToBeVisibleAsync();
        await Expect(page.Locator("button[type='submit']")).ToBeVisibleAsync();

        var scrollWidth = await page.EvaluateAsync<int>("document.documentElement.scrollWidth");
        var clientWidth = await page.EvaluateAsync<int>("document.documentElement.clientWidth");
        Assert.True(scrollWidth <= clientWidth, $"Horizontal overflow detected: scrollWidth={scrollWidth} > clientWidth={clientWidth}");

        await page.CloseAsync();
    }

    [Fact]
    public async Task DesktopViewport_AllElementsVisible_LayoutAdapts()
    {
        var page = await CreatePageWithViewportAsync(1024, 768);

        await Expect(page.Locator("h1")).ToBeVisibleAsync();
        await Expect(page.Locator("#workout-select")).ToBeVisibleAsync();
        await Expect(page.Locator("button[type='submit']")).ToBeVisibleAsync();

        var scrollWidth = await page.EvaluateAsync<int>("document.documentElement.scrollWidth");
        var clientWidth = await page.EvaluateAsync<int>("document.documentElement.clientWidth");
        Assert.True(scrollWidth <= clientWidth, $"Horizontal overflow detected: scrollWidth={scrollWidth} > clientWidth={clientWidth}");

        await page.CloseAsync();
    }

    [Fact]
    public async Task NarrowViewport_320px_NoOverflow()
    {
        var page = await CreatePageWithViewportAsync(320, 568);

        await Expect(page.Locator("h1")).ToBeVisibleAsync();

        var scrollWidth = await page.EvaluateAsync<int>("document.documentElement.scrollWidth");
        var clientWidth = await page.EvaluateAsync<int>("document.documentElement.clientWidth");
        Assert.True(scrollWidth <= clientWidth, $"Horizontal overflow at 320px: scrollWidth={scrollWidth} > clientWidth={clientWidth}");

        await page.CloseAsync();
    }

    [Fact]
    public async Task WideViewport_1920px_ContentCentered()
    {
        var page = await CreatePageWithViewportAsync(1920, 1080);

        var appEl = page.Locator(".app");
        var box = await appEl.BoundingBoxAsync();

        Assert.NotNull(box);
        Assert.True(box.Width < 1920, "Content should be constrained, not full viewport width");

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
