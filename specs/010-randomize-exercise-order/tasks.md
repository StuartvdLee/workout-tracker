# Tasks: Randomize Workout Exercise Order

**Input**: Design documents from `/specs/010-randomize-exercise-order/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Automated tests are REQUIRED. Frontend Vitest unit tests cover `shuffle<T>()`. Backend xUnit integration tests cover `POST /api/workouts/{id}/sessions` with `Sequence` values. All existing tests must continue to pass.

**Organization**: Tasks are grouped by user story. US1 (shuffle + start) is the MVP. US2 (preview + re-shuffle) builds on US1's modal infrastructure. US3 (template immutability) is verified by the existing data path and a regression test.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the one foundational infrastructure piece (the `shuffle<T>()` utility and its tests) before any modal or session work begins. This is the only shared prerequisite.

- [X] T001 [P] Add `shuffle<T>()` pure Fisher-Yates function to `src/WorkoutTracker.Web/wwwroot/ts/utils.ts`
- [X] T002 [P] Add Vitest unit tests for `shuffle<T>()` in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts` — covers: empty array, length-1, length-2, length-n (all original elements present, no duplicates), generic type preservation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema change and backend DTO extension that both US1 and US3 depend on. Must complete before session save tests can be written.

**⚠️ CRITICAL**: US1 session save integration tests and US3 regression test cannot be verified until this phase is complete.

- [X] T003 Add `int? Sequence` property to `src/WorkoutTracker.Infrastructure/Data/Models/LoggedExercise.cs`
- [X] T004 Add `int? Sequence` field to `SessionLoggedExerciseItem` DTO in `src/WorkoutTracker.Api/Program.cs` and map it to `LoggedExercise.Sequence` in the session creation handler
- [X] T005 Generate EF Core migration `AddSequenceToLoggedExercise` adding nullable `int` column `sequence` to `logged_exercise` table (`--project src/WorkoutTracker.Infrastructure --startup-project src/WorkoutTracker.Api`)

**Checkpoint**: Foundation ready — session save now accepts and stores `sequence`; user story work can begin

---

## Phase 3: User Story 1 — Shuffle Exercises Before Starting (Priority: P1) 🎯 MVP

**Goal**: User can click "Start" on a workout, see a pre-start modal, enable the "Randomise order" toggle, and begin a session with exercises presented in a shuffled order. Non-shuffled start continues to work exactly as before.

**Independent Test**: Navigate to a workout with 3+ exercises → click "Start" → modal appears → enable "Randomise order" → click "Start Workout" → active session shows exercises in a different order than the template. Also verify: clicking "Start" without toggling still works as before.

### Tests for User Story 1

- [X] T006 [P] Add backend integration tests to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` for `POST /api/workouts/{workoutId}/sessions` with `Sequence`: (a) session submitted with explicit non-template `sequence` values returns 201 and stores the submitted values; (b) session submitted with `sequence: null` for all items returns 201 (backward-compat path)

### Implementation for User Story 1

- [X] T007 [US1] Add pre-start modal HTML to the `render()` function in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — `prestart-modal-backdrop` containing `prestart-modal` with title, shuffle toggle (`role="switch"`, `aria-checked="false"`), exercise `<ol>` list, and action buttons ("Start Workout", "Cancel") per `contracts/ui-contract.md`
- [X] T008 [US1] Implement `openPreStartModal(workout)`, `closePreStartModal()`, and `renderExercisePreview(exercises)` functions in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — modal opens on "Start" click, renders exercise list in template order, hides shuffle row when `exercises.length < 2`, focus moves to "Start Workout" button on open, focus returns to triggering "Start" button on close
- [X] T009 [US1] Implement `handleShuffleToggle()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — toggles `aria-checked` on the switch button, calls `shuffle()` from `utils.ts` and re-renders the exercise list when turned on, restores template order when turned off, shows/hides the "Re-shuffle" button accordingly
- [X] T010 [US1] Implement `handleConfirmStart(workoutId, currentOrder, isShuffled)` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — navigates to `/active-session?id=<workoutId>` when shuffle is off; navigates to `/active-session?id=<workoutId>&order=<guid1>,<guid2>,...` when shuffle is on
- [X] T011 [US1] Read `?order=` URL parameter in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — parse comma-separated exercise IDs; implement `applyOrder(exercises, order)` helper that reorders the API-returned `workout.exercises` array in memory, gracefully falling back to template order for any unrecognised IDs or if the parameter is absent/malformed
- [X] T012 [US1] Update `handleSave()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — populate `sequence: index` (0-based display position) for each exercise entry in the `LoggedExercises` array sent to `POST /api/workouts/{workoutId}/sessions`
- [X] T013 [US1] Add `prestart-modal-backdrop`, `prestart-modal`, and all `prestart-modal__*` BEM styles to `src/WorkoutTracker.Web/wwwroot/css/styles.css` — modal card, overlay, shuffle row, toggle switch visual states (`aria-checked` true/false), exercise list items, action button row; use existing CSS custom properties (`--color-surface`, `--color-primary`, `--color-text`, `--color-text-muted`, `--color-border`)
- [X] T014 [US1] Add focus trap and Escape-key handler to the pre-start modal in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — follows the exact `Tab`/`Shift-Tab` trap pattern used in `initDiscardModal()` in `active-session.ts`
- [X] T015 [US1] Verify the "Randomise order" toggle row is hidden when a workout has exactly 1 exercise (no visual change for single-exercise workouts) in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [X] T027 [US1] Guard the pre-start modal against an empty exercises array in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — if `workout.exercises` is empty, disable the "Start Workout" button and display a "No exercises configured" message; completes UX-002 empty-state coverage (loading and error states are inherited from the workouts page — the "Start" button is only reachable after exercises have loaded into module state)

**Checkpoint**: User Story 1 fully functional — "Start" opens pre-start modal, shuffle can be enabled, session starts with shuffled order, session save stores `sequence`

---

## Phase 4: User Story 2 — Preview Shuffled Order Before Committing (Priority: P2)

**Goal**: When shuffle is enabled, the user sees the full exercise list in the shuffled order inside the pre-start modal before clicking "Start Workout". A "Re-shuffle" button lets them generate a new random order. The session starts with exactly the order shown.

**Independent Test**: Enable shuffle in pre-start modal → exercise list shows shuffled order → click "Re-shuffle" → list updates to a new order → click "Start Workout" → active session exercises match the last previewed order exactly.

### Tests for User Story 2

- [X] T016 [P] [US2] Add Vitest unit tests to `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts` for `applyOrder()` helper (if extracted to `utils.ts`): correctly orders exercises by ID list, unknown IDs are ignored, exercises not in the order list are appended at end

### Implementation for User Story 2

- [X] T017 [US2] Implement `handleReshuffle()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — calls `shuffle()` again and calls `renderExercisePreview()` with the new order, updating the module-level `currentOrder` state used by `handleConfirmStart()`
- [X] T018 [US2] Wire the "Re-shuffle" button click to `handleReshuffle()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — button is shown only when shuffle toggle is on (already handled by T009); clicking it re-renders the list and updates the pending order
- [X] T019 [US2] Verify in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` that `applyOrder()` maps the `?order=` IDs to the API exercises exactly, so the session display matches the modal preview order — no discrepancy between what was shown and what is performed

**Checkpoint**: User Story 2 fully functional — shuffled preview visible in modal, re-shuffle generates new order, session matches preview exactly

---

## Phase 5: User Story 3 — Session Order Does Not Affect Workout Template (Priority: P3)

**Goal**: Completing or abandoning a shuffled session leaves the `PlannedWorkoutExercise.Sequence` values unchanged. Subsequent starts show the original template order.

**Independent Test**: Complete a shuffled session → open the workout in the edit modal → exercises are in the original template order → start a second session without shuffle → exercises appear in original template order.

### Tests for User Story 3

- [X] T020 [P] [US3] Add backend integration test to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`: after creating a session with shuffled `sequence` values, `GET /api/workouts/{workoutId}` returns exercises in original `PlannedWorkoutExercise.Sequence` order, confirming the template is unmodified

### Implementation for User Story 3

- [X] T021 [US3] Verify by code review that no code path in `workouts.ts`, `active-session.ts`, or `Program.cs` sends a `PUT`/`PATCH` to any workout template endpoint during or after a shuffled session — document confirmation in a code comment at the `handleSave()` call site in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

**Checkpoint**: Template immutability confirmed structurally and by integration test — all user stories complete

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility, UX consistency, and regression verification across all flows

- [ ] T022 [P] Verify all four pre-start modal states render correctly: (1) closed, (2) open shuffle-off, (3) open shuffle-on, (4) single-exercise workout (toggle hidden) — spot-check in browser against `contracts/ui-contract.md`
- [ ] T023 [P] Confirm British English copy throughout: "Randomise order", "Re-shuffle", "Start Workout", "Cancel" — check rendered HTML in `workouts.ts` and any CSS `content` values in `styles.css`
- [ ] T024 [P] Confirm backdrop-click and Escape-key close the pre-start modal and return focus to the originating "Start" button in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [X] T025 Run all existing Vitest tests (`npm test` in `src/WorkoutTracker.Web`) and confirm all pass — regression check for `utils.test.ts`, `router.test.ts`, `theme.test.ts`
- [X] T026 Run all existing xUnit integration tests and confirm all pass — regression check that the `Sequence` column addition and DTO change do not break existing session creation, history, or performance-hint tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T001 and T002 can run in parallel.
- **Foundational (Phase 2)**: Depends on Phase 1 completion. T003, T004, T005 must run sequentially (T003 before T004 before T005).
- **US1 (Phase 3)**: Depends on Foundational completion. T007–T015 depend on T005 (migration). T006 (integration tests) can start after T004.
- **US2 (Phase 4)**: Depends on US1 modal infrastructure (T007, T008, T009) being complete. T017/T018 depend on T009. T016 can run in parallel with US1 implementation.
- **US3 (Phase 5)**: T020 depends on Foundational (T004/T005). T021 depends on US1 save implementation (T012).
- **Polish (Phase 6)**: Depends on all user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 1 + Phase 2 only. No story dependencies. This is the MVP.
- **US2 (P2)**: Depends on US1 modal (T007, T008) and US1 toggle (T009). Specifically T017/T018 require T009's `currentOrder` state to exist.
- **US3 (P3)**: Depends on Phase 2 (for T020 test) and US1 save (T012 for T021 verification). Independently testable.

### Within Each Story

- Implementation before integration (modal before start navigation before active-session reading)
- CSS (T013) can be developed in parallel with TypeScript logic (T007–T012)
- Backend tests (T006) can be written immediately after T004 (DTO extension)

---

## Parallel Opportunities

### Phase 1
```
T001 (utils.ts — shuffle function)
T002 (utils.test.ts — shuffle tests)
← Both touch different files, run in parallel
```

### Phase 3 (US1) — parallel within story
```
T006 (backend integration tests — SessionApiTests.cs)
T013 (CSS — styles.css)
← Both independent of TypeScript modal work; run in parallel with T007–T012
```

### Phase 4 (US2) — parallel setup
```
T016 (Vitest unit tests for applyOrder)
← Can run in parallel with T017/T018/T019 if applyOrder is extracted to utils.ts
```

### Phase 5 (US3) — parallel
```
T020 (backend integration test — template unmodified after shuffle)
← Can run in parallel with T021 (code review) once T004/T005 are done
```

### Phase 6 (Polish) — all parallel
```
T022, T023, T024  ← all independent spot-checks, run in parallel
T025, T026        ← run sequentially (Vitest then xUnit)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: `shuffle<T>()` function + tests
2. Complete Phase 2: Migration + DTO extension
3. Complete Phase 3: Pre-start modal + shuffle + session save with `sequence`
4. **STOP and VALIDATE**: Start a workout, toggle shuffle, confirm session saves correctly
5. Ship: US1 alone is a complete, independently valuable feature

### Incremental Delivery

1. Phase 1 + Phase 2 → Foundation ready
2. Phase 3 (US1) → Pre-start modal with shuffle → **MVP**
3. Phase 4 (US2) → Re-shuffle preview refinement → **Enhanced UX**
4. Phase 5 (US3) → Verified template immutability → **Trust guarantee**
5. Phase 6 → Polish & regression → **Ready to merge**

---

## Task Count Summary

| Phase | Tasks | Notes |
|-------|-------|-------|
| Phase 1: Setup | 2 | `shuffle<T>()` utility + Vitest tests |
| Phase 2: Foundational | 3 | Entity, DTO, migration |
| Phase 3: US1 (MVP) | **11** | Modal, toggle, active-session order, CSS, focus, empty-state guard |
| Phase 4: US2 | **4** | Re-shuffle, preview fidelity |
| Phase 5: US3 | **2** | Template immutability test + verification |
| Phase 6: Polish | **5** | Accessibility, copy, regression |
| **Total** | **27** | |

**Files changed**: 8 existing files modified + 1 new migration file
