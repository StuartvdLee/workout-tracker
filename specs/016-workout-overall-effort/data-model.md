# Data Model: Workout Overall Effort

**Feature**: `016-workout-overall-effort`

---

## Entity Changes

### WorkoutSession (MODIFIED)

Existing entity in `src/WorkoutTracker.Infrastructure/Data/Models/WorkoutSession.cs`.

**New property**:
```csharp
public int? OverallEffort { get; set; }
```

| Property | C# Type | DB Column | Nullable | Constraint |
|---|---|---|---|---|
| `OverallEffort` | `int?` | `overall_effort` | Yes (NULL = not rated) | `CHECK (overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10))` |

**No other entity changes.** `LoggedExercise` is unchanged.

---

## EF Core Configuration Change

In `WorkoutTrackerDbContext.OnModelCreating`, inside the existing `WorkoutSession` entity block:

```csharp
modelBuilder.Entity<WorkoutSession>(entity =>
{
    entity.HasKey(e => e.WorkoutSessionId);
    entity.Property(e => e.WorkoutName).HasMaxLength(150);
    entity.HasOne(e => e.PlannedWorkout)...;
    entity.Property<DateTime>("CompletedAt").HasDefaultValueSql("now()");

    // NEW:
    entity.HasCheckConstraint(
        "ck_workout_session_overall_effort_range",
        "overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10)");
});
```

---

## Migration

**File**: `src/WorkoutTracker.Infrastructure/Data/Migrations/[timestamp]_AddOverallEffortToWorkoutSession.cs`

**Up**:
```csharp
migrationBuilder.AddColumn<int>(
    name: "overall_effort",
    table: "workout_session",
    type: "integer",
    nullable: true);

migrationBuilder.AddCheckConstraint(
    name: "ck_workout_session_overall_effort_range",
    table: "workout_session",
    sql: "overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10)");
```

**Down**:
```csharp
migrationBuilder.DropCheckConstraint(
    name: "ck_workout_session_overall_effort_range",
    table: "workout_session");

migrationBuilder.DropColumn(
    name: "overall_effort",
    table: "workout_session");
```

**Backward compatibility**: All existing `workout_session` rows receive `overall_effort = NULL`. No data backfill required. All existing queries, endpoints, and tests continue to work unchanged.

---

## API DTO Changes

### SessionCreateRequest (MODIFIED)

In `src/WorkoutTracker.Api/Program.cs`:

```csharp
internal sealed class SessionCreateRequest
{
    public SessionLoggedExerciseItem[] LoggedExercises { get; set; } = [];
    public int? OverallEffort { get; set; }   // NEW — nullable; omitted payload deserializes as null
}
```

No changes to `SessionLoggedExerciseItem`.

---

## Response Shape Changes

### POST /api/workouts/{workoutId}/sessions — Response (201)

Adds `overallEffort` to the response body:
```json
{
  "workoutSessionId": "guid",
  "plannedWorkoutId": "guid",
  "workoutName": "string",
  "overallEffort": 7,
  "loggedExercises": [ ... ]
}
```

### GET /api/sessions — Per-session object

Adds `overallEffort` to each session in the array:
```json
{
  "workoutSessionId": "guid",
  "plannedWorkoutId": "guid | null",
  "workoutName": "string",
  "completedAt": "ISO 8601",
  "overallEffort": 7,
  "loggedExercises": [ { "exerciseId": "guid", ... } ]
}
```

### GET /api/sessions/{sessionId} — Session detail object

Adds `overallEffort` and `previousOverallEffort` to the response:
```json
{
  "workoutSessionId": "guid",
  "plannedWorkoutId": "guid | null",
  "workoutName": "string",
  "completedAt": "ISO 8601",
  "overallEffort": 8,
  "previousOverallEffort": 6,
  "exercises": [ { "loggedExerciseId": "guid", "exerciseName": "...", "effort": 9, "previousEffort": 8, ... } ]
}
```

`previousOverallEffort` is `null` when:
- No prior session for the same planned workout exists.
- The prior session has no overall effort recorded.
- The current session is ad-hoc (no `plannedWorkoutId`).

---

## TypeScript Interface Changes

### `history.ts` — WorkoutSession interface

No net change. An `overallEffort` field was added then removed when the history page effort display was reverted.

### `session-detail.ts` — SessionDetailWithPrevious interface (MODIFIED)

```ts
interface SessionDetailWithPrevious {
  readonly workoutSessionId: string;
  readonly plannedWorkoutId: string | null;
  readonly workoutName: string | null;
  readonly completedAt: string;
  readonly overallEffort: number | null;           // NEW
  readonly previousOverallEffort: number | null;   // NEW
  readonly exercises: SessionExerciseWithPrevious[];
}
```

### `active-session.ts` — new module-level state (MODIFIED)

```ts
let pendingOverallEffort: number | null = null;
```

No interface changes — `overallEffort` is collected from the modal at save time, not stored in `LogEntry`.
