using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[Collection("E2E")]
public class WorkoutsPageTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public WorkoutsPageTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {
        WebAppFixture.ResetExercises();
        WebAppFixture.ResetWorkouts();

        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return page;
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);

    private static async Task NavigateToWorkoutsAsync(IPage page)
    {
        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();
        await page.WaitForSelectorAsync(".workouts-page");
    }

    private async Task SeedExerciseAsync(IPage page, string name)
    {
        await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
        {
            DataObject = new { name, muscleIds = Array.Empty<string>() },
        });
    }

    private static async Task CreateWorkoutViaUIAsync(IPage page, string name, string exerciseName)
    {
        await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
        await page.FillAsync("#workout-name", name);
        await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
            .Filter(new() { HasText = exerciseName }).ClickAsync();
        await page.Locator("#workout-form .workout-form__submit").ClickAsync();
        await page.Locator(".workout-list__name").Filter(new() { HasText = name }).WaitForAsync();
    }

    // ──────────────────────────────────────────
    // Navigation & Page Loading
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task NavigateViaSidebar_ShowsWorkoutsPage()
    {
        var page = await CreatePageAsync();
        try
        {
            await NavigateToWorkoutsAsync(page);

            await Expect(page.Locator(".workouts-page__title")).ToHaveTextAsync("Workouts");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task DeepLink_ShowsWorkoutsPage()
    {
        WebAppFixture.ResetExercises();
        WebAppFixture.ResetWorkouts();

        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        try
        {
            await page.GotoAsync($"{_webApp.BaseUrl}/workouts");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Expect(page.Locator(".workouts-page__title")).ToHaveTextAsync("Workouts");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task WorkoutsLink_HasActiveState()
    {
        var page = await CreatePageAsync();
        try
        {
            await NavigateToWorkoutsAsync(page);

            var link = page.Locator(".sidebar__link[data-page='workouts']");
            await Expect(link).ToHaveClassAsync(new Regex("sidebar__link--active"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Empty State
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EmptyState_ShownWhenNoWorkouts()
    {
        var page = await CreatePageAsync();
        try
        {
            await NavigateToWorkoutsAsync(page);

            var empty = page.Locator("#workout-empty");
            await Expect(empty).ToBeVisibleAsync();
            await Expect(empty).ToContainTextAsync("No workouts yet. Create your first workout above!");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EmptyState_HiddenAfterCreatingWorkout()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await Expect(page.Locator("#workout-empty")).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Create Workout
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_SuccessfullyCreatesAndAppearsInList()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "Push Day");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Bench Press" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            var item = page.Locator(".workout-list__item");
            await Expect(item).ToBeVisibleAsync();
            await Expect(item.Locator(".workout-list__name")).ToHaveTextAsync("Push Day");
            await Expect(item.Locator(".workout-list__exercise-count")).ToContainTextAsync("1 exercise");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_EmptyName_ShowsRequiredError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            // Select an exercise but leave name empty
            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-error")).ToHaveTextAsync("Workout name is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_WhitespaceOnlyName_ShowsRequiredError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "   ");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-error")).ToHaveTextAsync("Workout name is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_NameTooLong_ShowsLengthError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            var longName = new string('A', 151);
            await page.Locator("#workout-name").EvaluateAsync(
                "(el, name) => { el.removeAttribute('maxlength'); el.value = name; }",
                longName);
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-error")).ToHaveTextAsync("Workout name must be 150 characters or fewer.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_DuplicateName_ShowsApiError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            // Try creating duplicate
            await page.FillAsync("#workout-name", "push day");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Bench Press" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-api-error")).ToHaveTextAsync("A workout with this name already exists.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_NoExercisesSelected_ShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "Leg Day");
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-error")).ToHaveTextAsync("At least one exercise is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_FormClearsAfterSuccess()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await Expect(page.Locator("#workout-name")).ToHaveValueAsync("");
            // Exercise toggles should be unchecked
            var toggle = page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Bench Press" });
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "false");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_SubmitButtonDisabledDuringSubmission()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            // Slow down the POST response to observe loading state
            await page.RouteAsync("**/api/workouts", async route =>
            {
                if (route.Request.Method == "POST")
                {
                    await Task.Delay(500);
                }
                await route.FallbackAsync();
            });

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "Leg Day");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            var submitBtn = page.Locator("#workout-form .workout-form__submit");
            await Expect(submitBtn).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(submitBtn).ToHaveTextAsync("Saving...");

            // Wait for completion
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.UnrouteAsync("**/api/workouts");
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task CreateWorkout_ServerError_ShowsApiError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "__MOCK_SERVER_ERROR");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            var apiError = page.Locator("#workout-api-error");
            await Expect(apiError).ToBeVisibleAsync();
            await Expect(apiError).ToHaveAttributeAsync("role", "alert");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Workout List
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task WorkoutList_ShowsAllWorkoutsInAlphabeticalOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await CreateWorkoutViaUIAsync(page, "Gamma Workout", "Bench Press");
            await CreateWorkoutViaUIAsync(page, "Alpha Workout", "Squat");
            await CreateWorkoutViaUIAsync(page, "Beta Workout", "Bench Press");

            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(3);
            await Expect(page.Locator(".workout-list__name").Nth(0)).ToHaveTextAsync("Alpha Workout");
            await Expect(page.Locator(".workout-list__name").Nth(1)).ToHaveTextAsync("Beta Workout");
            await Expect(page.Locator(".workout-list__name").Nth(2)).ToHaveTextAsync("Gamma Workout");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task WorkoutList_ShowsCorrectExerciseCount()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            // Create workout with 2 exercises
            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "Full Body");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Bench Press" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();
            await page.WaitForSelectorAsync(".workout-list__item");

            await Expect(page.Locator(".workout-list__exercise-count")).ToContainTextAsync("2 exercises");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task WorkoutList_ShowsSingularExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await Expect(page.Locator(".workout-list__exercise-count")).ToContainTextAsync("1 exercise");
            // Ensure it does NOT say "exercises" (plural)
            await Expect(page.Locator(".workout-list__exercise-count")).Not.ToContainTextAsync("exercises");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task WorkoutList_EachItemHasEditDeleteStartButtons()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            var item = page.Locator(".workout-list__item").First;
            await Expect(item.Locator(".workout-list__edit-btn")).ToBeVisibleAsync();
            await Expect(item.Locator(".workout-list__delete-btn")).ToBeVisibleAsync();
            await Expect(item.Locator(".workout-list__start-btn")).ToBeVisibleAsync();

            await Expect(item.Locator(".workout-list__edit-btn")).ToHaveAttributeAsync("aria-label", "Edit Push Day");
            await Expect(item.Locator(".workout-list__delete-btn")).ToHaveAttributeAsync("aria-label", "Delete Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Edit Workout
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_OpensModalWithCurrentData()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            var modal = page.Locator(".edit-modal");
            await Expect(modal).ToHaveAttributeAsync("role", "dialog");
            await Expect(page.Locator("#edit-workout-name")).ToHaveValueAsync("Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_CanUpdateName()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            await page.Locator("#edit-workout-name").FillAsync("Chest Day");
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();

            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
            await Expect(page.Locator(".workout-list__name").First).ToHaveTextAsync("Chest Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_CancelClosesModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            await page.Locator("#workout-edit-cancel").ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();

            // Name should be unchanged
            await Expect(page.Locator(".workout-list__name").First).ToHaveTextAsync("Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_EscapeClosesModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            await page.Keyboard.PressAsync("Escape");
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_BackdropClickClosesModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            var backdrop = page.Locator("#workout-edit-backdrop");
            await Expect(backdrop).ToBeVisibleAsync();

            // Click the backdrop edge (not the modal content)
            await backdrop.ClickAsync(new LocatorClickOptions { Position = new Position { X = 5, Y = 5 } });
            await Expect(backdrop).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_EmptyNameShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            await page.Locator("#edit-workout-name").FillAsync("");
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#edit-workout-error")).ToHaveTextAsync("Workout name is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_DuplicateNameShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");
            await CreateWorkoutViaUIAsync(page, "Leg Day", "Squat");

            // Edit "Leg Day" to "Push Day" (duplicate)
            await page.Locator(".workout-list__edit-btn").Nth(0).ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            // Check which workout we're editing; list is alphabetical: Leg Day, Push Day
            await page.Locator("#edit-workout-name").FillAsync("push day");
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#edit-workout-api-error")).ToHaveTextAsync("A workout with this name already exists.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task EditWorkout_SameNameAllowed()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            // Submit without changing name
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();

            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
            await Expect(page.Locator(".workout-list__name").First).ToHaveTextAsync("Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Delete Workout
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task DeleteWorkout_OpensConfirmationDialog()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();

            var backdrop = page.Locator("#workout-delete-backdrop");
            await Expect(backdrop).ToBeVisibleAsync();

            var modal = page.Locator(".delete-modal");
            await Expect(modal).ToHaveAttributeAsync("role", "alertdialog");

            var desc = page.Locator("#workout-delete-desc");
            await Expect(desc).ToContainTextAsync("Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task DeleteWorkout_ConfirmDeleteRemovesFromList()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-delete-backdrop")).ToBeVisibleAsync();

            await page.Locator("#workout-delete-confirm").ClickAsync();

            await Expect(page.Locator("#workout-delete-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task DeleteWorkout_CancelDoesNotDelete()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-delete-backdrop")).ToBeVisibleAsync();

            await page.Locator("#workout-delete-cancel").ClickAsync();

            await Expect(page.Locator("#workout-delete-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task DeleteWorkout_EscapeDoesNotDelete()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-delete-backdrop")).ToBeVisibleAsync();

            await page.Keyboard.PressAsync("Escape");

            await Expect(page.Locator("#workout-delete-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task DeleteWorkout_LastWorkoutShowsEmptyState()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");
            await Expect(page.Locator("#workout-empty")).ToBeHiddenAsync();

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();
            await page.Locator("#workout-delete-confirm").ClickAsync();

            await Expect(page.Locator("#workout-delete-backdrop")).Not.ToBeVisibleAsync();
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(0);
            await Expect(page.Locator("#workout-empty")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Start Workout
    // ──────────────────────────────────────────

    [Fact(Skip = "Playwright E2E - disabled")]
    public async Task StartWorkout_NavigatesToActiveSession()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            var startBtn = page.Locator(".workout-list__start-btn").First;
            await startBtn.ClickAsync();

            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
