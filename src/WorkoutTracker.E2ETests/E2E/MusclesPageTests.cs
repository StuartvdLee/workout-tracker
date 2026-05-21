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
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/muscles");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Fact]
    public async Task NavigateToMusclesPage_ShowsMuscleGrid()
    {
        var page = await CreatePageAsync();
        try
        {
            await Expect(page.Locator("#muscle-grid")).ToBeVisibleAsync();
            var cardCount = await page.Locator("#muscle-grid .muscle-card").CountAsync();
            Assert.True(cardCount >= 12, $"Expected at least 12 muscle cards, found {cardCount}.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AddMuscle_AppearsInGridImmediately()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#muscle-name").FillAsync("Hip Flexors");
            await page.Locator("#muscle-form .muscle-form__submit").ClickAsync();

            await Expect(MuscleCard(page, "Hip Flexors")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AddMuscle_IsSortedAlphabetically()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#muscle-name").FillAsync("Hip Flexors");
            await page.Locator("#muscle-form .muscle-form__submit").ClickAsync();
            await Expect(MuscleCard(page, "Hip Flexors")).ToBeVisibleAsync();

            var names = await GetMuscleNamesAsync(page);
            var hipIndex = names.IndexOf("Hip Flexors");
            var hamstringsIndex = names.IndexOf("Hamstrings");
            var quadsIndex = names.IndexOf("Quads");

            Assert.True(hipIndex > hamstringsIndex, $"Hip Flexors (index {hipIndex}) should come after Hamstrings (index {hamstringsIndex}).");
            Assert.True(hipIndex < quadsIndex, $"Hip Flexors (index {hipIndex}) should come before Quads (index {quadsIndex}).");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AddMuscle_EmptyNameShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#muscle-form .muscle-form__submit").ClickAsync();

            await Expect(page.Locator("#muscle-error")).ToHaveTextAsync("Muscle name is required.");
            await Expect(page.Locator("#muscle-grid .muscle-card")).ToHaveCountAsync(12);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task AddMuscle_DuplicateNameShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            var initialCount = await page.Locator("#muscle-grid .muscle-card").CountAsync();

            await page.Locator("#muscle-name").FillAsync("chest");
            await page.Locator("#muscle-form .muscle-form__submit").ClickAsync();

            await Expect(page.Locator("#muscle-api-error")).ToHaveTextAsync("A muscle with this name already exists.");
            await Expect(page.Locator("#muscle-grid .muscle-card")).ToHaveCountAsync(initialCount);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMuscle_RenameAppearsInGrid()
    {
        var page = await CreatePageAsync();
        try
        {
            var chestCard = MuscleCard(page, "Chest");
            await chestCard.ClickAsync();

            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            await Expect(page.Locator("#edit-muscle-name")).ToHaveValueAsync("Chest");

            await page.Locator("#edit-muscle-name").FillAsync("Upper Chest");
            await page.Locator("#edit-modal-form .edit-modal__submit-btn").ClickAsync();

            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();
            await Expect(MuscleCard(page, "Upper Chest")).ToBeVisibleAsync();

            var names = await GetMuscleNamesAsync(page);
            Assert.Contains("Upper Chest", names);
            Assert.DoesNotContain("Chest", names);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMuscle_EscapeDiscardChanges()
    {
        var page = await CreatePageAsync();
        try
        {
            await MuscleCard(page, "Chest").ClickAsync();
            await page.Locator("#edit-muscle-name").FillAsync("Upper Chest");
            await page.Keyboard.PressAsync("Escape");

            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

            var names = await GetMuscleNamesAsync(page);
            Assert.Contains("Chest", names);
            Assert.DoesNotContain("Upper Chest", names);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMuscle_CloseButton_ClosesModalWithoutSaving()
    {
        var page = await CreatePageAsync();
        try
        {
            await MuscleCard(page, "Chest").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();

            await page.Locator("#edit-muscle-name").FillAsync("Changed Name");
            await page.Locator("#edit-modal-close").ClickAsync();

            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

            var names = await GetMuscleNamesAsync(page);
            Assert.Contains("Chest", names);
            Assert.DoesNotContain("Changed Name", names);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMuscle_DuplicateNameShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await MuscleCard(page, "Biceps").ClickAsync();
            await page.Locator("#edit-muscle-name").FillAsync("Chest");
            await page.Locator("#edit-modal-form .edit-modal__submit-btn").ClickAsync();

            await Expect(page.Locator("#edit-modal-api-error")).ToHaveTextAsync("A muscle with this name already exists.");

            var names = await GetMuscleNamesAsync(page);
            Assert.Contains("Biceps", names);
            Assert.Contains("Chest", names);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteMuscle_RemovedFromGrid()
    {
        var page = await CreatePageAsync();
        try
        {
            var initialCount = await page.Locator("#muscle-grid .muscle-card").CountAsync();
            await MuscleCard(page, "Chest").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();

            await page.Locator("#edit-modal-delete-btn").ClickAsync();
            await Expect(page.Locator("#delete-confirm-backdrop")).ToBeVisibleAsync();
            await page.Locator("#delete-confirm-btn").ClickAsync();

            await Expect(page.Locator("#delete-confirm-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator("#muscle-grid .muscle-card")).ToHaveCountAsync(initialCount - 1);

            var names = await GetMuscleNamesAsync(page);
            Assert.DoesNotContain("Chest", names);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteMuscle_CancelKeepsMuscle()
    {
        var page = await CreatePageAsync();
        try
        {
            var initialCount = await page.Locator("#muscle-grid .muscle-card").CountAsync();
            await MuscleCard(page, "Chest").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();

            await page.Locator("#edit-modal-delete-btn").ClickAsync();
            await Expect(page.Locator("#delete-confirm-backdrop")).ToBeVisibleAsync();
            await page.Locator("#delete-confirm-cancel").ClickAsync();

            await Expect(page.Locator("#delete-confirm-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator("#muscle-grid .muscle-card")).ToHaveCountAsync(initialCount);

            var names = await GetMuscleNamesAsync(page);
            Assert.Contains("Chest", names);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static async Task<List<string>> GetMuscleNamesAsync(IPage page)
    {
        var nameLocators = page.Locator("#muscle-grid .muscle-card__name");
        var count = await nameLocators.CountAsync();
        var names = new List<string>();

        for (var i = 0; i < count; i++)
        {
            names.Add((await nameLocators.Nth(i).TextContentAsync()) ?? string.Empty);
        }

        return names;
    }

    private static ILocator MuscleCard(IPage page, string name) =>
        page.Locator("#muscle-grid .muscle-card", new PageLocatorOptions { HasText = name }).First;

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
