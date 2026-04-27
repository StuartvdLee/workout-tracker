# Quickstart: Active Workout UI — Effort Tracking

**Feature**: `005-active-workout-effort`  
**Date**: 2026-04-27

## What Changed

This feature updates the active workout logging screen and the history view:

1. **No more reps** — the reps input is gone. You don't need to log reps since they're always the same.
2. **Weight is now clearly KG** — the weight field is labelled "Weight (KG)".
3. **Effort slider** — for each exercise, slide to rate how hard it felt on a scale from 1 (Easy) to 10 (All Out).

---

## Logging a Workout (Updated Flow)

### Step 1 — Navigate to Workouts

Go to `/workouts`. Your planned workouts are listed.

### Step 2 — Start a Session

Click **Start Workout** on any planned workout. The active session view opens with all the exercises from the template.

### Step 3 — Log Each Exercise

For each exercise you'll see:

| Field           | What to do                                                        |
|-----------------|-------------------------------------------------------------------|
| **Weight (KG)** | Enter the weight you lifted (e.g., `80`). Leave blank for bodyweight. |
| **Effort**      | Drag the slider to rate how hard the set felt:                    |
|                 | 1–3 = Easy · 4–6 = Moderate · 7–8 = Hard · 9–10 = All Out        |
| **Notes**       | Optional free-text notes.                                         |

The effort label updates live as you move the slider. If you don't touch the slider, no effort value is recorded for that exercise — that's fine.

### Step 4 — Save

Click **Save Workout**. The session is saved and you're taken to the History page.

### Step 5 — Cancel

Click **Cancel** or **← Back to Workouts**. If you've entered any data you'll be asked to confirm before discarding.

---

## Viewing History (Updated Display)

Go to `/history`. Each completed session expands to show its exercises.

For each exercise you'll see (only values that were recorded):

```
Bench Press   80 KG · 7 — Hard · Great set
Overhead Press   — (no data logged)
```

Reps are no longer shown. Weight is displayed with the KG unit. Effort is shown as a number and its band name.

---

## Running Locally

```bash
# From repo root — start the full stack (Aspire)
dotnet run --project src/WorkoutTracker.AppHost

# Run backend tests
dotnet test src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj

# Run frontend tests
cd src/WorkoutTracker.Web && npm test
```

The database migration runs automatically on startup via `db.Database.MigrateAsync()`.
