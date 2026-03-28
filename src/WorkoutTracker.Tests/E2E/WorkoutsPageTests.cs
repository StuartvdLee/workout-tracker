using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class WorkoutsPageTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public WorkoutsPageTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return page;
    }

    [Fact]
    public async Task NavigateViaSidebar_ShowsWorkoutsPage()
    {
        var page = await CreatePageAsync();

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();

        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");
        await Expect(page.Locator(".page-placeholder__text")).ToBeVisibleAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task DeepLink_ShowsWorkoutsPage()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/workouts");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");
        await Expect(page.Locator(".page-placeholder__text")).ToBeVisibleAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task WorkoutsLink_HasActiveState()
    {
        var page = await CreatePageAsync();

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();

        var link = page.Locator(".sidebar__link[data-page='workouts']");
        await Expect(link).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task WorkoutsPage_UsesPlaceholderStructure()
    {
        var page = await CreatePageAsync();

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();

        await Expect(page.Locator(".page-placeholder")).ToBeVisibleAsync();
        await Expect(page.Locator(".page-placeholder__title")).ToBeVisibleAsync();
        await Expect(page.Locator(".page-placeholder__text")).ToBeVisibleAsync();

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
