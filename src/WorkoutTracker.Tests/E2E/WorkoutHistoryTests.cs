using System.Text.RegularExpressions;
using Microsoft.Playwright;
using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

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
        await page.WaitForSelectorAsync("#workout-form .workout-form__exercises button[role='checkbox']");
        await page.FillAsync("#workout-name", name);
        await page.Locator("#workout-form .workout-form__exercises button[role='checkbox']")
            .Filter(new() { HasText = exerciseName }).ClickAsync();
        await page.Locator("#workout-form .workout-form__submit").ClickAsync();
        await page.Locator(".workout-list__name").Filter(new() { HasText = name }).WaitForAsync();
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
    public async Task HistoryPage_SessionExpandCollapse()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);

            await NavigateToHistoryAsync(page);

            var header = page.Locator(".history-session__header").First;
            await Expect(header).ToBeVisibleAsync();

            // Expand
            await header.ClickAsync();
            await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");

            var details = page.Locator(".history-session__details").First;
            await Expect(details).ToBeVisibleAsync();

            // Collapse
            await header.ClickAsync();
            await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_SessionDetails_ShowsExerciseData()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(
                page,
                exerciseName: "Bench Press",
                workoutName: "Push Day",
                loggedReps: 10,
                loggedWeight: "135 lbs",
                notes: "Good form");

            await NavigateToHistoryAsync(page);

            // Expand the session
            var header = page.Locator(".history-session__header").First;
            await header.ClickAsync();
            await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");

            var exerciseName = page.Locator(".history-session__exercise-name").First;
            await Expect(exerciseName).ToContainTextAsync("Bench Press");

            var exerciseData = page.Locator(".history-session__exercise-data").First;
            await Expect(exerciseData).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task HistoryPage_DateGrouping_ShowsToday()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);

            await NavigateToHistoryAsync(page);

            var dateLabel = page.Locator(".history-group__date-label").First;
            await Expect(dateLabel).ToContainTextAsync("Today");
        }
        finally
        {
            await page.CloseAsync();
        }
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
    public async Task ActiveSession_StartWorkout_NavigatesToSession()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await page.Locator(".workout-list__start-btn").First.ClickAsync();

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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            await page.Locator("#session-back").ClickAsync();

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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Get the exerciseId from the exercise item
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");

            // Fill in reps and weight inputs
            await page.Locator($"#reps-{exerciseId}").FillAsync("10");
            await page.Locator($"#weight-{exerciseId}").FillAsync("135");

            await page.Locator("#session-save").ClickAsync();

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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Make a change by filling in a reps input
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");
            await page.Locator($"#reps-{exerciseId}").FillAsync("10");

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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Make a change and cancel to trigger discard modal
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");
            await page.Locator($"#reps-{exerciseId}").FillAsync("10");

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

            await page.Locator(".workout-list__start-btn").First.ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            // Make a change and cancel to trigger discard modal
            var exerciseItem = page.Locator(".active-session__exercise-item").First;
            var exerciseId = await exerciseItem.GetAttributeAsync("data-exercise-id");
            await page.Locator($"#reps-{exerciseId}").FillAsync("10");

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
}
