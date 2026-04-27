using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[Collection("E2E")]
public class HomeLandingPageSelectionTests
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
        WebAppFixture.ResetWorkouts();
        WebAppFixture.SeedDefaultWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.GotoAsync(_webApp.BaseUrl);
        await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    [Fact]
    public async Task HomePage_DisplaysTitle_WorkoutTracker()
    {
        var page = await CreatePageAsync();

        var sidebarTitle = page.Locator(".sidebar__title");
        await Expect(sidebarTitle).ToHaveTextAsync("Workout Tracker");

        var pageTitle = page.Locator("h1");
        await Expect(pageTitle).ToHaveTextAsync("Home");

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
    [InlineData("Push")]
    [InlineData("Pull")]
    [InlineData("Legs")]
    public async Task SelectWorkout_AndClickStart_NoErrorDisplayed(string label)
    {
        var page = await CreatePageAsync();

        var select = page.Locator("#workout-select");
        await select.SelectOptionAsync(new SelectOptionValue { Label = label });

        var selectedValue = await select.InputValueAsync();
        Assert.NotEmpty(selectedValue);

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
        Assert.Equal(["Legs", "Pull", "Push"], texts);

        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
