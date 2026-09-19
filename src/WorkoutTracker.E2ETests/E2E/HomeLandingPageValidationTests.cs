using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class HomeLandingPageValidationTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPageValidationTests(WebAppFixture webApp, PlaywrightFixture playwright)
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
    public async Task ClickStartWithoutSelection_ShowsValidationError()
    {
        var page = await CreatePageAsync();

        await page.Locator("button[type='submit']").ClickAsync();

        var error = page.Locator("#workout-error");
        await Expect(error).ToBeVisibleAsync();
        await Expect(error).ToHaveTextAsync("Please select a workout");

        await page.CloseAsync();
    }

    [Fact]
    public async Task ClickStartWithoutSets_ShowsSetsErrorAndFocusesSets()
    {
        var page = await CreatePageAsync();
        await page.Locator("#workout-select").SelectOptionAsync(new SelectOptionValue { Label = "Push" });

        await page.Locator("button[type='submit']").ClickAsync();

        await Expect(page.Locator("#sets-error")).ToHaveTextAsync("Please select sets");
        Assert.Equal("sets-select", await page.EvaluateAsync<string>("document.activeElement?.id ?? ''"));

        await page.Locator("#sets-select").SelectOptionAsync("5");
        await Expect(page.Locator("#sets-error")).ToBeHiddenAsync();
        await page.CloseAsync();
    }

    [Fact]
    public async Task SelectWorkoutAfterError_ClearsError()
    {
        var page = await CreatePageAsync();

        await page.Locator("button[type='submit']").ClickAsync();
        var error = page.Locator("#workout-error");
        await Expect(error).ToBeVisibleAsync();

        var select = page.Locator("#workout-select");
        await select.SelectOptionAsync(new SelectOptionValue { Label = "Push" });

        await Expect(error).ToBeHiddenAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task SelectWorkoutAfterError_RemovesAriaInvalid()
    {
        var page = await CreatePageAsync();
        var select = page.Locator("#workout-select");

        await page.Locator("button[type='submit']").ClickAsync();
        await Expect(page.Locator("#workout-error")).ToBeVisibleAsync();
        Assert.Equal("true", await select.GetAttributeAsync("aria-invalid"));

        await select.SelectOptionAsync(new SelectOptionValue { Label = "Pull" });

        Assert.Null(await select.GetAttributeAsync("aria-invalid"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task SelectWorkoutAfterError_RemovesErrorStyling()
    {
        var page = await CreatePageAsync();
        var select = page.Locator("#workout-select");

        await page.Locator("button[type='submit']").ClickAsync();
        await Expect(page.Locator("#workout-error")).ToBeVisibleAsync();
        await Expect(select).ToHaveClassAsync(new Regex("workout-form__select--error"));

        await select.SelectOptionAsync(new SelectOptionValue { Label = "Legs" });

        await Expect(select).Not.ToHaveClassAsync(new Regex("workout-form__select--error"));

        await page.CloseAsync();
    }

    [Fact]
    public async Task SelectEachWorkoutAfterError_ClearsErrorForAll()
    {
        var page = await CreatePageAsync();
        var select = page.Locator("#workout-select");
        var error = page.Locator("#workout-error");
        var button = page.Locator("button[type='submit']");

        foreach (var label in new[] { "Push", "Pull", "Legs" })
        {
            // Navigate fresh to reset the form state
            await page.GotoAsync(_webApp.BaseUrl);
            await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await button.ClickAsync();
            await Expect(error).ToBeVisibleAsync();

            await select.SelectOptionAsync(new SelectOptionValue { Label = label });
            await Expect(error).ToBeHiddenAsync();
        }

        await page.CloseAsync();
    }

    [Fact]
    public async Task ErrorMessage_AppearsOnlyOnce_OnMultipleClicks()
    {
        var page = await CreatePageAsync();
        var button = page.Locator("button[type='submit']");

        await button.ClickAsync();
        await button.ClickAsync();
        await button.ClickAsync();

        var errors = page.Locator(".workout-form__error:visible");
        await Expect(errors).ToHaveCountAsync(1);
        await Expect(errors).ToHaveTextAsync("Please select a workout");

        await page.CloseAsync();
    }

    [Fact]
    public async Task ErrorState_AddsAriaInvalid_ToSelect()
    {
        var page = await CreatePageAsync();

        await page.Locator("button[type='submit']").ClickAsync();

        var select = page.Locator("#workout-select");
        var ariaInvalid = await select.GetAttributeAsync("aria-invalid");
        Assert.Equal("true", ariaInvalid);

        await page.CloseAsync();
    }

    [Fact]
    public async Task WorkoutListFailure_LeavesSetsUsableButBlocksStart()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.RouteAsync("**/api/workouts", route => route.FulfillAsync(new() { Status = 500 }));

        await page.GotoAsync(_webApp.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.Locator("#sets-select")).ToBeEnabledAsync();
        await page.Locator("#sets-select").SelectOptionAsync("3");
        await page.Locator("button[type='submit']").ClickAsync();
        await Expect(page.Locator("#workout-error")).ToHaveTextAsync("Please select a workout");

        await page.UnrouteAsync("**/api/workouts");
        await page.CloseAsync();
    }

    [Fact]
    public async Task WorkoutAndSetsControls_AreDisabledWhileWorkoutListLoads()
    {
        WebAppFixture.ResetWorkouts();
        WebAppFixture.SeedWorkout("Slow Workout");
        var releaseResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var page = await _playwright.Browser.NewPageAsync();
        await page.RouteAsync("**/api/workouts", async route =>
        {
            await releaseResponse.Task;
            await route.FallbackAsync();
        });

        await page.GotoAsync(_webApp.BaseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(page.Locator("#workout-select")).ToBeDisabledAsync();
        await Expect(page.Locator("#sets-select")).ToBeDisabledAsync();

        releaseResponse.SetResult();
        await Expect(page.Locator("#workout-select")).ToBeEnabledAsync();
        await Expect(page.Locator("#sets-select")).ToBeEnabledAsync();
        await page.UnrouteAsync("**/api/workouts");
        await page.CloseAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
