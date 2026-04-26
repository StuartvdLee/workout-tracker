using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[Collection("E2E")]
public class SidebarAccessibilityTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public SidebarAccessibilityTests(WebAppFixture webApp, PlaywrightFixture playwright)
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

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task SidebarLinks_AreReachableViaTab()
    {
        var page = await CreatePageAsync();

        var homeLink = page.Locator(".sidebar__link[data-page='home']");
        await homeLink.FocusAsync();
        await Expect(homeLink).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        var workoutsLink = page.Locator(".sidebar__link[data-page='workouts']");
        await Expect(workoutsLink).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Tab");
        var exercisesLink = page.Locator(".sidebar__link[data-page='exercises']");
        await Expect(exercisesLink).ToBeFocusedAsync();

        await page.CloseAsync();
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task SidebarLinks_ActivatableViaEnter()
    {
        var page = await CreatePageAsync();

        // Focus the workouts link and press Enter
        await page.Locator(".sidebar__link[data-page='workouts']").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.Locator(".workouts-page__title")).ToHaveTextAsync("Workouts");

        await page.CloseAsync();
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task Nav_HasAriaLabel()
    {
        var page = await CreatePageAsync();

        var nav = page.Locator(".sidebar__nav");
        var ariaLabel = await nav.GetAttributeAsync("aria-label");
        Assert.Equal("Main navigation", ariaLabel);

        await page.CloseAsync();
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task ActiveLink_HasAriaCurrent_InactiveLinksDoNot()
    {
        var page = await CreatePageAsync();

        // Home is active by default
        var homeLink = page.Locator(".sidebar__link[data-page='home']");
        Assert.Equal("page", await homeLink.GetAttributeAsync("aria-current"));

        var workoutsLink = page.Locator(".sidebar__link[data-page='workouts']");
        Assert.Null(await workoutsLink.GetAttributeAsync("aria-current"));

        var exercisesLink = page.Locator(".sidebar__link[data-page='exercises']");
        Assert.Null(await exercisesLink.GetAttributeAsync("aria-current"));

        await page.CloseAsync();
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task ToggleButton_HasCorrectAriaAttributes()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 375, Height = 667 },
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var toggle = page.Locator(".topbar__toggle");
        Assert.Equal("Toggle navigation", await toggle.GetAttributeAsync("aria-label"));
        Assert.Equal("sidebar", await toggle.GetAttributeAsync("aria-controls"));
        Assert.Equal("false", await toggle.GetAttributeAsync("aria-expanded"));

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
