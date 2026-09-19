# Data Model: Workout Sets Selection

**Feature**: `032-workout-sets-selection`

## Entity Change

### WorkoutSession (MODIFIED)

Existing entity: `src/WorkoutTracker.Infrastructure/Data/Models/WorkoutSession.cs`

```csharp
public int? Sets { get; set; }
```

| Property | C# Type | Database column | Nullable | Constraint |
|---|---|---|---:|---|
| `Sets` | `int?` | `sets` | Yes | `sets IS NULL OR sets IN (3, 5)` |

`NULL` represents a session created before sets were recorded. Every newly created session must provide 3 or 5. `LoggedExercise` remains unchanged.

## Relationships and Invariants

- One `WorkoutSession` has one sets value.
- Every `LoggedExercise` in that session displays the parent session's current sets value.
- A previous sets value displayed for an exercise comes from the historical session selected as that exercise's latest usable weight-or-effort comparison.
- Per-exercise sets are never persisted.
- Updating sets changes the one parent session value and therefore changes the displayed current sets value for every exercise row.

## EF Core Configuration

Add a check constraint in the existing `WorkoutSession` configuration:

```csharp
entity.HasCheckConstraint(
    "ck_workout_session_sets_allowed",
    "sets IS NULL OR sets IN (3, 5)");
```

## Migration

Create a migration adding a nullable integer `sets` column and the check constraint.

**Up**:

```csharp
migrationBuilder.AddColumn<int>(
    name: "sets",
    schema: "workout_tracker",
    table: "workout_session",
    type: "integer",
    nullable: true);

migrationBuilder.AddCheckConstraint(
    name: "ck_workout_session_sets_allowed",
    schema: "workout_tracker",
    table: "workout_session",
    sql: "sets IS NULL OR sets IN (3, 5)");
```

**Down**: Drop the check constraint, then drop the column.

Existing rows remain `NULL`; no backfill is performed.

## API Model Changes

### SessionCreateRequest

Add required-for-new-sessions `int? Sets`. Nullable deserialization distinguishes missing input, which the create endpoint rejects with a validation response.

### SessionUpdateRequest

Add presence-aware Sets input so the endpoint distinguishes an omitted property from an explicit JSON `null`; a plain `int?` property alone is insufficient for this contract.

- Omitted `sets`: preserve the stored value for backward compatibility with existing update clients.
- `sets: null`: explicitly clear the value for legacy/no-data compatibility.
- `sets: 3` or `sets: 5`: replace the stored value.
- Any other value: reject the request.

### Historical comparison records

Extend historical session/comparison projection so `LatestExerciseComparison` carries `Sets` from the source `HistoricalSessionData`. Keep the existing weight-or-effort usable-comparison predicate unchanged; Sets alone does not make a historical exercise row selectable.

## Validation Rules

| Operation | Accepted values | Rejected values |
|---|---|---|
| Create new session | 3 or 5 | Missing, `null`, non-integer, or any other integer |
| Update existing session | Omitted (preserve), 3 or 5 (replace), explicit `null` (clear) | Non-integer or any other integer |
| Read legacy session | `null` | N/A; render as absent/no-data |

## State Transitions

### New session

`No session` → user selects workout and sets → active workout retains sets → save validates and persists session with `Sets = 3 or 5`.

### Historical edit

`View` → `Editing` → synchronized Sets controls change one session-level draft value → save validates → `WorkoutSession.Sets` updated → response re-renders all rows.

### Previous comparison

Completed sessions ordered newest-first → selector chooses the latest entry with usable weight or effort per exercise → selected comparison carries that source session's sets → active-session and history detail render that value.
