using Microsoft.Playwright;
using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[Collection("E2E")]
public class HomeLandingPageRegressionTests
{
    private readonly WebAppFixture _webApp;
    private readonly PlaywrightFixture _playwright;

    public HomeLandingPageRegressionTests(WebAppFixture webApp, PlaywrightFixture playwright)
    {
        _webApp = webApp;
        _playwright = playwright;
    }

    private async Task<IPage> CreatePageAsync()
    {
        WebAppFixture.ResetWorkouts();
        WebAppFixture.SeedWorkout("Legs");
        WebAppFixture.SeedWorkout("Pull");
        WebAppFixture.SeedWorkout("Push");
        var page = await _playwright.Browser.NewPageAsync();
        await page.GotoAsync(_webApp.BaseUrl);
        await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        return page;
    }

    [Fact]
    public async Task RapidClicks_WithoutSelection_ErrorDisplayedOnce()
    {
        var page = await CreatePageAsync();
        var button = page.Locator("button[type='submit']");

        for (var i = 0; i < 10; i++)
        {
            await button.ClickAsync();
        }

        var errors = page.Locator("#workout-error:visible");
        await Assertions.Expect(errors).ToHaveCountAsync(1);
        await Assertions.Expect(errors).ToHaveTextAsync("Please select a workout");

        await page.CloseAsync();
    }

    [Fact]
    public async Task AllWorkoutTypes_CanBeSelectedAndSubmitted()
    {
        var page = await CreatePageAsync();
        string[] workoutLabels = ["Push", "Pull", "Legs"];

        foreach (var label in workoutLabels)
        {
            var select = page.Locator("#workout-select");
            var button = page.Locator("button[type='submit']");
            var error = page.Locator("#workout-error");
            await select.SelectOptionAsync(new SelectOptionValue { Label = label });
            await page.Locator("#sets-select").SelectOptionAsync("3");
            await button.ClickAsync();
            await Assertions.Expect(error).ToBeHiddenAsync();
            // Navigate back to home for the next iteration (form submit navigates away)
            WebAppFixture.ResetWorkouts();
            WebAppFixture.SeedWorkout("Legs");
            WebAppFixture.SeedWorkout("Pull");
            WebAppFixture.SeedWorkout("Push");
            await page.GotoAsync(_webApp.BaseUrl);
            await page.Locator("#workout-select option:not([disabled])").First.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        }

        await page.CloseAsync();
    }

    [Fact]
    public async Task InvalidDraft_CanBeClearedFromHomeWithoutWorkoutTemplates()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("localStorage.setItem('workout-tracker-active-draft', '{broken');");
        await page.GotoAsync(_webApp.BaseUrl);

        await Assertions.Expect(page.Locator("#home-draft"))
            .ToContainTextAsync("Your saved workout data is invalid and can’t be resumed.");
        await Assertions.Expect(page.Locator("#workout-select option:not([disabled])")).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }).ClickAsync();

        await Assertions.Expect(page.Locator("#home-draft")).ToBeEmptyAsync();
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
        await page.CloseAsync();
    }

    [Fact]
    public async Task DraftStorageWithoutLock_ShowsRetryAndDoesNotOfferUnsafeClear()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("""
            Object.defineProperty(navigator, 'locks', { value: undefined });
            localStorage.setItem('workout-tracker-active-draft', '{broken');
            """);
        await page.GotoAsync(_webApp.BaseUrl);

        await Assertions.Expect(page.Locator("#home-draft"))
            .ToContainTextAsync("Could not safely access saved workout data.");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Retry" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToHaveCountAsync(0);
        Assert.Equal("{broken", await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));
        await page.CloseAsync();
    }

    [Fact]
    public async Task UnavailableDraftStorage_ShowsRetryAndPreservesSavedData()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("""
            localStorage.setItem('workout-tracker-active-draft', '{broken');
            window.originalGetItem = Storage.prototype.getItem;
            Storage.prototype.getItem = function(key) {
              if (key === 'workout-tracker-active-draft') throw new Error('storage blocked');
              return window.originalGetItem.call(this, key);
            };
            """);
        await page.GotoAsync(_webApp.BaseUrl);

        await Assertions.Expect(page.Locator("#home-draft"))
            .ToContainTextAsync("Could not safely access saved workout data.");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToHaveCountAsync(0);
        await page.EvaluateAsync("() => { Storage.prototype.getItem = window.originalGetItem; }");
        Assert.Equal("{broken", await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Retry" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToBeVisibleAsync();
        await page.CloseAsync();
    }

    [Theory]
    [InlineData("normal")]
    [InlineData("randomised")]
    [InlineData("http-error")]
    [InlineData("network-error")]
    public async Task StartWorkout_NavigatesWithStartMarker(string mode)
    {
        const string workoutId = "22222222-2222-4222-8222-222222222222";
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("""
            window.homeNavigations = [];
            const originalPushState = history.pushState;
            history.pushState = function(state, title, url) {
              window.homeNavigations.push(String(url));
              return originalPushState.apply(this, arguments);
            };
            """);
        await page.RouteAsync("**/api/workouts", route => route.FulfillAsync(new()
        {
            ContentType = "application/json",
            Body = $$"""[{"plannedWorkoutId":"{{workoutId}}","name":"Test","exerciseCount":2}]"""
        }));
        await page.RouteAsync($"**/api/workouts/{workoutId}", route => mode switch
        {
            "http-error" => route.FulfillAsync(new() { Status = 500 }),
            "network-error" => route.AbortAsync(),
            _ => route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Body = $$"""{"plannedWorkoutId":"{{workoutId}}","name":"Test","exercises":[{"exerciseId":"33333333-3333-4333-8333-333333333333","name":"Squat"},{"exerciseId":"44444444-4444-4444-8444-444444444444","name":"Press"}]}"""
            })
        });
        await page.GotoAsync(_webApp.BaseUrl);
        await page.Locator("#workout-select").SelectOptionAsync(workoutId);
        if (mode != "normal") await page.Locator("#home-randomise-toggle").ClickAsync();
        await page.Locator("#workout-form button[type='submit']").ClickAsync();

        await page.WaitForFunctionAsync("window.homeNavigations.some(url => url.includes('/active-session?'))");
        var url = await page.EvaluateAsync<string>("window.homeNavigations.find(url => url.includes('/active-session?'))");
        var parameters = System.Web.HttpUtility.ParseQueryString(new Uri(new Uri(_webApp.BaseUrl), url).Query);
        Assert.Equal("1", parameters["start"]);
        Assert.Equal(workoutId, parameters["id"]);
        Assert.Null(parameters["resume"]);
        Assert.Equal(mode == "randomised", parameters["order"] is not null);
        await page.CloseAsync();
    }

    [Fact]
    public async Task ResumeWorkout_NavigatesWithDraftId()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("""
            localStorage.setItem('workout-tracker-active-draft', JSON.stringify({
              version: 1,
              draftId: '11111111-1111-4111-8111-111111111111',
              workoutId: '22222222-2222-4222-8222-222222222222',
              sets: 5,
              exerciseOrder: [],
              entries: {},
              completion: null
            }));
            """);
        await page.GotoAsync(_webApp.BaseUrl);
        await page.GetByRole(AriaRole.Button, new() { Name = "Resume workout" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(
            @"active-session\?id=22222222-2222-4222-8222-222222222222&sets=5&resume=11111111-1111-4111-8111-111111111111"));
        await page.CloseAsync();
    }

    [Fact]
    public async Task ClearInvalidDraft_RechecksStorageAndPreservesNewerValidDraft()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("localStorage.setItem('workout-tracker-active-draft', '{broken');");
        await page.GotoAsync(_webApp.BaseUrl);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToBeVisibleAsync();

        await page.EvaluateAsync("""
            localStorage.setItem('workout-tracker-active-draft', JSON.stringify({
              version: 1,
              draftId: '11111111-1111-4111-8111-111111111111',
              workoutId: '22222222-2222-4222-8222-222222222222',
              sets: 3,
              exerciseOrder: [],
              entries: {},
              completion: null
            }));
            """);
        var newerDraft = await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')");

        await page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }).ClickAsync();

        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Resume workout" })).ToBeVisibleAsync();
        Assert.Equal(newerDraft, await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Resume workout" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(
            @"active-session\?id=22222222-2222-4222-8222-222222222222&sets=3&resume=11111111-1111-4111-8111-111111111111"));
        await page.CloseAsync();
    }

    [Fact]
    public async Task ClearInvalidDraft_PreservesReplacementUntilExplicitlyCleared()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("localStorage.setItem('workout-tracker-active-draft', '{broken');");
        await page.GotoAsync(_webApp.BaseUrl);
        var clear = page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" });
        await Assertions.Expect(clear).ToBeVisibleAsync();

        await page.EvaluateAsync("localStorage.setItem('workout-tracker-active-draft', '{different')");
        await clear.ClickAsync();

        await Assertions.Expect(page.Locator("#home-draft")).ToContainTextAsync("Your saved workout data changed");
        Assert.Equal("{different", await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));
        await clear.ClickAsync();
        await Assertions.Expect(page.Locator("#home-draft")).ToBeEmptyAsync();
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('workout-tracker-active-draft')"));
        await page.CloseAsync();
    }

    [Theory]
    [InlineData("getItem")]
    [InlineData("removeItem")]
    public async Task ClearInvalidDraft_StorageFailurePreservesDataAndAllowsRetry(string method)
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("localStorage.setItem('workout-tracker-active-draft', '{broken');");
        await page.GotoAsync(_webApp.BaseUrl);
        var clear = page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" });
        await Assertions.Expect(clear).ToBeVisibleAsync();

        await page.EvaluateAsync("""
            method => {
              window.originalStorageMethod = Storage.prototype[method];
              Storage.prototype[method] = function(key) {
                if (key === 'workout-tracker-active-draft') throw new Error('storage blocked');
                return window.originalStorageMethod.call(this, key);
              };
            }
            """, method);
        await clear.ClickAsync();
        await Assertions.Expect(page.Locator("#home-draft"))
            .ToContainTextAsync("Could not safely access saved workout data.");
        await Assertions.Expect(clear).ToHaveCountAsync(0);
        await page.EvaluateAsync("method => { Storage.prototype[method] = window.originalStorageMethod; }", method);
        Assert.Equal("{broken", await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Retry" }).ClickAsync();
        await Assertions.Expect(clear).ToBeVisibleAsync();
        await clear.ClickAsync();
        await Assertions.Expect(page.Locator("#home-draft")).ToBeEmptyAsync();
        await page.CloseAsync();
    }

    [Fact]
    public async Task InvalidDraft_SnapshotReadFailureDoesNotOfferClear()
    {
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync();
        await page.AddInitScriptAsync("""
            localStorage.setItem('workout-tracker-active-draft', '{broken');
            window.originalGetItem = Storage.prototype.getItem;
            let reads = 0;
            Storage.prototype.getItem = function(key) {
              if (key === 'workout-tracker-active-draft' && ++reads === 3) throw new Error('storage blocked');
              return window.originalGetItem.call(this, key);
            };
            """);
        await page.GotoAsync(_webApp.BaseUrl);

        await Assertions.Expect(page.Locator("#home-draft"))
            .ToContainTextAsync("Could not safely access saved workout data.");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToHaveCountAsync(0);
        Assert.Equal("{broken", await page.EvaluateAsync<string>("localStorage.getItem('workout-tracker-active-draft')"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Retry" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Clear invalid saved workout" }))
            .ToBeVisibleAsync();
        await page.CloseAsync();
    }
}
