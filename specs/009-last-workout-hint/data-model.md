# Data Model: Last Workout Hint on Home Page

**Feature**: `009-last-workout-hint`  
**Date**: 2026-05-06

## Summary of Changes

This feature makes **no schema changes**. All data required to display the last workout hint already exists in the `workout_session` table introduced in feature `004-add-workouts` and extended in `005-active-workout-effort`. This feature is entirely a new read path on existing data.

---

## Entity Used (Read-Only)

### `WorkoutSession`

| Column              | Type          | Nullable | Notes                                                              |
|---------------------|---------------|----------|--------------------------------------------------------------------|
| workout_session_id  | uuid          | NOT NULL | Primary key                                                        |
| planned_workout_id  | uuid          | NOT NULL | FK → planned_workout (not used in this query)                      |
| workout_name        | varchar       | NULL     | Snapshot of planned workout name at session time — displayed in hint |
| completed_at        | timestamptz   | NOT NULL | Shadow property — used to find the globally most recent session    |

**Query use**: No `Where` filter (all sessions considered). Order by `completed_at` descending, take the first result. Project to `{ WorkoutName, CompletedAt }` only — no joins, no `Include`.

---

## No Migrations Required

No `MigrationBuilder` operations are needed. The data model is unchanged.

---

## Query Projection

The new endpoint projects the most recent session into a lightweight DTO:

```csharp
// C# — anonymous type (inline in Program.cs, consistent with other endpoints)
var latest = await db.WorkoutSessions
    .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
    .Select(ws => new
    {
        ws.WorkoutName,
        CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
    })
    .FirstOrDefaultAsync();
```

Response when no sessions exist:
```json
{ "hasSession": false }
```

Response when a session exists:
```json
{
  "hasSession": true,
  "workoutName": "Push",
  "completedAt": "2026-05-03T09:15:00Z"
}
```

---

## Existing Entity Relationships (Context)

```
PlannedWorkout (1) ──── (many) WorkoutSession
                                     │
                              workout_name (snapshot)
                              completed_at (shadow)
```

The new endpoint reads only from `WorkoutSession` — no traversal to `PlannedWorkout` or `LoggedExercise` is required.
