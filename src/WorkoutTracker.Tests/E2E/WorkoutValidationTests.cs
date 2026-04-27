using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[Collection("E2E")]
public class WorkoutValidationTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public WorkoutValidationTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {
        WebAppFixture.ResetExercises();
        WebAppFixture.SeedDefaultExercises();
        WebAppFixture.ResetWorkouts();
        WebAppFixture.SeedDefaultWorkouts();
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 768 },
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return page;
    }

    private async Task SeedExerciseAsync(IPage page, string name)
    {
        await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
        {
            DataObject = new { name, muscleIds = Array.Empty<string>() },
        });
    }

    private static async Task NavigateToWorkoutsAsync(IPage page)
    {
        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();
        await page.WaitForSelectorAsync(".workouts-page");
    }

    private static async Task CreateWorkoutViaUIAsync(IPage page, string name, string exerciseName)
    {
        await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
        await page.FillAsync("#workout-name", name);
        await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
            .Filter(new() { HasText = exerciseName }).ClickAsync();
        await page.Locator("#workout-form .workout-form__submit").ClickAsync();
        await page.Locator(".workout-list__item").Filter(new() { HasText = name }).WaitForAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    // ──────────────────────────────────────────
    // Name Validation
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_EmptyName_ShowsRequiredError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-error")).ToHaveTextAsync("Workout name is required.");
            await Expect(page.Locator("#workout-name")).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(page.Locator("#workout-name")).ToHaveClassAsync(new Regex("workout-form__input--error"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
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
            await Expect(page.Locator("#workout-name")).ToHaveAttributeAsync("aria-invalid", "true");
            await Expect(page.Locator("#workout-name")).ToHaveClassAsync(new Regex("workout-form__input--error"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateWorkout_MaxLengthName_Succeeds()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            var maxName = new string('A', 150);
            await CreateWorkoutViaUIAsync(page, maxName, "Squat");

            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateWorkout_ExceedsMaxLength_ShowsError()
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

    [Fact]
    public async Task CreateWorkout_SpecialCharactersInName_Succeeds()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            var specialName = "Workout & Routine #1 (Morning)";
            await CreateWorkoutViaUIAsync(page, specialName, "Squat");

            await Expect(page.Locator(".workout-list__name").Filter(new() { HasText = specialName })).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateWorkout_UnicodeCharactersInName_Succeeds()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            var unicodeName = "Workout 💪🔥";
            await CreateWorkoutViaUIAsync(page, unicodeName, "Squat");

            await Expect(page.Locator(".workout-list__name").Filter(new() { HasText = unicodeName })).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Exercise Selection Validation
    // ──────────────────────────────────────────

    [Fact]
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

    [Fact]
    public async Task CreateWorkout_ToggleExerciseTwice_DeselectsIt()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            var toggle = page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" });

            await toggle.ClickAsync();
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "true");
            await toggle.ClickAsync();
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "false");

            await page.FillAsync("#workout-name", "Leg Day");
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-error")).ToHaveTextAsync("At least one exercise is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditWorkout_RemoveAllExercises_ShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Leg Day", "Squat");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await page.WaitForSelectorAsync("#workout-edit-backdrop");

            var editToggle = page.Locator("#workout-edit-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" });
            await editToggle.ClickAsync();
            await Expect(editToggle).ToHaveAttributeAsync("aria-checked", "false");

            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#edit-workout-error")).ToHaveTextAsync("At least one exercise is required.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Duplicate Name
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_DuplicateName_ShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Morning Routine", "Bench Press");

            await page.FillAsync("#workout-name", "Morning Routine");
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

    [Fact]
    public async Task EditWorkout_SameNameAllowed()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Leg Day", "Squat");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await page.WaitForSelectorAsync("#workout-edit-backdrop");
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator(".workout-list__name").First).ToHaveTextAsync("Leg Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task EditWorkout_DuplicateNameOfOther_ShowsError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Leg Day", "Squat");
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            // Workouts sorted alphabetically: Leg Day (0), Push Day (1)
            await page.Locator(".workout-list__edit-btn").Nth(1).ClickAsync();
            await page.WaitForSelectorAsync("#workout-edit-backdrop");
            await page.Locator("#edit-workout-name").FillAsync("Leg Day");
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#edit-workout-api-error")).ToHaveTextAsync("A workout with this name already exists.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Server Error
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_ServerError_ShowsApiError()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "Test Workout");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();

            await page.RouteAsync("**/api/workouts", route => route.FulfillAsync(new()
            {
                Status = 500,
                ContentType = "application/json",
                Body = "{\"error\":\"An unexpected error occurred. Please try again.\"}",
            }));

            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-api-error")).ToBeVisibleAsync();
            await Expect(page.Locator("#workout-api-error")).ToHaveTextAsync("An unexpected error occurred. Please try again.");
        }
        finally
        {
            await page.UnrouteAsync("**/api/workouts");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateWorkout_ServerError_RetainsFormData()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "My Workout");
            var toggle = page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" });
            await toggle.ClickAsync();

            await page.RouteAsync("**/api/workouts", route => route.FulfillAsync(new()
            {
                Status = 500,
                ContentType = "application/json",
                Body = "{\"error\":\"An unexpected error occurred. Please try again.\"}",
            }));

            await page.Locator("#workout-form .workout-form__submit").ClickAsync();
            await Expect(page.Locator("#workout-api-error")).ToBeVisibleAsync();

            await Expect(page.Locator("#workout-name")).ToHaveValueAsync("My Workout");
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "true");
        }
        finally
        {
            await page.UnrouteAsync("**/api/workouts");
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Whitespace Trimming
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_LeadingTrailingSpaces_TrimmedOnSave()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);

            await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
            await page.FillAsync("#workout-name", "  Morning Routine  ");
            await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Squat" }).ClickAsync();
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await page.Locator(".workout-list__item").WaitForAsync();
            await Expect(page.Locator(".workout-list__name").First).ToHaveTextAsync("Morning Routine");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Delete
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteWorkout_WorkoutRemovedFromList()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Leg Day", "Squat");

            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();
            await page.WaitForSelectorAsync("#workout-delete-backdrop");
            await page.Locator("#workout-delete-confirm").ClickAsync();

            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Form Reset
    // ──────────────────────────────────────────

    [Fact]
    public async Task CreateWorkout_SuccessResetsForm()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await Expect(page.Locator("#workout-name")).ToHaveValueAsync("");
            var toggle = page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
                .Filter(new() { HasText = "Bench Press" });
            await Expect(toggle).ToHaveAttributeAsync("aria-checked", "false");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
