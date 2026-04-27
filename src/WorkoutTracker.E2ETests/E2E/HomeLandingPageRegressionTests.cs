using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class HomeLandingPageRegressionTests
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
    public async Task AllWorkoutTypes_CanBeSelectedAndSubmitted()
    {
        var page = await CreatePageAsync();
        string[] workoutLabels = ["Push", "Pull", "Legs"];

        foreach (var label in workoutLabels)
        {
            var select = page.Locator("#workout-select");
            var button = page.Locator("button[type='submit']");
            var error = page.Locator("#workout-error");
            await select.SelectOptionAsync(new SelectOptionValue { Label = label });
            await button.ClickAsync();
            await Assertions.Expect(error).ToBeHiddenAsync();
            // Navigate back to home for the next iteration (form submit navigates away)
            WebAppFixture.ResetWorkouts();
            WebAppFixture.SeedWorkout("Legs");
            WebAppFixture.SeedWorkout("Pull");
            WebAppFixture.SeedWorkout("Push");
            await page.GotoAsync(_webApp.BaseUrl);
            await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        }

        await page.CloseAsync();
    }
}
