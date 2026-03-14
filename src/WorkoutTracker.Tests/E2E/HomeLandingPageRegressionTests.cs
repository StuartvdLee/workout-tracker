using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class HomeLandingPageRegressionTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPageRegressionTests(WebAppFixture webApp, PlaywrightFixture playwright)
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
    public async Task RapidClicks_WithoutSelection_ErrorDisplayedOnce()
    {
        var page = await CreatePageAsync();
        var button = page.Locator("button[type='submit']");

        for (var i = 0; i < 10; i++)
        {
            await button.ClickAsync();
        }

        var errors = page.Locator("#workout-error:visible");
        await Assertions.Expect(errors).ToHaveCountAsync(1);
        await Assertions.Expect(errors).ToHaveTextAsync("Please select a workout");

        await page.CloseAsync();
    }

    [Fact]
    public async Task SelectThenResetToPlaceholder_ValidationReappears()
    {
        var page = await CreatePageAsync();
        var select = page.Locator("#workout-select");
        var button = page.Locator("button[type='submit']");
        var error = page.Locator("#workout-error");

        // Select a workout and submit — no error
        await select.SelectOptionAsync(new SelectOptionValue { Value = "legs" });
        await button.ClickAsync();
        await Assertions.Expect(error).ToBeHiddenAsync();

        // Reset to placeholder and submit — error should appear
        await select.EvaluateAsync("el => el.value = ''");
        await button.ClickAsync();
        await Assertions.Expect(error).ToBeVisibleAsync();
        await Assertions.Expect(error).ToHaveTextAsync("Please select a workout");

        await page.CloseAsync();
    }

    [Fact]
    public async Task AllWorkoutTypes_CanBeSelectedAndSubmitted()
    {
        var page = await CreatePageAsync();
        var select = page.Locator("#workout-select");
        var button = page.Locator("button[type='submit']");
        var error = page.Locator("#workout-error");
        string[] workoutValues = ["push", "pull", "legs"];

        foreach (var value in workoutValues)
        {
            await select.SelectOptionAsync(new SelectOptionValue { Value = value });
            await button.ClickAsync();
            await Assertions.Expect(error).ToBeHiddenAsync();
        }

        await page.CloseAsync();
    }
}
