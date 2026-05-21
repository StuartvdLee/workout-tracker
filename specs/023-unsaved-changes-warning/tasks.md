# Tasks: Unsaved Changes Warning in Edit Modals

**Input**: Design documents from `/specs/023-unsaved-changes-warning/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅

**Tests**: Automated E2E tests are REQUIRED for every user story per the project constitution.
Tests must be written **before** or **alongside** the implementation of each entity's modal.

**Organization**: Tasks are grouped by entity (muscles → exercises → workouts) to enable
independent delivery and testing. Each entity phase covers both US1 (warning when changes
exist) and US2 (no warning when clean) since they share the same `hasEditChanges()` logic.
US3 (consistency across all entities) is verified in the final polish phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = warning when changes exist, US2 = no warning when no changes, US3 = consistency across entities

---

## Phase 1: Setup & Baseline

**Purpose**: Confirm the existing test suite is green before any changes are made.

- [ ] T001 Run `cd src/WorkoutTracker.Web && npm run build` to verify TypeScript compiles cleanly; confirm all E2E tests pass as the regression baseline before any edits

---

## Phase 2: Muscle Edit Modal — Warning & No-Warning (US1 + US2) 🎯 MVP

**Goal**: Implement and test the discard warning for the Muscle edit modal. This is the
simplest entity (one text field) and establishes the implementation pattern for the other two.

**Independent Test**: Open the Edit Muscle modal, type a new name, click ×. A "Discard changes?"
confirmation must appear. Click "Discard" → modal closes, original name persists in grid.
Then open it again without changing anything and click × → modal closes immediately with no warning.

### E2E Tests for Muscle Edit Modal

- [ ] T002 [P] [US1] Add test `EditMuscle_DiscardWarning_ShownWhenChangesExist` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — fill `#edit-muscle-name` with a new value, click `#edit-modal-close`, assert `#muscle-edit-discard-backdrop` is visible
- [ ] T003 [P] [US1] Add test `EditMuscle_DiscardWarning_DiscardConfirmed_ModalCloses` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — fill new name, click ×, click `#muscle-edit-discard-confirm`, assert `#edit-modal-backdrop` is hidden and original name still appears in `#muscle-grid`
- [ ] T004 [P] [US1] Add test `EditMuscle_DiscardWarning_KeepEditing_ModalStaysOpen` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — fill new name, click ×, click `#muscle-edit-discard-cancel`, assert `#edit-modal-backdrop` is visible and `#edit-muscle-name` still contains the changed value
- [ ] T005 [P] [US2] Add test `EditMuscle_NoWarning_WhenNoChanges` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — open modal, click `#edit-modal-close` without changing the name, assert `#edit-modal-backdrop` is immediately hidden and `#muscle-edit-discard-backdrop` is never visible
- [ ] T031 [P] [US1] Add test `EditMuscle_DiscardWarning_ShownOnEscape` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — fill `#edit-muscle-name` with a new value, press Escape, assert `#muscle-edit-discard-backdrop` is visible and `#edit-modal-backdrop` is still visible (Escape triggers warning, does not close edit modal directly)
- [ ] T032 [P] [US1] Add test `EditMuscle_DiscardWarning_ShownOnBackdropClick` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — fill `#edit-muscle-name` with a new value, click the `#edit-modal-backdrop` edge (`Position { X=5, Y=5 }`), assert `#muscle-edit-discard-backdrop` is visible and `#edit-modal-backdrop` is still visible

### Implementation for Muscle Edit Modal

- [ ] T006 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts` — add `let originalEditName: string = ""` module-level variable; in `openEditModal()` capture `originalEditName = muscle.name` after setting `input.value`; add `function hasEditChanges(): boolean` that returns `(document.getElementById("edit-muscle-name") as HTMLInputElement).value.trim() !== originalEditName`
- [ ] T007 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts` — add discard-confirm modal HTML after the `edit-modal-backdrop` closing `</div>` in `render()`: `<div class="discard-modal-backdrop" id="muscle-edit-discard-backdrop" style="display:none;"><div class="discard-modal" role="alertdialog" aria-modal="true" aria-labelledby="muscle-edit-discard-title" aria-describedby="muscle-edit-discard-desc">` with title "Discard changes?", desc "You have unsaved changes. Are you sure you want to discard them?", and buttons `id="muscle-edit-discard-confirm"` (class `discard-modal__discard`, text "Discard") and `id="muscle-edit-discard-cancel"` (class `discard-modal__continue`, text "Keep editing")
- [ ] T008 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts` — add `function openEditDiscardModal()` (show `#muscle-edit-discard-backdrop`, focus `#muscle-edit-discard-confirm`); add `function closeEditDiscardModal()` (hide `#muscle-edit-discard-backdrop`, return focus to `#edit-muscle-name`); add `function requestCloseEditModal()` (return if `isEditSubmitting`; call `openEditDiscardModal()` if `hasEditChanges()`, else call `closeEditModal()`)
- [ ] T009 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts` — wire discard modal in `initEventListeners()`: Discard button click → `closeEditModal()`; Keep editing click → `closeEditDiscardModal()`; discard backdrop keydown: Escape → `closeEditDiscardModal()`, Tab → trap focus between the two buttons; update X button handler and Escape/backdrop handlers to call `requestCloseEditModal()` instead of `closeEditModal()` directly

### Update Affected Existing Test

- [ ] T010 [US1] Update `EditMuscle_CloseButton_ClosesModalWithoutSaving` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — test fills a new name then clicks X, so it will now see the discard warning; add click on `#muscle-edit-discard-confirm` to dismiss the warning and confirm the test still verifies original data is preserved
- [ ] T033 [US1] Update `EditMuscle_EscapeDiscardChanges` in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` — this test fills "Upper Chest" then presses Escape and currently expects `#edit-modal-backdrop` to be hidden immediately; after implementation, Escape triggers the discard warning instead of a direct close; update to assert `#muscle-edit-discard-backdrop` is visible, then click `#muscle-edit-discard-confirm`, then assert `#edit-modal-backdrop` is hidden and the original name "Chest" is still in the grid

**Checkpoint**: Muscle edit modal fully functional — warning shown when changes exist, immediate close when no changes; all muscle E2E tests pass.

---

## Phase 3: Exercise Edit Modal — Warning & No-Warning (US1 + US2)

**Goal**: Apply the same discard-warning pattern to the Exercise edit modal. Exercises have
a text field plus a muscle-toggle Set, so change detection covers both inputs.

**Independent Test**: Open the Edit Exercise modal, change the name, click ×. Discard warning
appears. Click "Keep editing" → modal stays open, changed name preserved. Then open again,
toggle a muscle on/off, click × → warning appears (muscle selection counts as a change).

### E2E Tests for Exercise Edit Modal

- [ ] T011 [P] [US1] Add test `EditExercise_DiscardWarning_ShownWhenNameChanged` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — fill `#edit-exercise-name` with new value, click `#edit-modal-close`, assert `#exercise-edit-discard-backdrop` is visible
- [ ] T012 [P] [US1] Add test `EditExercise_DiscardWarning_DiscardConfirmed_ModalCloses` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — fill new name, click ×, click `#exercise-edit-discard-confirm`, assert `#edit-modal-backdrop` hidden and original name in exercise list
- [ ] T013 [P] [US1] Add test `EditExercise_DiscardWarning_KeepEditing_ModalStaysOpen` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — fill new name, click ×, click `#exercise-edit-discard-cancel`, assert edit modal visible and changed value still in `#edit-exercise-name`
- [ ] T014 [P] [US2] Add test `EditExercise_NoWarning_WhenNoChanges` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — open modal without changing anything, click `#edit-modal-close`, assert `#edit-modal-backdrop` immediately hidden and `#exercise-edit-discard-backdrop` never shown
- [ ] T034 [P] [US1] Add test `EditExercise_DiscardWarning_ShownOnEscape` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — fill `#edit-exercise-name` with a new value, press Escape, assert `#exercise-edit-discard-backdrop` is visible and `#edit-modal-backdrop` is still visible
- [ ] T035 [P] [US1] Add test `EditExercise_DiscardWarning_ShownOnBackdropClick` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — fill `#edit-exercise-name` with a new value, click the `#edit-modal-backdrop` edge (`Position { X=5, Y=5 }`), assert `#exercise-edit-discard-backdrop` is visible and `#edit-modal-backdrop` is still visible

### Implementation for Exercise Edit Modal

- [ ] T015 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` — add `let originalEditName: string = ""` and `let originalEditMuscleIds: ReadonlySet<string> = new Set()` module-level variables; in `openEditModal()` after setting `selectedEditMuscleIds`, add `originalEditName = exercise.name` and `originalEditMuscleIds = new Set(selectedEditMuscleIds)` (separate copy, not alias); add `function hasEditChanges(): boolean` returning `true` if name input differs from `originalEditName` OR if `selectedEditMuscleIds` differs from `originalEditMuscleIds` in size or member content (`selectedEditMuscleIds.size !== originalEditMuscleIds.size || ![...selectedEditMuscleIds].every(id => originalEditMuscleIds.has(id))`)
- [ ] T016 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` — add discard-confirm modal HTML after the `edit-modal-backdrop` closing `</div>` in `render()`, using `id="exercise-edit-discard-backdrop"`, `id="exercise-edit-discard-title"`, `id="exercise-edit-discard-desc"`, `id="exercise-edit-discard-confirm"`, `id="exercise-edit-discard-cancel"` following the same `.discard-modal-backdrop` / `.discard-modal` structure as muscles
- [ ] T017 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` — add `openEditDiscardModal()` (show backdrop, focus confirm button), `closeEditDiscardModal()` (hide backdrop, return focus to `#edit-exercise-name`), and `requestCloseEditModal()` (guard `isEditSubmitting`, show discard or force-close)
- [ ] T018 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` — wire discard modal in `initEventListeners()`: Discard → `closeEditModal()`, Keep editing → `closeEditDiscardModal()`, Escape/Tab trap in discard backdrop; update Cancel button (`#edit-modal-cancel`), X button (`#edit-modal-close`), backdrop click, and Escape keydown to call `requestCloseEditModal()`

### Update Affected Existing Test

- [ ] T019 [US1] Update `EditExercise_CloseButton_ClosesModalWithoutSaving` in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — test fills a new name then clicks ×; add click on `#exercise-edit-discard-confirm` to confirm discard, then assert original data is preserved

**Checkpoint**: Exercise edit modal fully functional; all exercise E2E tests pass.

---

## Phase 4: Workout Edit Modal — Warning & No-Warning (US1 + US2)

**Goal**: Apply the discard-warning pattern to the Workout edit modal. Workouts populate
asynchronously (name + exercise list are loaded via fetch), so originals are captured after
the load completes. Also includes the pre-existing stale-response guard fix.

**Independent Test**: Open the Edit Workout modal (wait for async load), change the name,
click ×. Warning appears. Confirm discard → modal closes, original name in list. Open again,
add or remove an exercise → warning appears (exercise list change counts).

### E2E Tests for Workout Edit Modal

- [ ] T020 [P] [US1] Add test `EditWorkout_DiscardWarning_ShownWhenNameChanged` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — wait for modal to load, fill `#edit-workout-name` with new value, click `#workout-edit-close`, assert `#workout-edit-discard-backdrop` is visible
- [ ] T021 [P] [US1] Add test `EditWorkout_DiscardWarning_DiscardConfirmed_ModalCloses` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — change name, click ×, click `#workout-edit-discard-confirm`, assert `#workout-edit-backdrop` hidden and original name in workout list
- [ ] T022 [P] [US1] Add test `EditWorkout_DiscardWarning_KeepEditing_ModalStaysOpen` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — change name, click ×, click `#workout-edit-discard-cancel`, assert `#workout-edit-backdrop` visible and changed value still in `#edit-workout-name`
- [ ] T023 [P] [US2] Add test `EditWorkout_NoWarning_WhenNoChanges` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — open modal, wait for async load (name + exercises populated), click `#workout-edit-cancel` without any changes, assert `#workout-edit-backdrop` immediately hidden and `#workout-edit-discard-backdrop` never shown
- [ ] T036 [P] [US1] Add test `EditWorkout_DiscardWarning_ShownOnEscape` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — wait for async modal load, fill `#edit-workout-name` with a new value, press Escape, assert `#workout-edit-discard-backdrop` is visible and `#workout-edit-backdrop` is still visible
- [ ] T037 [P] [US1] Add test `EditWorkout_DiscardWarning_ShownOnBackdropClick` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — wait for async modal load, fill `#edit-workout-name` with a new value, click the `#workout-edit-backdrop` edge (`Position { X=5, Y=5 }`), assert `#workout-edit-discard-backdrop` is visible and `#workout-edit-backdrop` is still visible

### Implementation for Workout Edit Modal

- [ ] T024 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — add `let originalEditName: string = ""` and `let originalEditExerciseIds: string[] = []` module-level variables; in `fetchAndPopulateEditModal()`, immediately after `nameInput.value = fullWorkout.name` and `editSelectedExercises = fullWorkout.exercises.map(...)`, add the stale-response guard `if (editingWorkoutId !== workoutId) return;` (move the guard before assigning `nameInput.value`), then capture `originalEditName = fullWorkout.name` and `originalEditExerciseIds = [...fullWorkout.exercises.map(ex => ex.exerciseId)]`; add `function hasEditChanges(): boolean` returning `true` if name differs OR `JSON.stringify(editSelectedExercises) !== JSON.stringify(originalEditExerciseIds)`
- [ ] T025 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — add discard-confirm modal HTML after the `workout-edit-backdrop` closing `</div>` in `render()`, using `id="workout-edit-discard-backdrop"`, `id="workout-edit-discard-title"`, `id="workout-edit-discard-desc"`, `id="workout-edit-discard-confirm"`, `id="workout-edit-discard-cancel"` following the `.discard-modal-backdrop` / `.discard-modal` structure
- [ ] T026 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — add `openEditDiscardModal()` (show backdrop, focus confirm button), `closeEditDiscardModal()` (hide backdrop, return focus to `#edit-workout-name`), and `requestCloseEditModal()` (guard `isEditSubmitting`, show discard or force-close)
- [ ] T027 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — wire discard modal in event handler initialization: Discard → `closeEditModal()`, Keep editing → `closeEditDiscardModal()`, Escape/Tab trap in discard backdrop; update Cancel button (`#workout-edit-cancel`), X button (`#workout-edit-close`), backdrop click, and Escape keydown to call `requestCloseEditModal()`

### Update Affected Existing Tests

- [ ] T028 [US1] Update `EditWorkout_CloseButton_ClosesModalWithoutSaving` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` — test fills `#edit-workout-name` with "Changed Name" then clicks ×; add click on `#workout-edit-discard-confirm` to go through the warning, then assert original name still in the workout list (tests the full discard flow)

**Checkpoint**: Workout edit modal fully functional; all workout E2E tests pass.

---

## Phase 5: Polish & Cross-Cutting Concerns (US3)

**Purpose**: Verify consistent behavior across all three entity types and confirm the full
build and test suite is green.

- [ ] T029 [P] [US3] Run `cd src/WorkoutTracker.Web && npm run build` — confirm TypeScript strict build passes with no unused variables or implicit returns across all three modified files (`muscles.ts`, `exercises.ts`, `workouts.ts`)
- [ ] T030 [US3] Run the full E2E suite to confirm all three entity modals behave identically: warning appears for all close triggers (Cancel, ×, backdrop, Escape) when changes exist; immediate close when no changes; discard and keep-editing paths work correctly in all three modals

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Muscles)**: Depends on Phase 1
- **Phase 3 (Exercises)**: Can start after Phase 2 is complete (pattern established); or in parallel if two developers
- **Phase 4 (Workouts)**: Can start after Phase 2; independent of Phase 3
- **Phase 5 (Polish)**: Depends on Phases 2, 3, and 4 all complete

### Within Each Phase (Muscles / Exercises / Workouts)

- E2E test tasks (T002–T005, T031–T032, T011–T014, T034–T035, T020–T023, T036–T037): Write first, run to confirm they **fail** (discard modal doesn't exist yet)
- Implementation tasks (T006–T009, T015–T018, T024–T027): Can start in any order within the phase; T007/T016/T025 (HTML) before T008/T017/T026 (functions) before T009/T018/T027 (wiring)
- Update existing tests (T010, T033, T019, T028): After implementation is complete

### Parallel Opportunities

- T002, T003, T004, T005, T031, T032 — all test tasks within Phase 2 can be written in parallel (different test methods in same file)
- T006, T007 within Phase 2 — different concerns (state vs HTML), parallelizable
- T011–T014, T034–T035 within Phase 3 — same as above for exercises
- T020–T023, T036–T037 within Phase 4 — same for workouts
- Phases 3 and 4 are fully independent of each other once Phase 2 is complete

---

## Parallel Example: Phase 2 (Muscle Edit Modal)

```
# Write all new E2E tests in parallel (same file, different methods):
Task T002: "Add EditMuscle_DiscardWarning_ShownWhenChangesExist test"
Task T003: "Add EditMuscle_DiscardWarning_DiscardConfirmed_ModalCloses test"
Task T004: "Add EditMuscle_DiscardWarning_KeepEditing_ModalStaysOpen test"
Task T005: "Add EditMuscle_NoWarning_WhenNoChanges test"
Task T031: "Add EditMuscle_DiscardWarning_ShownOnEscape test"
Task T032: "Add EditMuscle_DiscardWarning_ShownOnBackdropClick test"

# Run tests → confirm all 6 fail (discard modal HTML not yet present)

# Then implement in sequence:
Task T006: "Add state variable + hasEditChanges()"
Task T007: "Add discard modal HTML to render()"
Task T008: "Add open/close/request functions"
Task T009: "Wire event handlers"

# Run tests → all 6 should now pass
# Update existing tests:
Task T010: "Update EditMuscle_CloseButton_ClosesModalWithoutSaving"
Task T033: "Update EditMuscle_EscapeDiscardChanges (will break without this fix)"
```

---

## Implementation Strategy

### MVP First (Phase 2 Only — One Entity)

1. Complete Phase 1: Baseline verification
2. Complete Phase 2: Muscle edit modal (US1 + US2 for one entity)
3. **STOP and VALIDATE**: Muscle E2E tests all green, TypeScript build clean
4. Demo: unsaved changes warning works end-to-end on the Muscle modal

### Incremental Delivery

1. Phase 1 + Phase 2 → Muscle modal warning (MVP ✅)
2. Phase 3 → Exercise modal warning (US3 partially met)
3. Phase 4 → Workout modal warning (US3 fully met)
4. Phase 5 → Full consistency verification

### Notes

- The `requestCloseEditModal()` wrapper keeps `closeEditModal()` as a force-close function — never put the `hasEditChanges()` check inside `closeEditModal()` itself
- `originalEditMuscleIds` must be `new Set(selectedEditMuscleIds)` (copy), not an alias
- The stale-response guard (`if (editingWorkoutId !== workoutId) return`) in `fetchAndPopulateEditModal()` must be placed **before** any DOM writes or state assignments
- Discard modal backdrop must appear **after** the edit-modal backdrop in the DOM so it layers on top (both share z-index 200)
- Focus must return to the edit modal's primary text input when "Keep editing" is clicked or Escape is pressed in the discard modal
