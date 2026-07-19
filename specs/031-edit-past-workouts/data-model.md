# Data Model: Edit Past Workouts

## Overview

This feature does not add entities, columns, or migrations. It changes which fields on existing completed-session records may be edited after save.

## Existing Entities

### Workout Session

Represents a completed workout record.

**Existing fields used by this feature**:

| Field | Type | Editable | Notes |
|---|---|---:|---|
| `workoutSessionId` | GUID | No | Target session identity |
| `plannedWorkoutId` | GUID \| null | No | Preserved; used for previous comparisons and chart trends |
| `workoutName` | string \| null | No | Preserved historical display name |
| `completedAt` | DateTime shadow property | No | Preserved session completion timestamp |
| `overallEffort` | integer \| null | Yes | Optional 1-10 session-level effort |
| `loggedExercises` | collection | No membership changes | Existing session exercise rows |

**Validation rules**:

- `overallEffort` may be `null`.
- If provided, `overallEffort` must be an integer from 1 through 10.
- Editing a workout session must not change its ID, planned workout association, workout name, completion timestamp, or exercise membership.

### Logged Exercise

Represents one exercise entry recorded within a completed workout session.

**Existing fields used by this feature**:

| Field | Type | Editable | Notes |
|---|---|---:|---|
| `loggedExerciseId` | GUID | No | Stable row identity for update requests |
| `workoutSessionId` | GUID | No | Must match the target session |
| `exerciseId` | GUID | No | Preserved exercise identity |
| `loggedWeight` | string \| null | Yes | Optional weight value, max 100 characters |
| `effort` | integer \| null | Yes | Optional 1-10 per-exercise effort |
| `sequence` | integer \| null | No | Preserved display/order value |

**Validation rules**:

- `loggedWeight` may be `null` to represent intentionally empty. Empty string input from the UI is normalized to `null` before saving.
- If provided, `loggedWeight` must not exceed 100 characters, matching the session-create endpoint.
- `effort` may be `null`.
- If provided, `effort` must be an integer from 1 through 10.
- Every edited `loggedExerciseId` must belong to the target `workoutSessionId`.
- Editing must not add, remove, duplicate, or reorder logged exercises.

## Client-Side Editable Session Snapshot

Represents the values captured when the user enters edit mode on the session detail page.

| Field | Type | Notes |
|---|---|---|
| `overallEffort` | number \| null | Original session-level effort |
| `exercises` | array | One snapshot entry per rendered session exercise |
| `exercises[].loggedExerciseId` | string | Stable row key |
| `exercises[].loggedWeight` | string \| null | Original weight value |
| `exercises[].effort` | number \| null | Original per-exercise effort |

**State transitions**:

```text
View mode with loaded session detail
  -> user selects Edit
Edit mode; original snapshot captured
  -> user changes weight/effort/overall effort
Edit mode with dirty state
  -> Save: validate client-side, send PUT, reload/re-render updated detail
  -> Cancel with no changes: return to view mode
  -> Cancel/back with changes: show discard confirmation
  -> Discard confirmed: restore original snapshot and return to view mode
  -> Save failure: stay in edit mode with edits intact
```

## Persistence Semantics

- Updates are applied to existing `WorkoutSession` and `LoggedExercise` rows.
- One save request represents one logical edit operation for a completed workout.
- The database remains the source of truth for future history, comparison, and chart displays.
- No derived cache or secondary correction table is introduced.
