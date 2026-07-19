# Data Model: Latest Exercise Data

**Feature**: `029-latest-exercise-data`  
**Date**: 2026-07-19

## Summary of Changes

No schema changes are required. This feature changes how existing `WorkoutSession` and `LoggedExercise` rows are read for the active-session "Last time" display.

## Entities Used (Read-Only)

### `PlannedWorkout`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| planned_workout_id | uuid | NOT NULL | Scope for active workout and previous-performance lookup |

**Query use**: Validate the requested workout exists and obtain current planned exercise IDs for selection.

### `WorkoutSession`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| workout_session_id | uuid | NOT NULL | Stable tiebreaker when sessions share `completed_at` |
| planned_workout_id | uuid | NOT NULL | Restricts history to the same planned workout |
| completed_at | timestamptz | NOT NULL | Shadow property used to order history newest-first |

**Query use**: Filter by `planned_workout_id`, order by `completed_at` descending and then `workout_session_id` descending, then inspect logged exercises for latest usable data.

### `LoggedExercise`

| Column | Type | Nullable | Notes |
|---|---|---|---|
| logged_exercise_id | uuid | NOT NULL | Primary key |
| workout_session_id | uuid | NOT NULL | FK to the historical workout session |
| exercise_id | uuid | NOT NULL | Matches a current workout exercise |
| logged_weight | varchar | NULL | Usable comparison data when non-blank |
| effort | integer | NULL | Usable comparison data when present |
| sequence | integer | NULL | Context for selected row; not usable by itself |

**Query use**: A row is eligible only when its `exercise_id` belongs to the requested planned workout and it has non-blank `logged_weight` or non-null `effort`.

## Latest Available Selection Rule

For each exercise in the requested planned workout:

1. Sort completed sessions for that planned workout newest-first by `completed_at`, then by `workout_session_id`.
2. Inspect logged exercises matching the current exercise.
3. Skip rows where `logged_weight` is null/blank and `effort` is null.
4. Select the first usable row.
5. Return no entry for that exercise when no usable row exists.

Different exercises may select data from different sessions.

## Response Projection

The endpoint returns a lightweight projection:

```csharp
new
{
    hasPreviousSession = selectedExercises.Count > 0,
    completedAt = selectedExercises.Count > 0 ? selectedExercises.Max(e => e.CompletedAt) : (DateTime?)null,
    exercises = selectedExercises.Select(e => new
    {
        e.ExerciseId,
        e.LoggedWeight,
        e.Effort,
        e.Sequence,
        e.CompletedAt,
    })
}
```

`completedAt` at the response root is retained for compatibility and represents the newest selected exercise data in the response. Consumers that need exact freshness for an exercise should use `exercises[].completedAt`.

## No Migrations Required

No `MigrationBuilder` operations are needed. Existing columns and relationships are sufficient.

## Unchanged Entities

`Exercise`, `PlannedWorkoutExercise`, `Muscle`, `ExerciseMuscle`, and `WorkoutType` are not structurally changed.
