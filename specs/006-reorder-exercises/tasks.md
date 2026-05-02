# Tasks: Reorder Exercises in a Workout

**Input**: Design documents from `/specs/006-reorder-exercises/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

> **Scope note**: This is a **pure frontend change**. The backend already stores and returns exercises in `Sequence` order. No migrations, no API endpoint changes, and no backend test changes are required.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2)
- Exact file paths are included in every task description

---

## Phase 1: Setup

**Purpose**: No new project or infrastructure needed — the codebase is already in place. One shared utility must be added before user story work begins.

- [x] T001 Confirm all 45 xUnit integration tests and 7 Vitest unit tests pass on the current branch before any changes in `src/WorkoutTracker.Web/wwwroot/ts/` and `src/WorkoutTracker.Web/wwwroot/css/styles.css`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `reorder()` helper and its tests must exist before either user story can be implemented. The `Set<string>` → `string[]` state migration in `workouts.ts` underpins both stories and must be done first.

**⚠️ CRITICAL**: Both user story phases depend on this phase completing first.

- [x] T002 Add `reorder<T>(arr: T[], fromIndex: number, toIndex: number): void` to `src/WorkoutTracker.Web/wwwroot/ts/utils.ts` — mutates in place using `arr.splice(toIndex, 0, arr.splice(fromIndex, 1)[0])`; guard: do nothing if `fromIndex === toIndex` or either index is out of bounds
- [x] T003 [P] Add Vitest unit tests for `reorder()` in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/workouts.test.ts` — cover: move first to last, move last to first, move middle element to front, same-index no-op, single-element array no-op, out-of-bounds index no-op; confirm tests FAIL before T002, PASS after
- [x] T004 [P] Add CSS for drag interaction states to `src/WorkoutTracker.Web/wwwroot/css/styles.css`: `.workout-selected__drag-handle` (flex, 44×44px, `background: none`, `border: none`, `cursor: grab`, muted colour, hover/focus-visible styles), `.workout-selected__item--dragging { opacity: 0.4 }`, `.workout-selected__item--drag-over { border-top: 2px solid var(--color-primary) }`, `body.is-dragging .workout-selected__drag-handle { cursor: grabbing }`, update `.workout-selected__item` to `display: flex; align-items: center`, add `.sr-only` visually-hidden utility class if not already present
- [x] T005 Migrate `selectedExercises` and `editSelectedExercises` from `Set<string>` to `string[]` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — update all 8 affected sites: declarations (`let selectedExercises: string[] = []`, `let editSelectedExercises: string[] = []`), add guards (`includes` before push), `.size` → `.length`, `.delete()` → `.filter()`, `new Set()` resets → `[]`, `Array.from()` calls → direct array use, `renderSelectedExercisesList` / `renderEditSelectedExercisesList` iteration, `renderExerciseDropdown` / `renderEditExerciseDropdown` deduplication checks; run `npm test` to confirm existing 7 Vitest tests still pass after this change

**Checkpoint**: `reorder()` is tested, CSS states are in place, and `workouts.ts` uses `string[]` throughout — both user story phases can now proceed.

---

## Phase 3: User Story 1 — Reorder Exercises While Creating a Workout (Priority: P1) 🎯 MVP

**Goal**: Users can drag exercises into the desired order in the create-workout form before saving. The saved workout persists the displayed order.

**Independent Test**: Open the new workout form, add three or more exercises, drag one to a different position, save, re-open the workout in the edit modal, and confirm the exercises appear in the reordered sequence. Also confirm no drag handle appears when only one exercise is selected.

### Implementation for User Story 1

- [x] T006 [US1] Add the ARIA live-announce region `<div class="sr-only" aria-live="polite" aria-atomic="true" id="workout-reorder-announce"></div>` immediately before `<ul class="workout-selected__list" id="workout-selected-list">` in the `render()` HTML template in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [x] T007 [US1] Update `buildSelectedExerciseItem` (or extract a new `buildReorderableExerciseItem`) in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` to: conditionally render the six-dot grip `<button class="workout-selected__drag-handle">` (hidden / not rendered when list length < 2), add `draggable="true"` to `<li>` when list length ≥ 2, set `data-exercise-id` and `data-index` attributes on `<li>`, add `aria-roledescription="sortable item"` to `<li>`, add `aria-label="Drag to reorder [Exercise Name]"` to the handle button — use the SVG grip icon from the UI contract (two columns of three circles)
- [x] T008 [US1] Implement `initDragAndDrop(listId: string, announceId: string, getArray: () => string[], onReorder: () => void): void` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — attach HTML5 DnD listeners via event delegation on the `<ul>` container: `dragstart` (store dragging index, `dataTransfer.setData`, `effectAllowed = 'move'`, add `body.is-dragging`, add `.workout-selected__item--dragging` to dragged item), `dragover`/`dragenter` (`preventDefault()`, add `.workout-selected__item--drag-over` to target `<li>`), `dragleave` (remove `.workout-selected__item--drag-over`), `drop` (get target index from `closest('li[data-index]').dataset.index`, call `reorder()`, call `onReorder()`, announce result to the element identified by `announceId`), `dragend` (remove `body.is-dragging` and all drag visual classes)
- [x] T009 [US1] Add touch event support to `initDragAndDrop` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — attach `touchstart` (non-passive) on drag handle buttons: clone the `<li>`, position clone `fixed`, track touch position, add `.workout-selected__item--dragging` to original, add `body.is-dragging`; `touchmove` (non-passive): `preventDefault()`, reposition clone via `transform: translate`, use `document.elementFromPoint(touch.clientX, touch.clientY).closest('li[data-index]')` to find target (clone must have `pointer-events: none`), add `.workout-selected__item--drag-over` to target; `touchend`: determine final target index, call `reorder()`, call `onReorder()`, announce, remove clone and all visual state
- [x] T010 [US1] Add keyboard reorder support to the drag handle `<button>` in `buildReorderableExerciseItem` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — `keydown` handler: `Space` toggles picked-up state and `aria-pressed`; while picked up, `ArrowUp`/`ArrowDown` call `reorder()` and `onReorder()`, move focus to the handle button at the new index, and announce result; `Enter` confirms (same as second `Space`); `Escape` restores original position and clears picked-up state
- [x] T011 [US1] Wire `initDragAndDrop` into `initForm()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — call `initDragAndDrop('workout-selected-list', 'workout-reorder-announce', () => selectedExercises, renderExerciseDropdown)` after attaching the submit and select-change listeners
- [x] T012 [US1] Update `handleSubmit()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — replace `Array.from(selectedExercises)` with direct array use (`selectedExercises.map(...)`) to build the ordered exercises payload; confirm exercises are sent in array order (this is already how sequence is stored by the API)
- [ ] T013 [US1] Verify US1 acceptance scenarios manually via the quickstart walkthrough in `specs/006-reorder-exercises/quickstart.md` — create a workout, add 3+ exercises, drag to reorder, save, reopen in edit modal, confirm order persisted; also confirm single-exercise list has no drag handles

---

## Phase 4: User Story 2 — Reorder Exercises While Editing an Existing Workout (Priority: P2)

**Goal**: Users can drag exercises into the desired order in the edit-workout modal. Saving persists the new order; cancelling discards it.

**Independent Test**: Open an existing workout in the edit modal (it must already have 3+ exercises in a known order), drag one exercise to a different position, save, reopen the modal, and confirm the new order is shown. Then repeat, drag to reorder, cancel, reopen, and confirm the original order is unchanged.

### Implementation for User Story 2

- [x] T014 [US2] Add the ARIA live-announce region `<div class="sr-only" aria-live="polite" aria-atomic="true" id="edit-reorder-announce"></div>` immediately before `<ul class="workout-selected__list" id="edit-selected-list">` in the edit modal HTML template in `render()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`
- [x] T015 [US2] Update `fetchAndPopulateEditModal` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — change `editSelectedExercises = new Set()` (already migrated in T005 to `= []`) to populate from `fullWorkout.exercises` **in the order returned by the API** (already ordered by `Sequence`): `editSelectedExercises = fullWorkout.exercises.map(ex => ex.exerciseId)`; this ensures the edit modal shows the saved sequence
- [x] T016 [US2] Call `initDragAndDrop('edit-selected-list', 'edit-reorder-announce', () => editSelectedExercises, renderEditExerciseDropdown)` inside `fetchAndPopulateEditModal()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`, after `editSelectedExercises` has been populated but before the modal is shown — wiring here ensures the listener is attached exactly once per modal open, consistent with T011 which attaches once in `initForm()`; do NOT attach inside any list re-render function, as event delegation on the `<ul>` means listeners survive re-renders without re-attachment
- [x] T017 [US2] Confirm `closeEditModal()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` resets `editSelectedExercises = []` — this is the "cancel discards reorder" guarantee; the next `openEditModal()` call will re-fetch and re-populate from the server's saved order
- [x] T018 [US2] Update `handleEditSubmit()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — replace `Array.from(editSelectedExercises)` with direct array use (`editSelectedExercises.map(...)`) to build the ordered exercises payload
- [ ] T019 [US2] Verify US2 acceptance scenarios via the quickstart walkthrough in `specs/006-reorder-exercises/quickstart.md` — edit a workout with 3+ exercises, drag to reorder, save, reopen to confirm order; then drag to reorder, cancel, reopen to confirm original order preserved

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verify the full feature against all constitution requirements and the spec's edge cases.

- [x] T020 [P] Run `npm test` in `src/WorkoutTracker.Web/` to confirm all Vitest tests pass (original 7 + new `reorder()` tests from T003)
- [ ] T021 [P] Run `dotnet test` in `src/` to confirm all 45 xUnit integration tests still pass (no backend changes should mean no regressions)
- [ ] T022 Verify the drag handle is **not rendered** when a selected-exercises list contains exactly one exercise (create form and edit modal) — per UX-004 and spec acceptance scenario US1.4
- [ ] T023 Verify error state behaviour: trigger a save failure (disconnect network or mock API error), confirm the exercise list **retains the reordered state** and the existing `#workout-api-error` / `#edit-workout-api-error` message is shown — per FR-008 and spec edge case
- [ ] T024 Verify add-after-reorder behaviour: reorder exercises, then add a new one via the dropdown, confirm the new exercise appears at the bottom and further drag reordering still works correctly
- [ ] T025 Verify remove-after-reorder behaviour: reorder exercises, then remove one, confirm remaining exercises retain their relative order
- [x] T026 [P] Review `workouts.ts` for TypeScript strict-mode compliance — no `any`, no unused locals, no implicit returns — run `npx tsc --noEmit` in `src/WorkoutTracker.Web/` and fix any type errors introduced by the `Set→Array` migration or DnD additions
- [ ] T027 [P] Accessibility review — tab to each drag handle button in the create form and edit modal, confirm `aria-label` is present and correct, confirm keyboard reorder (Space / ↑↓ / Esc) works, confirm the `aria-live` region announces each move (test with browser DevTools accessibility inspector)
- [x] T028 Add a Playwright E2E test in `src/WorkoutTracker.E2ETests/E2E/` for the US1 drag-reorder journey: navigate to the new workout form, add 3 exercises via the dropdown, programmatically trigger a drag-and-drop to move the first exercise to the last position (using Playwright's `dragAndDrop` helper or `dispatchEvent`), save the workout, re-open the workout in the edit modal or re-fetch via the API, and assert the exercises are returned in the reordered sequence — required by Constitution II: "new user journeys MUST include integration or E2E coverage"
- [x] T029 Add a Playwright E2E test in `src/WorkoutTracker.E2ETests/E2E/` for the US2 drag-reorder journey: open an existing workout with 3+ exercises in edit mode, drag one exercise to a different position, save, reopen and assert the new order persists (spec US2.3); then repeat but cancel instead of saving and assert the original order is unchanged (spec US2.4)
- [x] T030 Verify SR-001 authorization: The API has no multi-user authentication model — there is no `userId` or `ownerId` field on workouts and no `ClaimsPrincipal` filtering in `Program.cs`. Ownership enforcement does not apply to this single-user application; SR-001 is N/A.
- [x] T031 Verify SR-002 input sanitization: Existing tests in `src/WorkoutTracker.UnitTests/Api/WorkoutApiTests.cs` cover the required cases — `CreateWorkout_Returns400_WhenNoExercises` (line 173), `CreateWorkout_Returns400_WhenInvalidExerciseId` (line 219), `UpdateWorkout_Returns400_WhenNoExercises` (line 254). No new tests required.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — run first
- **Phase 2 (Foundational)**: Depends on Phase 1; **BLOCKS** Phases 3 and 4
- **Phase 3 (US1)**: Depends on Phase 2 completion
- **Phase 4 (US2)**: Depends on Phase 2 completion; shares `initDragAndDrop` introduced in Phase 3 (T008–T010)
- **Phase 5 (Polish)**: Depends on Phases 3 and 4; T028–T029 (E2E) depend on T006–T019 (all user story implementation complete); T030–T031 (security verification) are independent of DnD implementation and can run once the backend test environment is available

### User Story Dependencies

- **US1 (P1)**: Can start immediately after Phase 2. Standalone — creates `initDragAndDrop` for use by US2.
- **US2 (P2)**: Depends on `initDragAndDrop` from US1 (T008–T010 must be complete before T016). Otherwise independently testable.

### Within US1

- T006 (HTML structure) → T007 (item builder) → T008 (DnD listener) → T009 (touch) → T010 (keyboard) → T011 (wire up) → T012 (submit fix) → T013 (verify)
- T006 and T007 can be done together (same render pass)

### Within US2

- T005 (Set→Array migration, Phase 2) must be complete before T015 (array population)
- T008–T010 (from US1) must be complete before T016 (wire up edit list)
- T014, T015, T017, T018 can proceed in parallel once Phase 2 and US1 are done

### Parallel Opportunities (Phase 2)

```
T002 (reorder helper in utils.ts)     ← independent
T003 (Vitest tests for reorder)       ← parallel with T002, depends on T002 to go green
T004 (CSS states in styles.css)       ← fully independent, run in parallel
T005 (Set→Array migration)            ← depends on T002 being available to import
```

---

## Implementation Strategy

### MVP First (US1 Only — Create Form Reordering)

1. Complete Phase 1 + Phase 2 (T001–T005)
2. Complete Phase 3 (T006–T013)
3. **Validate**: Create a workout, add 3 exercises, drag to reorder, save, confirm order persisted
4. Deploy/demo as MVP — edit-time reordering (US2) can follow

### Incremental Delivery

1. Phase 1 + Phase 2 → foundation ready
2. Phase 3 (US1) → create-form drag reorder working → Demo MVP
3. Phase 4 (US2) → edit-modal drag reorder working → Full feature complete
4. Phase 5 → polish, accessibility, and regression sign-off

---

## Notes

- No backend changes at all — the backend already handles sequence from array order
- `reorder()` goes in `utils.ts` (shared module), not in `workouts.ts`, consistent with `getEffortLabel` pattern from feature 005
- Event delegation on the `<ul>` container means DnD listeners survive full re-renders of the list contents
- Touch event listeners **must** be registered as `{ passive: false }` to allow `preventDefault()` in `touchmove`
- The dragged-item clone needs `pointer-events: none` so `document.elementFromPoint()` returns the element beneath, not the clone
- `dataTransfer.setData('text/plain', ...)` is mandatory for Firefox — without it, the drag is silently cancelled
- Cancel in edit modal already resets `editSelectedExercises = []` and re-fetches on next open — no additional logic needed for "cancel discards reorder"
