using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
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
        await page.Locator("#prestart-sets").SelectOptionAsync("3");
        await page.Locator("#prestart-no").ClickAsync();
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
        string notes = "Good form",
        int? overallEffort = null)
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
                sets = 3,
                overallEffort,
                loggedExercises = new[]
                {
                    new { exerciseId, loggedReps, loggedWeight, notes },
                },
            },
        });

        return (workoutId, exerciseId);
    }

    private async Task<(string WorkoutId, string[] ExerciseIds)> CreateWorkoutViaApiAsync(
        IPage page,
        string workoutName,
        params string[] exerciseNames)
    {
        var exerciseIds = new List<string>();
        foreach (var exerciseName in exerciseNames)
        {
            var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
            {
                DataObject = new { name = exerciseName, muscleIds = Array.Empty<string>() },
            });
            exerciseIds.Add((await response.JsonAsync())!.Value.GetProperty("exerciseId").GetString()!);
        }

        var workoutResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
        {
            DataObject = new
            {
                name = workoutName,
                exercises = exerciseIds.Select(exerciseId => new { exerciseId }).ToArray(),
            },
        });
        var workoutId = (await workoutResponse.JsonAsync())!.Value.GetProperty("plannedWorkoutId").GetString()!;
        return (workoutId, exerciseIds.ToArray());
    }

    private async Task<string> GetWorkoutSessionIdAsync(IPage page, string workoutId)
    {
        var sessions = await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync();
        return sessions!.Value.EnumerateArray()
            .Single(session => session.GetProperty("plannedWorkoutId").GetString() == workoutId)
            .GetProperty("workoutSessionId").GetString()!;
    }

    [Fact]
    public async Task StatisticsDashboard_ShowsPeriodSummaryAndRefreshesAfterDeletion()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(
                page,
                exerciseName: "Statistics Press",
                workoutName: "Statistics Workout",
                overallEffort: 7);
            var (workoutWithoutEffortId, _) = await CreateWorkoutAndSessionViaApiAsync(
                page,
                exerciseName: "Statistics Curl",
                workoutName: "Statistics Workout Without Effort");

            await page.Locator(".sidebar__link[data-page='statistics']").ClickAsync();
            await page.WaitForSelectorAsync(".statistics-page");
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("2");
            Assert.Contains("sr-only", await page.Locator("#statistics-status").GetAttributeAsync("class"));
            await Expect(page.Locator("#statistics-exercise-heading")).ToHaveCountAsync(0);
            await Expect(page.Locator("#statistics-average-heading")).ToHaveTextAsync("Average workouts per week");
            await Expect(page.Locator("#statistics-average-value")).ToHaveTextAsync("2");
            await Expect(page.Locator("#statistics-average-note")).ToContainTextAsync("weeks with no workouts");
            await Expect(page.Locator(".sidebar__link[data-page='statistics']")).ToHaveAttributeAsync("aria-current", "page");
            await Expect(page.Locator("#statistics-next")).ToBeDisabledAsync();

            await page.Locator("#statistics-kind-year").ClickAsync();
            await Expect(page.Locator("#statistics-kind-year")).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("2");
            await Expect(page.Locator("#statistics-average-heading")).ToHaveTextAsync("Average workouts per year");
            await Expect(page.Locator("#statistics-average-value")).ToHaveTextAsync("2");
            await page.Locator("#statistics-previous").FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("0");
            await Expect(page.Locator("#statistics-average-value")).ToHaveTextAsync("—");
            await Expect(page.Locator("#statistics-next")).ToBeEnabledAsync();
            await page.Locator("#statistics-current").ClickAsync();
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("2");

            var sessionId = await GetWorkoutSessionIdAsync(page, workoutId);
            var sessionWithoutEffortId = await GetWorkoutSessionIdAsync(page, workoutWithoutEffortId);
            await page.APIRequest.DeleteAsync($"{_webApp.BaseUrl}/api/sessions/{sessionId}");
            await page.Locator(".sidebar__link[data-page='history']").ClickAsync();
            await page.Locator(".sidebar__link[data-page='statistics']").ClickAsync();
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("1");
            await Expect(page.Locator("#statistics-average-value")).ToHaveTextAsync("1");

            await page.APIRequest.DeleteAsync($"{_webApp.BaseUrl}/api/sessions/{sessionWithoutEffortId}");
            await page.Locator(".sidebar__link[data-page='history']").ClickAsync();
            await page.Locator(".sidebar__link[data-page='statistics']").ClickAsync();
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("0");
            await Expect(page.Locator("#statistics-empty-message")).ToBeVisibleAsync();
            await Expect(page.Locator("#statistics-average-value")).ToHaveTextAsync("—");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task StatisticsDashboard_RetriesAfterFailureAndFitsNarrowViewports()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(375, 812);
            var shouldFail = true;
            await page.RouteAsync("**/api/statistics/workouts**", async route =>
            {
                if (shouldFail)
                {
                    await route.FulfillAsync(new RouteFulfillOptions
                    {
                        Status = 502,
                        ContentType = "application/json",
                        Body = "{\"error\":\"API unavailable.\"}",
                    });
                }
                else
                {
                    await route.ContinueAsync();
                }
            });

            await page.Locator(".topbar__toggle").ClickAsync();
            await page.Locator(".sidebar__link[data-page='statistics']").ClickAsync();
            await page.WaitForSelectorAsync(".statistics-page");
            await Expect(page.Locator("#statistics-error")).ToBeVisibleAsync();
            await Expect(page.Locator("#statistics-results")).ToBeHiddenAsync();

            shouldFail = false;
            await page.Locator("#statistics-retry").ClickAsync();
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("0");
            await Expect(page.Locator("#statistics-empty-message")).ToBeVisibleAsync();
            await page.Locator("#statistics-kind-month").FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            await Expect(page.Locator("#statistics-kind-month")).ToHaveAttributeAsync("aria-pressed", "true");
            await Expect(page.Locator("#statistics-average-heading")).ToHaveTextAsync("Average workouts per month");

            var bounds = await page.Locator(".statistics-page").BoundingBoxAsync();
            Assert.NotNull(bounds);
            Assert.True(bounds.Width <= 375);

            foreach (var width in new[] { 320, 375 })
            {
                await page.SetViewportSizeAsync(width, 812);
                var kinds = page.Locator(".statistics-controls__kind");
                var navigation = page.Locator(".statistics-controls__nav");
                for (var i = 0; i < 3; i++)
                {
                    var kindBounds = await kinds.Nth(i).BoundingBoxAsync();
                    var navigationBounds = await navigation.Nth(i).BoundingBoxAsync();
                    Assert.NotNull(kindBounds);
                    Assert.NotNull(navigationBounds);
                    Assert.InRange(Math.Abs(kindBounds.X - navigationBounds.X), 0, 0.5);
                    Assert.InRange(Math.Abs(kindBounds.Width - navigationBounds.Width), 0, 0.5);
                }
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task StatisticsDashboard_IgnoresLateResponseForPreviousPeriod()
    {
        var page = await CreatePageAsync();
        try
        {
            var requestNumber = 0;
            await page.RouteAsync("**/api/statistics/workouts**", async route =>
            {
                var sequence = Interlocked.Increment(ref requestNumber);
                if (sequence == 1)
                {
                    await Task.Delay(500);
                }

                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = "application/json",
                    Body = JsonSerializer.Serialize(new
                    {
                        workoutCount = sequence == 1 ? 4 : 2,
                        exerciseActivity = Array.Empty<object>(),
                        history = new { workoutCount = sequence == 1 ? 4 : 2, firstCompletedAt = DateTime.UtcNow },
                        effort = new { contributingCount = 0, observations = Array.Empty<object>() },
                    }),
                });
            });

            var firstRequest = page.WaitForRequestAsync(request =>
                request.Url.Contains("/api/statistics/workouts", StringComparison.Ordinal));
            await page.Locator(".sidebar__link[data-page='statistics']").ClickAsync();
            await firstRequest;
            await page.Locator("#statistics-kind-month").ClickAsync();
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("2");
            await Task.Delay(600);
            await Expect(page.Locator("#statistics-workout-count")).ToHaveTextAsync("2");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_RestoresInputsAcrossRepeatedRefreshAndRetainsHistory()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=5");
            await page.Locator($"#weight-{exerciseId}").FillAsync("42.5");
            await page.Locator($"#effort-{exerciseId}").FillAsync("7");
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("saved");
            await page.ReloadAsync();
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("42.5");
            await Expect(page.Locator($"#effort-{exerciseId}")).ToHaveValueAsync("7");
            await page.Locator($"#weight-{exerciseId}").FillAsync("45");
            await page.ReloadAsync();
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("45");
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Theory]
    [InlineData("home")]
    [InlineData("workouts")]
    public async Task ActiveSession_NewStartOffersResumeOrDiscardWhileRefreshResumes(string startForm)
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseIds) = await CreateWorkoutViaApiAsync(
                page, "Restart choice workout", "Restart first", "Restart second");
            var firstExerciseId = exerciseIds[0];
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{firstExerciseId}").FillAsync("42");
            var originalDraftId = await page.EvaluateAsync<string>(
                "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).draftId");

            async Task StartWithSelectedFormAsync()
            {
                if (startForm == "home")
                {
                    await page.GotoAsync(_webApp.BaseUrl);
                    await page.Locator($"#workout-select option[value='{workoutId}']")
                        .WaitForAsync(new() { State = WaitForSelectorState.Attached });
                    await page.Locator("#workout-select").SelectOptionAsync(workoutId);
                    await page.Locator("#sets-select").SelectOptionAsync("5");
                    await page.Locator("#home-randomise-toggle").ClickAsync();
                    await page.Locator("#workout-form .workout-form__button").ClickAsync();
                }
                else
                {
                    await page.GotoAsync($"{_webApp.BaseUrl}/workouts");
                    await page.Locator(".workout-list__start-btn").First.ClickAsync();
                    await page.Locator("#prestart-sets").SelectOptionAsync("5");
                    await page.Locator("#prestart-yes").ClickAsync();
                }
            }

            await StartWithSelectedFormAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?.*start=1"));
            await Expect(page.Locator(".active-session p[role='alert']"))
                .ToContainTextAsync("already in progress");
            await page.GetByRole(AriaRole.Button, new() { Name = "Resume saved workout" }).ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex($@"resume={Regex.Escape(originalDraftId)}"));
            await Expect(page.Locator($"#weight-{firstExerciseId}")).ToHaveValueAsync("42");
            Assert.Equal(3, await page.EvaluateAsync<int>(
                "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).sets"));

            await page.ReloadAsync();
            await Expect(page.Locator($"#weight-{firstExerciseId}")).ToHaveValueAsync("42");
            await Expect(page.Locator(".active-session p[role='alert']")).ToHaveCountAsync(0);

            await StartWithSelectedFormAsync();
            await Expect(page.Locator(".active-session p[role='alert']")).ToContainTextAsync("already in progress");
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Discard and start new workout" }).ClickAsync();
            await Expect(page.Locator($"#weight-{firstExerciseId}")).ToBeVisibleAsync();
            Assert.Equal(5, await page.EvaluateAsync<int>(
                "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).sets"));
            var encodedOrder = Regex.Match(page.Url, @"[?&]order=([^&]+)").Groups[1].Value;
            var selectedOrder = Uri.UnescapeDataString(encodedOrder).Split(",");
            Assert.Equal(selectedOrder, await page.EvaluateAsync<string[]>(
                "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).exerciseOrder"));

            await page.ReloadAsync();
            await Expect(page.Locator($"#weight-{firstExerciseId}")).ToBeVisibleAsync();
            await Expect(page.Locator(".active-session p[role='alert']")).ToHaveCountAsync(0);
            Assert.Equal(5, await page.EvaluateAsync<int>(
                "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).sets"));
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_DelayedWorkoutResponseCannotOverwriteNewActiveWorkout()
    {
        var page = await CreatePageAsync();
        try
        {
            var (oldWorkoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page, "Old liveness exercise", "Old liveness workout");
            var (newWorkoutId, newExerciseId) = await CreateWorkoutAndSessionViaApiAsync(page, "New liveness exercise", "New liveness workout");
            await page.AddInitScriptAsync($$"""
                (() => {
                    const originalJson = Response.prototype.json;
                    Response.prototype.json = async function (...args) {
                        const value = await originalJson.apply(this, args);
                        if (this.url.endsWith('/api/workouts/{{oldWorkoutId}}')) {
                            window.__oldWorkoutJsonWaiting = true;
                            await new Promise(resolve => { window.__releaseOldWorkoutJson = resolve; });
                            window.__oldWorkoutJsonResumed = true;
                        }
                        return value;
                    };
                })();
                """);

            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={oldWorkoutId}&sets=3");
            await page.WaitForFunctionAsync("() => window.__oldWorkoutJsonWaiting === true");
            await page.Locator(".sidebar__link[data-page='home']").ClickAsync();
            await page.Locator($"#workout-select option[value='{newWorkoutId}']")
                .WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#workout-select").SelectOptionAsync(newWorkoutId);
            await page.Locator("#workout-form .workout-form__button").ClickAsync();
            await Expect(page.Locator("#session-title")).ToHaveTextAsync("New liveness workout");
            await page.Locator($"#weight-{newExerciseId}").FillAsync("77");

            await page.EvaluateAsync("() => window.__releaseOldWorkoutJson()");
            await page.WaitForFunctionAsync("() => window.__oldWorkoutJsonResumed === true");
            await Expect(page.Locator("#session-title")).ToHaveTextAsync("New liveness workout");
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex("/history/session\\?id="));

            var sessions = await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync();
            Assert.Contains(sessions!.Value.EnumerateArray(), session =>
                session.GetProperty("plannedWorkoutId").GetString() == newWorkoutId &&
                session.GetProperty("loggedExercises")[0].GetProperty("loggedWeight").GetString() == "77");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_Confirmed409OffersCleanupAfterNormalCompletion()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            var completedSessionId = await GetWorkoutSessionIdAsync(page, workoutId);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("55");
            await page.RouteAsync($"**/api/workouts/{workoutId}/sessions", route => route.FulfillAsync(new()
            {
                Status = 409,
                ContentType = "application/json",
                Body = $$"""{"error":"Already completed","workoutSessionId":"{{completedSessionId}}"}""",
            }));

            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#session-api-error")).ToContainTextAsync("View completed workout");
            var cleanup = page.GetByRole(AriaRole.Button, new() { Name = "Clear completed workout draft" });
            await Expect(cleanup).ToBeVisibleAsync();
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await cleanup.ClickAsync();

            await Expect(page).ToHaveURLAsync(new Regex($@"/history/session\?id={Regex.Escape(completedSessionId)}"));
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_Confirmed409OffersCleanupFromPendingCompletion()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            var completedSessionId = await GetWorkoutSessionIdAsync(page, workoutId);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("55");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, route => route.AbortAsync());
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("may have succeeded");
            await page.UnrouteAsync(endpoint);
            await page.ReloadAsync();
            await page.RouteAsync(endpoint, route => route.FulfillAsync(new()
            {
                Status = 409,
                ContentType = "application/json",
                Body = $$"""{"error":"Already completed","workoutSessionId":"{{completedSessionId}}"}""",
            }));

            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page.Locator("#pending-error")).ToContainTextAsync("View completed workout");
            var cleanup = page.GetByRole(AriaRole.Button, new() { Name = "Clear completed workout draft" });
            await Expect(cleanup).ToBeVisibleAsync();
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await cleanup.ClickAsync();

            await Expect(page).ToHaveURLAsync(new Regex($@"/history/session\?id={Regex.Escape(completedSessionId)}"));
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_UncertainCompletionNeverOffersConfirmedCleanup()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            var sessionId = await GetWorkoutSessionIdAsync(page, workoutId);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("55");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, route => route.FulfillAsync(new()
            {
                Status = 500,
                ContentType = "application/json",
                Body = $$"""{"error":"Uncertain","workoutSessionId":"{{sessionId}}"}""",
            }));
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("may have succeeded");
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear completed workout draft" })).ToHaveCountAsync(0);
            await page.UnrouteAsync(endpoint);
            await page.ReloadAsync();
            await page.RouteAsync(endpoint, route => route.FulfillAsync(new()
            {
                Status = 409,
                ContentType = "application/json",
                Body = """{"error":"Cannot confirm completion"}""",
            }));

            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page.Locator("#pending-status")).ToContainTextAsync("cannot be retried");
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear completed workout draft" })).ToHaveCountAsync(0);
            Assert.NotNull(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_IncompleteWeightSurvivesUnrelatedEditsAndBlocksReorderAndSubmission()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseIds) = await CreateWorkoutViaApiAsync(
                page, "Incomplete weight workout", "Incomplete first", "Incomplete second");
            var exerciseId = exerciseIds[0];
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            var weight = page.Locator($"#weight-{exerciseId}");
            var warning = page.Locator($"#weight-warning-{exerciseId}");
            await Expect(warning).ToBeHiddenAsync();
            await weight.FillAsync("42");
            await weight.FocusAsync();
            await page.Keyboard.PressAsync("End");
            await page.Keyboard.TypeAsync("e");
            var invalidState = await weight.EvaluateAsync<string>(
                "input => JSON.stringify({ value: input.value, badInput: input.validity.badInput })");
            Assert.True(await weight.EvaluateAsync<bool>("input => input.validity.badInput"), invalidState);
            await Expect(warning).ToContainTextAsync("has not been saved");
            Assert.Equal($"weight-warning-{exerciseId}", await weight.GetAttributeAsync("aria-describedby"));
            Assert.True(await warning.EvaluateAsync<bool>(
                "element => element.previousElementSibling.classList.contains('active-session__input-group')"));

            await page.Locator("#effort-" + exerciseIds[1]).EvaluateAsync(
                "input => { input.value = '7'; input.dispatchEvent(new Event('input', { bubbles: true })); }");
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("incomplete weight entry is not saved");
            await Expect(warning).ToContainTextAsync("has not been saved");
            Assert.Equal("42", await page.EvaluateAsync<string>(
                "id => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).entries[id].loggedWeight", exerciseId));

            await page.Locator("#session-edit-order").ClickAsync();
            await Expect(page.Locator("#session-order-list")).ToHaveCountAsync(0);
            await Expect(weight).ToBeVisibleAsync();
            await Expect(page.Locator("#session-error")).ToContainTextAsync("before changing exercise order");
            await Expect(warning).ToContainTextAsync("has not been saved");
            await weight.FillAsync("43");
            await Expect(page.Locator("#session-error")).ToHaveTextAsync("");

            var completionRequests = 0;
            page.Request += (_, request) =>
            {
                if (request.Url.Contains($"/api/workouts/{workoutId}/sessions") && request.Method == "POST")
                    Interlocked.Increment(ref completionRequests);
            };
            await weight.FocusAsync();
            await weight.PressAsync("End");
            await page.Keyboard.TypeAsync("e");
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#session-error")).ToContainTextAsync("valid number");
            Assert.Equal(0, Volatile.Read(ref completionRequests));
            Assert.Equal("43", await page.EvaluateAsync<string>(
                "id => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).entries[id].loggedWeight", exerciseId));
            await weight.FillAsync("44");
            await Expect(page.Locator("#session-error")).ToHaveTextAsync("");

            await page.Locator("#session-error").EvaluateAsync("element => { element.textContent = 'An unrelated error'; }");
            await weight.FillAsync("45");
            await Expect(page.Locator("#session-error")).ToHaveTextAsync("An unrelated error");
        }
        finally { await page.CloseAsync(); }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("changed")]
    public async Task ActiveSession_StaleHomeResumeDoesNotCreateOrReplaceDraft(string mutation)
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("61");
            var originalDraftId = await page.EvaluateAsync<string>(
                "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).draftId");
            await page.GotoAsync(_webApp.BaseUrl);
            await page.GetByRole(AriaRole.Button, new() { Name = "Resume workout" }).WaitForAsync();
            await page.EvaluateAsync("""
                ({ mutation }) => {
                    const key = 'workout-tracker-active-draft';
                    if (mutation === 'missing') {
                        localStorage.removeItem(key);
                    } else {
                        const draft = JSON.parse(localStorage.getItem(key));
                        draft.draftId = crypto.randomUUID();
                        localStorage.setItem(key, JSON.stringify(draft));
                    }
                }
                """, new { mutation });

            await page.GetByRole(AriaRole.Button, new() { Name = "Resume workout" }).ClickAsync();
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync(
                mutation == "missing" ? "no longer available" : "changed while you were away");
            await Expect(page.Locator("#session-save")).ToHaveCountAsync(0);
            if (mutation == "missing")
            {
                Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            }
            else
            {
                Assert.NotEqual(originalDraftId, await page.EvaluateAsync<string>(
                    "() => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).draftId"));
                Assert.Equal("61", await page.EvaluateAsync<string>(
                    "id => JSON.parse(localStorage.getItem('workout-tracker-active-draft')).entries[id].loggedWeight", exerciseId));
            }
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_RestoresCommittedOrderAndPreservesCancelledOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            var first = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new() { DataObject = new { name = "First exercise" } });
            var second = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new() { DataObject = new { name = "Second exercise" } });
            var firstId = (await first.JsonAsync())?.GetProperty("exerciseId").GetString();
            var secondId = (await second.JsonAsync())?.GetProperty("exerciseId").GetString();
            var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Ordered draft", exercises = new[] { new { exerciseId = firstId }, new { exerciseId = secondId } } },
            });
            var workoutId = (await response.JsonAsync())?.GetProperty("plannedWorkoutId").GetString();
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=5");
            await page.Locator("#session-edit-order").ClickAsync();
            await page.Locator("#session-order-list .workout-selected__drag-handle").First.FocusAsync();
            await page.Keyboard.PressAsync(" ");
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("Enter");
            await page.Locator("#session-save").ClickAsync();
            await page.Locator($"#weight-{firstId}").FillAsync("32");
            await page.ReloadAsync();
            await Expect(page.Locator(".active-session__exercise-item").First).ToHaveAttributeAsync("data-exercise-id", secondId!);
            await Expect(page.Locator($"#weight-{firstId}")).ToHaveValueAsync("32");
            await page.Locator("#session-edit-order").ClickAsync();
            await page.Locator("#session-order-list .workout-selected__drag-handle").First.FocusAsync();
            await page.Keyboard.PressAsync(" ");
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("Enter");
            await page.Locator("#session-cancel").ClickAsync();
            await page.Locator("#discard-confirm").ClickAsync();
            await page.ReloadAsync();
            await Expect(page.Locator(".active-session__exercise-item").First).ToHaveAttributeAsync("data-exercise-id", secondId!);
            Assert.Equal(0, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_RefreshAndAutosaveMeetHundredTrialBudgets()
    {
        var page = await CreatePageAsync();
        try
        {
            var ids = new List<string>();
            for (var index = 0; index < 25; index++)
            {
                var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
                {
                    DataObject = new { name = $"Recovery budget exercise {index}", muscleIds = Array.Empty<string>() },
                });
                ids.Add((await response.JsonAsync())?.GetProperty("exerciseId").GetString()!);
            }
            var workout = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Recovery budget workout", exercises = ids.Select(exerciseId => new { exerciseId }).ToArray() },
            });
            var workoutId = (await workout.JsonAsync())?.GetProperty("plannedWorkoutId").GetString();
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=5");
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("saved");
            var requestCount = 0;
            page.Request += (_, request) => { if (request.Url.Contains("/api/")) Interlocked.Increment(ref requestCount); };
            var saveResults = await page.EvaluateAsync<JsonElement>("""
                () => {
                    const inputs = [...document.querySelectorAll('#session-exercises input[type=number]')];
                    inputs.forEach((input, i) => { input.value = String(i + 1); input.dispatchEvent(new Event('input')); });
                    const efforts = [...document.querySelectorAll('#session-exercises input[type=range]')];
                    efforts.forEach((input, i) => { input.value = String(i % 10 + 1); input.dispatchEvent(new Event('input')); });
                    const times = [];
                    for (let i = 0; i < 100; i++) {
                        const start = performance.now();
                        inputs[0].value = String(i + 100);
                        inputs[0].dispatchEvent(new Event('input'));
                        times.push(performance.now() - start);
                    }
                    const saved = document.querySelector('#draft-status').textContent.includes('saved');
                    const original = Storage.prototype.setItem;
                    Storage.prototype.setItem = () => { throw new Error('test failure'); };
                    const start = performance.now();
                    inputs[0].value = '200';
                    inputs[0].dispatchEvent(new Event('input'));
                    const failureTime = performance.now() - start;
                    const failed = document.querySelector('#draft-status').textContent.includes('could not be saved');
                    Storage.prototype.setItem = original;
                    document.querySelector('#draft-retry').click();
                    return { times, saved, failed, failureTime };
                }
                """);
            Assert.True(saveResults.GetProperty("saved").GetBoolean());
            Assert.True(saveResults.GetProperty("failed").GetBoolean());
            Assert.True(saveResults.GetProperty("failureTime").GetDouble() <= 2000);
            Assert.All(saveResults.GetProperty("times").EnumerateArray(), sample => Assert.True(sample.GetDouble() <= 2000));
            var autosaveRequests = Volatile.Read(ref requestCount);
            Assert.Equal(0, autosaveRequests);

            var refreshTimes = new List<double>();
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator($"#weight-{ids[0]}").WaitForAsync();
            for (var trial = 0; trial < 100; trial++)
            {
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                var timer = Stopwatch.StartNew();
                await page.Locator($"#weight-{ids[0]}").WaitForAsync();
                var restored = await page.EvaluateAsync<bool>("""
                    ids => {
                        const values = [...document.querySelectorAll('#session-exercises input[type=number]')].map(e => e.value);
                        const efforts = [...document.querySelectorAll('#session-exercises input[type=range]')];
                        const order = [...document.querySelectorAll('.active-session__exercise-item')].map(e => e.dataset.exerciseId);
                        const saved = JSON.parse(localStorage.getItem('workout-tracker-active-draft'));
                        return values.length === 25 && values[0] === '200' &&
                            values.slice(1).every((v, i) => v === String(i + 2)) &&
                            efforts.length === 25 && efforts.every((e, i) => e.value === String(i % 10 + 1) && e.dataset.touched === 'true') &&
                            order.every((id, i) => id === ids[i]) && saved.sets === 5;
                    }
                    """, ids.ToArray());
                timer.Stop();
                Assert.True(restored, $"Refresh trial {trial + 1} did not restore all values and sets.");
                refreshTimes.Add(timer.Elapsed.TotalMilliseconds);
            }
            Assert.True(refreshTimes.Count(time => time <= 2000) >= 95);
            Console.WriteLine($"Recovery budget: 100 saves max={saveResults.GetProperty("times").EnumerateArray().Max(x => x.GetDouble()):F2}ms; 100 refreshes max={refreshTimes.Max():F2}ms; <=2s={refreshTimes.Count(x => x <= 2000)}; autosave requests={autosaveRequests}");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_HundredCompletionRetriesAndDiscardsPreserveHistory()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            var mutationRequests = 0;
            page.Request += (_, request) =>
            {
                if (request.Url.Contains("/api/") && request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
                    Interlocked.Increment(ref mutationRequests);
            };
            for (var trial = 0; trial < 100; trial++)
            {
                await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
                await page.Locator($"#weight-{exerciseId}").FillAsync((trial + 1).ToString(CultureInfo.InvariantCulture));
                await page.RouteAsync(endpoint, async route => { await route.FetchAsync(); await route.AbortAsync(); });
                await page.Locator("#session-save").ClickAsync();
                await page.Locator("#effort-modal-skip").ClickAsync();
                await Expect(page.Locator("#draft-status")).ToContainTextAsync("may have succeeded");
                await page.UnrouteAsync(endpoint);
                await page.ReloadAsync();
                await page.Locator("#pending-retry").ClickAsync();
                await Expect(page).ToHaveURLAsync(new Regex("/history/session\\?id="));
                await page.ReloadAsync();
                Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
                var sessions = await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync();
                Assert.Equal(trial + 2, sessions?.GetArrayLength());
                Assert.Equal((trial + 1) * 2, Volatile.Read(ref mutationRequests));
            }
            for (var trial = 0; trial < 100; trial++)
            {
                await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
                await page.Locator($"#weight-{exerciseId}").FillAsync((trial + 1).ToString(CultureInfo.InvariantCulture));
                await page.Locator("#session-cancel").ClickAsync();
                await page.Locator("#discard-confirm").ClickAsync();
                await Expect(page).Not.ToHaveURLAsync(new Regex("/active-session"));
                await page.ReloadAsync();
                Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
                var sessions = await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync();
                Assert.Equal(101, sessions?.GetArrayLength());
                Assert.Equal(200, Volatile.Read(ref mutationRequests));
            }
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_SecondTabMustWaitForLock()
    {
        WebAppFixture.ResetExercises();
        WebAppFixture.ResetWorkouts();
        var context = await _playwright.Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_webApp.BaseUrl);
        IPage? other = null;
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("30");
            other = await context.NewPageAsync();
            await other.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await Expect(other.GetByRole(AriaRole.Alert)).ToContainTextAsync("another tab");
            await Expect(other.Locator("#session-save")).ToHaveCountAsync(0);
            await page.GotoAsync(_webApp.BaseUrl);
            await other.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true }).ClickAsync();
            await Expect(other.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("30");
            await other.Locator("#session-save").ClickAsync();
            await other.Locator("#effort-modal-skip").ClickAsync();
            await Expect(other).ToHaveURLAsync(new Regex("/history/session\\?id="));
            Assert.Equal(2, (await (await other.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await context.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_LostCompletionResponseRetriesFrozenOperation()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("50");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, async route =>
            {
                await route.FetchAsync();
                await route.AbortAsync();
            });
            await ObserveCompletionRequestsAsync(page);
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("may have succeeded");
            Assert.Equal(1, await page.EvaluateAsync<int>("JSON.parse(sessionStorage.getItem('completion-request-audit')).length"));
            await page.UnrouteAsync(endpoint);
            await page.APIRequest.DeleteAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}");
            await page.ReloadAsync();
            await Expect(page.Locator("#pending-retry")).ToHaveTextAsync("Retry completion");
            await ObserveCompletionRequestsAsync(page);
            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex("/history/session\\?id="));
            Assert.Equal(1, await page.EvaluateAsync<int>("JSON.parse(sessionStorage.getItem('completion-request-audit')).length"));
            Assert.Equal(2, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
        }
        finally { await page.CloseAsync(); }
    }

    private static Task<JsonElement?> ObserveCompletionRequestsAsync(IPage page) => page.EvaluateAsync("""
        () => {
            const original = window.fetch;
            let capturing = true;
            sessionStorage.setItem('completion-request-audit', '[]');
            window.fetch = (...args) => {
                if (capturing) {
                    const calls = JSON.parse(sessionStorage.getItem('completion-request-audit'));
                    calls.push(String(args[0]));
                    sessionStorage.setItem('completion-request-audit', JSON.stringify(calls));
                }
                return original(...args).finally(() => { capturing = false; });
            };
        }
        """);

    [Fact]
    public async Task ActiveSession_RejectedRetryDoesNotUnfreezeAnUncertainCompletion()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("50");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, route => route.FulfillAsync(new() { Status = 400, ContentType = "application/json", Body = """{"error":"Rejected"}""" }));
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("remains locked");
            await page.ReloadAsync();
            await Expect(page.Locator("#pending-retry")).ToBeVisibleAsync();
            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page.Locator("#pending-status")).ToContainTextAsync("remains locked");
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveCountAsync(0);
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_FencedRejectionAllowsCorrectionWithSameOperationId()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("50");
            var operationId = await page.EvaluateAsync<string>("JSON.parse(localStorage.getItem('workout-tracker-active-draft')).draftId");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, route => route.FulfillAsync(new()
            {
                Status = 400, ContentType = "application/json", Body = """{"error":"Correct the input","safeToEdit":true}""",
            }));
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("safely blocked");
            await page.Locator($"#weight-{exerciseId}").FillAsync("55");
            await page.UnrouteAsync(endpoint);
            Assert.Equal(operationId, await page.EvaluateAsync<string>("JSON.parse(localStorage.getItem('workout-tracker-active-draft')).draftId"));
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex("/history/session\\?id="));
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_FencedRejectionAfterRefreshOffersEditing()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("50");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, route => route.AbortAsync());
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("may have succeeded");
            await page.UnrouteAsync(endpoint);
            await page.ReloadAsync();
            await page.RouteAsync(endpoint, route => route.FulfillAsync(new()
            {
                Status = 400, ContentType = "application/json", Body = """{"error":"Correct the input","safeToEdit":true}""",
            }));
            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page.Locator("#pending-retry")).ToHaveTextAsync("Edit workout");
            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("50");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_DeletedCompletionCanBeExplicitlyCleared()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            var before = (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())!.Value;
            var existingId = before.EnumerateArray().Single().GetProperty("workoutSessionId").GetString();
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("50");
            var endpoint = $"**/api/workouts/{workoutId}/sessions";
            await page.RouteAsync(endpoint, async route => { await route.FetchAsync(); await route.AbortAsync(); });
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("may have succeeded");
            await page.UnrouteAsync(endpoint);
            var sessions = (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())!.Value;
            var newId = sessions.EnumerateArray().Select(s => s.GetProperty("workoutSessionId").GetString()).Single(id => id != existingId);
            await page.APIRequest.DeleteAsync($"{_webApp.BaseUrl}/api/sessions/{newId}");
            await page.ReloadAsync();
            await page.Locator("#pending-retry").ClickAsync();
            await Expect(page.Locator("#pending-error")).ToContainTextAsync("deleted");
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Clear deleted completion" }).ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex("/$"));
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_FailedAutosaveKeepsInputAndCanRetry()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("saved");
            await page.EvaluateAsync("""
                () => {
                    window.originalStorageSetItem = Storage.prototype.setItem;
                    Storage.prototype.setItem = function () { throw new Error("storage blocked"); };
                }
                """);
            await page.Locator($"#weight-{exerciseId}").FillAsync("72");
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("could not be saved");
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("72");
            await page.EvaluateAsync("() => { Storage.prototype.setItem = window.originalStorageSetItem; }");
            await page.Locator("#draft-retry").ClickAsync();
            await page.ReloadAsync();
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("72");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_InvalidDraftIsPreservedUntilExplicitDiscard()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.EvaluateAsync("localStorage.setItem('workout-tracker-active-draft', '{broken')");
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("invalid");
            Assert.Equal("{broken", await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Discard saved workout" }).ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("saved");
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_OversizedDraftRequiresExplicitDiscard()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.EvaluateAsync("localStorage.setItem('workout-tracker-active-draft', 'x'.repeat(1048577))");
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("too large");
            Assert.Equal(1048577, await page.EvaluateAsync<int>("localStorage.getItem('workout-tracker-active-draft').length"));
            page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Discard saved workout" }).ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("saved");
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_UnavailableRestoreCanBeRetriedWithoutLosingDraft()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("64");
            await page.AddInitScriptAsync("""
                (() => {
                    const original = Storage.prototype.getItem;
                    Storage.prototype.getItem = function(key) {
                        if (key === 'workout-tracker-active-draft' && sessionStorage.getItem('block-draft-read') === 'yes')
                            throw new Error('storage blocked');
                        return original.call(this, key);
                    };
                })();
                """);
            await page.EvaluateAsync("sessionStorage.setItem('block-draft-read', 'yes')");
            await page.ReloadAsync();
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("storage is unavailable");
            await page.EvaluateAsync("sessionStorage.removeItem('block-draft-read')");
            await page.GetByRole(AriaRole.Button, new() { Name = "Retry", Exact = true }).ClickAsync();
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("64");
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_OtherWorkoutCannotReplaceSavedDraft()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Other workout", exercises = new[] { new { exerciseId } } },
            });
            var otherId = (await response.JsonAsync())?.GetProperty("plannedWorkoutId").GetString();
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("64");
            await page.GotoAsync(_webApp.BaseUrl);
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Resume workout", Exact = true })).ToBeVisibleAsync();
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={otherId}&sets=3");
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Another workout");
            await page.GetByRole(AriaRole.Button, new() { Name = "Resume saved workout" }).ClickAsync();
            await Expect(page.Locator($"#weight-{exerciseId}")).ToHaveValueAsync("64");
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_UnavailableLockBlocksEditing()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.AddInitScriptAsync("Object.defineProperty(navigator, 'locks', { value: undefined });");
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("lock support");
            await Expect(page.Locator("#session-save")).ToHaveCountAsync(0);
            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_FailedCompletionCleanupCanBeRetriedWithoutDuplicate()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("55");
            await page.EvaluateAsync("""
                () => {
                    window.originalStorageRemoveItem = Storage.prototype.removeItem;
                    Storage.prototype.removeItem = function () { throw new Error('blocked'); };
                }
                """);
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#draft-status")).ToContainTextAsync("could not be removed");
            await page.EvaluateAsync("() => { Storage.prototype.removeItem = window.originalStorageRemoveItem; }");
            await page.Locator("#draft-retry").ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex("/history/session\\?id="));
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            Assert.Equal(2, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task ActiveSession_FailedDiscardKeepsDraftAndCanBeRetried()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.Locator($"#weight-{exerciseId}").FillAsync("55");
            await page.EvaluateAsync("""
                () => {
                    window.originalStorageRemoveItem = Storage.prototype.removeItem;
                    Storage.prototype.removeItem = function () { throw new Error('blocked'); };
                }
                """);
            await page.Locator("#session-cancel").ClickAsync();
            await page.Locator("#discard-confirm").ClickAsync();
            await Expect(page.Locator("#session-error")).ToContainTextAsync("Could not discard");
            Assert.NotNull(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            await page.EvaluateAsync("() => { Storage.prototype.removeItem = window.originalStorageRemoveItem; }");
            await page.Locator("#session-cancel").ClickAsync();
            await page.Locator("#discard-confirm").ClickAsync();
            await Expect(page).Not.ToHaveURLAsync(new Regex("/active-session"));
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
            Assert.Equal(1, (await (await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions")).JsonAsync())?.GetArrayLength());
        }
        finally { await page.CloseAsync(); }
    }

    [Fact]
    public async Task SessionDetail_LoadsTwentyFiveExercisesWithinRequestAndTimeBudgets()
    {
        var page = await CreatePageAsync();
        try
        {
            var exerciseIds = new List<string>();
            for (var index = 0; index < 25; index++)
            {
                var exerciseResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
                {
                    DataObject = new { name = $"Budget Exercise {index}", muscleIds = Array.Empty<string>() },
                });
                exerciseIds.Add((await exerciseResponse.JsonAsync())?.GetProperty("exerciseId").GetString()!);
            }

            var workoutResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Budget Workout",
                    exercises = exerciseIds.Select(exerciseId => new { exerciseId }).ToArray(),
                },
            });
            var workoutId = (await workoutResponse.JsonAsync())?.GetProperty("plannedWorkoutId").GetString()!;
            var sessionResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new
                {
                    sets = 5,
                    loggedExercises = exerciseIds.Select(exerciseId => new { exerciseId, loggedWeight = "50" }).ToArray(),
                },
            });
            var sessionId = (await sessionResponse.JsonAsync())?.GetProperty("workoutSessionId").GetString()!;

            var apiRequestCount = 0;
            page.Request += (_, request) =>
            {
                if (request.Url.StartsWith($"{_webApp.BaseUrl}/api/", StringComparison.Ordinal))
                {
                    apiRequestCount++;
                }
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.Locator(".session-detail__row").Nth(24).WaitForAsync();
            stopwatch.Stop();

            Assert.Equal(2, apiRequestCount);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Session detail took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static async Task<string> GetInlineColorAsync(IPage page, string selector)
    {
        return await page.EvaluateAsync<string>(
            @"sel => {
                const el = document.querySelector(sel);
                return el ? el.style.color : '';
            }",
            selector);
    }

    private static async Task<double> MeasureColorUpdateLatencyMsAsync(
        IPage page,
        string sliderSelector,
        string valueSelector,
        string sliderValue)
    {
        return await page.EvaluateAsync<double>(
            @"async ({ sliderSelector, valueSelector, sliderValue }) => {
                const slider = document.querySelector(sliderSelector);
                const valueEl = document.querySelector(valueSelector);
                if (!slider || !valueEl) return 10000;

                // Clear any previously applied inline colour so repeated samples don't short-circuit.
                valueEl.style.color = '';

                const start = performance.now();
                slider.value = sliderValue;
                slider.dispatchEvent(new Event('input', { bubbles: true }));

                const timeoutMs = 500;
                while ((performance.now() - start) <= timeoutMs) {
                    if ((valueEl.style.color ?? '').length > 0) return performance.now() - start;
                    await new Promise(resolve => requestAnimationFrame(resolve));
                }
                return timeoutMs + 1;
            }",
            new { sliderSelector, valueSelector, sliderValue });
    }

    private static async Task StubSessionDetailAsync(
        IPage page,
        Guid sessionId,
        IReadOnlyList<string> exerciseNames)
    {
        var exercises = exerciseNames.Select((name, index) => new
        {
            loggedExerciseId = Guid.NewGuid(),
            exerciseId = Guid.NewGuid(),
            exerciseName = name,
            loggedWeight = $"{50 + index}",
            effort = (index % 10) + 1,
            previousWeight = $"{45 + index}",
            previousSets = 3,
            previousEffort = ((index + 8) % 10) + 1,
        });
        var body = JsonSerializer.Serialize(new
        {
            workoutSessionId = sessionId,
            plannedWorkoutId = (Guid?)null,
            workoutName = "Sticky Column Workout",
            completedAt = "2026-09-19T10:00:00Z",
            sets = 5,
            overallEffort = 7,
            previousOverallEffort = 6,
            exercises,
        });

        await page.RouteAsync($"**/api/sessions/{sessionId}", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json",
                Body = body,
            });
        });
    }

    private static async Task<double> SetHorizontalScrollAsync(ILocator wrapper, double ratio)
    {
        return await wrapper.EvaluateAsync<double>(
            @"async (element, ratio) => {
                const maxScroll = element.scrollWidth - element.clientWidth;
                element.scrollLeft = maxScroll * ratio;
                await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                return element.scrollLeft;
            }",
            ratio);
    }

    private static async Task AssertStickyCellsAlignedAsync(
        ILocator wrapper,
        ILocator header,
        ILocator exerciseCell,
        double tolerance = 2)
    {
        var wrapperBox = await wrapper.BoundingBoxAsync();
        var headerBox = await header.BoundingBoxAsync();
        var exerciseBox = await exerciseCell.BoundingBoxAsync();

        Assert.NotNull(wrapperBox);
        Assert.NotNull(headerBox);
        Assert.NotNull(exerciseBox);
        Assert.InRange(Math.Abs(headerBox.X - wrapperBox.X), 0, tolerance);
        Assert.InRange(Math.Abs(exerciseBox.X - wrapperBox.X), 0, tolerance);
        Assert.InRange(Math.Abs(headerBox.X - exerciseBox.X), 0, tolerance);
    }

    private static async Task<double[]> MeasureScrollPaintLatenciesAsync(
        ILocator wrapper,
        IReadOnlyList<double> ratios)
    {
        return await wrapper.EvaluateAsync<double[]>(
            @"async (element, ratios) => {
                const samples = [];
                const maxScroll = element.scrollWidth - element.clientWidth;
                for (const ratio of ratios) {
                    const start = performance.now();
                    element.scrollLeft = maxScroll * ratio;
                    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                    samples.push(performance.now() - start);
                }
                return samples;
            }",
            ratios);
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
            await Expect(headers).ToHaveCountAsync(7);
            Assert.Equal(
                ["Exercise", "Weight (kg)", "Prev. Weight (kg)", "Sets", "Prev. Sets", "Effort", "Prev. Effort"],
                await headers.AllTextContentsAsync());
            var headersFitTheirColumns = await headers.EvaluateAllAsync<bool>(
                "elements => elements.every(element => element.scrollWidth <= element.clientWidth)");
            Assert.True(headersFitTheirColumns, "Session detail headers must not overflow into adjacent columns.");

            // Exercise row
            await Expect(page.Locator(".session-detail__cell--exercise").First).ToContainTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_ExerciseColumnStaysFixedWhileStatisticsScroll()
    {
        var page = await CreatePageAsync();
        var sessionId = Guid.NewGuid();
        try
        {
            await page.SetViewportSizeAsync(600, 800);
            await StubSessionDetailAsync(page, sessionId, ["Bench Press", "Incline Dumbbell Press", "Cable Fly"]);
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");

            var wrapper = page.Locator(".session-detail__table-wrapper");
            var header = page.Locator(".session-detail__th").First;
            var exerciseCell = page.Locator(".session-detail__cell--exercise").First;
            var statisticCell = page.Locator(".session-detail__row").First.Locator(".session-detail__cell").Nth(1);
            var lastHeader = page.Locator(".session-detail__th").Last;
            var hasOverflow = await wrapper.EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth");
            Assert.True(hasOverflow, "The session detail table must overflow at the test viewport.");

            var initialStatisticBox = await statisticCell.BoundingBoxAsync();
            var initialExerciseBox = await exerciseCell.BoundingBoxAsync();
            Assert.NotNull(initialStatisticBox);
            Assert.NotNull(initialExerciseBox);

            foreach (var ratio in new[] { 0d, 0.25d, 0.5d, 0.75d, 1d })
            {
                var scrollLeft = await SetHorizontalScrollAsync(wrapper, ratio);
                await AssertStickyCellsAlignedAsync(wrapper, header, exerciseCell);
                if (ratio > 0)
                {
                    Assert.True(scrollLeft > 0, $"Expected a positive scroll offset at ratio {ratio}.");
                    var statisticBox = await statisticCell.BoundingBoxAsync();
                    Assert.NotNull(statisticBox);
                    Assert.True(statisticBox.X < initialStatisticBox.X,
                        $"Statistic cell did not move left at ratio {ratio}.");
                }
            }

            var wrapperBox = await wrapper.BoundingBoxAsync();
            var exerciseBox = await exerciseCell.BoundingBoxAsync();
            var lastHeaderBox = await lastHeader.BoundingBoxAsync();
            Assert.NotNull(wrapperBox);
            Assert.NotNull(exerciseBox);
            Assert.NotNull(lastHeaderBox);
            Assert.True(lastHeaderBox.X + lastHeaderBox.Width <= wrapperBox.X + wrapperBox.Width + 2,
                "The final statistic column must be fully visible at maximum scroll.");

            var allExerciseCellsOccludeScrollingContent = await page
                .Locator(".session-detail__cell--exercise")
                .EvaluateAllAsync<bool>(
                    @"cells => cells.every(cell => {
                        const rect = cell.getBoundingClientRect();
                        return document.elementFromPoint(rect.right - 2, rect.top + (rect.height / 2))
                            ?.closest('td') === cell;
                    })");
            Assert.True(allExerciseCellsOccludeScrollingContent,
                "Every tested exercise cell must cover statistic content scrolling behind it.");

            await SetHorizontalScrollAsync(wrapper, 0);
            await AssertStickyCellsAlignedAsync(wrapper, header, exerciseCell);
            var resetExerciseBox = await exerciseCell.BoundingBoxAsync();
            Assert.NotNull(resetExerciseBox);
            Assert.InRange(Math.Abs(resetExerciseBox.X - initialExerciseBox.X), 0, 2);
        }
        finally
        {
            await page.UnrouteAsync($"**/api/sessions/{sessionId}");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditModeKeepsExerciseColumnFixed()
    {
        var page = await CreatePageAsync();
        var sessionId = Guid.NewGuid();
        try
        {
            await page.SetViewportSizeAsync(600, 800);
            await StubSessionDetailAsync(page, sessionId, ["Bench Press", "Incline Dumbbell Press"]);
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync("#session-detail-edit");
            await page.Locator("#session-detail-edit").ClickAsync();

            var wrapper = page.Locator(".session-detail__table-wrapper");
            var header = page.Locator(".session-detail__th").First;
            var exerciseCell = page.Locator(".session-detail__cell--exercise").First;
            var statisticControls = wrapper.Locator(".session-detail__input, .session-detail__select");
            var initialControlXs = await statisticControls.EvaluateAllAsync<double[]>(
                "controls => controls.map(control => control.getBoundingClientRect().x)");
            Assert.NotEmpty(initialControlXs);

            await SetHorizontalScrollAsync(wrapper, 0.5);
            await AssertStickyCellsAlignedAsync(wrapper, header, exerciseCell);
            var scrolledControlXs = await statisticControls.EvaluateAllAsync<double[]>(
                "controls => controls.map(control => control.getBoundingClientRect().x)");
            Assert.Equal(initialControlXs.Length, scrolledControlXs.Length);
            for (var index = 0; index < initialControlXs.Length; index++)
            {
                Assert.True(scrolledControlXs[index] < initialControlXs[index],
                    $"Editable statistic control {index} did not move with the scrolling columns.");
            }
        }
        finally
        {
            await page.UnrouteAsync($"**/api/sessions/{sessionId}");
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public async Task SessionDetailPage_FixedColumnUsesOpaqueThemeSurfaces(string theme)
    {
        var page = await CreatePageAsync();
        var sessionId = Guid.NewGuid();
        try
        {
            await page.EvaluateAsync(
                "theme => localStorage.setItem('workout-tracker-theme', theme)",
                theme);
            await StubSessionDetailAsync(page, sessionId, ["Bench Press"]);
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");

            var wrapper = page.Locator(".session-detail__table-wrapper");
            await SetHorizontalScrollAsync(wrapper, 0.5);
            var styles = await page.EvaluateAsync<string[]>(
                @"() => {
                    const header = document.querySelector('.session-detail__th:first-child');
                    const body = document.querySelector('.session-detail__cell--exercise');
                    const headerRow = document.querySelector('.session-detail__head-row');
                    const table = document.querySelector('.session-detail__table');
                    const wrapper = document.querySelector('.session-detail__table-wrapper');
                    const overallEffort = document.querySelector('.session-detail__overall-effort-row');
                    if (!header || !body || !headerRow || !table || !wrapper || !overallEffort) return [];
                    const headerStyle = getComputedStyle(header);
                    const bodyStyle = getComputedStyle(body);
                    return [
                        headerStyle.backgroundColor,
                        getComputedStyle(headerRow).backgroundColor,
                        bodyStyle.backgroundColor,
                        getComputedStyle(table).backgroundColor,
                        headerStyle.zIndex,
                        bodyStyle.zIndex,
                        headerStyle.borderBottomWidth,
                        headerStyle.boxShadow,
                        bodyStyle.boxShadow,
                        getComputedStyle(wrapper).borderBottomWidth,
                        getComputedStyle(overallEffort).borderTopWidth,
                    ];
                }");

            Assert.Equal(11, styles.Length);
            Assert.Equal(styles[1], styles[0]);
            Assert.Equal(styles[1], styles[2]);
            Assert.DoesNotContain("rgba(0, 0, 0, 0)", styles.Take(4));
            Assert.Equal("3", styles[4]);
            Assert.Equal("2", styles[5]);
            Assert.Equal("0px", styles[6]);
            Assert.Equal("none", styles[7]);
            Assert.Equal("none", styles[8]);
            Assert.Equal("1px", styles[9]);
            Assert.Equal("0px", styles[10]);
        }
        finally
        {
            await page.UnrouteAsync($"**/api/sessions/{sessionId}");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_ExerciseColumnConstrainsMaximumLengthName()
    {
        var page = await CreatePageAsync();
        var sessionId = Guid.NewGuid();
        try
        {
            await page.SetViewportSizeAsync(600, 800);
            var longName = string.Concat(Enumerable.Repeat("Maximum Length Exercise Name ", 6))[..150];
            await StubSessionDetailAsync(page, sessionId, ["Abductor", longName, "Leg Press"]);
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");

            var exerciseCells = page.Locator(".session-detail__cell--exercise");
            var longestCell = exerciseCells.Nth(1);
            var measurements = await longestCell.EvaluateAsync<JsonElement>(
                @"element => {
                    const range = document.createRange();
                    range.selectNodeContents(element);
                    const style = getComputedStyle(element);
                    return {
                        cellWidth: element.getBoundingClientRect().width,
                        textWidth: range.getBoundingClientRect().width,
                        paddingLeft: parseFloat(style.paddingLeft),
                        paddingRight: parseFloat(style.paddingRight),
                        whiteSpace: style.whiteSpace,
                        overflow: style.overflow,
                        textOverflow: style.textOverflow,
                    };
                }");
            var requiredWidth = measurements.GetProperty("textWidth").GetDouble()
                + measurements.GetProperty("paddingLeft").GetDouble()
                + measurements.GetProperty("paddingRight").GetDouble();
            var cellWidth = measurements.GetProperty("cellWidth").GetDouble();
            var wrapper = page.Locator(".session-detail__table-wrapper");
            var wrapperBox = await wrapper.BoundingBoxAsync();
            Assert.NotNull(wrapperBox);

            Assert.Equal("nowrap", measurements.GetProperty("whiteSpace").GetString());
            Assert.Equal("hidden", measurements.GetProperty("overflow").GetString());
            Assert.Equal("ellipsis", measurements.GetProperty("textOverflow").GetString());
            Assert.True(measurements.GetProperty("paddingRight").GetDouble() > 0,
                "The exercise column must leave trailing space after the longest name.");
            Assert.True(cellWidth < requiredWidth,
                "A maximum-length exercise name must be truncated rather than widening the sticky column indefinitely.");
            Assert.True(cellWidth < wrapperBox.Width,
                "The sticky Exercise column must leave visible space for statistic columns.");
            await Expect(longestCell).ToHaveAttributeAsync("title", longName);
            await Expect(longestCell).ToHaveAttributeAsync("aria-label", longName);

            var columnWidths = await exerciseCells.EvaluateAllAsync<double[]>(
                "cells => cells.map(cell => cell.getBoundingClientRect().width)");
            Assert.All(columnWidths, width => Assert.InRange(Math.Abs(width - cellWidth), 0, 1));

            await SetHorizontalScrollAsync(wrapper, 1);
            await AssertStickyCellsAlignedAsync(
                wrapper,
                page.Locator(".session-detail__th").First,
                longestCell);
            var lastHeaderBox = await page.Locator(".session-detail__th").Last.BoundingBoxAsync();
            Assert.NotNull(lastHeaderBox);
            Assert.True(lastHeaderBox.X + lastHeaderBox.Width <= wrapperBox.X + wrapperBox.Width + 2,
                "The final statistic column must remain fully visible with a maximum-length exercise name.");
            await SetHorizontalScrollAsync(wrapper, 0);
            await AssertStickyCellsAlignedAsync(
                wrapper,
                page.Locator(".session-detail__th").First,
                longestCell);
        }
        finally
        {
            await page.UnrouteAsync($"**/api/sessions/{sessionId}");
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public async Task SessionDetailPage_FiftyRowsScrollPaintMeetsBudget(string theme)
    {
        var page = await CreatePageAsync();
        var sessionId = Guid.NewGuid();
        try
        {
            await page.SetViewportSizeAsync(600, 900);
            await page.EvaluateAsync(
                "selectedTheme => localStorage.setItem('workout-tracker-theme', selectedTheme)",
                theme);
            var exerciseNames = Enumerable.Range(1, 50).Select(index => $"Exercise {index}").ToArray();
            await StubSessionDetailAsync(page, sessionId, exerciseNames);
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__row:nth-child(50)");

            var offsets = new[] { 0d, 0.25d, 0.5d, 0.75d, 1d };
            var ratios = new double[20];
            for (var index = 0; index < ratios.Length; index++)
            {
                ratios[index] = offsets[index % offsets.Length];
            }
            var samples = await MeasureScrollPaintLatenciesAsync(
                page.Locator(".session-detail__table-wrapper"),
                ratios);
            var passingSamples = samples.Count(sample => sample <= 100);
            var orderedSamples = samples.OrderBy(sample => sample).ToArray();
            var p95 = orderedSamples[18];

            var formattedSamples = string.Join(", ",
                samples.Select(sample => sample.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)));
            Console.WriteLine(FormattableString.Invariant(
                $"Sticky column {theme} samples (ms): {formattedSamples}; p95={p95:F2}"));
            Assert.True(passingSamples >= 19,
                FormattableString.Invariant(
                    $"Expected at least 19 of 20 {theme} samples within 100ms, got {passingSamples}. p95={p95:F2}ms."));
        }
        finally
        {
            await page.UnrouteAsync($"**/api/sessions/{sessionId}");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_OmitsUndefinedText_WhenLegacyResponseLacksSetsFields()
    {
        var page = await CreatePageAsync();
        var sessionId = Guid.NewGuid();
        try
        {
            await page.RouteAsync($"**/api/sessions/{sessionId}", async route =>
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    ContentType = "application/json",
                    Body = $$"""
                    {
                      "workoutSessionId":"{{sessionId}}",
                      "plannedWorkoutId":null,
                      "workoutName":"Legacy Workout",
                      "completedAt":"2026-09-19T10:00:00Z",
                      "overallEffort":null,
                      "previousOverallEffort":null,
                      "exercises":[{
                        "loggedExerciseId":"{{Guid.NewGuid()}}",
                        "exerciseId":"{{Guid.NewGuid()}}",
                        "exerciseName":"Bench Press",
                        "loggedWeight":"80",
                        "effort":7,
                        "previousWeight":"75",
                        "previousEffort":6
                      }]
                    }
                    """,
                });
            });

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");

            await Expect(page.Locator(".session-detail__table")).Not.ToContainTextAsync("undefined");
            var rowCells = page.Locator(".session-detail__row").First.Locator(".session-detail__cell");
            await Expect(rowCells.Nth(3)).ToHaveTextAsync("—");
            await Expect(rowCells.Nth(4)).ToHaveTextAsync("—");
        }
        finally
        {
            await page.UnrouteAsync($"**/api/sessions/{sessionId}");
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
    public async Task SessionDetailPage_EditSession_SavesExerciseAndOverallEffort()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page, exerciseName: "Bench Press", workoutName: "Edit Push Day");
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator(".session-detail__input").First.FillAsync("82.5");
            await page.Locator("[data-session-edit-sets]").First.SelectOptionAsync("5");
            await page.Locator("[data-session-edit-effort]").First.SelectOptionAsync("9");
            await page.Locator("#session-edit-overall-effort").SelectOptionAsync("8");
            await page.Locator("#session-detail-save").ClickAsync();

            await Expect(page.Locator("#session-detail-edit")).ToBeVisibleAsync();
            var cells = page.Locator(".session-detail__row").First.Locator(".session-detail__cell");
            await Expect(cells.Nth(1)).ToContainTextAsync("82.5");
            await Expect(cells.Nth(3)).ToContainTextAsync("5");
            await Expect(cells.Nth(5)).ToContainTextAsync("9");
            await Expect(page.Locator(".session-detail__overall-effort-value")).ToContainTextAsync("8");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionApi_EditSession_RejectsMissingJsonBody()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Missing Body Press");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First().GetProperty("exerciseId").GetString()!;

            var workoutResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Missing Body Workout",
                    exercises = new[] { new { exerciseId } },
                },
            });
            var workout = await workoutResponse.JsonAsync();
            var workoutId = workout?.GetProperty("plannedWorkoutId").GetString()!;

            var sessionResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 8, loggedExercises = new[] { new { exerciseId, loggedWeight = "55 KG", effort = 7 } } },
            });
            var session = await sessionResponse.JsonAsync();
            var sessionId = session?.GetProperty("workoutSessionId").GetString()!;

            var response = await page.APIRequest.PutAsync($"{_webApp.BaseUrl}/api/sessions/{sessionId}");

            Assert.Equal(400, response.Status);
            var error = await response.JsonAsync();
            Assert.Equal("A JSON request body is required.", error?.GetProperty("error").GetString());

            var detailResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/sessions/{sessionId}");
            var detail = await detailResponse.JsonAsync();
            Assert.Equal(8, detail?.GetProperty("overallEffort").GetInt32());
            var exercise = detail?.GetProperty("exercises").EnumerateArray().Single();
            Assert.Equal("55 KG", exercise?.GetProperty("loggedWeight").GetString());
            Assert.Equal(7, exercise?.GetProperty("effort").GetInt32());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_PersistsAfterReopen()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page, exerciseName: "Squat", workoutName: "Edit Legs Day");
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator(".session-detail__input").First.FillAsync("120");
            await page.Locator("[data-session-edit-sets]").First.SelectOptionAsync("5");
            await page.Locator("[data-session-edit-effort]").First.SelectOptionAsync("7");
            await page.Locator("#session-edit-overall-effort").SelectOptionAsync("6");
            await page.Locator("#session-detail-save").ClickAsync();
            await Expect(page.Locator("#session-detail-edit")).ToBeVisibleAsync();

            await page.Locator(".session-detail__back").ClickAsync();
            await page.WaitForSelectorAsync(".history-page");
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            var cells = page.Locator(".session-detail__row").First.Locator(".session-detail__cell");
            await Expect(cells.Nth(1)).ToContainTextAsync("120");
            await Expect(cells.Nth(3)).ToContainTextAsync("5");
            await Expect(cells.Nth(5)).ToContainTextAsync("7");
            await Expect(page.Locator(".session-detail__overall-effort-value")).ToContainTextAsync("6");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_PreviousOverallEffortUsesCorrectedSourceData()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Deadlift");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First().GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Overall Comparison Day",
                    exercises = new[] { new { exerciseId } },
                },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            var firstResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 8, loggedExercises = new[] { new { exerciseId, loggedWeight = "100" } } },
            });
            var firstSession = await firstResponse.JsonAsync();
            var firstSessionId = firstSession?.GetProperty("workoutSessionId").GetString()!;

            var secondResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 6, loggedExercises = new[] { new { exerciseId, loggedWeight = "105" } } },
            });
            var secondSession = await secondResponse.JsonAsync();
            var secondSessionId = secondSession?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={firstSessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");
            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator("#session-edit-overall-effort").SelectOptionAsync("4");
            await page.Locator("#session-detail-save").ClickAsync();
            await Expect(page.Locator("#session-detail-edit")).ToBeVisibleAsync();

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={secondSessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");
            await Expect(page.Locator(".session-detail__overall-effort-prev-value")).ToContainTextAsync("4");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_CancelWithChanges_ShowsDiscardModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator(".session-detail__input").First.FillAsync("90");
            await page.Locator("#session-detail-cancel").ClickAsync();

            await Expect(page.Locator("#session-edit-discard-backdrop")).ToBeVisibleAsync();
            await page.Locator("#session-edit-discard-cancel").ClickAsync();
            await Expect(page.Locator("#session-edit-discard-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator(".session-detail__input").First).ToHaveValueAsync("90");

            await page.Locator("#session-detail-cancel").ClickAsync();
            await page.Locator("#session-edit-discard-confirm").ClickAsync();
            await Expect(page.Locator("#session-detail-edit")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_CancelWithoutChanges_ExitsEditModeWithoutDiscardPrompt()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator("#session-detail-cancel").ClickAsync();

            await Expect(page.Locator("#session-edit-discard-backdrop")).ToHaveCountAsync(0);
            await Expect(page.Locator("#session-detail-edit")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_BackWithChanges_UsesDiscardModalBeforeLeaving()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator(".session-detail__input").First.FillAsync("88");
            await page.Locator(".session-detail__back").ClickAsync();

            await Expect(page.Locator("#session-edit-discard-backdrop")).ToBeVisibleAsync();
            await page.Locator("#session-edit-discard-cancel").ClickAsync();
            await Expect(page).ToHaveURLAsync(new Regex(@"/history/session\?id="));
            await Expect(page.Locator(".session-detail__input").First).ToHaveValueAsync("88");

            await page.Locator(".session-detail__back").ClickAsync();
            await page.Locator("#session-edit-discard-confirm").ClickAsync();
            await page.WaitForSelectorAsync(".history-page");
            await Expect(page.Locator(".history-page")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_DiscardModal_TrapsKeyboardFocus()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator(".session-detail__input").First.FillAsync("90");
            await page.Locator("#session-detail-cancel").ClickAsync();

            await Expect(page.Locator("#session-edit-discard-confirm")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#session-edit-discard-cancel")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#session-edit-discard-confirm")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Expect(page.Locator("#session-edit-discard-cancel")).ToBeFocusedAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_SaveFailure_KeepsEditValues()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);
            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync(".session-detail__table");

            await page.RouteAsync("**/api/sessions/*", async route =>
            {
                if (route.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    await route.FulfillAsync(new()
                    {
                        Status = 500,
                        ContentType = "application/json",
                        Body = """{"error":"Unable to save."}""",
                    });
                    return;
                }

                await route.FallbackAsync();
            });

            await page.Locator("#session-detail-edit").ClickAsync();
            await page.Locator(".session-detail__input").First.FillAsync("95");
            await page.Locator("[data-session-edit-sets]").First.SelectOptionAsync("5");
            await page.Locator("[data-session-edit-effort]").First.SelectOptionAsync("10");
            await page.Locator("#session-detail-save").ClickAsync();

            await Expect(page.Locator("#session-detail-edit-error")).ToContainTextAsync("Unable to save.");
            await Expect(page.Locator(".session-detail__input").First).ToHaveValueAsync("95");
            await Expect(page.Locator("[data-session-edit-sets]").First).ToHaveValueAsync("5");
            await Expect(page.Locator("[data-session-edit-effort]").First).ToHaveValueAsync("10");
        }
        finally
        {
            await page.UnrouteAsync("**/api/sessions/*");
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_EditSession_SynchronizesSessionSetsAndPersists()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await SeedExerciseAsync(page, "Overhead Press");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercises = (await exercisesResponse.JsonAsync())!.Value.EnumerateArray().ToArray();
            var createWorkoutResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Synchronized Sets",
                    exercises = exercises.Select(ex => new { exerciseId = ex.GetProperty("exerciseId").GetString() }).ToArray(),
                },
            });
            var workoutId = (await createWorkoutResponse.JsonAsync())?.GetProperty("plannedWorkoutId").GetString()!;
            var sessionResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new
                {
                    sets = 3,
                    loggedExercises = exercises.Select(ex => new
                    {
                        exerciseId = ex.GetProperty("exerciseId").GetString(),
                        loggedWeight = "50",
                    }).ToArray(),
                },
            });
            var sessionId = (await sessionResponse.JsonAsync())?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");
            await page.Locator("#session-detail-edit").ClickAsync();

            var selects = page.Locator("[data-session-edit-sets]");
            await Expect(selects).ToHaveCountAsync(2);
            await Expect(selects.First).ToHaveAttributeAsync("aria-describedby", "session-edit-sets-description");
            await selects.First.SelectOptionAsync("5");
            await Expect(selects.Nth(1)).ToHaveValueAsync("5");
            await page.Locator("#session-detail-save").ClickAsync();

            var rows = page.Locator(".session-detail__row");
            await Expect(rows.Nth(0).Locator(".session-detail__cell").Nth(3)).ToHaveTextAsync("5");
            await Expect(rows.Nth(1).Locator(".session-detail__cell").Nth(3)).ToHaveTextAsync("5");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_OmitsUndefinedSets_WhenLegacyPreviousResponseLacksSets()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, exerciseId) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.RouteAsync($"**/api/workouts/{workoutId}/previous-performance", async route =>
            {
                await route.FulfillAsync(new()
                {
                    Status = 200,
                    ContentType = "application/json",
                    Body = $$"""
                    {
                      "hasPreviousSession":true,
                      "completedAt":"2026-09-19T10:00:00Z",
                      "exercises":[{
                        "exerciseId":"{{exerciseId}}",
                        "loggedWeight":"80",
                        "effort":7,
                        "sequence":0,
                        "completedAt":"2026-09-19T10:00:00Z"
                      }]
                    }
                    """,
                });
            });

            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=5");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var previous = page.Locator($"#previous-{exerciseId}");
            await Expect(previous).ToContainTextAsync("80 KG");
            await Expect(previous).Not.ToContainTextAsync("undefined");
            await Expect(previous).Not.ToContainTextAsync("sets");
        }
        finally
        {
            await page.UnrouteAllAsync();
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task StartPageSelectedSets_PersistThroughSaveAndHistoryDetail()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Persisted Sets Exercise");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Persisted Sets Workout", "Persisted Sets Exercise");

            await page.GotoAsync(_webApp.BaseUrl);
            await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(
                new() { State = WaitForSelectorState.Attached });
            await page.Locator("#workout-select").SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await page.Locator("#sets-select").SelectOptionAsync("5");
            await page.Locator("#workout-form button[type='submit']").ClickAsync();
            await page.WaitForSelectorAsync(".active-session");
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await page.WaitForURLAsync(new Regex(@".*/history/session\?id="));
            await page.WaitForSelectorAsync(".session-detail__table");
            await Expect(page.Locator(".session-detail__row").First.Locator(".session-detail__cell").Nth(3))
                .ToHaveTextAsync("5");
            await Expect(page.Locator("#session-chart-select")).ToBeEnabledAsync(new() { Timeout = 15000 });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("&sets=4")]
    [InlineData("&sets=abc")]
    public async Task ActiveSession_MissingOrInvalidSetsCannotSave(string setsQuery)
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            var createRequests = 0;
            page.Request += (_, request) =>
            {
                if (request.Method == "POST" && request.Url.Contains($"/api/workouts/{workoutId}/sessions", StringComparison.Ordinal))
                {
                    createRequests++;
                }
            };

            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}{setsQuery}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Sets must be 3 or 5");
            await Expect(page.Locator("#session-save")).ToHaveCountAsync(0);
            Assert.Equal(0, createRequests);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_SaveSendsOneTopLevelSetsValue()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            string? requestBody = null;
            page.Request += (_, request) =>
            {
                if (request.Method == "POST" && request.Url.Contains($"/api/workouts/{workoutId}/sessions", StringComparison.Ordinal))
                {
                    requestBody = request.PostData;
                }
            };

            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=5");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();
            await page.WaitForURLAsync(new Regex(@".*/history/session\?id="));

            using var document = System.Text.Json.JsonDocument.Parse(requestBody!);
            Assert.Equal(5, document.RootElement.GetProperty("sets").GetInt32());
            Assert.All(document.RootElement.GetProperty("loggedExercises").EnumerateArray(), exercise =>
                Assert.False(exercise.TryGetProperty("sets", out _)));
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
                    sets = 3,
                    loggedExercises = new[] { new { exerciseId, loggedWeight = "70 KG", effort = 6 } },
                },
            });

            // Second session: weight 75 KG, effort 7
            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new
                {
                    sets = 5,
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
            await Expect(cells.Nth(3)).ToContainTextAsync("5");
            await Expect(cells.Nth(4)).ToContainTextAsync("3");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_PreviousColumns_FallBackToOlderUsableData_WhenPriorSessionSkippedExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First().GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Review Fallback Push Day",
                    exercises = new[] { new { exerciseId } },
                },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 3, loggedExercises = new[] { new { exerciseId, loggedWeight = "70 KG", effort = 6 } } },
            });

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 5, loggedExercises = new[] { new { exerciseId } } },
            });

            var currentSessionResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 5, loggedExercises = new[] { new { exerciseId, loggedWeight = "75 KG", effort = 7 } } },
            });
            var currentSession = await currentSessionResponse.JsonAsync();
            var sessionId = currentSession?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForSelectorAsync(".session-detail__table");

            var cells = page.Locator(".session-detail__row").First.Locator(".session-detail__cell");
            await Expect(cells.Nth(2)).ToContainTextAsync("70 KG");
            await Expect(cells.Nth(4)).ToContainTextAsync("3");
            await Expect(cells.Nth(6)).ToContainTextAsync("6");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_LastTime_FallsBackToOlderUsableData_WhenLatestSessionSkippedExercise()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Romanian Deadlift");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray().First().GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new
                {
                    name = "Fallback Pull Day",
                    exercises = new[] { new { exerciseId } },
                },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 3, loggedExercises = new[] { new { exerciseId, loggedWeight = "70", effort = 6 } } },
            });

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 5, loggedExercises = new[] { new { exerciseId } } },
            });

            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var previousValue = page.Locator(".active-session__previous-value").First;
            await Expect(previousValue).ToContainTextAsync("70 KG");
            await Expect(previousValue).ToContainTextAsync("3 sets");
            await Expect(previousValue).ToContainTextAsync("6");
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
            await page.Locator("#discard-confirm").ClickAsync();

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
    public async Task ActiveSession_CancelWithoutChanges_ConfirmsDiscard()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press");
            await NavigateToWorkoutsAsync(page);
            await CreateWorkoutViaUIAsync(page, "Push Day", "Bench Press");

            await StartWorkoutViaPrestartModalAsync(page);
            await Expect(page).ToHaveURLAsync(new Regex(@"/active-session\?id="));

            await Expect(page.Locator("#draft-status")).ToContainTextAsync("saved");
            await page.Locator("#session-cancel").ClickAsync();
            await Expect(page.Locator("#discard-backdrop")).ToBeVisibleAsync();
            await page.Locator("#discard-confirm").ClickAsync();
            await Expect(page).Not.ToHaveURLAsync(new Regex(@"/active-session"));
            Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
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
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
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
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#effort-modal-skip").ClickAsync();

            await page.WaitForURLAsync(new Regex(@".*/history/session\?id="));

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
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();

            await Expect(page.Locator("#effort-modal-save")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#effort-modal-skip")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#effort-modal-close")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#overall-effort-slider")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Tab");
            await Expect(page.Locator("#effort-modal-save")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Expect(page.Locator("#overall-effort-slider")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Expect(page.Locator("#effort-modal-close")).ToBeFocusedAsync();
            await page.Keyboard.PressAsync("Shift+Tab");
            await Expect(page.Locator("#effort-modal-skip")).ToBeFocusedAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_CloseButton_DismissesWithoutSaving()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync();

            await page.Locator("#effort-modal-close").ClickAsync();

            await Expect(page.Locator("#effort-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator("#session-save")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_BackdropClick_DismissesWithoutSaving()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync();

            await page.Locator("#effort-backdrop").ClickAsync(new LocatorClickOptions { Position = new Position { X = 5, Y = 5 } });

            await Expect(page.Locator("#effort-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator("#session-save")).ToBeVisibleAsync();

            await page.Locator("#session-save").ClickAsync();
            await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync();
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
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            // Move the slider to 7
            await page.Locator("#overall-effort-slider").FillAsync("7");
            await page.Locator("#overall-effort-slider").DispatchEventAsync("input");
            await page.Locator("#effort-modal-save").ClickAsync();

            await page.WaitForURLAsync(new Regex(@".*/history/session\?id="));

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

    [Fact]
    public async Task ActiveSession_PerExerciseEffortSlider_AppliesExpectedColour()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var slider = page.Locator(".active-session__effort-slider").First;
            await slider.FillAsync("7");
            await slider.DispatchEventAsync("input");

            var valueColor = await GetInlineColorAsync(page, ".active-session__effort-value");
            var bandColor = await GetInlineColorAsync(page, ".active-session__effort-band");
            var expected = await page.EvaluateAsync<string>(
                @"() => { const el = document.createElement('span'); el.style.color = '#F97316'; return el.style.color; }");
            Assert.Equal(expected, valueColor);
            Assert.Equal(expected, bandColor);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_AppliesExpectedColour()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync();

            await page.Locator("#overall-effort-slider").FillAsync("10");
            await page.Locator("#overall-effort-slider").DispatchEventAsync("input");

            var valueColor = await GetInlineColorAsync(page, "#overall-effort-value");
            var bandColor = await GetInlineColorAsync(page, "#overall-effort-band");
            var expected = await page.EvaluateAsync<string>(
                @"() => { const el = document.createElement('span'); el.style.color = '#DC2626'; return el.style.color; }");
            Assert.Equal(expected, valueColor);
            Assert.Equal(expected, bandColor);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_EffortColours_AreConsistentAcrossSliders()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var perExerciseSlider = page.Locator(".active-session__effort-slider").First;
            await perExerciseSlider.FillAsync("5");
            await perExerciseSlider.DispatchEventAsync("input");
            var perExerciseColor = await GetInlineColorAsync(page, ".active-session__effort-value");

            await page.Locator("#session-save").ClickAsync();
            await page.Locator("#overall-effort-slider").FillAsync("5");
            await page.Locator("#overall-effort-slider").DispatchEventAsync("input");
            var overallColor = await GetInlineColorAsync(page, "#overall-effort-value");

            Assert.Equal(perExerciseColor, overallColor);
            Assert.NotEqual(string.Empty, overallColor);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SaveWorkout_EffortModal_UntouchedState_IsNeutral()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await page.Locator("#session-save").ClickAsync();
            await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync();

            await Expect(page.Locator("#overall-effort-value")).ToHaveTextAsync("Not rated");
            await Expect(page.Locator("#overall-effort-band")).ToHaveTextAsync(string.Empty);

            var valueInlineColor = await page.EvaluateAsync<string>(
                "document.querySelector('#overall-effort-value')?.style.color ?? ''");
            var bandInlineColor = await page.EvaluateAsync<string>(
                "document.querySelector('#overall-effort-band')?.style.color ?? ''");
            var sliderInlineAccent = await page.EvaluateAsync<string>(
                "document.querySelector('#overall-effort-slider')?.style.accentColor ?? ''");

            Assert.Equal(string.Empty, valueInlineColor);
            Assert.Equal(string.Empty, bandInlineColor);
            Assert.Equal(string.Empty, sliderInlineAccent);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task ActiveSession_EffortColourUpdate_LatencyMeetsBudget()
    {
        var page = await CreatePageAsync();
        try
        {
            var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}&sets=3");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var sampleCount = 20;
            var passCount = 0;

            for (var i = 0; i < sampleCount; i++)
            {
                var elapsed = await MeasureColorUpdateLatencyMsAsync(
                    page,
                    ".active-session__effort-slider",
                    ".active-session__effort-value",
                    "5");
                if (elapsed <= 100)
                {
                    passCount++;
                }
            }

            Assert.True(passCount >= 19, $"Expected at least 19/20 interactions <= 100ms, got {passCount}/20.");
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

    // --- T007: Chart section renders for a planned workout session ---

    [Fact]
    public async Task SessionDetailPage_ShowsChartSection_WhenWorkoutHasPreviousSession()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Squat");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Squat")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Leg Day Chart", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            // First session (provides historical data)
            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 6, loggedExercises = new[] { new { exerciseId, loggedWeight = "100" } } },
            });

            // Second session (the one we navigate to)
            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 7, loggedExercises = new[] { new { exerciseId, loggedWeight = "105" } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Chart section must be visible
            await Expect(page.Locator(".session-chart")).ToBeVisibleAsync();
            // Dropdown must exist and be enabled
            await Expect(page.Locator("#session-chart-select")).ToBeVisibleAsync();
            await Expect(page.Locator("#session-chart-select")).ToBeEnabledAsync(new() { Timeout = 15000 });
            // SVG chart must be rendered
            await Expect(page.Locator(".session-chart__svg")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // --- T008: No chart section for ad-hoc sessions ---
    // NOTE: No POST /api/sessions endpoint exists (sessions are always linked to a planned workout).
    // Ad-hoc session (plannedWorkoutId == null) path cannot be tested via E2E.
    // This case is covered by the unit-level guard in session-detail.ts:
    //   if (session.plannedWorkoutId === null) return;

    // --- T011: Dropdown switches between series ---

    [Fact]
    public async Task SessionDetailPage_ChartDropdown_SwitchesSeriesAndRendersNewSvg()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Deadlift");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Deadlift")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Pull Day Chart", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 5, loggedExercises = new[] { new { exerciseId, loggedWeight = "80", effort = 5 } } },
            });
            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 7, loggedExercises = new[] { new { exerciseId, loggedWeight = "90", effort = 7 } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var selectEl = page.Locator("#session-chart-select");
            await Expect(selectEl).ToBeEnabledAsync(new() { Timeout = 15000 });

            // Default is "overall" — SVG should already be present
            await Expect(page.Locator(".session-chart__svg")).ToBeVisibleAsync();

            // Switch to exercise combined series (weight + effort)
            await selectEl.SelectOptionAsync(new SelectOptionValue { Label = "Deadlift" });
            await Expect(page.Locator(".session-chart__svg")).ToBeVisibleAsync();

            // Switch back to overall
            await selectEl.SelectOptionAsync(new SelectOptionValue { Value = "overall" });
            await Expect(page.Locator(".session-chart__svg")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // --- Sets series on the chart ---

    [Fact]
    public async Task SessionDetailPage_Chart_ShowsSetsSeriesOnOverallAndExerciseSelections()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Bench Press Sets");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Bench Press Sets")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Sets Chart Workout", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 3, overallEffort = 5, loggedExercises = new[] { new { exerciseId, loggedWeight = "80", effort = 5 } } },
            });
            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { sets = 5, overallEffort = 7, loggedExercises = new[] { new { exerciseId, loggedWeight = "90", effort = 7 } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var selectEl = page.Locator("#session-chart-select");
            await Expect(selectEl).ToBeEnabledAsync(new() { Timeout = 15000 });

            // Overall selection renders one sets bar per session with its own legend entry
            await Expect(page.Locator(".session-chart__bar--sets")).ToHaveCountAsync(2, new() { Timeout = 15000 });
            await Expect(page.Locator(".session-chart__legend-swatch--sets")).ToBeVisibleAsync();

            // Per-exercise selection also renders the sets bars
            await selectEl.SelectOptionAsync(new SelectOptionValue { Label = "Bench Press Sets" });
            await Expect(page.Locator(".session-chart__bar--sets")).ToHaveCountAsync(2);
            await Expect(page.Locator(".session-chart__legend-swatch--sets")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task SessionDetailPage_Chart_OmitsSetsSeries_WhenNoSessionRecordedSets()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Legacy Row");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Legacy Row")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Legacy Sets Workout", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 6, loggedExercises = new[] { new { exerciseId, loggedWeight = "60", effort = 6 } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Expect(page.Locator("#session-chart-select")).ToBeEnabledAsync(new() { Timeout = 15000 });
            await Expect(page.Locator(".session-chart__svg")).ToBeVisibleAsync();
            await Expect(page.Locator(".session-chart__bar--sets")).ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // --- T013: Empty state when exercise has no data ---
    [Fact]
    public async Task SessionDetailPage_Chart_ShowsEmptyState_WhenExerciseHasNoWeightData()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Plank");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Plank")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Core Day Chart", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            // Log sessions with no weight
            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { loggedExercises = new[] { new { exerciseId, loggedWeight = (string?)null } } },
            });
            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { loggedExercises = new[] { new { exerciseId, loggedWeight = (string?)null } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var selectEl = page.Locator("#session-chart-select");
            await Expect(selectEl).ToBeEnabledAsync(new() { Timeout = 15000 });

            // Switch to exercise combined series (no weight/effort data logged)
            await selectEl.SelectOptionAsync(new SelectOptionValue { Label = "Plank" });

            // No SVG — empty message shown
            await Expect(page.Locator(".session-chart__svg")).ToHaveCountAsync(0);
            await Expect(page.Locator(".session-chart__empty")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // --- T015: Overall effort renders on load ---

    [Fact]
    public async Task SessionDetailPage_Chart_RendersOverallEffortByDefault()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Row");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Row")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Back Chart Default", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 4, loggedExercises = new[] { new { exerciseId } } },
            });
            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { overallEffort = 6, loggedExercises = new[] { new { exerciseId } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Default selection should be "overall"
            var selectEl = page.Locator("#session-chart-select");
            await Expect(selectEl).ToBeEnabledAsync(new() { Timeout = 15000 });
            var selectedValue = await selectEl.InputValueAsync();
            Assert.Equal("overall", selectedValue);

            // SVG chart is rendered immediately (no interaction needed)
            await Expect(page.Locator(".session-chart__svg")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    // ──────────────────────────────────────────
    // Delete Session (feature 026)
    // ──────────────────────────────────────────

    [Fact]
    public async Task DeleteSession_DeleteButton_VisibleOnSessionDetailPage()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync("#session-detail-content");

            await Expect(page.Locator("#session-detail-delete")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteSession_ClickDelete_ShowsConfirmationModal()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync("#session-detail-delete");
            await page.ClickAsync("#session-detail-delete");

            await Expect(page.Locator("#session-delete-confirm-backdrop")).ToBeVisibleAsync();
            await Expect(page.Locator("#session-delete-confirm-ok")).ToBeVisibleAsync();
            await Expect(page.Locator("#session-delete-confirm-cancel")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteSession_CancelModal_KeepsSessionAndStaysOnPage()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync("#session-detail-delete");
            await page.ClickAsync("#session-detail-delete");
            await Expect(page.Locator("#session-delete-confirm-backdrop")).ToBeVisibleAsync();

            await page.ClickAsync("#session-delete-confirm-cancel");

            await Expect(page.Locator("#session-delete-confirm-backdrop")).ToBeHiddenAsync();
            await Expect(page.Locator("#session-detail-content")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteSession_ConfirmDelete_RedirectsToHistoryWithBanner()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page);
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync("#session-detail-delete");
            await page.ClickAsync("#session-detail-delete");
            await Expect(page.Locator("#session-delete-confirm-ok")).ToBeVisibleAsync();

            await page.ClickAsync("#session-delete-confirm-ok");

            await Expect(page).ToHaveURLAsync(new Regex(".*/history$"));
            await Expect(page.Locator(".history-page__banner")).ToBeVisibleAsync();
            await Expect(page.Locator(".history-page__banner")).ToContainTextAsync("Session deleted.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteSession_ConfirmedSession_AbsentFromHistoryList()
    {
        var page = await CreatePageAsync();
        try
        {
            await CreateWorkoutAndSessionViaApiAsync(page, workoutName: "Push Day Deletion Test");
            await NavigateToHistoryAsync(page);

            await page.Locator(".history-session__header").First.ClickAsync();
            await page.WaitForSelectorAsync("#session-detail-delete");
            await page.ClickAsync("#session-detail-delete");
            await Expect(page.Locator("#session-delete-confirm-ok")).ToBeVisibleAsync();
            await page.ClickAsync("#session-delete-confirm-ok");

            await Expect(page).ToHaveURLAsync(new Regex(".*/history$"));
            await page.WaitForSelectorAsync(".history-page");

            await Expect(page.Locator(".history-session__workout-name").Filter(new() { HasText = "Push Day Deletion Test" }))
                .ToHaveCountAsync(0);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task DeleteSession_StaleUrl_ShowsSessionNotFound()
    {
        var page = await CreatePageAsync();
        try
        {
            await SeedExerciseAsync(page, "Stale Session Exercise");
            var exercisesResponse = await page.APIRequest.GetAsync($"{_webApp.BaseUrl}/api/exercises");
            var exercisesJson = await exercisesResponse.JsonAsync();
            var exerciseId = exercisesJson?.EnumerateArray()
                .First(e => e.GetProperty("name").GetString() == "Stale Session Exercise")
                .GetProperty("exerciseId").GetString()!;

            var createResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
            {
                DataObject = new { name = "Stale URL Workout", exercises = new[] { new { exerciseId } } },
            });
            var workoutData = await createResponse.JsonAsync();
            var workoutId = workoutData?.GetProperty("plannedWorkoutId").GetString()!;

            var sessionResp = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts/{workoutId}/sessions", new()
            {
                DataObject = new { loggedExercises = new[] { new { exerciseId } } },
            });
            var sessionData = await sessionResp.JsonAsync();
            var sessionId = sessionData?.GetProperty("workoutSessionId").GetString()!;

            // Delete the session directly via API (simulating a stale URL)
            await page.APIRequest.DeleteAsync($"{_webApp.BaseUrl}/api/sessions/{sessionId}");

            // Navigate directly to the now-deleted session URL
            await page.GotoAsync($"{_webApp.BaseUrl}/history/session?id={sessionId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Expect(page.Locator("#session-detail-error")).ToBeVisibleAsync();
            await Expect(page.Locator("#session-detail-error")).ToContainTextAsync("Session not found.");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
