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
            // Slow down the POST /api/exercises response so we can observe the loading state
            await page.RouteAsync("**/api/exercises", async route =>
            {
                if (route.Request.Method == "POST")
                {
                    await Task.Delay(500);
                }
                await route.FallbackAsync();
            });

            await page.Locator("#exercise-name").FillAsync("Pull Ups");
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator(".exercise-form__submit")).ToHaveAttributeAsync("aria-disabled", "true");

            // Wait for the request to complete and list to update
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.UnrouteAsync("**/api/exercises");
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

    // === User Story 2: Assign Targeted Muscles ===

    [Fact]
    public async Task MuscleToggles_AllElevenDisplayed()
    {
        var page = await CreatePageAsync();
        try
        {
            await Expect(page.Locator(".muscle-toggle")).ToHaveCountAsync(11);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task MuscleToggle_ClickTogglesActiveState()
    {
        var page = await CreatePageAsync();
        try
        {
            var toggle = page.Locator(".muscle-toggle").First;
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "false");
            await toggle.ClickAsync();
            await Expect(toggle).ToHaveClassAsync(new Regex("muscle-toggle--active"));
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "true");

            // Click again to deselect
            await toggle.ClickAsync();
            await Expect(toggle).Not.ToHaveClassAsync(new Regex("muscle-toggle--active"));
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "false");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateExerciseWithMuscles_ShowsMuscleChipsInList()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Bench Press");

            // Select Chest muscle
            var chestToggle = page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Chest" });
            await chestToggle.ClickAsync();

            await page.Locator(".exercise-form__submit").ClickAsync();

            var item = page.Locator(".exercise-list__item");
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__muscle-chip")).ToHaveTextAsync(["Chest"]);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateExerciseWithoutMuscles_Succeeds()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Running");
            await page.Locator(".exercise-form__submit").ClickAsync();

            var item = page.Locator(".exercise-list__item");
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__name")).ToHaveTextAsync("Running");
            await Expect(item.Locator(".exercise-list__muscle-chip")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateExerciseWithMultipleMuscles_DisplaysAllChips()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Deadlift");

            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Hamstrings" }).ClickAsync();
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Glutes" }).ClickAsync();

            await page.Locator(".exercise-form__submit").ClickAsync();

            var item = page.Locator(".exercise-list__item");
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__muscle-chip")).ToHaveCountAsync(3);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // === User Story 3: View Exercise List ===

    [Fact]
    public async Task ExerciseList_MultipleExercisesAllAppear()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Alpha");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Beta");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            await page.Locator("#exercise-name").FillAsync("Gamma");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(3);

            await Expect(page.Locator(".exercise-list__name").Nth(0)).ToHaveTextAsync("Alpha");
            await Expect(page.Locator(".exercise-list__name").Nth(1)).ToHaveTextAsync("Beta");
            await Expect(page.Locator(".exercise-list__name").Nth(2)).ToHaveTextAsync("Gamma");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExerciseList_ExerciseWithMusclesShowsChips()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Bench Press");
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Chest" }).ClickAsync();
            await page.Locator(".exercise-form__submit").ClickAsync();

            var item = page.Locator(".exercise-list__item").First;
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__muscles")).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__muscle-chip")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExerciseList_ExerciseWithoutMusclesShowsNameOnly()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Running");
            await page.Locator(".exercise-form__submit").ClickAsync();

            var item = page.Locator(".exercise-list__item").First;
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".exercise-list__name")).ToHaveTextAsync("Running");
            await Expect(item.Locator(".exercise-list__muscles")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExerciseList_EmptyStateMessage()
    {
        var page = await CreatePageAsync();
        try
        {
            await Expect(page.Locator("#exercise-empty")).ToBeVisibleAsync();
            await Expect(page.Locator("#exercise-empty")).ToContainTextAsync("No exercises yet");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ExerciseList_HeadingVisible()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await Expect(page.Locator(".exercise-list__heading")).ToHaveTextAsync("Your Exercises");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // === User Story 4: Edit an Existing Exercise ===

    [Fact]
    public async Task EditExercise_FormPopulatesWithExistingData()
    {
        var page = await CreatePageAsync();
        try
        {
            // Create an exercise with muscles
            await page.Locator("#exercise-name").FillAsync("Bench Press");
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Chest" }).ClickAsync();
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            // Click edit
            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();

            // Verify form populated
            await Expect(page.Locator("#exercise-name")).ToHaveValueAsync("Bench Press");
            var chestToggle = page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Chest" });
            await Expect(chestToggle).ToHaveAttributeAsync("aria-checked", "true");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMode_ShowsUpdateButtonAndCancelButton()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();

            await Expect(page.Locator(".exercise-form__submit")).ToHaveTextAsync("Update Exercise");
            await Expect(page.Locator("#exercise-cancel")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditExercise_UpdateNameAppearsInList()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Sqat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();

            // Should update in list, not duplicate
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
            await Expect(page.Locator(".exercise-list__name").First).ToHaveTextAsync("Squat");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditExercise_UpdateMusclesAppearsInList()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Deadlift");
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            // Add Hamstrings, remove Back
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
            await page.Locator(".muscle-toggle", new PageLocatorOptions { HasText = "Hamstrings" }).ClickAsync();
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
            await Expect(page.Locator(".exercise-list__muscle-chip")).ToHaveTextAsync(["Hamstrings"]);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMode_CancelReturnToCreateMode()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator(".exercise-form__submit")).ToHaveTextAsync("Update Exercise");

            await page.Locator("#exercise-cancel").ClickAsync();

            await Expect(page.Locator(".exercise-form__submit")).ToHaveTextAsync("Add Exercise");
            await Expect(page.Locator("#exercise-name")).ToHaveValueAsync("");
            await Expect(page.Locator("#exercise-cancel")).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMode_ClearNameShowsRequiredError()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await page.Locator("#exercise-name").FillAsync("");
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-error")).ToHaveTextAsync("Exercise name is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMode_DuplicateNameShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            // Create two exercises
            await page.Locator("#exercise-name").FillAsync("Alpha");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Beta");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            // Edit Beta to Alpha (duplicate)
            await page.Locator(".exercise-list__edit-btn").Nth(1).ClickAsync();
            await page.Locator("#exercise-name").FillAsync("alpha");
            await page.Locator(".exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-api-error")).ToHaveTextAsync("An exercise with this name already exists.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMode_OwnNameAllowed()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squat");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            // Submit without changing name
            await page.Locator(".exercise-form__submit").ClickAsync();

            // Should succeed - back to create mode
            await Expect(page.Locator(".exercise-form__submit")).ToHaveTextAsync("Add Exercise");
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditMode_SwitchToAnotherExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            // Create two exercises
            await page.Locator("#exercise-name").FillAsync("Alpha");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Beta");
            await page.Locator(".exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            // Edit first exercise
            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#exercise-name")).ToHaveValueAsync("Alpha");

            // Switch to editing second exercise
            await page.Locator(".exercise-list__edit-btn").Nth(1).ClickAsync();
            await Expect(page.Locator("#exercise-name")).ToHaveValueAsync("Beta");
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
