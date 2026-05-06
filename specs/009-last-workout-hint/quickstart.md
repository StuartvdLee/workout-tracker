# Quickstart: Last Workout Hint on Home Page

**Feature**: `009-last-workout-hint`  
**Date**: 2026-05-06

## What This Feature Does

The home page now shows a small hint below the "Start Workout" button telling you which workout you completed most recently and when. This lets you pick a different workout without navigating to the history page.

The hint is:
- **Read-only** — it does not pre-select anything in the dropdown.
- **Non-blocking** — the page is fully interactive before the hint loads.
- **Silently absent** — if you have no completed sessions, or if the fetch fails, nothing is shown.

---

## User Flow

### First time user (no sessions)

1. Open the app. The home page shows the title, workout dropdown, and "Start Workout" button.
2. No hint appears below the button — the page looks identical to before this feature.

### Returning user

1. Open the app.
2. Below the "Start Workout" button, a line appears:
   ```
   Last workout: Push — 3 May 2026
   ```
3. If you did Push last time and don't want to repeat it, select Pull or Legs from the dropdown before starting.

### Hint appears after the page loads

The hint is fetched asynchronously — the dropdown and button are usable immediately. The hint text appears a moment later once the API responds. If the API is slow or fails, the hint simply does not appear; the page continues working normally.

---

## Changes to Existing Screens

### Home Page (`/`)

**Before**: Workout dropdown → "Start Workout" button (end of form).

**After**: Workout dropdown → "Start Workout" button → *(hint line, if sessions exist)*.

The hint renders as a single line of muted text: `Last workout: [Name] — [Date]`.

### All Other Screens

No changes.

---

## Technical Walkthrough

### Backend: New Endpoint

`WorkoutTracker.Api/Program.cs` — new handler added alongside existing session endpoints:

```csharp
app.MapGet("/api/sessions/latest", async (WorkoutTrackerDbContext db) =>
{
    var latest = await db.WorkoutSessions
        .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
        .Select(ws => new
        {
            ws.WorkoutName,
            CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
        })
        .FirstOrDefaultAsync();

    if (latest is null)
        return Results.Ok(new { hasSession = false });

    return Results.Ok(new
    {
        hasSession = true,
        workoutName = latest.WorkoutName,
        completedAt = (DateTime?)latest.CompletedAt,
    });
});
```

### Web Proxy

`WorkoutTracker.Web/Program.cs` — new proxy route added following the same pattern as other routes:

```csharp
app.MapGet("/api/sessions/latest", async (ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.GetAsync("/api/sessions/latest");
        var content = await response.Content.ReadAsStringAsync();
        return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, "GET /api/sessions/latest", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});
```

### Frontend: `home.ts`

Two additions to the existing `home.ts`:

1. A typed interface for the API response:
```typescript
interface LastWorkoutDto {
  readonly hasSession: boolean;
  readonly workoutName?: string;
  readonly completedAt?: string;
}
```

2. An async hint loader called from `render()` after the form is set up:
```typescript
void loadLastWorkoutHint();

async function loadLastWorkoutHint(): Promise<void> {
  try {
    const response = await fetch("/api/sessions/latest");
    if (!response.ok) return;
    const data: LastWorkoutDto = await response.json();
    if (!data.hasSession || !data.workoutName || !data.completedAt) return;

    const formEl = document.getElementById("workout-form");
    if (!formEl) return;

    const date = new Date(data.completedAt).toLocaleDateString("en-GB", {
      day: "numeric",
      month: "long",
      year: "numeric",
    });

    const hint = document.createElement("p");
    hint.className = "workout-form__last-workout";
    hint.textContent = `Last workout: ${data.workoutName} — ${date}`;
    formEl.appendChild(hint);
  } catch {
    // silent fail
  }
}
```

### CSS: `styles.css`

One new rule added after the `.workout-form__button` block:

```css
/* === Last Workout Hint === */
.workout-form__last-workout {
  font-size: var(--font-size-sm);
  color: var(--color-text-light);
  text-align: center;
}
```

---

## Running Locally

No additional steps beyond the existing dev setup. Run the app normally:

```bash
cd src
dotnet run --project WorkoutTracker.AppHost
```

To see the hint:
1. Navigate to the home page.
2. Complete at least one workout (Workouts → Start → Save Workout).
3. Return to the home page — the hint appears below the "Start Workout" button.

---

## Running Tests

```bash
cd src
dotnet test WorkoutTracker.UnitTests
```

New tests in `SessionApiTests.cs` cover:
- `GET /api/sessions/latest` returns `hasSession: false` when no sessions exist.
- Returns `hasSession: true` with `workoutName` and `completedAt` after one session is created.
- Returns only the most recently completed session when multiple sessions exist.
- Returns the correct workout name for the most recent session.
