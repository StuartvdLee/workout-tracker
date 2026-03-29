# Data Model: Add Exercises

**Feature**: 003-add-exercises
**Date**: 2026-07-15

## Overview

This feature extends the existing PostgreSQL schema with a Muscle entity (predefined seed data), an ExerciseMuscle junction table, and adds constraints to the existing Exercise entity. The frontend manages transient form state for create/edit mode toggling.

## Entities

### Exercise (modified)

Represents a single exercise in the user's library.

| Attribute  | Type         | Constraints                          | Description                        |
| ---------- | ------------ | ------------------------------------ | ---------------------------------- |
| ExerciseId | Guid (PK)    | Auto-generated                       | Unique identifier                  |
| Name       | string       | Required, max 150 chars, unique (CI) | Display name of the exercise       |

**Changes from existing schema**:
- Add `MaxLength(150)` constraint (currently unbounded `text`)
- Add case-insensitive unique index on `LOWER(name)`
- Add navigation property: `ICollection<ExerciseMuscle> ExerciseMuscles`

**Validation rules**:
- Name must not be empty or whitespace-only after trimming
- Name must be ≤ 150 characters after trimming
- Name must be unique (case-insensitive) across all exercises; on edit, exclude the exercise being updated from the uniqueness check

### Muscle (new)

Represents a predefined body muscle that can be targeted by exercises.

| Attribute | Type       | Constraints    | Description               |
| --------- | ---------- | -------------- | ------------------------- |
| MuscleId  | Guid (PK)  | Auto-generated | Unique identifier         |
| Name      | string     | Required       | Display name of the muscle |

**Predefined values** (seeded via `HasData` in `OnModelCreating`):

| Name       |
| ---------- |
| Chest      |
| Back       |
| Shoulders  |
| Biceps     |
| Triceps    |
| Forearms   |
| Core       |
| Quads      |
| Hamstrings |
| Glutes     |
| Calves     |

**Notes**: Muscles are read-only from the application's perspective. No create/update/delete API is exposed. The seed data is applied via EF Core migration.

### ExerciseMuscle (new)

Junction table representing the many-to-many relationship between exercises and muscles.

| Attribute        | Type      | Constraints                  | Description                        |
| ---------------- | --------- | ---------------------------- | ---------------------------------- |
| ExerciseMuscleId | Guid (PK) | Auto-generated               | Unique identifier for the junction |
| ExerciseId       | Guid (FK) | References Exercise, cascade | The exercise                       |
| MuscleId         | Guid (FK) | References Muscle, cascade   | The targeted muscle                |

**Constraints**:
- Composite uniqueness: (ExerciseId, MuscleId) — an exercise cannot target the same muscle twice
- Cascade delete on both foreign keys: deleting an exercise removes its muscle associations; deleting a muscle (unlikely since predefined) removes associated exercise links

## Relationships

```text
Exercise (1) ──── (*) ExerciseMuscle (*) ──── (1) Muscle
    │                                              │
    └── An exercise targets zero or more muscles   └── A muscle is targeted by zero or more exercises
```

- Exercise → ExerciseMuscle: One-to-many (an exercise has 0..N muscle associations)
- Muscle → ExerciseMuscle: One-to-many (a muscle is associated with 0..N exercises)
- Exercise ↔ Muscle: Logical many-to-many (through ExerciseMuscle junction)

## Existing Entities (unchanged)

- **WorkoutType**: Workout categories (Push, Pull, Legs)
- **Workout**: User workout sessions
- **WorkoutExercise**: Junction linking workouts to exercises (existing, unchanged)

## Client-Side State

### Form State

| Attribute         | Type              | Description                                         |
| ----------------- | ----------------- | --------------------------------------------------- |
| editingExerciseId | string \| null    | Null = create mode; GUID = edit mode for that exercise |
| selectedMuscleIds | Set\<string\>     | Currently toggled muscle IDs in the form             |
| isSubmitting      | boolean           | True while an API save/update is in progress         |

### Page State

| Attribute  | Type        | Description                                     |
| ---------- | ----------- | ----------------------------------------------- |
| exercises  | Exercise[]  | Loaded exercise list with muscle associations    |
| muscles    | Muscle[]    | Predefined muscle list loaded from API           |

### Form State Transitions

```text
[Create Mode] ──(click edit on exercise)──> [Edit Mode: form populated]
[Edit Mode]   ──(click cancel)────────────> [Create Mode: form cleared]
[Edit Mode]   ──(click edit on ANOTHER)───> [Edit Mode: form re-populated with new exercise]
[Edit Mode]   ──(submit success)──────────> [Create Mode: form cleared, list updated]
[Create Mode] ──(submit success)──────────> [Create Mode: form cleared, list updated]

[Any Mode]    ──(submit)──────────────────> [Submitting: button disabled]
[Submitting]  ──(API success)─────────────> [Previous mode cleared]
[Submitting]  ──(API error)───────────────> [Previous mode preserved with error]
```

## Migration Plan

A single EF Core migration adds all schema changes:

1. Create `muscles` table with seed data for 11 predefined muscles
2. Create `exercise_muscles` junction table with foreign keys and composite unique index
3. Add `varchar(150)` max length to `exercises.name` column
4. Add case-insensitive unique index on `exercises.name` using `LOWER(name)` expression
