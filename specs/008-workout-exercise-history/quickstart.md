# Quickstart: Previous Exercise Performance in Active Workout

**Feature**: `008-workout-exercise-history`  
**Date**: 2026-05-06

## What This Feature Does

When you start an active workout session from a planned workout you have done before, each exercise row now shows what weight and effort you recorded **the last time you ran this same workout**. This gives you an immediate reference point without needing to remember or check history separately.

The previous data is:
- **Scoped to the planned workout** — "Bench Press" in your Push Day shows Push Day history, not data from any other workout that also includes Bench Press.
- **Always from the most recent completed session** — you see what you did last time, not an average or an older result.
- **Read-only** — it does not pre-fill your inputs. You enter your current values fresh, using the previous data as a guide.

---

## User Flow

### First time running a workout

1. Open **Workouts**, click **Start** on a planned workout.
2. Each exercise card shows:
   ```
   First session — no previous data
   ```
3. Enter your weight (KG) and effort for each exercise, then click **Save Workout**.

### Subsequent sessions

1. Open **Workouts**, click **Start** on the same planned workout.
2. Each exercise card now shows a "Last time" line above the inputs, for example:
   ```
   Last time: 80 KG · 7 — Hard
   ```
   or, if only weight was recorded:
   ```
   Last time: 80 KG
   ```
   or, if only effort was recorded:
   ```
   Last time: 7 — Hard
   ```
3. Use the reference to decide your weight and effort for today.
4. Enter your values and click **Save Workout**.

### If previous data cannot be loaded

If there is a network or server issue when loading previous performance, each exercise shows:
```
Could not load previous data
```
The session is still fully usable — you can enter and save your workout normally.

---

## Changes to Existing Screens

### Active Session View (`/active-session?id=...`)

**Before**: Each exercise card showed only the exercise name, optional target weight, and input fields (Weight KG + Effort slider).

**After**: Each exercise card adds a "Last time" line (or "First session" / error message) between the target weight and the input fields. All input fields are unchanged.

### History View (`/history`)

No changes.

### Workouts View (`/workouts`)

No changes.

---

## Technical Walkthrough

### Backend: New API Endpoint

`WorkoutTracker.Api/Program.cs` — new handler:

```
GET /api/workouts/{workoutId}/previous-performance
```

1. Validates `workoutId` exists in `planned_workout` — returns 404 if not found.
2. Queries `workout_session` filtered by `planned_workout_id`, ordered by `completed_at` descending, takes the first result with its `logged_exercise` rows.
3. Returns `{ hasPreviousSession, completedAt, exercises: [{ exerciseId, loggedWeight, effort }] }`.

`WorkoutTracker.Web/Program.cs` — new proxy route that forwards the request to the API backend (same pattern as all other workout endpoints).

### Frontend: `active-session.ts`

1. `loadWorkout()` is changed to use `Promise.allSettled` to fetch both `/api/workouts/{workoutId}` and `/api/workouts/{workoutId}/previous-performance` concurrently.
2. The result of the previous-performance fetch is passed into `renderExerciseInputs()` as a `Map<exerciseId, PreviousExerciseData>`.
3. `renderExerciseInputs()` inserts a `<div class="active-session__exercise-previous">` element for each exercise with the appropriate state content (first session, data, error).

### Backend Tests: `SessionApiTests.cs`

New test cases added:
- `GetPreviousPerformance_ReturnsNoSession_WhenNoSessionsExist`
- `GetPreviousPerformance_ReturnsMostRecentSession_WhenMultipleSessionsExist`
- `GetPreviousPerformance_ReturnsOnlyDataFromSpecifiedWorkout`
- `GetPreviousPerformance_Returns404_WhenWorkoutNotFound`
- `GetPreviousPerformance_HandlesPartialData_WhenWeightOrEffortNull`

### CSS: `styles.css`

New rules for:
- `.active-session__exercise-previous` — container sizing and spacing
- `.active-session__previous-label` — muted colour, font size
- `.active-session__previous-value` — muted colour, font size
- `.active-session__previous-empty` — muted colour, italic
- `.active-session__previous-error` — error colour
