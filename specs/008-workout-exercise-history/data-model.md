# Data Model: Previous Exercise Performance in Active Workout

**Feature**: `008-workout-exercise-history`  
**Date**: 2026-05-06

## Summary of Changes

This feature makes **no schema changes**. All data required to display previous exercise performance already exists in the `workout_session` and `logged_exercise` tables introduced in features `004-add-workouts` and `005-active-workout-effort`. This feature is entirely a new read path on the existing data.

---

## Entities Used (Read-Only)

### `WorkoutSession`

| Column              | Type          | Nullable | Notes                                                   |
|---------------------|---------------|----------|---------------------------------------------------------|
| workout_session_id  | uuid          | NOT NULL | Primary key                                             |
| planned_workout_id  | uuid          | NOT NULL | FK → planned_workout — **scope for "previous" queries** |
| workout_name        | varchar       | NULL     | Snapshot of planned workout name at session time        |
| completed_at        | timestamptz   | NOT NULL | Shadow property — used to find the most recent session  |

**Query use**: Filter by `planned_workout_id`, order by `completed_at` descending, take the first result. This retrieves the single most recently completed session for the given planned workout.

### `LoggedExercise`

| Column              | Type          | Nullable | Notes                                                   |
|---------------------|---------------|----------|---------------------------------------------------------|
| logged_exercise_id  | uuid          | NOT NULL | Primary key                                             |
| workout_session_id  | uuid          | NOT NULL | FK → workout_session                                    |
| exercise_id         | uuid          | NOT NULL | FK → exercise — used to match against planned exercises |
| logged_weight       | varchar       | NULL     | Previous weight to display (e.g., "80")                 |
| effort              | integer       | NULL     | Previous effort rating (1–10)                           |

**Query use**: Retrieved via `Include(ws => ws.LoggedExercises)` on the most recent session. Each row is projected to `{ exerciseId, loggedWeight, effort }`.

---

## No Migrations Required

No `MigrationBuilder` operations are needed. The data model is unchanged.

---

## Query Projection

The new endpoint projects the most recent session's `LoggedExercise` rows into a lightweight DTO:

```csharp
// C# — anonymous type (inline in Program.cs, consistent with other endpoints)
new
{
    HasPreviousSession = lastSession != null,
    CompletedAt = lastSession != null ? (DateTime?)EF.Property<DateTime>(lastSession, "CompletedAt") : null,
    Exercises = lastSession?.LoggedExercises.Select(le => new
    {
        le.ExerciseId,
        le.LoggedWeight,
        le.Effort,
    }).ToList() ?? new List<object>()
}
```

---

## Existing Entity Relationships (Context)

```
PlannedWorkout (1) ──── (many) PlannedWorkoutExercise ──── (1) Exercise
      │
      │ (1)
      │
WorkoutSession (many)
      │
      │ (1)
      │
LoggedExercise (many) ──── (1) Exercise
```

The new endpoint traverses: `PlannedWorkout → WorkoutSession (most recent) → LoggedExercise[]`.

---

## Unchanged Entities

`Exercise`, `PlannedWorkoutExercise`, `Muscle`, `ExerciseMuscle`, `WorkoutType` are not touched by this feature.
