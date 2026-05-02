# Data Model: Reorder Exercises in a Workout

**Feature**: `006-reorder-exercises`  
**Date**: 2026-05-02

## Summary of Changes

**No database schema changes are required for this feature.**

The `Sequence` field already exists on the `planned_workout_exercise` table, is already populated by the API, and is already used to order exercises in all GET responses. This feature adds the UI affordance for users to control that sequence.

---

## Existing Entity: `PlannedWorkoutExercise`

The following table shows the current state of the entity, which is unchanged by this feature.

| Column                    | Type          | Nullable | Notes                                         |
|---------------------------|---------------|----------|-----------------------------------------------|
| planned_workout_exercise_id | uuid        | NOT NULL | Primary key                                   |
| planned_workout_id        | uuid          | NOT NULL | FK → planned_workout                          |
| exercise_id               | uuid          | NOT NULL | FK → exercise                                 |
| **sequence**              | **integer**   | **NOT NULL** | **Position of this exercise in the workout (1-based)** |
| target_reps               | varchar       | NULL     | Optional target rep range (e.g., "8-12")      |
| target_weight             | varchar       | NULL     | Optional target weight (e.g., "60kg")         |

### How `sequence` Is Already Managed

**On create** (`POST /api/workouts`): `Sequence = i + 1` for each exercise at array index `i` in the request body.  
**On update** (`PUT /api/workouts/{id}`): The existing `PlannedWorkoutExercise` rows are deleted and recreated. `Sequence = i + 1` for each exercise at array index `i` in the request body.  
**On retrieve**: All endpoints return exercises `OrderBy(e => e.Sequence)`.

The implication is that **the order of the `exercises[]` array in the request body determines the sequence**. Sending exercises in the user's desired order is all that is required.

---

## Application-Level Change: `workouts.ts` State Model

The only change is in the TypeScript module `workouts.ts` in the frontend.

### Current State

```
selectedExercises: Set<string>       // create form — preserves insertion order only
editSelectedExercises: Set<string>   // edit modal — preserves insertion order only
```

`Set<string>` provides no API for arbitrary reordering. Users can add and remove exercises but cannot change the order after adding.

### New State

```
selectedExercises: string[]          // create form — ordered array; supports arbitrary reorder
editSelectedExercises: string[]      // edit modal — ordered array; supports arbitrary reorder
```

Each array is the authoritative ordered list of exercise IDs. When the workout is submitted to the API, the array index determines the `Sequence` value assigned by the backend.

### Invariants Maintained by Guard Functions

| Operation | Guard |
|-----------|-------|
| Add | `if (!arr.includes(id)) arr.push(id)` — prevents duplicates |
| Remove | `arr = arr.filter(id => id !== removeId)` — removes by value |
| Reorder | `arr.splice(toIndex, 0, arr.splice(fromIndex, 1)[0])` — moves element in-place |

These replace the automatic deduplication that `Set` provided.

---

## No Other Entities Affected

All other entities (`PlannedWorkout`, `WorkoutSession`, `LoggedExercise`, `Exercise`, `Muscle`, `ExerciseMuscle`) are unaffected by this feature.
