using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

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
        WebAppFixture.SeedWorkout("Legs");
        WebAppFixture.SeedWorkout("Pull");
        WebAppFixture.SeedWorkout("Push");
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
        await Expect(pageTitle).ToHaveTextAsync("Let's go!");

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
    public async Task HomePage_DisplaysSetsDropdownBelowWorkout_WithExactOptionsAndSharedStyle()
    {
        var page = await CreatePageAsync();
        var sets = page.Locator("#sets-select");

        await Expect(page.Locator("label[for='sets-select']")).ToHaveTextAsync("Sets");
        await Expect(sets).ToHaveClassAsync("workout-form__select");
        Assert.Equal("", await sets.InputValueAsync());
        Assert.Equal(["Select sets", "3", "5"], await sets.Locator("option").AllTextContentsAsync());
        Assert.Equal(
            "sets-select",
            await page.Locator(".workout-form__group").Nth(1).Locator("select").GetAttributeAsync("id"));

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
        await page.Locator("#sets-select").SelectOptionAsync("3");

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

    [Fact]
    public async Task StartWorkout_NavigationIncludesSelectedSets()
    {
        var page = await CreatePageAsync();
        await page.Locator("#workout-select").SelectOptionAsync(new SelectOptionValue { Label = "Push" });
        await page.Locator("#sets-select").SelectOptionAsync("5");

        await page.Locator("button[type='submit']").ClickAsync();

        await Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"active-session\?.*sets=5"));
        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);
}
