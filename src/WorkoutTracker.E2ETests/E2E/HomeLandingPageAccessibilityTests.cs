using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class HomeLandingPageAccessibilityTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPageAccessibilityTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync(int width = 375, int height = 667)
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
    public async Task TouchTargets_MeetMinimumSize_44x44()
    {
        var page = await CreatePageAsync();
        const int minTouchTarget = 44;

        var select = page.Locator("#workout-select");
        var selectBox = await select.BoundingBoxAsync();
        Assert.NotNull(selectBox);
        Assert.True(selectBox.Height >= minTouchTarget, $"Select height {selectBox.Height}px < {minTouchTarget}px minimum");

        var button = page.Locator("button[type='submit']");
        var buttonBox = await button.BoundingBoxAsync();
        Assert.NotNull(buttonBox);
        Assert.True(buttonBox.Height >= minTouchTarget, $"Button height {buttonBox.Height}px < {minTouchTarget}px minimum");

        await page.CloseAsync();
    }

    [Fact]
    public async Task Select_HasAssociatedLabel()
    {
        var page = await CreatePageAsync();

        var label = page.Locator("label[for='workout-select']");
        await Assertions.Expect(label).ToBeVisibleAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task ErrorRegion_HasAriaLiveAttribute()
    {
        var page = await CreatePageAsync();

        var error = page.Locator("#workout-error");
        var ariaLive = await error.GetAttributeAsync("aria-live");
        Assert.Equal("polite", ariaLive);

        var role = await error.GetAttributeAsync("role");
        Assert.Equal("alert", role);

        await page.CloseAsync();
    }

    [Fact]
    public async Task Select_HasAriaDescribedBy_ErrorElement()
    {
        var page = await CreatePageAsync();

        var select = page.Locator("#workout-select");
        var describedBy = await select.GetAttributeAsync("aria-describedby");
        Assert.Equal("workout-error", describedBy);

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
