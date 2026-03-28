using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class SidebarNavigationTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public SidebarNavigationTests(WebAppFixture webApp, PlaywrightFixture playwright)
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
    public async Task Sidebar_IsVisible_WithThreeMenuItems()
    {
        var page = await CreatePageAsync();

        var sidebar = page.Locator(".sidebar");
        await Expect(sidebar).ToBeVisibleAsync();

        var links = page.Locator(".sidebar__link");
        await Expect(links).ToHaveCountAsync(3);

        await page.CloseAsync();
    }

    [Fact]
    public async Task Sidebar_MenuItems_HaveIconsAndLabels()
    {
        var page = await CreatePageAsync();

        var labels = page.Locator(".sidebar__label");
        var texts = await labels.AllTextContentsAsync();
        Assert.Equal(["Home", "Workouts", "Exercises"], texts);

        var icons = page.Locator(".sidebar__icon");
        await Expect(icons).ToHaveCountAsync(3);

        await page.CloseAsync();
    }

    [Fact]
    public async Task ClickingMenuItem_UpdatesContentAndUrl()
    {
        var page = await CreatePageAsync();

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();
        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");
        Assert.EndsWith("/workouts", page.Url);

        await page.Locator(".sidebar__link[data-page='exercises']").ClickAsync();
        await Expect(page.Locator(".exercises-page__title")).ToHaveTextAsync("Exercises");
        Assert.EndsWith("/exercises", page.Url);

        await page.Locator(".sidebar__link[data-page='home']").ClickAsync();
        await Expect(page.Locator("#workout-form")).ToBeVisibleAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task ActiveLink_GetsActiveClass_AndAriaCurrent()
    {
        var page = await CreatePageAsync();

        // Home is active by default
        var homeLink = page.Locator(".sidebar__link[data-page='home']");
        await Expect(homeLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));
        Assert.Equal("page", await homeLink.GetAttributeAsync("aria-current"));

        // Navigate to workouts
        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();
        var workoutsLink = page.Locator(".sidebar__link[data-page='workouts']");
        await Expect(workoutsLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));
        Assert.Equal("page", await workoutsLink.GetAttributeAsync("aria-current"));

        // Home should no longer be active
        await Expect(homeLink).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));
        Assert.Null(await homeLink.GetAttributeAsync("aria-current"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task DeepLink_Workouts_ShowsCorrectContent()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/workouts");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");

        var workoutsLink = page.Locator(".sidebar__link[data-page='workouts']");
        await Expect(workoutsLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task DeepLink_Exercises_ShowsCorrectContent()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/exercises");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(page.Locator(".exercises-page__title")).ToHaveTextAsync("Exercises");

        var exercisesLink = page.Locator(".sidebar__link[data-page='exercises']");
        await Expect(exercisesLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task UnknownRoute_RedirectsToHome()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/nonexistent");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await Expect(page.Locator("#workout-form")).ToBeVisibleAsync();

        var homeLink = page.Locator(".sidebar__link[data-page='home']");
        await Expect(homeLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task BrowserBackForward_UpdatesActiveState()
    {
        var page = await CreatePageAsync();

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();
        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");

        await page.Locator(".sidebar__link[data-page='exercises']").ClickAsync();
        await Expect(page.Locator(".exercises-page__title")).ToHaveTextAsync("Exercises");

        await page.GoBackAsync();
        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");
        var workoutsLink = page.Locator(".sidebar__link[data-page='workouts']");
        await Expect(workoutsLink).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar__link--active"));

        await page.GoForwardAsync();
        await Expect(page.Locator(".exercises-page__title")).ToHaveTextAsync("Exercises");

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
