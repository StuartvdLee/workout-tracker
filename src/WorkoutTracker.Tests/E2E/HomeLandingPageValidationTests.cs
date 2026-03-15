using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class HomeLandingPageValidationTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
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

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
