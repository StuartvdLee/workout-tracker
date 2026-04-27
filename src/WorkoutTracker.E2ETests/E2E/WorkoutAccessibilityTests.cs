using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class WorkoutAccessibilityTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public WorkoutAccessibilityTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync(int width = 1024, int height = 768)
    {
        WebAppFixture.ResetExercises();
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
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
        // On mobile viewports the sidebar is hidden; open it via the topbar toggle
        var toggle = page.Locator(".topbar__toggle");
        if (await toggle.IsVisibleAsync())
        {
            await toggle.ClickAsync();
        }

        await page.Locator(".sidebar__link[data-page='workouts']").ClickAsync();
        await page.WaitForSelectorAsync(".workouts-page");
    }

    private static async Task CreateWorkoutViaUIAsync(IPage page, string name, string exerciseName)
    {
        await page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        await page.FillAsync("#workout-name", name);
        await page.Locator("#workout-exercise-select").SelectOptionAsync(new SelectOptionValue { Label = exerciseName });
        await page.Locator("#workout-form .workout-form__submit").ClickAsync();
        await page.Locator(".workout-list__name").Filter(new() { HasText = name }).WaitForAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);

    // ──────────────────────────────────────────
    // T050: ARIA & Keyboard Accessibility
    // ──────────────────────────────────────────

    [Fact]
    public async Task Aria_ExerciseSelectAndLabelArePresent()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });

            var options = page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])");
            var count = await options.CountAsync();
            Assert.True(count > 0, "Expected at least one exercise option in the select");

            await Expect(page.Locator("#workout-exercise-select")).ToBeVisibleAsync();
            await Expect(page.Locator("label[for='workout-exercise-select']")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_ExerciseSelect_SelectAndRemoveExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });

            // Select exercise via dropdown - adds to selected list
            await page.Locator("#workout-exercise-select").SelectOptionAsync(new SelectOptionValue { Label = "Bench Press" });

            // Verify the exercise appears in the selected list
            await Expect(page.Locator("#workout-selected-section")).ToBeVisibleAsync();
            await Expect(page.Locator("#workout-selected-list .workout-selected__name")).ToContainTextAsync("Bench Press");

            // Remove exercise via the remove button - should hide the selected section
            await page.Locator(".workout-form__remove-btn[aria-label='Remove Bench Press']").ClickAsync();
            await Expect(page.Locator("#workout-selected-section")).ToBeHiddenAsync();
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
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await Expect(page.Locator("#workout-exercise-select")).ToBeVisibleAsync();

            // Submit empty form to trigger validation error
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            var errorEl = page.Locator("#workout-error");
            await Expect(errorEl).ToBeVisibleAsync();
            await Expect(errorEl).ToHaveAttributeAsync("role", "alert");
            await Expect(errorEl).ToHaveAttributeAsync("aria-live", "polite");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_EditModalHasDialogRole()
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
            await Expect(modal).ToHaveAttributeAsync("aria-modal", "true");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_DeleteModalHasAlertdialogRole()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__delete-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-delete-backdrop")).ToBeVisibleAsync();

            var modal = page.Locator(".delete-modal");
            await Expect(modal).ToHaveAttributeAsync("role", "alertdialog");
            await Expect(modal).ToHaveAttributeAsync("aria-modal", "true");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_InputShowsInvalidState()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await Expect(page.Locator("#workout-exercise-select")).ToBeVisibleAsync();

            // Submit empty form
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();

            await Expect(page.Locator("#workout-name")).ToHaveAttributeAsync("aria-invalid", "true");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Aria_PageHasH1Heading()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            var heading = page.Locator("h1.workouts-page__title");
            await Expect(heading).ToBeVisibleAsync();
            await Expect(heading).ToHaveTextAsync("Workouts");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Keyboard_TabNavigatesFormFields()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });

            // Focus the workout name input
            await page.Locator("#workout-name").FocusAsync();
            await Expect(page.Locator("#workout-name")).ToBeFocusedAsync();

            // Tab to exercise select dropdown
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#workout-exercise-select")).ToBeFocusedAsync();
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
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });

            await page.FillAsync("#workout-name", "Keyboard Workout");
            await page.Locator("#workout-exercise-select").SelectOptionAsync(new SelectOptionValue { Label = "Bench Press" });

            // Press Enter on the submit button
            await page.Locator("#workout-form .workout-form__submit").FocusAsync();
            await page.Keyboard.PressAsync("Enter");

            await page.Locator(".workout-list__name").Filter(new() { HasText = "Keyboard Workout" }).WaitForAsync();
            await Expect(page.Locator(".workout-list__item")).ToHaveCountAsync(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Keyboard_EscapeClosesEditModal()
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

    [Fact]
    public async Task Keyboard_EscapeClosesDeleteModal()
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
            await Expect(page.Locator("#workout-delete-backdrop")).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // T051: Mobile Responsive
    // ──────────────────────────────────────────

    [Fact]
    public async Task Mobile_WorkoutsPageRendersAt375px()
    {
        var page = await CreatePageAsync(375, 667);
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);

            await Expect(page.Locator(".workouts-page")).ToBeVisibleAsync();
            await Expect(page.Locator(".workouts-page__title")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_FormInputsAreFullWidth()
    {
        var page = await CreatePageAsync(375, 667);
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await Expect(page.Locator("#workout-exercise-select")).ToBeVisibleAsync();

            var inputBox = await page.Locator("#workout-name").BoundingBoxAsync();
            Assert.NotNull(inputBox);
            Assert.True(inputBox.Width >= 280,
                $"Workout name input width {inputBox.Width}px is less than 280px on mobile viewport");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_WorkoutListItemsReadable()
    {
        var page = await CreatePageAsync(375, 667);
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            var item = page.Locator(".workout-list__item").First;
            await Expect(item).ToBeVisibleAsync();

            var nameEl = page.Locator(".workout-list__name").First;
            await Expect(nameEl).ToBeVisibleAsync();
            await Expect(nameEl).ToHaveTextAsync("Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_ActionButtonsAreUsable()
    {
        var page = await CreatePageAsync(375, 667);
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            var item = page.Locator(".workout-list__item").First;

            var editBox = await item.Locator(".workout-list__edit-btn").BoundingBoxAsync();
            Assert.NotNull(editBox);
            Assert.True(editBox.Height >= 44, $"Edit button height {editBox.Height}px < 44px");

            var deleteBox = await item.Locator(".workout-list__delete-btn").BoundingBoxAsync();
            Assert.NotNull(deleteBox);
            Assert.True(deleteBox.Height >= 44, $"Delete button height {deleteBox.Height}px < 44px");

            var startBox = await item.Locator(".workout-list__start-btn").BoundingBoxAsync();
            Assert.NotNull(startBox);
            Assert.True(startBox.Height >= 44, $"Start button height {startBox.Height}px < 44px");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task Mobile_EditModalFitsViewport()
    {
        var page = await CreatePageAsync(375, 667);
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();

            var modalBox = await page.Locator(".edit-modal").BoundingBoxAsync();
            Assert.NotNull(modalBox);
            Assert.True(modalBox.X >= 0, $"Modal left edge {modalBox.X}px is outside viewport");
            Assert.True(modalBox.Y >= 0, $"Modal top edge {modalBox.Y}px is outside viewport");
            Assert.True(modalBox.X + modalBox.Width <= 375,
                $"Modal right edge {modalBox.X + modalBox.Width}px exceeds 375px viewport");
            Assert.True(modalBox.Y + modalBox.Height <= 667,
                $"Modal bottom edge {modalBox.Y + modalBox.Height}px exceeds 667px viewport");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
