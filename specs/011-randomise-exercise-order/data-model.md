# Data Model: Randomise Exercise Order UX Simplification

**Feature**: `011-randomise-exercise-order`  
**Date**: 2026-05-09

---

## Overview

This feature makes no backend or database changes. There are no new entities, no schema changes, and no new API endpoints. All data required for the feature already exists:

- `exerciseCount` is already returned by `GET /api/workouts` and is available in the TypeScript `PlannedWorkout` interface once extended.
- The full `exercises` array is already available in the `workouts[]` module-level array on the Workouts page.
- The `?order=` URL parameter encoding (comma-separated exercise IDs) introduced in feature 010 is unchanged.

---

## TypeScript Interface Changes (frontend only)

### `home.ts` — `PlannedWorkout` interface

**Before (feature 010)**:
```typescript
interface PlannedWorkout {
  readonly plannedWorkoutId: string;
  readonly name: string;
}
```

**After (feature 011)**:
```typescript
interface PlannedWorkout {
  readonly plannedWorkoutId: string;
  readonly name: string;
  readonly exerciseCount: number;
}
```

**Reason**: `exerciseCount` is already returned by `GET /api/workouts` (has been since the API was created). Including it in the interface allows the homepage to show/hide the randomise toggle when the user changes the selected workout, without an additional API call.

### `home.ts` — Module-level state

**Before**: `let loadedWorkoutIds: Set<string>`  
**After**: `let loadedWorkouts: Map<string, PlannedWorkout>`

The `Map` replaces the `Set` — it satisfies the existing ID validation use case (`loadedWorkouts.has(id)`) and additionally provides access to `exerciseCount` for toggle visibility.

The module-level pre-start modal state variables (`prestartWorkout`, `prestartCurrentOrder`, `prestartIsShuffled`) are removed entirely from `home.ts`.

### `workouts.ts` — Pre-start modal state

**Removed**:
- `let prestartIsShuffled: boolean` — no longer needed (Yes/No replaces the toggle state machine)
- `let prestartCurrentOrder: WorkoutExercise[]` — no longer needed (shuffle happens inline on Yes click)

**Retained**:
- `let prestartWorkout: Workout | null` — needed to navigate with the correct workout ID and exercises
- `let prestartTriggerBtn: HTMLButtonElement | null` — needed for focus return on modal close

---

## No backend data model changes

The `LoggedExercise.Sequence` column introduced in feature 010 continues to record exercise display order as before. This feature does not alter how or when `Sequence` is set — the active session page (`active-session.ts`) remains unchanged.
