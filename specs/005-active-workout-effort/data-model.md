# Data Model: Active Workout UI — Effort Tracking

**Feature**: `005-active-workout-effort`  
**Date**: 2026-04-27

## Summary of Changes

This feature makes one additive change to the existing schema: a new nullable integer column `effort` is added to the `logged_exercise` table. No other tables are modified and no columns are dropped.

---

## Modified Entity: `LoggedExercise`

### Current State (feature 004)

| Column              | Type          | Nullable | Notes                              |
|---------------------|---------------|----------|------------------------------------|
| logged_exercise_id  | uuid          | NOT NULL | Primary key                        |
| workout_session_id  | uuid          | NOT NULL | FK → workout_session               |
| exercise_id         | uuid          | NOT NULL | FK → exercise                      |
| logged_reps         | integer       | NULL     | No longer surfaced in UI           |
| logged_weight       | varchar       | NULL     | Free-form weight string            |
| notes               | varchar       | NULL     | Free-form notes                    |

### New State (feature 005)

| Column              | Type          | Nullable | Notes                              |
|---------------------|---------------|----------|------------------------------------|
| logged_exercise_id  | uuid          | NOT NULL | Primary key                        |
| workout_session_id  | uuid          | NOT NULL | FK → workout_session               |
| exercise_id         | uuid          | NOT NULL | FK → exercise                      |
| logged_reps         | integer       | NULL     | Preserved; no longer surfaced in UI|
| logged_weight       | varchar       | NULL     | Free-form weight string            |
| notes               | varchar       | NULL     | Free-form notes                    |
| **effort**          | **integer**   | **NULL** | **NEW: perceived exertion 1–10**   |

### Validation Rules

- `effort` must be NULL or an integer in the inclusive range [1, 10]
- Enforced via DB check constraint: `effort IS NULL OR (effort >= 1 AND effort <= 10)`
- Enforced via server-side API validation (400 returned if out of range)
- The browser slider (min=1, max=10, step=1) provides first-line client-side enforcement

### C# Model Change

```csharp
// LoggedExercise.cs — add one property
public int? Effort { get; set; }
```

### EF Core Configuration Change

```csharp
// WorkoutTrackerDbContext.OnModelCreating — add check constraint
modelBuilder.Entity<LoggedExercise>(entity =>
{
    // ... existing config unchanged ...
    entity.ToTable(t => t.HasCheckConstraint(
        "ck_logged_exercise_effort_range",
        "effort IS NULL OR (effort >= 1 AND effort <= 10)"));
});
```

### Migration

**Migration name**: `AddEffortToLoggedExercise`

```csharp
migrationBuilder.AddColumn<int>(
    name: "effort",
    schema: "workout_tracker",
    table: "logged_exercise",
    nullable: true);

migrationBuilder.AddCheckConstraint(
    name: "ck_logged_exercise_effort_range",
    schema: "workout_tracker",
    table: "logged_exercise",
    sql: "effort IS NULL OR (effort >= 1 AND effort <= 10)");
```

Down method reverses both operations.

---

## Unchanged Entities

All other entities (`PlannedWorkout`, `WorkoutSession`, `PlannedWorkoutExercise`, `Exercise`, `Muscle`, `ExerciseMuscle`, `Workout`, `WorkoutExercise`, `WorkoutType`) are unaffected by this feature.

---

## Effort Band Derivation (Application Logic)

The intensity label is derived from the stored integer at render time. It is not stored in the database.

| Stored Value | Display Label |
|--------------|---------------|
| 1, 2, 3      | Easy          |
| 4, 5, 6      | Moderate      |
| 7, 8         | Hard          |
| 9, 10        | All Out       |
| NULL         | (not shown)   |
