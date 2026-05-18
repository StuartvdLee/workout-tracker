# Data Model: Previous Exercise Order Indicator in Active Workout

**Feature**: `013-show-exercise-order`  
**Date**: 2026-05-18

## Summary of Changes

This feature makes **no schema changes**. `LoggedExercise.Sequence` already exists in the `logged_exercise` table and is populated on every session save. This feature is entirely a new read projection on an already-stored field.

---

## Entity Used (Read Path Addition)

### `LoggedExercise` — Updated Projection

The `previous-performance` endpoint's `Select` projection is extended to include `Sequence`:

| Column              | Type          | Nullable | Notes                                                         |
|---------------------|---------------|----------|---------------------------------------------------------------|
| logged_exercise_id  | uuid          | NOT NULL | Primary key (not projected to client)                        |
| workout_session_id  | uuid          | NOT NULL | FK → workout_session (not projected to client)               |
| exercise_id         | uuid          | NOT NULL | FK → exercise — used to match against active session exercise |
| logged_weight       | varchar       | NULL     | Previous weight shown in "Last time" display                  |
| effort              | integer       | NULL     | Previous effort rating (1–10)                                 |
| **sequence**        | **integer**   | **NULL** | **0-based position in the previous session — NEW in projection** |

`Sequence` is `NULL` when the session was saved without a sequence value (old sessions or tests that omit the field). The frontend handles `null` gracefully by omitting the `#x` indicator.

---

## No Migrations Required

`LoggedExercise.Sequence` already exists in the EF Core model (`src/WorkoutTracker.Infrastructure/Data/Models/LoggedExercise.cs`) and in the DB snapshot (`WorkoutTrackerDbContextModelSnapshot.cs` line 107–109). No `MigrationBuilder` operations are needed.

---

## Updated Query Projection

```csharp
// Program.cs — GET /api/workouts/{workoutId}/previous-performance
// BEFORE (feature 008):
LoggedExercises = ws.LoggedExercises.Select(le => new
{
    le.ExerciseId,
    le.LoggedWeight,
    le.Effort,
}).ToList(),

// AFTER (feature 013 — add Sequence):
LoggedExercises = ws.LoggedExercises.Select(le => new
{
    le.ExerciseId,
    le.LoggedWeight,
    le.Effort,
    le.Sequence,
}).ToList(),
```

---

## Unchanged Entities

`WorkoutSession`, `PlannedWorkout`, `PlannedWorkoutExercise`, `Exercise`, `Muscle`, `ExerciseMuscle`, `WorkoutType` are not touched by this feature.

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
  ├── LoggedWeight (varchar, null)
  ├── Effort       (integer, null)
  └── Sequence     (integer, null)  ← NOW PROJECTED in previous-performance
```
