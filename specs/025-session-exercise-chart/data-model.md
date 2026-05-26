# Data Model: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Phase**: 1 — Design  
**Date**: 2026-05-26

## Schema Changes

**None.** All required data already exists in `workout_session` and `logged_exercise`. No migrations are needed.

## Existing Entities Used

### WorkoutSession

| Column              | Type          | Notes                                      |
|---------------------|---------------|--------------------------------------------|
| workout_session_id  | UUID (PK)     | Session identifier                         |
| planned_workout_id  | UUID (FK, nullable) | Links to PlannedWorkout; null for ad-hoc |
| workout_name        | TEXT (nullable) | Snapshot of workout name at save time     |
| overall_effort      | INT (nullable, 1–10) | Session-level effort rating           |
| CompletedAt         | TIMESTAMP     | Shadow property; `HasDefaultValueSql("now()")` |

### LoggedExercise

| Column              | Type          | Notes                                      |
|---------------------|---------------|--------------------------------------------|
| logged_exercise_id  | UUID (PK)     | Exercise log identifier                    |
| workout_session_id  | UUID (FK)     | Parent session                             |
| exercise_id         | UUID (FK)     | Links to Exercise                          |
| logged_weight       | TEXT (nullable, max 100) | Free-text weight string (e.g., "80.5") |
| effort              | INT (nullable, 1–10) | Per-exercise effort rating             |
| sequence            | INT           | Exercise order within session              |

### Exercise

| Column      | Type | Notes               |
|-------------|------|---------------------|
| exercise_id | UUID | Exercise identifier |
| name        | TEXT | Exercise display name |

## New API Response Shape

No new database entities. The new endpoint projects existing data into a new read-only DTO shape:

### SessionTrendItem (response array element)

| Field              | Type                   | Notes                                   |
|--------------------|------------------------|-----------------------------------------|
| workoutSessionId   | string (GUID)          | Session identifier                      |
| completedAt        | string (ISO 8601)      | Session completion timestamp            |
| overallEffort      | number \| null         | Overall session effort (1–10)           |
| exercises          | SessionTrendExercise[] | Exercises logged in this session        |

### SessionTrendExercise (nested)

| Field        | Type           | Notes                                      |
|--------------|----------------|--------------------------------------------|
| exerciseId   | string (GUID)  | Exercise identifier                        |
| exerciseName | string         | Exercise name at time of logging           |
| loggedWeight | string \| null | Raw weight string; client parses with Number() |
| effort       | number \| null | Per-exercise effort (1–10)                 |

## Frontend Type Interfaces

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

## Chart Data Transformation

The frontend transforms `SessionTrendItem[]` into chart series on demand (when dropdown selection changes):

```
ChartSeries = {
  label: string,           // e.g., "Bench Press – Weight"
  points: ChartPoint[]     // ordered chronologically
}

ChartPoint = {
  x: string,               // formatted date label
  y: number                // numeric value to plot
}
```

**Weight series**: `y = Number(exercise.loggedWeight)` — sessions where result is `NaN` or the exercise is absent are excluded.  
**Effort series**: `y = exercise.effort` (only sessions where value is non-null).  
**Overall effort series**: `y = session.overallEffort` (only sessions where value is non-null).

## Validation Rules (unchanged from existing feature)

- `loggedWeight`: max 100 characters, free-text — validated on write; chart reads it as-is
- `effort` / `overallEffort`: integer 1–10, enforced by DB check constraint — chart uses fixed 0–10 Y-axis
