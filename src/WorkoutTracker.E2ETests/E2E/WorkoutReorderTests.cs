using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class WorkoutReorderTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public WorkoutReorderTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private static async Task DragToEndAsync(IPage page, string listSelector, int fromNthChild, int toNthChild)
    {
        // Get the target's bounding box so we can drop at 75% of its height (reliably below
        // the midpoint), ensuring the dragged item is inserted AFTER the target.
        var targetLoc = page.Locator($"{listSelector} li:nth-child({toNthChild})");
        var box = await targetLoc.BoundingBoxAsync() ?? throw new InvalidOperationException("Target element not found");
        await page.DragAndDropAsync(
            $"{listSelector} li:nth-child({fromNthChild})",
            $"{listSelector} li:nth-child({toNthChild})",
            new() { TargetPosition = new() { X = box.Width / 2, Y = (float)(box.Height * 0.75) } });
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

    private static async Task AddExerciseToFormAsync(IPage page, string exerciseName, string selectId = "workout-exercise-select")
    {
        await page.Locator($"#{selectId} option:not([disabled]):not([value=''])").First.WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        await page.Locator($"#{selectId}").SelectOptionAsync(new SelectOptionValue { Label = exerciseName });
    }

    // ──────────────────────────────────────────
    // US1: Reorder while creating a workout
    // ──────────────────────────────────────────

    /// <summary>
    /// T028 (US1): Adding multiple exercises shows drag handles, and dragging the first
    /// exercise to the last position persists the new order when the workout is saved.
    /// </summary>
    [Fact]
    public async Task CreateWorkout_DragReorder_PersistsOrderOnSave()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await SeedExerciseAsync(page, "Deadlift");

            await NavigateToWorkoutsAsync(page);

            // Add all 3 exercises
            await AddExerciseToFormAsync(page, "Bench Press");
            await AddExerciseToFormAsync(page, "Squat");
            await AddExerciseToFormAsync(page, "Deadlift");

            // Verify 3 items are visible in the selected list
            await page.WaitForSelectorAsync("#workout-selected-list li:nth-child(3)");
            var items = page.Locator("#workout-selected-list li");
            Assert.Equal(3, await items.CountAsync());

            // Verify drag handles appear (list has ≥ 2 items)
            var handles = page.Locator("#workout-selected-list .workout-selected__drag-handle");
            Assert.Equal(3, await handles.CountAsync());

            // Drag the first item (Bench Press) to the last position
            await DragToEndAsync(page, "#workout-selected-list", 1, 3);

            // Wait for re-render: Bench Press should now be last
            await page.WaitForFunctionAsync(
                @"() => {
                    const items = document.querySelectorAll('#workout-selected-list li .workout-selected__name');
                    return items.length === 3 && items[2].textContent === 'Bench Press';
                }");

            var nameSpans = page.Locator("#workout-selected-list .workout-selected__name");
            await Expect(nameSpans.Nth(0)).ToHaveTextAsync("Squat");
            await Expect(nameSpans.Nth(1)).ToHaveTextAsync("Deadlift");
            await Expect(nameSpans.Nth(2)).ToHaveTextAsync("Bench Press");

            // Save the workout
            await page.FillAsync("#workout-name", "Reorder Test");
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();
            await page.Locator(".workout-list__name").Filter(new() { HasText = "Reorder Test" }).WaitForAsync();

            // Open the edit modal to verify persisted order
            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();
            await page.WaitForSelectorAsync("#edit-selected-list li:nth-child(3)");

            var editNameSpans = page.Locator("#edit-selected-list .workout-selected__name");
            await Expect(editNameSpans.Nth(0)).ToHaveTextAsync("Squat");
            await Expect(editNameSpans.Nth(1)).ToHaveTextAsync("Deadlift");
            await Expect(editNameSpans.Nth(2)).ToHaveTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// T028 (US1, US1.4): When only one exercise is selected in the create form,
    /// no drag handles are rendered.
    /// </summary>
    [Fact]
    public async Task CreateWorkout_SingleExercise_NoDragHandle()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await AddExerciseToFormAsync(page, "Bench Press");

            await page.WaitForSelectorAsync("#workout-selected-list li");

            var handles = page.Locator("#workout-selected-list .workout-selected__drag-handle");
            Assert.Equal(0, await handles.CountAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // US2: Reorder while editing a workout
    // ──────────────────────────────────────────

    /// <summary>
    /// T029 (US2.3): Dragging an exercise to a different position in the edit modal
    /// and saving persists the new order.
    /// </summary>
    [Fact]
    public async Task EditWorkout_DragReorder_PersistsNewOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await SeedExerciseAsync(page, "Deadlift");

            await NavigateToWorkoutsAsync(page);

            // Create workout with 3 exercises via UI (in Bench→Squat→Deadlift order)
            await AddExerciseToFormAsync(page, "Bench Press");
            await AddExerciseToFormAsync(page, "Squat");
            await AddExerciseToFormAsync(page, "Deadlift");
            await page.FillAsync("#workout-name", "Edit Reorder Test");
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();
            await page.Locator(".workout-list__name").Filter(new() { HasText = "Edit Reorder Test" }).WaitForAsync();

            // Open the edit modal
            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await page.WaitForSelectorAsync("#edit-selected-list li:nth-child(3)");

            // Drag Bench Press (first item) to last position
            await DragToEndAsync(page, "#edit-selected-list", 1, 3);

            // Verify Bench Press is now last
            var editNameSpans = page.Locator("#edit-selected-list .workout-selected__name");
            await Expect(editNameSpans.Nth(2)).ToHaveTextAsync("Bench Press");

            // Save
            await page.Locator("#workout-edit-form .workout-form__submit").ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();

            // Re-open the edit modal and confirm new order is persisted
            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();
            await page.WaitForSelectorAsync("#edit-selected-list li:nth-child(3)");

            var nameSpans = page.Locator("#edit-selected-list .workout-selected__name");
            await Expect(nameSpans.Nth(0)).ToHaveTextAsync("Squat");
            await Expect(nameSpans.Nth(1)).ToHaveTextAsync("Deadlift");
            await Expect(nameSpans.Nth(2)).ToHaveTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// T029 (US2.4): After reordering in the edit modal, cancelling discards the
    /// reorder and the original order is preserved.
    /// </summary>
    [Fact]
    public async Task EditWorkout_DragReorder_ThenCancel_OriginalOrderPreserved()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await SeedExerciseAsync(page, "Deadlift");

            await NavigateToWorkoutsAsync(page);

            // Create workout
            await AddExerciseToFormAsync(page, "Bench Press");
            await AddExerciseToFormAsync(page, "Squat");
            await AddExerciseToFormAsync(page, "Deadlift");
            await page.FillAsync("#workout-name", "Cancel Reorder Test");
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();
            await page.Locator(".workout-list__name").Filter(new() { HasText = "Cancel Reorder Test" }).WaitForAsync();

            // Open edit, drag to reorder, then cancel
            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await page.WaitForSelectorAsync("#edit-selected-list li:nth-child(3)");

            await DragToEndAsync(page, "#edit-selected-list", 1, 3);

            await Expect(page.Locator("#edit-selected-list .workout-selected__name").Nth(2)).ToHaveTextAsync("Bench Press");

            // Cancel without saving — confirm discard since changes were made
            await page.Locator("#workout-edit-cancel").ClickAsync();
            await Expect(page.Locator("#workout-edit-discard-backdrop")).ToBeVisibleAsync();
            await page.Locator("#workout-edit-discard-confirm").ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync();

            // Re-open and confirm original order is unchanged
            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync();
            await page.WaitForSelectorAsync("#edit-selected-list li:nth-child(3)");

            var nameSpans = page.Locator("#edit-selected-list .workout-selected__name");
            await Expect(nameSpans.Nth(0)).ToHaveTextAsync("Bench Press");
            await Expect(nameSpans.Nth(1)).ToHaveTextAsync("Squat");
            await Expect(nameSpans.Nth(2)).ToHaveTextAsync("Deadlift");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>
    /// T029 (US2.1): Opening an existing workout in edit mode shows exercises
    /// in their saved sequence order.
    /// </summary>
    [Fact]
    public async Task EditWorkout_ExercisesDisplayedInSavedSequenceOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Squat");
            await SeedExerciseAsync(page, "Deadlift");

            await NavigateToWorkoutsAsync(page);

            // Create with explicit order: Deadlift, Bench Press, Squat
            await AddExerciseToFormAsync(page, "Deadlift");
            await AddExerciseToFormAsync(page, "Bench Press");
            await AddExerciseToFormAsync(page, "Squat");
            await page.FillAsync("#workout-name", "Sequence Order Test");
            await page.Locator("#workout-form .workout-form__submit").ClickAsync();
            await page.Locator(".workout-list__name").Filter(new() { HasText = "Sequence Order Test" }).WaitForAsync();

            // Open edit modal and verify saved order is shown
            await page.Locator(".workout-list__edit-btn").First.ClickAsync();
            await page.WaitForSelectorAsync("#edit-selected-list li:nth-child(3)");

            var nameSpans = page.Locator("#edit-selected-list .workout-selected__name");
            await Expect(nameSpans.Nth(0)).ToHaveTextAsync("Deadlift");
            await Expect(nameSpans.Nth(1)).ToHaveTextAsync("Bench Press");
            await Expect(nameSpans.Nth(2)).ToHaveTextAsync("Squat");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
