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
        var baseUrl = _webApp.BaseUrl;
        await page.GotoAsync(baseUrl);
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
        await select.SelectOptionAsync(new SelectOptionValue { Value = "push" });
        await page.Locator("button[type='submit']").ClickAsync();

        await Expect(error).ToBeHiddenAsync();

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
    public async Task SelectThenDeselect_ShowsErrorOnSubmit()
    {
        var page = await CreatePageAsync();
        var select = page.Locator("#workout-select");
        var button = page.Locator("button[type='submit']");

        await select.SelectOptionAsync(new SelectOptionValue { Value = "pull" });
        await button.ClickAsync();

        var error = page.Locator("#workout-error");
        await Expect(error).ToBeHiddenAsync();

        // Deselect by choosing the placeholder
        await select.EvaluateAsync("el => el.value = ''");
        await button.ClickAsync();

        await Expect(error).ToBeVisibleAsync();
        await Expect(error).ToHaveTextAsync("Please select a workout");

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
