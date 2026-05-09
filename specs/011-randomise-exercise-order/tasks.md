# Tasks: Randomise Exercise Order UX Simplification

**Input**: Design documents from `/specs/011-randomise-exercise-order/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Existing `shuffle<T>()` and `applyOrder()` Vitest unit tests are unaffected. The `renderPrestartExercisePreview` Vitest test was removed (function deleted). Two new E2E tests were added: T024 (`PrestartModal_ClickYes_NavigatesWithOrderParam`) and T025 (`HomeToggle_Enabled_NavigatesWithOrderParam`), covering the two new user journeys mandated by Constitution Principle II. E2E helpers that previously clicked `#prestart-start` were updated to click `#prestart-no`.

**Organisation**: Tasks are grouped by phase. US1 (homepage toggle) and US2 (workouts modal) are independent after the foundational phase completes. US3 (verify re-shuffle removed) is a verification step after US1 and US2.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in descriptions

---

## Phase 1: Foundational (Shared Prerequisites)

**Purpose**: Trim `prestart-modal.ts` and update `styles.css` before any page-level work begins. Both are shared prerequisites for US1 and US2.

- [x] T001 Remove the `PrestartExercisePreview` interface and `renderPrestartExercisePreview` function from `src/WorkoutTracker.Web/wwwroot/ts/prestart-modal.ts` — retain `getVisibleModalButtons` and `trapModalTabKey`; confirm no remaining callers with a project-wide search for `renderPrestartExercisePreview` and `PrestartExercisePreview`
- [x] T002 [P] Update `src/WorkoutTracker.Web/wwwroot/css/styles.css` — **Remove**: `.prestart-modal__reshuffle-btn` block and all its variants (`hover`, `focus-visible`); `.prestart-modal__exercise-list`, `.prestart-modal__exercise-item`, `.prestart-modal__exercise-empty`; `.prestart-modal__shuffle`, `.prestart-modal__shuffle-label`, `.prestart-modal__shuffle-btn` (the modal-variant shuffle row). **Add**: `workout-form__randomise` row, `workout-form__randomise-label`, `workout-form__randomise-btn` (iOS-style toggle: 2.75 rem × 1.5 rem pill, animated knob, `--color-primary` fill when `[aria-checked="true"]`). **Add**: `prestart-modal__yes-btn` (same visual style as existing `prestart-modal__start-btn`) and `prestart-modal__no-btn` (same visual style as existing `prestart-modal__cancel-btn`)

**Checkpoint**: Foundation ready — dead CSS and dead prestart-modal.ts exports removed; new toggle and modal button styles available

---

## Phase 2: User Story 1 — Homepage Inline Toggle (Priority: P1) 🎯 MVP

**Goal**: Replacing the homepage pre-start modal with an inline iOS-style toggle. When the toggle is off, clicking "Start Workout" navigates directly. When on, it fetches the workout, shuffles exercises in memory, and navigates with `?order=`.

**Independent Test**: Select a workout with ≥ 2 exercises → "Randomise exercise order" row appears → toggle on → click "Start Workout" → active session shows exercises in shuffled order. Select a workout with 1 exercise → row is hidden. Toggle off and click "Start Workout" → active session shows original order.

- [x] T003 [US1] Update the `PlannedWorkout` interface in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` to add `exerciseCount: number`; replace the module-level `loadedWorkoutIds: Set<string>` with `loadedWorkouts: Map<string, PlannedWorkout>`; remove all modal state variables (`prestartWorkout`, `prestartCurrentOrder`, `prestartIsShuffled`, and any other modal-related state); remove unused imports (`renderPrestartExercisePreview`, `trapModalTabKey`)
- [x] T004 [US1] Remove the pre-start modal HTML from the `render()` function in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts`; insert the `workout-form__randomise` toggle row (per `contracts/ui-contract.md`) immediately before the submit button, with `id="home-randomise-row"` and `style="display:none;"` initially, containing a `<label>` and a `<button id="home-randomise-toggle" role="switch" aria-checked="false">`
- [x] T005 [US1] Update `populateWorkoutOptions()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` to populate `loadedWorkouts` Map from the API response; update `isValidWorkoutValue()` to use `loadedWorkouts.has()`; update the workout select `change` handler to show `#home-randomise-row` when the selected workout has `exerciseCount >= 2` and hide it (resetting `aria-checked` to `"false"`) otherwise
- [x] T006 [US1] Add a click handler for `#home-randomise-toggle` in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` that toggles `aria-checked` between `"true"` and `"false"`
- [x] T007 [US1] Rewrite `handleStartWorkout()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` — when `#home-randomise-toggle` `aria-checked === "false"` navigate directly to `/active-session?id=<workoutId>` (no API call); when `aria-checked === "true"` call `GET /api/workouts/<workoutId>`, shuffle `workout.exercises` using `shuffle()` from `utils.ts`, navigate to `/active-session?id=<workoutId>&order=<comma-separated-exercise-ids>`; on fetch failure fall back to direct navigation without `?order=`. **Format constraint (SR-002)**: the `?order=` value MUST be bare comma-separated exercise ID strings (e.g. `abc-1,abc-2,abc-3`) with no spaces and no URL encoding beyond what `URLSearchParams` does automatically — this must round-trip through `active-session.ts`'s `split(",").map(id => id.trim())` parser exactly
- [x] T008 [US1] Remove all pre-start modal functions from `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts`: `initPreStartModal()`, `openPreStartModal()`, `closePreStartModal()`, `renderExercisePreview()`, `handleShuffleToggle()`, `handleReshuffle()`, `handleConfirmStart()`, and any event listeners wired to modal elements

**Checkpoint**: US1 complete — homepage has inline toggle; shuffle works end-to-end; no modal involved in homepage start flow

---

## Phase 3: User Story 2 — Workouts Page Yes/No Modal (Priority: P2)

**Goal**: Replace the complex shuffle/reshuffle/preview modal on the Workouts page with a minimal "Randomise exercise order?" dialog that has only Yes and No buttons. Yes shuffles in memory and starts; No starts without shuffling.

**Independent Test**: Click Start on a workout with ≥ 2 exercises → "Randomise exercise order?" modal appears, focus on Yes → click Yes → active session shows shuffled order. Click Start → click No → active session shows original order. Click Start → press Escape → modal closes, no navigation. Click Start on a workout with 1 exercise → modal is skipped, session starts directly.

- [x] T009 [US2] Replace the pre-start modal HTML in `workouts.ts` `render()` with the simplified Yes/No structure per `contracts/ui-contract.md` — `#workout-prestart-backdrop` → `role="dialog"` `prestart-modal` with `<h2 id="prestart-modal-title">Randomise exercise order?</h2>` and `prestart-modal__actions` div containing `<button id="prestart-no" class="prestart-modal__no-btn">No</button>` (left) and `<button id="prestart-yes" class="prestart-modal__yes-btn">Yes</button>` (right)
- [x] T010 [US2] Remove `renderPrestartExercisePreview` import from `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`; remove module-level `prestartIsShuffled: boolean` and `prestartCurrentOrder: string[]` state variables; delete `handleShuffleToggle()`, `handleReshuffle()`, and `renderExercisePreview()` functions entirely
- [x] T011 [US2] Rewrite `openPreStartModal(workout)` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — if `workout.exercises.length < 2` skip the modal and navigate directly to `/active-session?id=<workoutId>`; otherwise set `prestartWorkout` module state, show `#workout-prestart-backdrop` (remove `display:none`), and move focus to `#prestart-yes`
- [x] T012 [US2] Rewrite `closePreStartModal()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — set `#workout-prestart-backdrop` `style.display = "none"`, clear `prestartWorkout` state, return focus to `prestartTriggerBtn`
- [x] T013 [US2] Replace `handleConfirmStart()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` with `handleYes()` — shuffles `prestartWorkout.exercises` in memory using `shuffle()` from `utils.ts`, calls `closePreStartModal()`, navigates to `/active-session?id=<workoutId>&order=<comma-separated-exercise-ids>` — and `handleNo()` — calls `closePreStartModal()`, navigates to `/active-session?id=<workoutId>` without `?order=`. **Format constraint (SR-002)**: `?order=` value MUST be the same bare comma-separated exercise ID format as produced by T007 — no spaces, no additional encoding — to remain compatible with `active-session.ts`'s `split(",").map(id => id.trim())` parser
- [x] T014 [US2] Rewrite `initPreStartModal()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` — wire `#prestart-yes` click to `handleYes()`; wire `#prestart-no` click to `handleNo()`; wire `keydown` on `#workout-prestart-backdrop` for Escape to `closePreStartModal()`; wire backdrop click (but not modal-card click) to `closePreStartModal()`; retain `trapModalTabKey` call from `prestart-modal.ts` for Tab key trapping between `#prestart-yes` and `#prestart-no`

**Checkpoint**: US2 complete — workouts page modal is simplified to Yes/No; reshuffle and preview gone; keyboard and focus management intact

---

## Phase 4: User Story 3 — Verify Re-shuffle Removed (Priority: P3)

**Goal**: Confirm no trace of the Re-shuffle button, shuffle toggle row (modal variant), or exercise preview list remains anywhere in the codebase.

- [x] T015 [US3] Run a project-wide search across `src/WorkoutTracker.Web/wwwroot/ts/` for `reshuffle`, `handleReshuffle`, `prestart-modal__reshuffle`, `prestart-modal__exercise-list`, `prestart-modal__exercise-item`, `prestart-modal__exercise-empty`, `prestart-modal__shuffle` — confirm zero matches in all TypeScript files; also verify `styles.css` has no remaining occurrences of these class names

**Checkpoint**: US3 verified — all re-shuffle surface area confirmed removed

---

## Phase 5: Update E2E Tests

**Purpose**: The E2E helper `StartWorkoutViaPrestartModalAsync` clicks `#prestart-start` which no longer exists. Update to click `#prestart-no` (start without randomising — same observable behaviour as before).

- [x] T016 [P] Update `StartWorkoutViaPrestartModalAsync` in `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` (line ~66) to click `#prestart-no` instead of `#prestart-start`; also update the inline usage on line ~848 that directly references `#prestart-start`; update the XML doc comment to reflect the new modal UI
- [x] T017 [P] Update `StartWorkoutViaPrestartModalAsync` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` (line ~69) to click `#prestart-no` instead of `#prestart-start`; update the XML doc comment to reflect the new modal UI
- [x] T024 [US2] Add a new E2E test `PrestartModal_ClickYes_NavigatesWithOrderParam` to `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` (Constitution II — new user journey): seed two exercises ("Bench Press", "Squat"), create a workout containing both via the existing `CreateWorkoutViaUIAsync` helper (add exercise 2 before submitting), navigate to the Workouts page, click `.workout-list__start-btn`, wait for `#workout-prestart-backdrop` to be visible, click `#prestart-yes`, then assert URL matches regex `/active-session\?id=.*&order=` — confirming the Yes path navigates with a `?order=` parameter
- [x] T025 [US1] Add a new E2E test `HomeToggle_Enabled_NavigatesWithOrderParam` to `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` (Constitution II — new user journey): seed two exercises via API, create a workout containing both via API (`POST /api/workouts` with two exercise entries), navigate to `_webApp.BaseUrl` (homepage), wait for `#workout-select` to have a non-disabled option, select the new workout from `#workout-select`, wait for `#home-randomise-row` to be visible, click `#home-randomise-toggle`, click the `#workout-form` submit button, then assert URL matches regex `/active-session\?id=.*&order=` — confirming the toggle-on path navigates with `?order=`

**Checkpoint**: E2E tests updated — existing callers use `#prestart-no`; new tests cover the Yes path (T024) and homepage toggle-on journey (T025) mandated by Constitution Principle II

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: UX consistency, accessibility spot-checks, copy verification, and regression runs across all flows.

- [x] T018 [P] Verify all homepage toggle states render correctly in browser: (1) hidden on page load, (2) hidden when single-exercise workout selected, (3) visible/off when multi-exercise workout selected, (4) visible/on after toggle clicked — spot-check against `contracts/ui-contract.md`
- [x] T019 [P] Verify all workouts page modal states in browser: (1) modal closed, (2) modal open with focus on Yes (≥ 2 exercises), (3) modal skipped / direct navigation (< 2 exercises) — spot-check against `contracts/ui-contract.md`
- [x] T020 [P] Confirm British English copy throughout: "Randomise exercise order" (homepage row), "Randomise exercise order?" (modal title), "Yes", "No" — check rendered HTML strings in `home.ts` and `workouts.ts`
- [x] T021 [P] Confirm Escape-key and backdrop-click close the workouts pre-start modal without starting a session and return focus to the triggering Start button
- [x] T022 Run all existing Vitest tests (`npm test` in `src/WorkoutTracker.Web`) and confirm all pass — regression check for `utils.test.ts`, `router.test.ts`, `theme.test.ts`
- [x] T023 Run all existing xUnit integration tests and confirm all pass — regression check confirming no backend behaviour was inadvertently changed

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: No dependencies — start immediately. T001 and T002 can run in parallel.
- **US1 (Phase 2)**: Depends on Phase 1 completion (T001 import removal, T002 CSS). T003–T008 run sequentially (same file, each builds on prior state).
- **US2 (Phase 3)**: Depends on Phase 1 completion. T009–T014 run sequentially (same file). **Independent of US1** — can be done before, after, or interleaved with Phase 2.
- **US3 (Phase 4)**: Depends on US1 (Phase 2) and US2 (Phase 3) completion.
- **E2E Tests (Phase 5)**: Depends on US2 (Phase 3) completion (button IDs changed). T016 and T017 can run in parallel. T024 depends on T009 (modal HTML with `#prestart-yes`). T025 depends on T004 (homepage toggle row with `#home-randomise-toggle` and `#home-randomise-row`).
- **Polish (Phase 6)**: Depends on all prior phases. T018–T021 can run in parallel. T022–T023 run after polish spot-checks.

### Within Each Story

- T003 before T004 (interface changes before HTML render)
- T004 before T005 (HTML elements must exist before handlers are wired)
- T005 before T006 (select handler and toggle handler share the same element)
- T006 before T007 (toggle state read by start handler)
- T007 before T008 (rewrite start handler before deleting old modal functions to avoid accidental reference removal)
- T009 before T010 (new HTML before removing old state)
- T010 before T011–T013 (state removed before rewriting functions that depend on it)
- T011 before T014 (openPreStartModal rewritten before wiring init)
- T012 before T013 (closePreStartModal rewritten before handleYes/handleNo call it)
- T013 before T014 (handlers exist before being wired)

---

## Parallel Opportunities

### Phase 1
```
T001 (prestart-modal.ts — remove dead exports)
T002 (styles.css — remove dead CSS, add new CSS)
← Both touch different files, run in parallel
```

### Phase 2 vs Phase 3
```
Phase 2 (home.ts — T003–T008)
Phase 3 (workouts.ts — T009–T014)
← Both depend only on Phase 1; fully independent of each other
```

### Phase 5
```
T016 (WorkoutsPageTests.cs — helper button update)
T017 (WorkoutHistoryTests.cs — helper button update)
← T016 and T017 touch different files, run in parallel
T024 (WorkoutsPageTests.cs — Yes-path E2E test) — depends on T009
T025 (WorkoutsPageTests.cs — homepage toggle-on E2E test) — depends on T004
← T024 and T025 can be written in parallel (different test methods, same file)
```

### Phase 6 (Polish)
```
T018, T019, T020, T021 ← all independent spot-checks, run in parallel
T022, T023             ← run after spot-checks (sequential: Vitest then xUnit)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: trim prestart-modal.ts + update CSS
2. Complete Phase 2 (T003–T008): homepage inline toggle
3. **STOP and VALIDATE**: Start a workout from homepage with toggle on and off — confirm correct navigation and exercise order
4. US1 alone is independently shippable

### Incremental Delivery

1. Phase 1 → Foundation ready
2. Phase 2 (US1) → Homepage inline toggle → **MVP**
3. Phase 3 (US2) → Workouts page Yes/No modal → **Full parity**
4. Phase 4 (US3) → Re-shuffle verified removed → **Clean**
5. Phase 5 → E2E tests updated → **Tests green**
6. Phase 6 → Polish & regression → **Ready to merge**

---

## Task Count Summary

| Phase | Tasks | Notes |
|-------|-------|-------|
| Phase 1: Foundational | 2 | prestart-modal.ts + styles.css |
| Phase 2: US1 (Homepage toggle) | **6** | PlannedWorkout interface, render, handlers, start flow, cleanup |
| Phase 3: US2 (Workouts modal) | **6** | HTML, state removal, open/close, Yes/No handlers, init wiring |
| Phase 4: US3 (Verify removed) | **1** | Code search verification |
| Phase 5: E2E test updates | **4** | T016/T017: button ID update; T024: Yes-path journey; T025: homepage toggle-on journey |
| Phase 6: Polish | **6** | UX spot-checks, copy, regression |
| **Total** | **25** | |

**Files changed**: `home.ts`, `workouts.ts`, `prestart-modal.ts`, `styles.css`, `WorkoutsPageTests.cs`, `WorkoutHistoryTests.cs` — no backend changes
