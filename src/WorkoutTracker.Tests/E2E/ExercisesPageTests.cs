using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

public class ExercisesPageTests : IClassFixture<WebAppFixture>, IClassFixture<PlaywrightFixture>
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public ExercisesPageTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {
        WebAppFixture.ResetExercises();
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/exercises");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Fact]
    public async Task CreateExercise_AppearsInList()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Bench Press");
            await page.Locator(".exercise-form__submit").ClickAsync();

            var item = page.Locator(".exercise-list__item");
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__name")).ToHaveTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SubmitEmptyName_ShowsRequiredError()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-error")).ToHaveTextAsync("Exercise name is required.");
            await Expect(page.Locator("#exercise-name")).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(page.Locator("#exercise-name")).ToHaveClassAsync(new Regex("exercise-form__input--error"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SubmitWhitespaceOnlyName_ShowsRequiredError()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("   ");
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-error")).ToHaveTextAsync("Exercise name is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SubmitNameExceedingMaxLength_ShowsMaxLengthError()
    {
        var page = await CreatePageAsync();
        try
        {
            var longName = new string('A', 151);
            await page.Locator("#exercise-name").FillAsync(longName);
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-error")).ToHaveTextAsync("Exercise name must be 150 characters or fewer.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateDuplicateName_ShowsDuplicateError()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator("#exercise-name").FillAsync("squat");
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-api-error")).ToHaveTextAsync("An exercise with this name already exists.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AfterSuccessfulSave_FormClears()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Deadlift");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await Expect(page.Locator("#exercise-name")).ToHaveValueAsync("");
            await Expect(page.Locator("#exercise-error")).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EmptyState_ShownWhenNoExercises()
    {
        var page = await CreatePageAsync();
        try
        {
            await Expect(page.Locator("#exercise-empty")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeepLink_LoadsExercisesPage()
    {
        var page = await CreatePageAsync();
        try
        {
            await Expect(page.Locator(".exercises-page__title")).ToHaveTextAsync("Exercises");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DoubleSubmissionPrevention_DisabledDuringSubmit()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Pull Ups");

            var responseTask = page.WaitForResponseAsync(
                url => url.Url.Contains("/api/exercises"),
                new PageWaitForResponseOptions { Timeout = 5000 });
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator(".exercise-form__submit")).ToHaveAttributeAsync("aria-disabled", "true");

            await responseTask;
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ServerError_ShowsErrorAndPreservesInput()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("__MOCK_SERVER_ERROR");
            await page.Locator(".exercise-form__submit").ClickAsync();

            var apiError = page.Locator("#exercise-api-error");
            await Expect(apiError).ToBeVisibleAsync();
            await Expect(apiError).ToHaveAttributeAsync("role", "alert");
            await Expect(page.Locator("#exercise-name")).ToHaveValueAsync("__MOCK_SERVER_ERROR");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);
}
