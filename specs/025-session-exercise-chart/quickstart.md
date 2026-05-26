# Quickstart: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Date**: 2026-05-26

## What This Feature Adds

A historical line chart on the session detail page. When viewing a completed workout session linked to a planned workout, a chart section appears below the overall effort row. A dropdown selects what to plot: overall session effort, or an individual exercise's weight or effort over time.

## Where to Start

### 1. Backend — New API Endpoint

Add to `src/WorkoutTracker.Api/Program.cs` (after the existing `GET /api/workouts/{workoutId}/previous-performance` handler):

```csharp
app.MapGet("/api/workouts/{workoutId:guid}/session-trends", async (Guid workoutId, WorkoutTrackerDbContext db) =>
{
    var workoutExists = await db.PlannedWorkouts
        .AnyAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (!workoutExists)
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);

    var sessions = await db.WorkoutSessions
        .Where(ws => ws.PlannedWorkoutId == workoutId)
        .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
        .ThenByDescending(ws => ws.WorkoutSessionId)
        .Take(50)
        .Select(ws => new
        {
            ws.WorkoutSessionId,
            CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
            ws.OverallEffort,
            Exercises = ws.LoggedExercises
                .OrderBy(le => le.Sequence)
                .Select(le => new
                {
                    le.ExerciseId,
                    ExerciseName = le.Exercise.Name,
                    le.LoggedWeight,
                    le.Effort,
                }).ToList(),
        })
        .ToListAsync();

    // Reverse to chronological order for left-to-right chart rendering
    sessions.Reverse();

    return Results.Ok(sessions);
});
```

**Key constraint**: Use `EF.Property<DateTime>(ws, "CompletedAt")` — `CompletedAt` is a shadow property.

### 2. Web Proxy

Add to `src/WorkoutTracker.Web/Program.cs` (alongside other `/api/workouts/*` proxy routes):

```csharp
app.MapGet("/api/workouts/{workoutId}/session-trends", async (string workoutId, HttpClient http) =>
    await http.GetAsync($"/api/workouts/{workoutId}/session-trends"));
```

### 3. Frontend — New TypeScript Interfaces

Add to `session-detail.ts`:

```typescript
interface SessionTrendExercise {
  readonly exerciseId: string;
  readonly exerciseName: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
}

interface SessionTrendItem {
  readonly workoutSessionId: string;
  readonly completedAt: string;
  readonly overallEffort: number | null;
  readonly exercises: SessionTrendExercise[];
}
```

### 4. Chart Math Helpers — Add to `utils.ts`

```typescript
export function normaliseValue(value: number, min: number, max: number): number {
  if (min === max) return 120; // flat-line midpoint
  return 220 - ((value - min) / (max - min)) * 200;
}

export function buildYTicks(min: number, max: number, tickCount: number): number[] {
  const ticks: number[] = [];
  for (let i = 0; i < tickCount; i++) {
    ticks.push(min + ((max - min) / (tickCount - 1)) * i);
  }
  return ticks;
}

export function buildXLabels(dates: readonly string[], maxLabels: number): (string | null)[] {
  const n = dates.length;
  if (n === 0) return [];
  const step = n <= maxLabels ? 1 : Math.ceil(n / maxLabels);
  return dates.map((d, i) => {
    if (i % step !== 0 && i !== n - 1) return null;
    const date = new Date(d);
    return date.toLocaleDateString("en-GB", { day: "2-digit", month: "short" });
  });
}
```

### 5. Running Tests

```bash
# Backend integration tests (requires PostgreSQL)
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj

# Frontend unit tests (Vitest — no PostgreSQL needed)
cd src/WorkoutTracker.Web && npm test

# TypeScript strict build check
cd src/WorkoutTracker.Web && npm run build

# E2E tests
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```

## Key Decisions Reference

| Decision | Choice | Reason |
|----------|--------|--------|
| Chart library | None — inline SVG | No external JS runtime dependencies |
| Session cap | Take(50) most-recent | Bounded query; ~6 months of twice-weekly training |
| Weight parsing | `Number(value)` | Avoids silent partial-parse of strings like "60abc" |
| Effort Y-axis | Fixed 0–10 | Bounded by DB constraint; prevents visual inflation |
| Weight Y-axis | Dynamic min/max | No upper bound; varies per exercise |
| Flat-line handling | Range ±1 around value | Prevents division-by-zero in normalisation |
| Trends fetch timing | Eager post-session-load | `plannedWorkoutId` only known after session response |
| Ad-hoc sessions | No chart rendered | No comparable history exists without plannedWorkoutId |

## Files Changed

| File | Change |
|------|--------|
| `src/WorkoutTracker.Api/Program.cs` | ADD `GET /api/workouts/{workoutId}/session-trends` |
| `src/WorkoutTracker.Web/Program.cs` | ADD proxy for session-trends |
| `src/WorkoutTracker.Web/wwwroot/ts/utils.ts` | ADD `normaliseValue`, `buildYTicks`, `buildXLabels` |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` | ADD chart section, dropdown, SVG renderer |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | ADD `.session-chart__*` BEM block |
| `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` | ADD 6 session-trends tests |
| `src/WorkoutTracker.Web/__tests__/utils.test.ts` | ADD chart math unit tests |
| `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` | ADD 4 chart E2E tests |
