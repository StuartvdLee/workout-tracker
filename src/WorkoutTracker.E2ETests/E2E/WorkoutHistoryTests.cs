using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class WorkoutHistoryTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public WorkoutHistoryTests(WebAppFixture webApp, PlaywrightFixture playwright)
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

    private static async Task NavigateToHistoryAsync(IPage page)
    {
        await page.Locator(".sidebar__link[data-page='history']").ClickAsync();
        await page.WaitForSelectorAsync(".history-page");
    }

    private static async Task CreateWorkoutViaUIAsync(IPage page, string name, string exerciseName)
    {
        await page.Locator("#workout-exercise-select option:not([disabled]):not([value=''])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        await page.FillAsync("#workout-name", name);
        await page.Locator("#workout-exercise-select").SelectOptionAsync(new SelectOptionValue { Label = exerciseName });
        await page.Locator("#workout-form .workout-form__submit").ClickAsync();
        await page.Locator(".workout-list__name").Filter(new() { HasText = name }).WaitForAsync();
    }

    /// <summary>
    /// Clicks the Start button on the first workout and confirms through the pre-start modal.
    /// </summary>
    private static async Task StartWorkoutViaPrestartModalAsync(IPage page)
    {
        await page.Locator(".workout-list__start-btn").First.ClickAsync();
        // Workouts with fewer than 2 exercises skip the pre-start modal and navigate directly.
    }

    /// <summary>
    /// Seeds an exercise, creates a workout via API, logs a session, and returns the exerciseId.
    /// </summary>
    private async Task<(string WorkoutId, string ExerciseId)> CreateWorkoutAndSessionViaApiAsync(
        IPage page,
        string exerciseName = "Bench Press",
        string workoutName = "Push Day",
        int loggedReps = 10,
        string loggedWeight = "135 lbs",
        string notes = "Good form")
    {
        await SeedExerciseAsync(page, exerciseName);

        // Get exerciseId from API
        var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
        var exercisesJson = await exercisesResponse.JsonAsync();
        var exerciseId = exercisesJson?.EnumerateArray().First().GetProperty("exerciseId").GetString()!;

        // Create workout via API
        var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
        {
            DataObject = new
            {
                name = workoutName,
                exercises = new[] { new { exerciseId, targetReps = "8-12", targetWeight = "135 lbs" } },
            },
        });
        var workoutData = await createResponse.JsonAsync();
        var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

        // Log a session
        await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
        {
            DataObject = new
            {
                loggedExercises = new[]
                {
                    new { exerciseId, loggedReps, loggedWeight, notes },
                },
            },
        });

        return (workoutId, exerciseId);
    }

    // ──────────────────────────────────────────
    // History Page
    // ──────────────────────────────────────────

    [Fact]
    public async Task HistoryPage_EmptyState_ShowsMessage()
    {
        var page = await CreatePageAsync();
        try
        {
            await NavigateToHistoryAsync(page);

            var empty = page.Locator("#history-empty");
            await Expect(empty).ToBeVisibleAsync();
            await Expect(empty).ToContainTextAsync(new Regex(".+"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_LoadingState_ShownInitially()
    {
        var page = await CreatePageAsync();
        try
        {
            // Intercept the sessions API to delay the response
            await page.RouteAsync("**/api/sessions", async route =>
            {
                await Task.Delay(1000);
                await route.FallbackAsync();
            });

            await NavigateToHistoryAsync(page);

            var loading = page.Locator("#history-loading");
            await Expect(loading).ToBeVisibleAsync();

            // Wait for loading to disappear after response arrives
            await Expect(loading).ToBeHiddenAsync(new() { Timeout = 5000 });
        }
        finally
        {
            await page.UnrouteAsync("**/api/sessions");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_WithSessions_ShowsSessions()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);

            await NavigateToHistoryAsync(page);

            var session = page.Locator(".history-session");
            await Expect(session).ToBeVisibleAsync();
            await Expect(session).ToContainTextAsync("Push Day");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_SessionEntry_NavigatesToDetailPage()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            var header = page.Locator(".history-session__header").First;
            await Expect(header).ToBeVisibleAsync();

            await header.ClickAsync();

            await Expect(page).ToHaveURLAsync(new Regex(@"/history/session\?id="));
            await page.WaitForSelectorAsync(".session-detail");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_NoExpandCollapseAffordance()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            // No toggle element should be present
            await Expect(page.Locator(".history-session__toggle")).ToHaveCountAsync(0);
            await Expect(page.Locator(".history-session__details")).ToHaveCountAsync(0);

            // Headers should not have aria-expanded
            var header = page.Locator(".history-session__header").First;
            await Expect(header).ToBeVisibleAsync();
            await Expect(header).Not.ToHaveAttributeAsync("aria-expanded", new Regex(".+"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_ShowsExerciseTable()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page, exerciseName: "Bench Press", workoutName: "Push Day");
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await Expect(page.Locator(".session-detail__title")).ToContainTextAsync("Push Day");
            await Expect(page.Locator(".session-detail__table")).ToBeVisibleAsync();

            // Column headers
            var headers = page.Locator(".session-detail__th");
            await Expect(headers).ToHaveCountAsync(5);

            // Exercise row
            await Expect(page.Locator(".session-detail__cell--exercise").First).ToContainTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_BackButton_NavigatesToHistory()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail");

            await page.Locator(".session-detail__back").ClickAsync();

            await page.WaitForSelectorAsync(".history-page");
            await Expect(page.Locator(".history-page")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_NoGroupHeaders_FlatList()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            await Expect(page.Locator(".history-session")).ToBeVisibleAsync();
            await Expect(page.Locator(".history-group__date-label")).ToHaveCountAsync(0);
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task HistoryPage_EntryShowsDateBelowName()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            var dateEl = page.Locator(".history-session__date").First;
            await Expect(dateEl).ToBeVisibleAsync();
            await Expect(dateEl).ToContainTextAsync(new Regex(".+"));
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task HistoryPage_HasH1Heading()
    {
        var page = await CreatePageAsync();
        try
        {
            await NavigateToHistoryAsync(page);

            await Expect(page.Locator(".history-page__title")).ToHaveTextAsync("Workout History");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Active Session Page
    // ──────────────────────────────────────────

    [Fact]
    public async Task SessionDetailPage_ShowsPreviousData_WhenPriorSessionExists()
    {
        var page = await CreatePageAsync();
        try
        {
            // Seed exercise and create workout with it
            await SeedExerciseAsync(page, "Bench Press");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First().GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Push Day",
                    exercises = new[] { new { exerciseId, targetReps = "8-12", targetWeight = "100 KG" } },
                },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            // First session: weight 70 KG, effort 6
            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new
                {
                    loggedExercises = new[] { new { exerciseId, loggedWeight = "70 KG", effort = 6 } },
                },
            });

            // Second session: weight 75 KG, effort 7
            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new
                {
                    loggedExercises = new[] { new { exerciseId, loggedWeight = "75 KG", effort = 7 } },
                },
            });

            await NavigateToHistoryAsync(page);

            // Click the most recent session (first in list)
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            var rows = page.Locator(".session-detail__row");
            await Expect(rows).ToHaveCountAsync(1);

            // Weight cells: current and previous
            var cells = rows.First.Locator(".session-detail__cell");
            await Expect(cells.Nth(1)).ToContainTextAsync("75 KG");
            await Expect(cells.Nth(2)).ToContainTextAsync("70 KG");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_StartWorkout_NavigatesToSession()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);

            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_ShowsWorkoutExercises()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            var exerciseItem = page.Locator(".active-session__exercise-item");
            await Expect(exerciseItem).ToBeVisibleAsync();
            await Expect(exerciseItem).ToContainTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_BackButton_NavigatesToWorkouts()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            await page.Locator("#session-cancel").ClickAsync();

            await page.WaitForSelectorAsync(".workouts-page");
            await Expect(page.Locator(".workouts-page")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_SaveButton_SavesAndNavigates()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Get the exerciseId from the exercise item
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");

            // Fill in weight input
            await page.Locator($"#weight-{exerciseId}").FillAsync("135");

            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();

            // Should navigate away from active session
            await Expect(page).Not.ToHaveURLAsync(new Regex(@"/active-session"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_CancelWithoutChanges_GoesBack()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Cancel without making any changes
            await page.Locator("#session-cancel").ClickAsync();

            // Should navigate back without showing discard modal
            await Expect(page.Locator("#discard-backdrop")).ToBeHiddenAsync(new() { Timeout = 2000 });
            await Expect(page).Not.ToHaveURLAsync(new Regex(@"/active-session"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_CancelWithChanges_ShowsDiscardModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Make a change by filling in a reps input
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");
            await page.Locator($"#weight-{exerciseId}").FillAsync("60");

            // Click cancel
            await page.Locator("#session-cancel").ClickAsync();

            // Discard modal should appear
            await Expect(page.Locator("#discard-backdrop")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_DiscardModal_DiscardNavigatesAway()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Make a change and cancel to trigger discard modal
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");
            await page.Locator($"#weight-{exerciseId}").FillAsync("60");

            await page.Locator("#session-cancel").ClickAsync();
            await Expect(page.Locator("#discard-backdrop")).ToBeVisibleAsync();

            // Click discard to confirm
            await page.Locator("#discard-confirm").ClickAsync();

            // Should navigate away
            await Expect(page).Not.ToHaveURLAsync(new Regex(@"/active-session"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_DiscardModal_ContinueStaysOnPage()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Make a change and cancel to trigger discard modal
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");
            await page.Locator($"#weight-{exerciseId}").FillAsync("60");

            await page.Locator("#session-cancel").ClickAsync();
            await Expect(page.Locator("#discard-backdrop")).ToBeVisibleAsync();

            // Click continue to stay on page
            await page.Locator("#discard-cancel").ClickAsync();

            // Should still be on active session page
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));
            await Expect(page.Locator("#discard-backdrop")).ToBeHiddenAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // T012 — US1: Effort Modal E2E Tests
    // ──────────────────────────────────────────

    [Fact]
    public async Task SaveWorkout_EffortModal_AppearsOnSave()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            // Navigate to the active session page (start a new session)
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var saveBtn = page.Locator("#session-save");
            await saveBtn.ClickAsync();

            var backdrop = page.Locator("#effort-backdrop");
            await Expect(backdrop).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_SkipSavesWithoutEffort()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();

            // Should navigate to history
            await page.WaitForURLAsync(new Regex(".*/history.*"));

            // Check most recent session has null overallEffort
            var sessionsResp = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions");
            var sessionsJson = await sessionsResp.JsonAsync();
            var latestSession = sessionsJson?.EnumerateArray().First();
            Assert.NotNull(latestSession);
            Assert.Equal(System.Text.Json.JsonValueKind.Null, latestSession.Value.GetProperty("overallEffort").ValueKind);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_TrapsKeyboardFocus()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();

            await Expect(page.Locator("#effort-modal-save")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#effort-modal-skip")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#overall-effort-slider")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#effort-modal-save")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Expect(page.Locator("#overall-effort-slider")).ToBeFocusedAsync();

            await page.Keyboard.PressAsync("Shift+Tab");
            await Expect(page.Locator("#effort-modal-skip")).ToBeFocusedAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_ConfirmSavesWithEffort()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            // Move the slider to 7
            await page.Locator("#overall-effort-slider").FillAsync("7");
            await page.Locator("#overall-effort-slider").DispatchEventAsync("input");
            await page.Locator("#effort-modal-save").ClickAsync();

            await page.WaitForURLAsync(new Regex(".*/history.*"));

            var sessionsResp = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions");
            var sessionsJson = await sessionsResp.JsonAsync();
            var latestSession = sessionsJson?.EnumerateArray().First();
            Assert.NotNull(latestSession);
            Assert.Equal(7, latestSession.Value.GetProperty("overallEffort").GetInt32());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // T020 — US3: Session Detail Effort Display
    // ──────────────────────────────────────────

    [Fact]
    public async Task HistoryPage_NoEffortShown_WhenSessionHasNoEffort()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);

            await NavigateToHistoryAsync(page);

            var effortSpan = page.Locator(".history-session__overall-effort");
            await Expect(effortSpan).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // T022 — US3: Session Detail Effort Row
    // ──────────────────────────────────────────

    [Fact]
    public async Task SessionDetailPage_ShowsOverallEffortSummaryRow()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Leg Press");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First(e => e.GetProperty("name").GetString() == "Leg Press").GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Leg Day", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 8, loggedExercises = new[] { new { exerciseId } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            Assert.Equal(8, sessionData?.GetProperty("overallEffort").GetInt32());
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var row = page.Locator(".session-detail__overall-effort-row");
            await Expect(row).ToBeVisibleAsync();
            await Expect(row).ToContainTextAsync("8");
            await Expect(row).ToContainTextAsync("Hard");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_ShowsPreviousOverallEffort_WhenPriorSessionExists()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Cable Row");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First(e => e.GetProperty("name").GetString() == "Cable Row").GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Back Day", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            // First session: effort 6
            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 6, loggedExercises = new[] { new { exerciseId } } },
            });

            // Second session: effort 8
            var secondSessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 8, loggedExercises = new[] { new { exerciseId } } },
            });
            var secondSessionData = await secondSessionResp.JsonAsync();
            var secondSessionId = secondSessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={secondSessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var row = page.Locator(".session-detail__overall-effort-row");
            await Expect(row).ToBeVisibleAsync();
            await Expect(row).ToContainTextAsync("8");
            await Expect(row).ToContainTextAsync("6");
            await Expect(row).ToContainTextAsync("Moderate");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_ShowsNoPreviousComparison_ForAdHocSession()
    {
        var page = await CreatePageAsync();
        try
        {
            // Create a session that is the first (and only) for its workout → no previous
            await SeedExerciseAsync(page, "Machine Fly");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First(e => e.GetProperty("name").GetString() == "Machine Fly").GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Chest Isolation", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 5, loggedExercises = new[] { new { exerciseId } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var row = page.Locator(".session-detail__overall-effort-row");
            await Expect(row).ToBeVisibleAsync();
            // Current effort is shown
            await Expect(row).ToContainTextAsync("5");
            // Previous effort is — (no prior session)
            await Expect(row).ToContainTextAsync("—");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
