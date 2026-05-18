# Data Model: Workout History Detail Page

**Feature**: `014-workout-history-detail-page`
**Branch**: `014-workout-history-detail-page`

---

## Schema Changes

**None.** All required data already exists in the database. No new tables, columns, or migrations are needed.

---

## Existing Entities Used

### WorkoutSession

| Field | Type | Notes |
|-------|------|-------|
| `WorkoutSessionId` | `Guid` (PK) | Used as the session identifier in `?id=` URL param |
| `PlannedWorkoutId` | `Guid?` (FK → PlannedWorkout) | Used to scope previous-session lookup |
| `WorkoutName` | `string?` | Display name on detail page header |
| `CompletedAt` | `DateTime` (shadow property) | DB default `now()`; used for ordering |
| `LoggedExercises` | nav. collection | Exercises in this session |

### LoggedExercise

| Field | Type | Notes |
|-------|------|-------|
| `LoggedExerciseId` | `Guid` (PK) | Identifies each row |
| `WorkoutSessionId` | `Guid` (FK → WorkoutSession) | Groups exercises by session |
| `ExerciseId` | `Guid` (FK → Exercise) | Used to match exercises across sessions |
| `LoggedWeight` | `string?` | Weight string, e.g. "80 KG"; may be `null` |
| `Effort` | `int?` (1–10) | Effort rating; may be `null` |
| `Sequence` | `int?` | 0-based order index within the session |

### PlannedWorkout (read-only reference)

| Field | Type | Notes |
|-------|------|-------|
| `PlannedWorkoutId` | `Guid` (PK) | Referenced by `WorkoutSession.PlannedWorkoutId` |
| `Name` | `string` | Used as fallback workout name if `WorkoutSession.WorkoutName` is null |

---

## API Response Shape: GET /api/sessions/{sessionId}

This is the computed shape returned by the new endpoint — it is not a stored entity but a projection assembled at query time.

### SessionDetailResponse

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `workoutSessionId` | `string` (GUID) | `WorkoutSession.WorkoutSessionId` | |
| `plannedWorkoutId` | `string?` (GUID) | `WorkoutSession.PlannedWorkoutId` | `null` for ad-hoc sessions |
| `workoutName` | `string` | `WorkoutSession.WorkoutName` ?? `PlannedWorkout.Name` | |
| `completedAt` | `string` (ISO 8601) | `CompletedAt` shadow property | |
| `exercises` | `SessionExerciseDetail[]` | See below | |

### SessionExerciseDetail

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `loggedExerciseId` | `string` (GUID) | `LoggedExercise.LoggedExerciseId` | |
| `exerciseName` | `string` | `Exercise.Name` | |
| `loggedWeight` | `string?` | `LoggedExercise.LoggedWeight` | `null` if not recorded |
| `effort` | `number?` | `LoggedExercise.Effort` | `null` if not recorded; 1–10 |
| `previousWeight` | `string?` | Prior session's matching `LoggedWeight` | `null` if no prior session or no match |
| `previousEffort` | `number?` | Prior session's matching `Effort` | `null` if no prior session or no match |

---

## Visual State Model: Session Detail Page

| State | Trigger | Display |
|-------|---------|---------|
| Loading | Fetch in progress | Skeleton / "Loading..." in content area; workout name may be shown from URL state |
| Success (with exercises) | Fetch succeeded, exercises > 0 | Five-column table |
| Success (empty) | Fetch succeeded, exercises = 0 | "No exercises were logged for this session." message |
| Error | Fetch failed (network / non-200) | "Could not load session details." message; back navigation still visible |
| Not found | API returns 404 | "Session not found." message; back navigation still visible |

---

## Previous-Session Match Logic

For each exercise row in the viewed session:

1. If `session.PlannedWorkoutId == null`: `previousWeight = null`, `previousEffort = null`
2. If no prior session exists for the same `PlannedWorkoutId`: same as above
3. If a prior session exists: find the `LoggedExercise` in that session where `ExerciseId` matches the current row's `ExerciseId`
   - Match found: use its `LoggedWeight` and `Effort`
   - No match (exercise not in prior session): `previousWeight = null`, `previousEffort = null`

The prior session is defined as the most recently completed session for the same `PlannedWorkoutId` with `CompletedAt ≤ session.CompletedAt` and `WorkoutSessionId ≠ sessionId`.
