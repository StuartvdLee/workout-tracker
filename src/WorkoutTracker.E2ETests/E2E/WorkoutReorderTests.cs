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

    private async Task<string> SeedExerciseAndReturnIdAsync(IPage page, string name)
    {
        var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
        {
            DataObject = new { name, muscleIds = Array.Empty<string>() },
        });
        var data = await response.JsonAsync();
        return data?.GetProperty("exerciseId").GetString()!;
    }

    private async Task<(string WorkoutId, string BenchPressId, string SquatId, string DeadliftId)> CreateThreeExerciseWorkoutViaApiAsync(IPage page)
    {
        var benchPressId = await SeedExerciseAndReturnIdAsync(page, "Bench Press");
        var squatId = await SeedExerciseAndReturnIdAsync(page, "Squat");
        var deadliftId = await SeedExerciseAndReturnIdAsync(page, "Deadlift");

        var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
        {
            DataObject = new
            {
                name = "Current Reorder Test",
                exercises = new[]
                {
                    new { exerciseId = benchPressId, targetReps = "8-12", targetWeight = "80" },
                    new { exerciseId = squatId, targetReps = "5", targetWeight = "120" },
                    new { exerciseId = deadliftId, targetReps = "3", targetWeight = "140" },
                },
            },
        });
        var data = await response.JsonAsync();
        var workoutId = data?.GetProperty("plannedWorkoutId").GetString()!;
        return (workoutId, benchPressId, squatId, deadliftId);
    }

    private async Task<(string WorkoutId, string BenchPressId, string SquatId, string DeadliftId)> OpenCurrentWorkoutWithThreeExercisesAsync(IPage page)
    {
        var workout = await CreateThreeExerciseWorkoutViaApiAsync(page);
        await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workout.WorkoutId}&sets=3");
        await page.WaitForSelectorAsync(".active-session__exercise-item:nth-child(3)");
        return workout;
    }

    private async Task OpenLongCurrentWorkoutAsync(IPage page)
    {
        var exercises = new List<object>();
        for (var index = 1; index <= 50; index++)
        {
            var response = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/exercises", new()
            {
                DataObject = new { name = index == 1 ? new string('W', 150) : $"Exercise {index:D2}", muscleIds = Array.Empty<string>() },
            });
            Assert.True(response.Ok, $"Exercise seed failed: {response.Status}");
            var data = await response.JsonAsync();
            exercises.Add(new { exerciseId = data?.GetProperty("exerciseId").GetString(), targetReps = "8", targetWeight = "20" });
        }

        var workoutResponse = await page.APIRequest.PostAsync($"{_webApp.BaseUrl}/api/workouts", new()
        {
            DataObject = new { name = "Long Workout", exercises },
        });
        Assert.True(workoutResponse.Ok, $"Workout seed failed: {workoutResponse.Status}");
        var workout = await workoutResponse.JsonAsync();
        await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workout?.GetProperty("plannedWorkoutId").GetString()}&sets=3");
        await page.WaitForSelectorAsync(".active-session__exercise-item:nth-child(50)");
    }

    private static async Task AddExerciseToFormAsync(IPage page, string exerciseName, string selectId = "workout-exercise-select")
    {
        await page.Locator($"#{selectId} option:not([disabled]):not([value=''])").First.WaitForAsync(
            new() { State = WaitForSelectorState.Attached });
        await page.Locator($"#{selectId}").SelectOptionAsync(new SelectOptionValue { Label = exerciseName });
    }

    // ──────────────────────────────────────────
    // Current workout: order editing
    // ──────────────────────────────────────────

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public async Task CurrentWorkout_EditOrder_FloatsAboveFullWidthContentOnMobile(string theme)
    {
        var page = await CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(375, 667);
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);
            await page.EvaluateAsync("(theme) => document.documentElement.setAttribute('data-theme', theme)", theme);

            var geometry = await page.EvaluateAsync<System.Text.Json.JsonElement>(@"() => {
                const workout = document.querySelector('.active-session').getBoundingClientRect();
                const content = document.querySelector('.active-session__content').getBoundingClientRect();
                const button = document.querySelector('#session-edit-order');
                const style = getComputedStyle(button);
                const surface = getComputedStyle(document.querySelector('.active-session__exercise-item')).backgroundColor;
                const luminance = color => {
                    const channels = color.match(/\d+/g).slice(0, 3).map(Number).map(value => {
                        const c = value / 255;
                        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
                    });
                    return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
                };
                const contrast = (a, b) => {
                    a = luminance(a); b = luminance(b);
                    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
                };
                return { workoutWidth: workout.width, contentWidth: content.width,
                    position: style.position, iconContrast: contrast(style.color, style.backgroundColor),
                    edgeContrast: Math.max(contrast(style.backgroundColor, surface), contrast(style.borderColor, surface)) };
            }");
            Assert.InRange(Math.Abs(geometry.GetProperty("workoutWidth").GetDouble() - geometry.GetProperty("contentWidth").GetDouble()), 0, 1);
            Assert.Equal("fixed", geometry.GetProperty("position").GetString());
            Assert.True(geometry.GetProperty("iconContrast").GetDouble() >= 3);
            Assert.True(geometry.GetProperty("edgeContrast").GetDouble() >= 3);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_FloatingOrderButton_LeavesWorkoutControlsReachable()
    {
        var page = await CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(320, 568);
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);
            foreach (var selector in new[] { ".active-session__input", ".active-session__effort-slider", "#session-save", "#session-cancel" })
            {
                var control = page.Locator(selector).First;
                await control.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
                var reachable = await control.EvaluateAsync<bool>(@"el => {
                    const rect = el.getBoundingClientRect();
                    const hit = document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2);
                    return hit === el || el.contains(hit);
                }");
                Assert.True(reachable, $"{selector} is obstructed after scrolling into view");
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData(320, 568, "light")]
    [InlineData(320, 568, "dark")]
    [InlineData(375, 667, "light")]
    [InlineData(375, 667, "dark")]
    [InlineData(768, 768, "dark")]
    [InlineData(768, 768, "light")]
    [InlineData(1024, 768, "light")]
    [InlineData(1024, 768, "dark")]
    [InlineData(640, 480, "light")]
    public async Task CurrentWorkout_EditOrder_RemainsReachableWhileScrolling(int width, int height, string theme)
    {
        var page = await CreatePageAsync();
        try
        {
            await page.SetViewportSizeAsync(width, height);
            await OpenLongCurrentWorkoutAsync(page);
            await page.EvaluateAsync("(theme) => document.documentElement.setAttribute('data-theme', theme)", theme);
            Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
            Assert.True(await page.Locator("#session-edit-order").EvaluateAsync<bool>(@"button => {
                const style = getComputedStyle(button);
                const luminance = color => {
                    const channels = color.match(/\d+/g).slice(0, 3).map(Number).map(value => {
                        const c = value / 255;
                        return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
                    });
                    return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
                };
                const a = luminance(style.color), b = luminance(style.backgroundColor);
                return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05) >= 3;
            }"));

            foreach (var fraction in new[] { 0.0, 0.5, 1.0 })
            {
                await page.EvaluateAsync("(fraction) => window.scrollTo(0, (document.documentElement.scrollHeight - innerHeight) * fraction)", fraction);
                var button = page.Locator("#session-edit-order");
                var box = await button.BoundingBoxAsync() ?? throw new InvalidOperationException("Order button is missing");
                var topInset = width < 768 ? 56 : 0;
                Assert.True(box.Y >= topInset && box.Y + box.Height <= height, $"Order button is outside the unobscured viewport at {fraction:P0}");
                Assert.True(box.X >= 0 && box.X + box.Width <= width, $"Order button is outside the viewport at {fraction:P0}");
                var overlapsTopbar = await page.EvaluateAsync<bool>(@"() => {
                    const button = document.querySelector('#session-edit-order').getBoundingClientRect();
                    const topbar = document.querySelector('.topbar').getBoundingClientRect();
                    return button.left < topbar.right && button.right > topbar.left &&
                        button.top < topbar.bottom && button.bottom > topbar.top;
                }");
                Assert.False(overlapsTopbar, $"Order button overlaps the topbar at {fraction:P0}");
                var hit = await page.EvaluateAsync<bool>(@"() => {
                    const button = document.querySelector('#session-edit-order');
                    const box = button.getBoundingClientRect();
                    const target = document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2);
                    return button && target && (button === target || button.contains(target));
                }");
                Assert.True(hit, $"Order button is obstructed at {fraction:P0}");
                var overlapsContent = await page.EvaluateAsync<bool>(@"() => {
                    const button = document.querySelector('#session-edit-order').getBoundingClientRect();
                    return [...document.querySelectorAll('.active-session__input, .active-session__effort-slider, #session-save, #session-cancel')]
                        .some(element => {
                            const rect = element.getBoundingClientRect();
                            return rect.left < button.right && rect.right > button.left &&
                                rect.top < button.bottom && rect.bottom > button.top;
                        });
                }");
                if (width >= 768)
                {
                    Assert.False(overlapsContent, $"Order button covers a workout control at {fraction:P0}");
                }

                await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
                await Expect(page.Locator("#session-order-list")).ToBeVisibleAsync();
                Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= document.documentElement.clientWidth"));
                await page.Locator("#session-cancel").ClickAsync();
                await Expect(page.Locator(".active-session__exercise-item")).ToHaveCountAsync(50);
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData("Space")]
    public async Task CurrentWorkout_EditOrder_IconIsAccessibleByKeyboard(string key)
    {
        var page = await CreatePageAsync();
        try
        {
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);
            var button = page.GetByRole(AriaRole.Button, new() { Name = "Edit order", Exact = true });
            await Expect(button).ToBeVisibleAsync();
            await Expect(button).ToHaveTextAsync("");
            await Expect(button.Locator("svg")).ToHaveAttributeAsync("aria-hidden", "true");
            var box = await button.BoundingBoxAsync() ?? throw new InvalidOperationException("Order button is missing");
            Assert.True(box.Width >= 44 && box.Height >= 44);
            await button.FocusAsync();
            Assert.True(await button.EvaluateAsync<bool>("el => el.matches(':focus-visible') && getComputedStyle(el).outlineStyle !== 'none'"));
            await page.Keyboard.PressAsync(key);
            await Expect(page.Locator("#session-order-list")).ToBeVisibleAsync();
            await Expect(page.Locator("#session-order-list .workout-selected__drag-handle").First).ToBeFocusedAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_EditOrder_IsReachableByTouchAtBottom()
    {
        WebAppFixture.ResetExercises();
        WebAppFixture.ResetWorkouts();
        var page = await _playwright.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 375, Height = 667 },
            HasTouch = true,
            IsMobile = true,
        });
        try
        {
            await page.GotoAsync(_webApp.BaseUrl);
            await OpenLongCurrentWorkoutAsync(page);
            await page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight)");
            var box = await page.Locator("#session-edit-order").BoundingBoxAsync()
                ?? throw new InvalidOperationException("Order button is missing");
            Assert.InRange(box.Y, 56, 667 - box.Height);
            await page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
            await Expect(page.Locator("#session-order-list")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_EditOrder_CollapsesExercisesToNameOnlyRows()
    {
        var page = await CreatePageAsync();
        try
        {
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);

            await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Edit order", Exact = true })).ToBeVisibleAsync();
            await page.Locator("#session-edit-order").ClickAsync();

            await Expect(page.Locator("#session-edit-order")).ToBeHiddenAsync();
            await Expect(page.Locator("#session-save")).ToHaveTextAsync("Done");
            await page.WaitForSelectorAsync("#session-order-list li:nth-child(3)");
            var names = page.Locator("#session-order-list .workout-selected__name");
            await Expect(names.Nth(0)).ToHaveTextAsync("Bench Press");
            await Expect(names.Nth(1)).ToHaveTextAsync("Squat");
            await Expect(names.Nth(2)).ToHaveTextAsync("Deadlift");

            Assert.Equal(0, await page.Locator(".active-session__exercise-item").CountAsync());
            Assert.Equal(0, await page.Locator(".active-session__input").CountAsync());
            Assert.Equal(0, await page.Locator(".active-session__effort-slider").CountAsync());
            Assert.Equal(0, await page.Locator(".active-session__exercise-previous").CountAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_DragReorder_UpdatesCollapsedOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);
            await page.Locator("#session-edit-order").ClickAsync();
            await page.WaitForSelectorAsync("#session-order-list li:nth-child(3)");

            await DragToEndAsync(page, "#session-order-list", 1, 3);

            await page.WaitForFunctionAsync(
                @"() => {
                    const items = document.querySelectorAll('#session-order-list li .workout-selected__name');
                    return items.length === 3 && items[2].textContent === 'Bench Press';
                }");
            var names = page.Locator("#session-order-list .workout-selected__name");
            await Expect(names.Nth(0)).ToHaveTextAsync("Squat");
            await Expect(names.Nth(1)).ToHaveTextAsync("Deadlift");
            await Expect(names.Nth(2)).ToHaveTextAsync("Bench Press");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_KeyboardReorder_UpdatesCollapsedOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);
            await page.Locator("#session-edit-order").ClickAsync();
            await page.WaitForSelectorAsync("#session-order-list li:nth-child(3)");

            await page.Locator("#session-order-list li:nth-child(1) .workout-selected__drag-handle").FocusAsync();
            await page.Keyboard.PressAsync(" ");
            await page.Keyboard.PressAsync("ArrowDown");
            await page.Keyboard.PressAsync("Enter");

            await page.WaitForFunctionAsync(
                @"() => {
                    const items = document.querySelectorAll('#session-order-list li .workout-selected__name');
                    return items.length === 3 && items[1].textContent === 'Bench Press';
                }");
            var names = page.Locator("#session-order-list .workout-selected__name");
            await Expect(names.Nth(0)).ToHaveTextAsync("Squat");
            await Expect(names.Nth(1)).ToHaveTextAsync("Bench Press");
            await Expect(names.Nth(2)).ToHaveTextAsync("Deadlift");
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_DoneRestoresControlsAndPreservesExerciseValues()
    {
        var page = await CreatePageAsync();
        try
        {
            var sessionSaveRequests = 0;
            page.Request += (_, request) =>
            {
                if (request.Method == "POST" && request.Url.Contains("/sessions", StringComparison.Ordinal))
                {
                    sessionSaveRequests++;
                }
            };

            await page.SetViewportSizeAsync(375, 667);
            var workout = await OpenCurrentWorkoutWithThreeExercisesAsync(page);

            await page.Locator($"#weight-{workout.BenchPressId}").FillAsync("80");
            await page.Locator($"#effort-{workout.BenchPressId}").FillAsync("7");
            await page.Locator($"#effort-{workout.BenchPressId}").DispatchEventAsync("input");

            await page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight)");
            var orderButton = await page.Locator("#session-edit-order").BoundingBoxAsync()
                ?? throw new InvalidOperationException("Order button is missing");
            Assert.InRange(orderButton.Y, 56, 667 - orderButton.Height);
            await page.Mouse.ClickAsync(orderButton.X + orderButton.Width / 2, orderButton.Y + orderButton.Height / 2);
            await page.WaitForSelectorAsync("#session-order-list li:nth-child(3)");
            await DragToEndAsync(page, "#session-order-list", 1, 3);
            await page.Locator("#session-save").ClickAsync();

            await page.WaitForSelectorAsync(".active-session__exercise-item:nth-child(3)");
            var exerciseItems = page.Locator(".active-session__exercise-item");
            await Expect(exerciseItems.Nth(2).Locator(".active-session__exercise-name")).ToHaveTextAsync("Bench Press");
            await Expect(page.Locator("#session-save")).ToHaveTextAsync("Save Workout");
            await Expect(page.Locator("#session-edit-order")).ToBeVisibleAsync();
            Assert.Equal(0, sessionSaveRequests);
            await Expect(page.Locator($"#weight-{workout.BenchPressId}")).ToHaveValueAsync("80");
            await Expect(page.Locator($"#effort-value-{workout.BenchPressId}")).ToHaveTextAsync("7");
            Assert.Equal(3, await page.Locator(".active-session__effort-slider").CountAsync());
            Assert.Equal(3, await page.Locator(".active-session__input").CountAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_OrderEditCancel_DiscardWarningRestoresOriginalOrder()
    {
        var page = await CreatePageAsync();
        try
        {
            await OpenCurrentWorkoutWithThreeExercisesAsync(page);

            await page.Locator("#session-edit-order").ClickAsync();
            await page.WaitForSelectorAsync("#session-order-list li:nth-child(3)");
            await DragToEndAsync(page, "#session-order-list", 1, 3);

            await page.Locator("#session-cancel").ClickAsync();
            await Expect(page.Locator("#discard-title")).ToHaveTextAsync("Discard changes?");
            await Expect(page.Locator("#discard-cancel")).ToHaveTextAsync("Keep editing");
            await page.Locator("#discard-cancel").ClickAsync();
            await Expect(page.Locator("#session-order-list")).ToBeVisibleAsync();
            await page.Locator("#session-cancel").ClickAsync();
            await page.Locator("#discard-confirm").ClickAsync();

            await page.WaitForSelectorAsync(".active-session__exercise-item:nth-child(3)");
            var exerciseItems = page.Locator(".active-session__exercise-item");
            await Expect(exerciseItems.Nth(0).Locator(".active-session__exercise-name")).ToHaveTextAsync("Bench Press");
            await Expect(exerciseItems.Nth(1).Locator(".active-session__exercise-name")).ToHaveTextAsync("Squat");
            await Expect(exerciseItems.Nth(2).Locator(".active-session__exercise-name")).ToHaveTextAsync("Deadlift");
            await Expect(page.Locator("#session-save")).ToHaveTextAsync("Save Workout");
            await Expect(page.Locator("#session-edit-order")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_OrderControl_PreservesAvailabilityAfterErrors()
    {
        var page = await CreatePageAsync();
        try
        {
            var workout = await CreateThreeExerciseWorkoutViaApiAsync(page);
            await page.RouteAsync($"**/api/workouts/{workout.WorkoutId}", route => route.FulfillAsync(new() { Status = 500 }));
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workout.WorkoutId}&sets=3");
            await Expect(page.Locator("#session-error")).ToContainTextAsync("Failed to load workout");
            await Expect(page.Locator("#session-edit-order")).ToBeHiddenAsync();
            await page.UnrouteAsync($"**/api/workouts/{workout.WorkoutId}");

            await page.RouteAsync("**/previous-performance", route => route.FulfillAsync(new() { Status = 500 }));
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workout.WorkoutId}&sets=3");
            await Expect(page.Locator(".active-session__previous-error").First).ToBeVisibleAsync();
            await Expect(page.Locator("#session-edit-order")).ToBeVisibleAsync();

            await page.RouteAsync("**/sessions", route => route.FulfillAsync(new()
            {
                Status = 500,
                ContentType = "application/json",
                Body = "{\"error\":\"Save failed\"}",
            }));
            await page.Locator("#session-save").ClickAsync();
            await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync();
            await Expect(page.Locator("#effort-modal-save")).ToBeFocusedAsync();
            Assert.True(await page.EvaluateAsync<bool>(@"() => {
                const button = document.querySelector('#session-edit-order').getBoundingClientRect();
                const hit = document.elementFromPoint(button.x + button.width / 2, button.y + button.height / 2);
                return document.querySelector('#effort-backdrop').contains(hit);
            }"));
            await page.Locator("#effort-modal-skip").ClickAsync();
            await Expect(page.Locator("#session-api-error")).ToHaveTextAsync("Save failed");
            await Expect(page.Locator("#session-edit-order")).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_OrderControl_IsHiddenDuringLoading()
    {
        var page = await CreatePageAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var workout = await CreateThreeExerciseWorkoutViaApiAsync(page);
            await page.RouteAsync($"**/api/workouts/{workout.WorkoutId}", async route =>
            {
                await release.Task;
                await route.ContinueAsync();
            });
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workout.WorkoutId}&sets=3", new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
            });
            await Expect(page.Locator("#session-edit-order")).ToBeHiddenAsync();
            release.TrySetResult();
            await Expect(page.Locator("#session-edit-order")).ToBeVisibleAsync();
        }
        finally
        {
            release.TrySetResult();
            await page.CloseAsync();
        }
    }

    [Fact]
    public async Task CurrentWorkout_OrderControl_MeetsScrollAndTransitionBudgets()
    {
        var page = await CreatePageAsync();
        try
        {
            await OpenLongCurrentWorkoutAsync(page);
            var requestCount = 0;
            page.Request += (_, _) => requestCount++;
            var results = await page.EvaluateAsync<System.Text.Json.JsonElement>(@"async () => {
                const frame = () => new Promise(resolve => requestAnimationFrame(resolve));
                const settle = async () => { await frame(); await frame(); };
                const scrollSamples = [], entrySamples = [], exitSamples = [];
                const maxScroll = document.documentElement.scrollHeight - innerHeight;
                for (let i = 0; i < 20; i++) {
                    const start = performance.now();
                    window.scrollTo(0, maxScroll * i / 19);
                    await settle();
                    const elapsed = performance.now() - start;
                    const button = document.querySelector('#session-edit-order');
                    const rect = button.getBoundingClientRect();
                    const style = getComputedStyle(button);
                    const hit = document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2);
                    scrollSamples.push({ elapsed, reachable: rect.top >= 0 && rect.bottom <= innerHeight &&
                        style.visibility === 'visible' && Number(style.opacity) > 0 && (hit === button || button.contains(hit)) });
                }
                for (let i = 0; i < 20; i++) {
                    let start = performance.now();
                    document.querySelector('#session-edit-order').click();
                    await settle();
                    entrySamples.push({ elapsed: performance.now() - start, ready: !!document.querySelector('#session-order-list') });
                    start = performance.now();
                    document.querySelector('#session-save').click();
                    await settle();
                    exitSamples.push({ elapsed: performance.now() - start, ready: document.querySelectorAll('.active-session__exercise-item').length === 50 });
                }
                return { scrollSamples, entrySamples, exitSamples };
            }");
            Console.WriteLine($"Performance samples: {results}");
            var scroll = results.GetProperty("scrollSamples").EnumerateArray().ToArray();
            var entries = results.GetProperty("entrySamples").EnumerateArray().ToArray();
            var exits = results.GetProperty("exitSamples").EnumerateArray().ToArray();
            Assert.All(scroll, sample => Assert.True(sample.GetProperty("reachable").GetBoolean()));
            Assert.True(scroll.Count(sample => sample.GetProperty("elapsed").GetDouble() <= 100) >= 19);
            Assert.All(entries, sample => Assert.True(sample.GetProperty("ready").GetBoolean()));
            Assert.All(exits, sample => Assert.True(sample.GetProperty("ready").GetBoolean()));
            Assert.True(entries.Count(sample => sample.GetProperty("elapsed").GetDouble() <= 1000) >= 19);
            Assert.True(exits.Count(sample => sample.GetProperty("elapsed").GetDouble() <= 1000) >= 19);
            Assert.Equal(0, requestCount);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CurrentWorkout_OrderControl_HandlesEmptyAndSingleExercise(int count)
    {
        var page = await CreatePageAsync();
        try
        {
            var workout = await CreateThreeExerciseWorkoutViaApiAsync(page);
            var exercises = count == 0 ? "[]" : $"[{{\"exerciseId\":\"{workout.BenchPressId}\",\"name\":\"Bench Press\",\"targetReps\":null,\"targetWeight\":null}}]";
            await page.RouteAsync($"**/api/workouts/{workout.WorkoutId}", route => route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json",
                Body = $"{{\"plannedWorkoutId\":\"{workout.WorkoutId}\",\"name\":\"Test\",\"exercises\":{exercises}}}",
            }));
            await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workout.WorkoutId}&sets=3");
            await Expect(page.Locator("#session-title")).ToHaveTextAsync("Test");
            var button = page.Locator("#session-edit-order");
            if (count == 0)
            {
                await Expect(button).ToBeHiddenAsync();
                return;
            }

            await Expect(button).ToBeVisibleAsync();
            await button.ClickAsync();
            await Expect(page.Locator("#session-order-list li")).ToHaveCountAsync(1);
            await Expect(page.Locator("#session-order-list .workout-selected__drag-handle")).ToHaveCountAsync(0);
            await Expect(page.Locator("#session-save")).ToBeFocusedAsync();
            await page.Locator("#session-cancel").ClickAsync();
            await Expect(page.Locator("#discard-backdrop")).ToBeHiddenAsync();
            await Expect(button).ToBeVisibleAsync();
        }
        finally
        {
            await page.CloseAsync();
        }
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
