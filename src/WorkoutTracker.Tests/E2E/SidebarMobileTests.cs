using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class SidebarMobileTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public SidebarMobileTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreateMobilePageAsync()
    {
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 375, Height = 667 },
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return page;
    }

    [Fact]
    public async Task Mobile_SidebarIsHiddenByDefault()
    {
        var page = await CreateMobilePageAsync();

        var sidebar = page.Locator(".sidebar");
        await Expect(sidebar).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar--open"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task Mobile_ToggleButtonIsVisible()
    {
        var page = await CreateMobilePageAsync();

        var toggle = page.Locator(".topbar__toggle");
        await Expect(toggle).ToBeVisibleAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task Mobile_ClickingToggle_OpensSidebar()
    {
        var page = await CreateMobilePageAsync();

        await page.Locator(".topbar__toggle").ClickAsync();

        var sidebar = page.Locator(".sidebar");
        await Expect(sidebar).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar--open"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task Mobile_AriaExpanded_TogglesCorrectly()
    {
        var page = await CreateMobilePageAsync();

        var toggle = page.Locator(".topbar__toggle");
        Assert.Equal("false", await toggle.GetAttributeAsync("aria-expanded"));

        await toggle.ClickAsync();
        Assert.Equal("true", await toggle.GetAttributeAsync("aria-expanded"));

        // Close via backdrop (standard mobile UX — toggle is behind sidebar overlay)
        await page.Locator(".sidebar__backdrop").ClickAsync(new LocatorClickOptions { Force = true });
        Assert.Equal("false", await toggle.GetAttributeAsync("aria-expanded"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task Mobile_SelectingMenuItem_ClosesSidebarAndNavigates()
    {
        var page = await CreateMobilePageAsync();

        await page.Locator(".topbar__toggle").ClickAsync();
        await Expect(page.Locator(".sidebar")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar--open"));

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();

        await Expect(page.Locator(".sidebar")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar--open"));
        await Expect(page.Locator(".page-placeholder__title")).ToHaveTextAsync("Workouts");

        await page.CloseAsync();
    }

    [Fact]
    public async Task Mobile_ClickingBackdrop_ClosesSidebar()
    {
        var page = await CreateMobilePageAsync();

        await page.Locator(".topbar__toggle").ClickAsync();
        await Expect(page.Locator(".sidebar")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar--open"));

        await page.Locator(".sidebar__backdrop").ClickAsync(new LocatorClickOptions { Force = true });

        await Expect(page.Locator(".sidebar")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("sidebar--open"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task Mobile_ResizingToDesktop_ShowsSidebarAndHidesTopbar()
    {
        var page = await CreateMobilePageAsync();

        // Topbar visible at mobile
        await Expect(page.Locator(".topbar__toggle")).ToBeVisibleAsync();

        // Resize to desktop
        await page.SetViewportSizeAsync(1024, 768);

        // Sidebar should be visible without toggle
        await Expect(page.Locator(".sidebar")).ToBeVisibleAsync();

        // Topbar toggle should be hidden
        await Expect(page.Locator(".topbar__toggle")).Not.ToBeVisibleAsync();

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
