# Data Model: Edit Exercise Order in Current Workout

## Overview

This feature does not add database entities, columns, or API resource types. It changes the client-side state model of the active/current workout screen by allowing the loaded exercise list to be reordered before the session is saved.

## Entities

### Current Workout

Represents the workout currently loaded in `active-session.ts`.

**Existing fields used by this feature**:

| Field | Type | Notes |
|---|---|---|
| `plannedWorkoutId` | string | Existing planned workout identifier |
| `name` | string | Displayed in the active-session header |
| `exercises` | `WorkoutExercise[]` | Existing ordered display list; this feature reorders it in memory |

**State transitions**:

```text
Normal logging mode
  -> user selects "Edit order"
Order-editing mode with collapsed rows (orderBeforeEditing snapshot taken)
  -> drag/touch/keyboard reorder
Order-editing mode with updated array order
  -> user selects "Done"
Normal logging mode in updated order
  -> user saves workout
Session POST logs sequence values from updated order

  -- alternative exit path --
Order-editing mode (no changes made)
  -> user selects "Cancel"
Normal logging mode in original order (no confirmation needed)

  -- alternative exit path with changes --
Order-editing mode (changes made)
  -> user selects "Cancel"
Discard confirmation modal
  -> user selects "Keep editing"
Order-editing mode resumes (changes intact)

  -> user selects "Discard"
Normal logging mode in original order (orderBeforeEditing restored)
```

### Workout Exercise

Represents an exercise row within the current workout.

**Existing fields used by this feature**:

| Field | Type | Notes |
|---|---|---|
| `exerciseId` | string | Stable key used to preserve weight/effort data during reorder |
| `name` | string | The only visible field in order-editing mode |
| `targetReps` | string or null | Hidden in order-editing mode |
| `targetWeight` | string or null | Hidden in order-editing mode |

**Validation rules**:

- Only exercises already present in the loaded current workout may appear in the order-editing list.
- Reorder operations must not add, remove, or duplicate exercises.
- Exercise names are rendered as text content.

### Log Entry

Existing client-side state for unsaved per-exercise workout-entry data.

**Existing fields used by this feature**:

| Field | Type | Notes |
|---|---|---|
| map key | string | `exerciseId`; preserves association across reorder |
| `loggedWeight` | string | Hidden while editing order, restored after exit |
| `loggedEffort` | number or null | Hidden while editing order, restored after exit |

**Validation rules**:

- Reordering exercises must not change log-entry values.
- When the normal view is restored, each input must show or maintain the value associated with the same `exerciseId`.

### Exercise Order

Client-side sequence represented by the order of `CurrentWorkout.exercises`.

**Fields**:

| Field | Type | Notes |
|---|---|---|
| position | number | 0-based array index |
| exerciseId | string | Exercise at that position |

**State transitions**:

```text
Normal logging mode
  -> user selects "Edit order"
Order-editing mode with collapsed rows
  -> drag/touch/keyboard reorder
Order-editing mode with updated array order
  -> user selects "Done"
Normal logging mode in updated order
  -> user saves workout
Session POST logs sequence values from updated order

  -- or Cancel with changes -> discard confirmed --
Normal logging mode in original order
```

## Persistence

- No planned-workout template order is changed.
- No new storage is added.
- When the session is saved, the existing `POST /api/workouts/{workoutId}/sessions` request includes `sequence` per exercise based on the current display order, as already implemented in `active-session.ts`.
