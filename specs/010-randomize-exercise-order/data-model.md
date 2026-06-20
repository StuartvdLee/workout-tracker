# Data Model: Randomize Workout Exercise Order

**Feature**: `010-randomize-exercise-order`  
**Date**: 2026-05-08

## Summary of Changes

This feature introduces **one schema change**: a nullable `Sequence` column is added to the `logged_exercise` table to record the actual order in which exercises were performed in a session. All other data model entities are unchanged.

The shuffle operation is performed entirely client-side. No changes are made to `planned_workout_exercise.sequence` — the workout template is never mutated.

---

## Schema Change: `logged_exercise.sequence`

### Modified Entity: `LoggedExercise`

**Current schema**:

| Column             | Type        | Nullable | Notes                                |
|--------------------|-------------|----------|--------------------------------------|
| logged_exercise_id | uuid        | NOT NULL | Primary key                          |
| workout_session_id | uuid        | NOT NULL | FK → workout_session                 |
| exercise_id        | uuid        | NOT NULL | FK → exercise                        |
| logged_reps        | int         | NULL     |                                      |
| logged_weight      | varchar     | NULL     |                                      |
| notes              | varchar     | NULL     |                                      |
| effort             | int         | NULL     | Added in feature 005; check (1–10)   |

**After this feature**:

| Column             | Type        | Nullable | Notes                                                                         |
|--------------------|-------------|----------|-------------------------------------------------------------------------------|
| logged_exercise_id | uuid        | NOT NULL | Primary key                                                                   |
| workout_session_id | uuid        | NOT NULL | FK → workout_session                                                          |
| exercise_id        | uuid        | NOT NULL | FK → exercise                                                                 |
| logged_reps        | int         | NULL     |                                                                               |
| logged_weight      | varchar     | NULL     |                                                                               |
| notes              | varchar     | NULL     |                                                                               |
| effort             | int         | NULL     | Added in feature 005; check (1–10)                                            |
| **sequence**       | **int**     | **NULL** | **0-based position in the session's exercise display order (0 = first shown)**|

**Change**: One new nullable `int` column `sequence`.

- **NULL meaning**: Existing sessions created before this feature was deployed. `NULL` is a valid historical state; no backfill is required.
- **Non-null meaning**: The 0-based index at which this exercise was displayed during the session. For a non-shuffled session: matches the `PlannedWorkoutExercise.Sequence` display order. For a shuffled session: reflects the shuffled order.
- **No check constraint**: No range constraint is applied — the valid range is implicitly `0` to `exerciseCount - 1` and is enforced by the frontend at write time.

### C# Entity Change

```csharp
// WorkoutTracker.Infrastructure/Data/Models/LoggedExercise.cs
public class LoggedExercise
{
    public Guid LoggedExerciseId { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public Guid ExerciseId { get; set; }
    public int? LoggedReps { get; set; }
    public string? LoggedWeight { get; set; }
    public string? Notes { get; set; }
    public int? Effort { get; set; }
    public int? Sequence { get; set; }   // NEW
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}
```

---

## Migration

**Migration name**: `AddSequenceToLoggedExercise`

**Operation**:
```csharp
migrationBuilder.AddColumn<int>(
    name: "sequence",
    schema: "workout_tracker",
    table: "logged_exercises",
    type: "integer",
    nullable: true);
```

**Pattern reference**: Identical pattern to `AddEffortToLoggedExercise` migration (feature 005), which added a single nullable `int` column to `logged_exercises`.

---

## API DTO Change: `SessionLoggedExerciseItem`

The request DTO for `POST /api/workouts/{workoutId}/sessions` gains one new nullable field:

```csharp
// Before:
internal sealed class SessionLoggedExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public string? Notes { get; set; }
    public int? Effort { get; set; }
}

// After:
internal sealed class SessionLoggedExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public string? Notes { get; set; }
    public int? Effort { get; set; }
    public int? Sequence { get; set; }   // NEW — 0-based display position
}
```

The session creation handler maps `Sequence` from the DTO to the entity:

```csharp
new LoggedExercise
{
    ExerciseId = item.ExerciseId,
    LoggedWeight = item.LoggedWeight,
    Notes = item.Notes,
    Effort = item.Effort,
    Sequence = item.Sequence,   // NEW
}
```

**Backward compatibility**: Existing callers that omit `Sequence` will have it deserialize as `null`. The nullable type ensures no breaking change.

---

## Unchanged Entities

| Entity                   | Reason unchanged                                                                |
|--------------------------|---------------------------------------------------------------------------------|
| `PlannedWorkout`         | The template definition. Never modified by shuffle; template immutability guaranteed. |
| `PlannedWorkoutExercise` | The template exercise order (`Sequence`). Never modified by shuffle.           |
| `WorkoutSession`         | Session header. No new fields needed; session creation flow is unchanged.      |
| `Exercise`               | Exercise catalogue. Unaffected.                                                |

---

## Entity Relationship (context, unchanged)

```
PlannedWorkout (1) ──── (many) PlannedWorkoutExercise  ← Sequence NEVER modified
      │
      └──── (many) WorkoutSession
                        │
                        └──── (many) LoggedExercise  ← sequence (NEW, nullable)
```

The `PlannedWorkoutExercise.Sequence` records the template order; `LoggedExercise.Sequence` records the actual session display order. These may differ when a shuffle is applied.

---

## Frontend State Model Change

`active-session.ts` gains an optional `exerciseOrder` derived from the URL `order` parameter:

```typescript
// Parsed from ?order=guid1,guid2,...
let exerciseOrder: string[] | null = null;
```

When non-null, `workout.exercises` is reordered in memory before rendering. The template's `exercises` array (from the API) is never persisted back to the server with a different order.

The `handleSave()` function populates `Sequence` based on display index:

```typescript
// In handleSave(), when building the LoggedExercises array:
const displayedExercises = exerciseOrder !== null
  ? reorderByIds(workout.exercises, exerciseOrder)
  : workout.exercises;

const loggedExercises = displayedExercises.map((ex, index) => ({
  exerciseId: ex.exerciseId,
  loggedWeight: logEntries.get(ex.exerciseId)?.loggedWeight ?? "",
  effort: logEntries.get(ex.exerciseId)?.loggedEffort ?? null,
  sequence: index,
}));
```
