using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class MusclesPageTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public MusclesPageTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {
        WebAppFixture.ResetMuscles();
        WebAppFixture.ResetExercises();

        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/muscles");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Fact]
    public async Task MusclesPage_LoadsWithExistingMuscles()
    {
        var page = await CreatePageAsync();
        try
        {
            await Expect(page.Locator(".muscles-page__title")).ToHaveTextAsync("Targeted Muscles");
            await Expect(page.Locator(".muscle-tile")).ToHaveCountAsync(12);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AddMuscle_CreatesNewTile()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#muscle-name").FillAsync("Hip Flexors");
            await page.Locator("#muscle-form .exercise-form__submit").ClickAsync();

            await Expect(page.Locator(".muscle-tile__name", new PageLocatorOptions { HasText = "Hip Flexors" })).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMuscle_UpdatesName()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator(".muscle-tile", new PageLocatorOptions { HasText = "Chest" })
                .Locator(".muscle-tile__edit-btn")
                .ClickAsync();

            await page.Locator("#edit-muscle-name").FillAsync("Pectorals");
            await page.Locator("#muscle-edit-form .exercise-form__submit").ClickAsync();

            await Expect(page.Locator(".muscle-tile__name", new PageLocatorOptions { HasText = "Pectorals" })).ToBeVisibleAsync();
            await Expect(page.Locator(".muscle-tile__name", new PageLocatorOptions { HasText = "Chest" })).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteMuscle_RemovesTile()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator(".muscle-tile", new PageLocatorOptions { HasText = "Chest" })
                .Locator(".muscle-tile__edit-btn")
                .ClickAsync();

            await page.Locator("#muscle-edit-delete").ClickAsync();
            await page.Locator("#muscle-delete-confirm").ClickAsync();

            await Expect(page.Locator(".muscle-tile__name", new PageLocatorOptions { HasText = "Chest" })).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
