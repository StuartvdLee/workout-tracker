using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class HomeLandingPageSelectionTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPageSelectionTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {

        var page = await _playwright.Browser.NewPageAsync();
        var baseUrl = _webApp.BaseUrl;
        await page.GotoAsync(baseUrl);
        return page;
    }

    [Fact]
    public async Task HomePage_DisplaysTitle_WorkoutTracker()
    {
        var page = await CreatePageAsync();

        var heading = page.Locator("h1");
        await Expect(heading).ToHaveTextAsync("Workout Tracker");

        await page.CloseAsync();
    }

    [Fact]
    public async Task HomePage_DisplaysDropdown_WithPlaceholder()
    {
        var page = await CreatePageAsync();

        var select = page.Locator("#workout-select");
        await Expect(select).ToBeVisibleAsync();

        var selectedOption = await select.InputValueAsync();
        Assert.Equal("", selectedOption);

        await page.CloseAsync();
    }

    [Fact]
    public async Task HomePage_DisplaysStartWorkoutButton()
    {
        var page = await CreatePageAsync();

        var button = page.Locator("button[type='submit']");
        await Expect(button).ToHaveTextAsync("Start Workout");

        await page.CloseAsync();
    }

    [Theory]
    [InlineData("push")]
    [InlineData("pull")]
    [InlineData("legs")]
    public async Task SelectWorkout_AndClickStart_NoErrorDisplayed(string value)
    {
        var page = await CreatePageAsync();

        var select = page.Locator("#workout-select");
        await select.SelectOptionAsync(new SelectOptionValue { Value = value });

        var selectedValue = await select.InputValueAsync();
        Assert.Equal(value, selectedValue);

        await page.Locator("button[type='submit']").ClickAsync();

        var error = page.Locator("#workout-error");
        await Expect(error).ToBeHiddenAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task Dropdown_ContainsExactlyThreeWorkoutOptions()
    {
        var page = await CreatePageAsync();

        var options = page.Locator("#workout-select option:not([disabled])");
        await Expect(options).ToHaveCountAsync(3);

        var texts = await options.AllTextContentsAsync();
        Assert.Equal(["Push", "Pull", "Legs"], texts);

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
