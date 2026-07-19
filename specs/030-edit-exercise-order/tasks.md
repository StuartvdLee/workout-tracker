# Tasks: Edit Exercise Order in Current Workout

**Input**: Design documents from `/specs/030-edit-exercise-order/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Automated tests are required by the project constitution. E2E tests prove the current-workout DOM journey; existing Vitest coverage continues to prove the shared `reorder<T>()` utility.

**Organization**: Tasks are grouped by user story so each story is independently testable after its phase. US2 depends on the order-editing mode from US1 because its starting condition is "order-editing mode is active".

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label for story-specific tasks
- All tasks include exact repository paths

---

## Phase 1: Setup (Shared Understanding)

**Purpose**: Confirm existing implementation points from previous specs before refactoring.

- [X] T001 [P] Review active-session workout ordering, `logEntries`, and session `sequence` save behavior in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T002 [P] Review existing create/edit workout drag, touch, keyboard, and announcement behavior in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [X] T003 [P] Review existing sortable-row CSS and visually hidden utility styles in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [X] T004 [P] Review existing reorder utility tests and keep `reorder<T>()` behavior unchanged in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extract the existing "Edit Workout" sortable-list behavior so current workouts can reuse the same implementation.

**⚠️ CRITICAL**: No current-workout story implementation should start until this shared helper is complete.

- [X] T005 Create shared sortable list module with typed options and public initializer in `src/WorkoutTracker.Web/wwwroot/ts/sortable-list.ts`
- [X] T006 Move HTML5 drag/drop, touch drag, keyboard reorder, live announcement, and cancel behavior from `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` into `src/WorkoutTracker.Web/wwwroot/ts/sortable-list.ts`
- [X] T007 Update create-workout and edit-workout selected exercise lists to call the shared initializer in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [X] T008 Preserve existing create/edit workout reorder DOM structure and CSS class compatibility in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [X] T009 [P] Update or extend existing create/edit reorder regression assertions only if helper extraction changes selectors in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`

**Checkpoint**: Existing workout create/edit reorder behavior still uses the same visible interaction and remains ready for regression validation.

---

## Phase 3: User Story 1 - Enter order editing mode from current workout (Priority: P1) 🎯 MVP

**Goal**: Show a top-right "Edit order" action on the current workout and switch into a collapsed name-only order-editing mode.

**Independent Test**: Open a current workout, select "Edit order", and verify the screen stays on the current workout while exercise rows show names only and hide weight/effort controls.

### Tests for User Story 1 ⚠️

> **NOTE: Write this test FIRST and ensure it fails before implementation.**

- [X] T010 [US1] Add E2E coverage for the active-session "Edit order" button and collapsed name-only mode in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`

### Implementation for User Story 1

- [X] T011 [US1] Add `isOrderEditing` state, show an `#session-edit-order` header button in normal mode, hide it while editing, and relabel the footer `#session-save` button to "Done" while order-editing mode is active in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T012 [US1] Implement order-editing render path that replaces normal exercise cards with `#session-order-list` name-only rows in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T013 [US1] Hide weight inputs, effort sliders, targets, and previous-performance details while order-editing mode is active in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T014 [US1] Add active-session header action, order list, and order row styles that reuse existing sortable classes in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [X] T015 [US1] Handle zero-exercise and single-exercise current workout states without rendering broken drag affordances in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

**Checkpoint**: User Story 1 is fully functional and testable independently.

---

## Phase 4: User Story 2 - Reorder current workout exercises (Priority: P1)

**Goal**: Let users drag, touch-drag, or keyboard-move collapsed current-workout exercises into a new order using the same behavior as "Edit Workout".

**Independent Test**: Enter order-editing mode, move an exercise to a different position, and verify the visible collapsed order changes immediately.

### Tests for User Story 2 ⚠️

> **NOTE: Write these tests FIRST and ensure they fail before implementation.**

- [X] T016 [US2] Add E2E coverage for mouse drag reorder changing current-workout collapsed row order in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`
- [X] T017 [P] [US2] Add E2E coverage for keyboard reorder changing current-workout collapsed row order in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`

### Implementation for User Story 2

- [X] T018 [US2] Wire the shared sortable-list initializer to `#session-order-list` and `#session-reorder-announce` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T019 [US2] Reorder the in-memory `workout.exercises` array through the existing `reorder<T>()` helper while preserving exercise IDs in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T020 [US2] Ensure touch drag and keyboard movement focus restoration work for active-session order rows in `src/WorkoutTracker.Web/wwwroot/ts/sortable-list.ts`
- [X] T021 [US2] Confirm saving still derives `sequence` from the reordered `workout.exercises` array and does not call workout-template update endpoints in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

**Checkpoint**: User Stories 1 and 2 both work; current-workout order can be changed in collapsed mode.

---

## Phase 5: User Story 3 - Return to normal workout entry (Priority: P2)

**Goal**: Exit order-editing mode and restore the normal workout-entry view in the updated order without losing per-exercise data.

**Independent Test**: Enter weight and effort, reorder exercises, select "Done", and verify full controls return with values still attached to the correct exercise.

### Tests for User Story 3 ⚠️

> **NOTE: Write these tests FIRST and ensure they fail before implementation.**

- [X] T022 [US3] Add E2E coverage that "Done" restores normal weight and effort controls after current-workout order editing in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`
- [X] T023 [P] [US3] Add E2E coverage that entered weight and effort values remain associated with the same exercise after reorder and exit in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`

### Implementation for User Story 3

- [X] T024 [US3] Re-render the normal active-session exercise cards in updated order when the user selects "Done" in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T025 [US3] Preserve `logEntries` by `exerciseId` across order-mode renders so entered weight and effort values restore correctly in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T026 [US3] Keep existing save and cancel actions outside order-editing rows and preserve their behavior after exiting order mode in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate shared regressions, performance, security, and documentation consistency.

- [X] T027 [P] Update quickstart steps only if final selectors or copy differ from the plan in `specs/030-edit-exercise-order/quickstart.md`
- [X] T028 [P] Confirm the unchanged API contract still matches implementation and no backend changes were introduced in `specs/030-edit-exercise-order/contracts/api-contract.md`
- [X] T029 Run frontend unit tests and TypeScript build, then fix any failures in `src/WorkoutTracker.Web`
- [X] T030 Run targeted Playwright reorder E2E tests, then fix any failures in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`
- [X] T031 Verify order-mode entry/exit and drag operations introduce no extra network calls or unbounded work in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T032 Review active-session order-editing UI for keyboard accessibility, live announcements, and name-only collapsed rows in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T033 Review security posture for no new endpoints or trust boundaries, safe exercise-name rendering, existing edit permissions, and hidden weight/effort data exposure in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on setup; blocks all user-story work.
- **US1 (Phase 3)**: Depends on foundational shared sortable helper.
- **US2 (Phase 4)**: Depends on US1 order-editing mode because the user story starts from active order-editing mode.
- **US3 (Phase 5)**: Depends on US1 and US2 because it restores normal view after order edits.
- **Polish (Phase 6)**: Depends on desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: MVP entry point and collapsed view.
- **US2 (P1)**: Adds actual reorder capability to US1's collapsed view.
- **US3 (P2)**: Restores normal entry view and data preservation after reorder.

### Within Each User Story

- Write E2E tests first and confirm they fail.
- Implement UI/state behavior.
- Confirm story-specific tests pass before moving to the next story.
- Keep current-workout data keyed by `exerciseId`; never by list index.

---

## Parallel Opportunities

- T001-T004 can run in parallel during setup.
- T009 can run in parallel with T005-T008 if selectors remain unchanged.
- T017 can be authored in parallel with T016 because both target different reorder input methods in the same test file section.
- T023 can be authored in parallel with T022 because it validates value preservation separately from control restoration.
- T027 and T028 can run in parallel during polish.

---

## Parallel Example: User Story 2

```bash
# Author story tests in parallel:
Task: "T016 [US2] Add E2E coverage for mouse drag reorder changing current-workout collapsed row order in src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs"
Task: "T017 [P] [US2] Add E2E coverage for keyboard reorder changing current-workout collapsed row order in src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs"

# After tests fail and implementation starts:
Task: "T018 [US2] Wire the shared sortable-list initializer to #session-order-list and #session-reorder-announce in src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts"
Task: "T020 [US2] Ensure touch drag and keyboard movement focus restoration work for active-session order rows in src/WorkoutTracker.Web/wwwroot/ts/sortable-list.ts"
```

## Parallel Example: User Story 3

```bash
Task: "T022 [US3] Add E2E coverage that Done restores normal weight and effort controls after current-workout order editing in src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs"
Task: "T023 [P] [US3] Add E2E coverage that entered weight and effort values remain associated with the same exercise after reorder and exit in src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs"
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 shared sortable-list extraction.
3. Complete Phase 3 US1.
4. Validate that "Edit order" appears, enters name-only collapsed mode, and hides weight/effort controls.

### Incremental Delivery

1. **US1**: Entry point and collapsed mode.
2. **US2**: Drag/touch/keyboard reorder in collapsed mode.
3. **US3**: Exit mode and preserve workout-entry data.
4. **Polish**: Regression, accessibility, performance, and docs validation.

### Single-Developer Flow

1. Extract shared helper carefully, keeping existing workout editor tests green.
2. Add active-session order-mode shell.
3. Wire reorder.
4. Restore normal mode/data preservation.
5. Run targeted frontend and E2E validation.
