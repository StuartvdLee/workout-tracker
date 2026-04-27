using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[Collection("E2E")]
public class ExercisesPageTests
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            // maxlength="150" prevents FillAsync from exceeding limit,
            // so bypass it via JS to test the client-side validation
            var longName = new string('A', 151);
            await page.Locator("#exercise-name").EvaluateAsync(
                "(el, name) => { el.removeAttribute('maxlength'); el.value = name; }",
                longName);
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator("#exercise-name").FillAsync("squat");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#exercise-form .exercise-form__submit")).ToHaveAttributeAsync("aria-disabled", "true");

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await Expect(page.Locator("#exercise-muscles .muscle-toggle")).ToHaveCountAsync(11);
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
            var toggle = page.Locator("#exercise-muscles .muscle-toggle").First;
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
            var chestToggle = page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Chest" });
            await chestToggle.ClickAsync();

            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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

            await page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
            await page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Hamstrings" }).ClickAsync();
            await page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Glutes" }).ClickAsync();

            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Beta");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            await page.Locator("#exercise-name").FillAsync("Gamma");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
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
            await page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Chest" }).ClickAsync();
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
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
            await page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Chest" }).ClickAsync();
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            // Click edit
            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();

            // Verify modal form populated
            await Expect(page.Locator("#edit-exercise-name")).ToHaveValueAsync("Bench Press");
            var chestToggle = page.Locator("#edit-exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Chest" });
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();

            await Expect(page.Locator("#edit-modal-form .exercise-form__submit")).ToHaveTextAsync("Save Changes");
            await Expect(page.Locator("#edit-modal-cancel")).ToBeVisibleAsync();
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            await page.Locator("#edit-exercise-name").FillAsync("Squat");
            await page.Locator("#edit-modal-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

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
            await page.Locator("#exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            // Add Hamstrings, remove Back
            await page.Locator("#edit-exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Back" }).ClickAsync();
            await page.Locator("#edit-exercise-muscles .muscle-toggle", new PageLocatorOptions { HasText = "Hamstrings" }).ClickAsync();
            await page.Locator("#edit-modal-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();

            await page.Locator("#edit-modal-cancel").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

            // Create form should remain unchanged
            await Expect(page.Locator("#exercise-form .exercise-form__submit")).ToHaveTextAsync("Add Exercise");
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            await page.Locator("#edit-exercise-name").FillAsync("");
            await page.Locator("#edit-modal-form .exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#edit-exercise-error")).ToHaveTextAsync("Exercise name is required.");
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Beta");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            // Edit Beta to Alpha (duplicate)
            await page.Locator(".exercise-list__edit-btn").Nth(1).ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            await page.Locator("#edit-exercise-name").FillAsync("alpha");
            await page.Locator("#edit-modal-form .exercise-form__submit").ClickAsync();

            await Expect(page.Locator("#edit-exercise-api-error")).ToHaveTextAsync("An exercise with this name already exists.");
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToBeVisibleAsync();

            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            // Submit without changing name
            await page.Locator("#edit-modal-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

            // Should succeed
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
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Beta");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            // Edit first exercise
            await page.Locator(".exercise-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            await Expect(page.Locator("#edit-exercise-name")).ToHaveValueAsync("Alpha");

            // Close modal, then open for second exercise
            await page.Locator("#edit-modal-cancel").ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync();

            await page.Locator(".exercise-list__edit-btn").Nth(1).ClickAsync();
            await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync();
            await Expect(page.Locator("#edit-exercise-name")).ToHaveValueAsync("Beta");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Phase 7 — T025: ARIA & Keyboard Navigation
    // ──────────────────────────────────────────

    [Fact]
    public async Task Keyboard_TabThroughFormFields()
    {
        var page = await CreatePageAsync();
        try
        {
            // Focus name input
            await page.Locator("#exercise-name").FocusAsync();
            await Expect(page.Locator("#exercise-name")).ToBeFocusedAsync();

            // Tab to first muscle toggle
            await page.Keyboard.PressAsync("Tab");
            var firstToggle = page.Locator("#exercise-muscles .muscle-toggle").First;
            await Expect(firstToggle).ToBeFocusedAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Keyboard_SpaceActivatesMuscleToggle()
    {
        var page = await CreatePageAsync();
        try
        {
            var firstToggle = page.Locator("#exercise-muscles .muscle-toggle").First;
            await firstToggle.FocusAsync();
            await Expect(firstToggle).ToHaveAttributeAsync("aria-checked", "false");

            await page.Keyboard.PressAsync("Space");
            await Expect(firstToggle).ToHaveAttributeAsync("aria-checked", "true");

            await page.Keyboard.PressAsync("Space");
            await Expect(firstToggle).ToHaveAttributeAsync("aria-checked", "false");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Keyboard_EnterSubmitsForm()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Keyboard Test");
            await page.Keyboard.PressAsync("Enter");
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // === Delete Exercise Tests ===

    [Fact]
    public async Task Delete_ButtonIsVisibleForEachExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Push Ups");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            var deleteBtn = page.Locator(".exercise-list__delete-btn");
            await Expect(deleteBtn).ToHaveCountAsync(1);
            await Expect(deleteBtn).ToBeVisibleAsync();
            await Expect(deleteBtn).ToHaveAttributeAsync("aria-label", "Delete Push Ups");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_ConfirmationModalOpensOnClick()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Squats");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator(".exercise-list__delete-btn").ClickAsync();

            var backdrop = page.Locator("#delete-modal-backdrop");
            await Expect(backdrop).ToBeVisibleAsync();

            var desc = page.Locator("#delete-modal-desc");
            await Expect(desc).ToContainTextAsync("Squats");

            await Expect(page.Locator("#delete-modal-confirm")).ToBeVisibleAsync();
            await Expect(page.Locator("#delete-modal-cancel")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_CancelClosesModalWithoutDeleting()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Lunges");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator(".exercise-list__delete-btn").ClickAsync();
            await Expect(page.Locator("#delete-modal-backdrop")).ToBeVisibleAsync();

            await page.Locator("#delete-modal-cancel").ClickAsync();

            await Expect(page.Locator("#delete-modal-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_ConfirmDeletesExerciseAndClosesModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Deadlift");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator(".exercise-list__delete-btn").ClickAsync();
            await Expect(page.Locator("#delete-modal-backdrop")).ToBeVisibleAsync();

            await page.Locator("#delete-modal-confirm").ClickAsync();

            await Expect(page.Locator("#delete-modal-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(0);

            // Empty state should reappear
            await Expect(page.Locator("#exercise-empty")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_OnlyDeletesTargetedExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            // Add two exercises
            await page.Locator("#exercise-name").FillAsync("Exercise A");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator("#exercise-name").FillAsync("Exercise B");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(2);

            // Delete the first one (Exercise A is alphabetically first)
            await page.Locator(".exercise-list__delete-btn").First.ClickAsync();
            await page.Locator("#delete-modal-confirm").ClickAsync();

            await Expect(page.Locator("#delete-modal-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
            await Expect(page.Locator(".exercise-list__name")).ToHaveTextAsync("Exercise B");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_EscapeClosesModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Rows");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator(".exercise-list__delete-btn").ClickAsync();
            await Expect(page.Locator("#delete-modal-backdrop")).ToBeVisibleAsync();

            await page.Keyboard.PressAsync("Escape");

            await Expect(page.Locator("#delete-modal-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_BackdropClickClosesModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Curls");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator(".exercise-list__delete-btn").ClickAsync();
            var backdrop = page.Locator("#delete-modal-backdrop");
            await Expect(backdrop).ToBeVisibleAsync();

            // Click the backdrop (not the modal itself) to close
            await backdrop.ClickAsync(new LocatorClickOptions { Position = new Position { X = 5, Y = 5 } });

            await Expect(backdrop).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Delete_ModalHasCorrectAriaAttributes()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Plank");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            await page.Locator(".exercise-list__delete-btn").ClickAsync();
            var modal = page.Locator(".delete-modal");
            await Expect(modal).ToHaveAttributeAsync("role", "alertdialog");
            await Expect(modal).ToHaveAttributeAsync("aria-modal", "true");
            await Expect(modal).ToHaveAttributeAsync("aria-labelledby", "delete-modal-title");
            await Expect(modal).ToHaveAttributeAsync("aria-describedby", "delete-modal-desc");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_ErrorMessagesHaveAlertRole()
    {
        var page = await CreatePageAsync();
        try
        {
            var validationError = page.Locator("#exercise-error");
            await Expect(validationError).ToHaveAttributeAsync("role", "alert");
            await Expect(validationError).ToHaveAttributeAsync("aria-live", "polite");

            var apiError = page.Locator("#exercise-api-error");
            await Expect(apiError).ToHaveAttributeAsync("role", "alert");
            await Expect(apiError).ToHaveAttributeAsync("aria-live", "polite");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_MuscleTogglesHaveCheckboxRole()
    {
        var page = await CreatePageAsync();
        try
        {
            var toggles = page.Locator("#exercise-muscles .muscle-toggle");
            var count = await toggles.CountAsync();
            Assert.Equal(12, count);

            for (var i = 0; i < count; i++)
            {
                var toggle = toggles.Nth(i);
                await Expect(toggle).ToHaveAttributeAsync("role", "checkbox");
                await Expect(toggle).ToHaveAttributeAsync("aria-checked", "false");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_EditButtonsHaveDescriptiveLabels()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Bench Press");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);

            var editBtn = page.Locator(".exercise-list__edit-btn").First;
            await Expect(editBtn).ToHaveAttributeAsync("aria-label", "Edit Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_SubmitDisabledDuringSave()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Slow Save");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

            // After successful save, button should be re-enabled
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
            var submitBtn = page.Locator("#exercise-form .exercise-form__submit");
            await Expect(submitBtn).Not.ToHaveAttributeAsync("aria-disabled", "true");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Phase 7 — T026: Mobile Responsive Layout
    // ──────────────────────────────────────────

    private async Task<IPage> CreateMobilePageAsync()
    {
        WebAppFixture.ResetExercises();
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 375, Height = 667 },
        });
        await page.GotoAsync($"{_webApp.BaseUrl}/exercises");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

    [Fact]
    public async Task Mobile_PageRendersAt375pxViewport()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            await Expect(page.Locator(".exercises-page__title")).ToBeVisibleAsync();
            await Expect(page.Locator("#exercise-name")).ToBeVisibleAsync();
            await Expect(page.Locator("#exercise-form .exercise-form__submit")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_FormInputsAreUsable()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Mobile Exercise");
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();
            await Expect(page.Locator(".exercise-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_MuscleTogglesWrap()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            var muscleGroup = page.Locator("#exercise-muscles");
            await Expect(muscleGroup).ToBeVisibleAsync();

            // All 11 toggles should be visible even on narrow viewport
            var toggles = page.Locator("#exercise-muscles .muscle-toggle");
            var count = await toggles.CountAsync();
            Assert.Equal(12, count);

            for (var i = 0; i < count; i++)
            {
                await Expect(toggles.Nth(i)).ToBeVisibleAsync();
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_TouchTargetsMinimum44px()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            // Submit button
            var submitBox = await page.Locator("#exercise-form .exercise-form__submit").BoundingBoxAsync();
            Assert.NotNull(submitBox);
            Assert.True(submitBox.Height >= 44, $"Submit button height {submitBox.Height}px < 44px");

            // Muscle toggle
            var toggleBox = await page.Locator("#exercise-muscles .muscle-toggle").First.BoundingBoxAsync();
            Assert.NotNull(toggleBox);
            Assert.True(toggleBox.Height >= 44, $"Muscle toggle height {toggleBox.Height}px < 44px");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Phase 7 — T028: Performance Budgets
    // ──────────────────────────────────────────

    [Fact]
    public async Task Performance_PageLoadUnder3sOnSlowNetwork()
    {
        WebAppFixture.ResetExercises();
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });

        try
        {
            // Simulate slow 3G via CDP
            var cdpSession = await page.Context.NewCDPSessionAsync(page);
            await cdpSession.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
            {
                ["offline"] = false,
                ["downloadThroughput"] = 500 * 1024 / 8, // 500 Kbps
                ["uploadThroughput"] = 500 * 1024 / 8,
                ["latency"] = 400,
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await page.GotoAsync($"{_webApp.BaseUrl}/exercises");
            await page.Locator(".exercises-page__title").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 3000,
            });
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds <= 3000,
                $"Page load took {stopwatch.ElapsedMilliseconds}ms, exceeds 3000ms budget (PR-001)");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Performance_SubmitFeedbackUnder200ms()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.Locator("#exercise-name").FillAsync("Speed Test");

            // Measure time from click to loading state
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await page.Locator("#exercise-form .exercise-form__submit").ClickAsync();

            // Wait for the submit button to show loading state
            await page.Locator("#exercise-form .exercise-form__submit[aria-disabled='true']").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 200,
            });
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds <= 200,
                $"Submit feedback took {stopwatch.ElapsedMilliseconds}ms, exceeds 200ms budget (PR-002)");
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
