---

description: "Task list template for feature implementation"
---

# Tasks: Modal Close Button

**Input**: Design documents from `/specs/021-modal-close-button/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅

**Tests**: Three new Playwright E2E tests are REQUIRED — one per modal (US1: Edit Muscle; US2: Edit Exercise, Edit Workout). No Vitest unit tests needed — the X button is a DOM event wire-up with no isolated logic.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- Exact file paths included in all descriptions

---

## Phase 1: Foundational (Shared CSS)

**Purpose**: CSS infrastructure shared by all three edit modals. MUST be complete before any user story implementation begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T001 Add `position: relative;` to the existing `.edit-modal` rule in `src/WorkoutTracker.Web/wwwroot/css/styles.css` (this enables absolute positioning of the close button within the modal boundary).
- [X] T002 Add the `.edit-modal__close` rule block to `src/WorkoutTracker.Web/wwwroot/css/styles.css` immediately after the `.edit-modal__cancel` rule (around line 912). The rule must: (1) `position: absolute; top: var(--spacing-md); right: var(--spacing-md);` to place the button in the top-right corner; (2) be a ghost icon button — `background: none; border: none; cursor: pointer; padding: var(--spacing-xs); border-radius: var(--radius); color: var(--color-text-light); font-size: var(--font-size-lg); line-height: 1; display: flex; align-items: center; justify-content: center; min-width: 2rem; min-height: 2rem;`; (3) add `.edit-modal__close:hover { color: var(--color-text); background-color: var(--color-bg); }`; (4) add `.edit-modal__close:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 2px; }`; (5) add `.edit-modal__close:disabled { opacity: 0.4; cursor: not-allowed; }`. Dark mode is automatic via CSS custom property overrides — no extra rules needed.

**Checkpoint**: `.edit-modal` has `position: relative` and `.edit-modal__close` styles are defined in `styles.css`.

---

## Phase 2: User Story 1 — Edit Muscle Modal Close Button (Priority: P1) 🎯 MVP

**Goal**: The Edit Muscle modal gains an X button in the top-right corner. Clicking it (or pressing Escape) closes the modal without saving changes.

**Independent Test**: Navigate to `/muscles`. Click any muscle card to open the Edit Muscle modal. Click the ✕ button in the top-right corner. Assert the modal disappears and the muscle data is unchanged.

### E2E Test for User Story 1

- [X] T003 [US1] Add test method `EditMuscle_CloseButton_ClosesModalWithoutSaving` to `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs`. The test must: (1) call `CreatePageAsync()` to navigate to `/muscles`; (2) click any `.muscle-card` to open the edit modal; (3) use `page.Locator("#edit-modal-backdrop").WaitForAsync()` to confirm the modal is visible; (4) fill in a new value with `page.Locator("#edit-muscle-name").FillAsync("Changed Name")`; (5) click the close button via `page.Locator("#edit-modal-close").ClickAsync()`; (6) assert `await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync()` — modal is gone; (7) assert the muscle card still shows the original name (not "Changed Name"), confirming no save occurred. Wrap in try/finally with `page.CloseAsync()`. Note: Escape key behaviour is already covered by the existing `EditMuscle_EscapeDiscardChanges` test — do not duplicate it here.

### Implementation for User Story 1

- [X] T004 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts`, inside the `render()` function's `container.innerHTML` template string, add the X button as the **last child of `.edit-modal`** (after the closing `</form>` tag, before the closing `</div>` of `.edit-modal`). The button HTML: `<button class="edit-modal__close" id="edit-modal-close" type="button" aria-label="Close">&#x2715;</button>`. The full `.edit-modal` element should end: `</form>\n          <button class="edit-modal__close" id="edit-modal-close" type="button" aria-label="Close">&#x2715;</button>\n        </div>`.
- [X] T005 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts`, inside `initEventListeners()`: (1) add `const closeBtn = document.getElementById("edit-modal-close") as HTMLButtonElement | null;` alongside the other element lookups at the top of the function; (2) add `closeBtn?.addEventListener("click", () => { if (!isEditSubmitting) { closeEditModal(); } });` after the existing `editBackdrop?.addEventListener("click", ...)` block. This closes the modal on X click unless a save is in progress.
- [X] T006 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts`, in `handleEditSave()`: (1) add `const closeBtn = document.getElementById("edit-modal-close") as HTMLButtonElement | null;` alongside the other `const` declarations; (2) when setting `isEditSubmitting = true` (around line 386), also set `if (closeBtn) closeBtn.disabled = true;`; (3) in the `finally` block where `isEditSubmitting = false` is reset (around line 410), also set `if (editingMuscleId !== null && closeBtn) closeBtn.disabled = false;`. Note: the Delete button in the edit modal calls `openDeleteConfirmModal()`, which closes the edit modal immediately (via `closeEditModal()`) before showing the delete confirmation. Therefore the X button is not visible during delete and no changes to the delete handler are needed.
- [X] T007 [US1] Run `cd src/WorkoutTracker.Web && npm run build` from the repository root and confirm zero TypeScript errors. Fix any strict-mode violations (unused variables, implicit any, etc.) before proceeding.

**Checkpoint**: Edit Muscle modal shows ✕ in top-right. Clicking ✕ closes modal without saving. ✕ is disabled during an in-flight save. TypeScript build passes. E2E test (T003) should now pass when run against a live app.

---

## Phase 3: User Story 2 — Edit Exercise & Edit Workout Modal Close Buttons (Priority: P2)

**Goal**: Both the Edit Exercise and Edit Workout modals gain the same ✕ close button for consistency. Their existing Cancel buttons are retained.

**Independent Test**: Open the Edit Exercise modal, click ✕ — modal closes, no save. Open the Edit Workout modal, click ✕ — modal closes, no save. Both behave identically to their respective Cancel buttons.

### E2E Tests for User Story 2

- [X] T008a [P] [US2] Add test method `EditExercise_CloseButton_ClosesModalWithoutSaving` to `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`. The test must: (1) call `CreatePageAsync()` to navigate to `/exercises`; (2) click `.exercise-list__edit-btn` (first) to open the Edit Exercise modal; (3) assert `await Expect(page.Locator("#edit-modal-backdrop")).ToBeVisibleAsync()`; (4) fill `page.Locator("#edit-exercise-name").FillAsync("Changed Name")`; (5) click the close button `page.Locator("#edit-modal-close").ClickAsync()`; (6) assert `await Expect(page.Locator("#edit-modal-backdrop")).ToBeHiddenAsync()`; (7) assert the exercise still shows the original name (not "Changed Name") — confirming no save occurred. Wrap in try/finally with `page.CloseAsync()`.
- [X] T009a [P] [US2] Add test method `EditWorkout_CloseButton_ClosesModalWithoutSaving` to `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs`. The test must: (1) call `CreatePageAsync()` to navigate to `/workouts`; (2) click `.workout-list__edit-btn` (first) to open the Edit Workout modal; (3) assert `await Expect(page.Locator("#workout-edit-backdrop")).ToBeVisibleAsync()`; (4) fill `page.Locator("#edit-workout-name").FillAsync("Changed Name")`; (5) click the close button `page.Locator("#workout-edit-close").ClickAsync()`; (6) assert `await Expect(page.Locator("#workout-edit-backdrop")).ToBeHiddenAsync()`; (7) assert the workout still shows the original name — confirming no save occurred. Wrap in try/finally with `page.CloseAsync()`.

### Implementation for User Story 2

- [X] T008 [P] [US2] In `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`, in the `render()` function's HTML template: add `<button class="edit-modal__close" id="edit-modal-close" type="button" aria-label="Close">&#x2715;</button>` as the last child of `.edit-modal` (after `</form>`, before the closing `</div>` of `.edit-modal`). In `initEditModal()`: (1) add `const closeBtn = document.getElementById("edit-modal-close") as HTMLButtonElement | null;` alongside existing element lookups; (2) add `closeBtn?.addEventListener("click", () => { if (!isEditSubmitting) { closeEditModal(); } });`. In `handleEditSubmit()`: (1) add `const closeBtn = document.getElementById("edit-modal-close") as HTMLButtonElement | null;` alongside `const submitBtn`; (2) when `isEditSubmitting = true` is set (around line 506), also set `if (closeBtn) closeBtn.disabled = true;`; (3) in the `finally` block (around line 536), also set `if (closeBtn) closeBtn.disabled = false;`.
- [X] T009 [P] [US2] In `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`, in the `render()` function's HTML template: add `<button class="edit-modal__close" id="workout-edit-close" type="button" aria-label="Close">&#x2715;</button>` as the last child of `.edit-modal` (after `</form>`, before the closing `</div>` of `.edit-modal`). In `initEditModal()` (the workout edit modal init): (1) add `const closeBtn = document.getElementById("workout-edit-close") as HTMLButtonElement | null;` alongside existing element lookups; (2) add `closeBtn?.addEventListener("click", () => { if (!isEditSubmitting) { closeEditModal(); } });`. In `handleEditSubmit()` (workout save handler): (1) add `const closeBtn = document.getElementById("workout-edit-close") as HTMLButtonElement | null;` alongside `const submitBtn`; (2) when `isEditSubmitting = true` is set (around line 917), also set `if (closeBtn) closeBtn.disabled = true;`; (3) in the `finally` block (around line 951), also set `if (closeBtn) closeBtn.disabled = false;`.
- [X] T010 [US2] Run `cd src/WorkoutTracker.Web && npm run build` from the repository root and confirm zero TypeScript errors across all three modified page files.

**Checkpoint**: All three edit modals (Edit Muscle, Edit Exercise, Edit Workout) display ✕ in the top-right corner. Clicking ✕ on any of them closes the modal without saving. TypeScript build passes. E2E tests (T008a, T009a) pass against a live app.

---

## Phase 4: Polish & Verification

**Purpose**: Confirm the full build and all pre-existing tests pass. No regressions.

- [X] T011 [P] Run `cd src/WorkoutTracker.Web && npm test` — confirm all Vitest tests pass (no new unit tests added by this feature, but existing tests must remain green).
- [X] T012 [P] Run `dotnet build src/WorkoutTracker.slnx` — confirm the .NET solution builds cleanly with no errors or warnings.
- [ ] T013 Manual smoke test in a browser — verify: (1) Edit Muscle modal shows ✕ in the top-right, clicking it closes the modal with no changes saved; (2) Edit Exercise modal shows ✕, clicking it closes with no changes saved, Cancel button also still works; (3) Edit Workout modal same as exercise; (4) dark mode active — ✕ button is visible against the modal background; (5) Tab key cycles: text input → Save → Delete [→ Cancel if present] → ✕ → back to text input.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: No dependencies — start immediately
- **US1 (Phase 2)**: Depends on Phase 1 complete (CSS rule must exist before the button renders)
- **US2 (Phase 3)**: Depends on Phase 1 complete; independent of US1 (different files)
- **Polish (Phase 4)**: Depends on Phases 1–3 complete

### Within Each Phase

- T001 must complete before T002 (both in `styles.css` — sequential to avoid write conflicts)
- T004 (HTML) must complete before T005 and T006 (TS wiring references the button ID)
- T005 and T006 can follow T004 sequentially (same file, adjacent functions)
- T008a and T009a are fully parallel (different files: `ExercisesPageTests.cs` vs `WorkoutsPageTests.cs`)
- T008 and T009 are fully parallel (different files: `exercises.ts` vs `workouts.ts`)
- T008a can run in parallel with T008 (both target exercises — tests and impl are independent); same for T009a and T009
- T010 depends on T008 and T009 (build check after both are done)
- T011 and T012 are parallel (different toolchains)

### Parallel Opportunities

- T008a, T008, T009a, and T009 (US2 tests + implementation) can all run in parallel — different files, no shared state
- T011 and T012 (final checks) can run in parallel — different toolchains
- US1 (Phase 2) and US2 (Phase 3) could run in parallel after Phase 1 if there are two developers — they touch entirely different files

---

## Parallel Example: Phase 3 (US2)

```
Parallel batch — independent files, no shared state:
  Task T008a: ExercisesPageTests.cs — add EditExercise_CloseButton_ClosesModalWithoutSaving E2E test
  Task T008:  exercises.ts — add X button HTML + wire closeEditModal() + disable during submit
  Task T009a: WorkoutsPageTests.cs — add EditWorkout_CloseButton_ClosesModalWithoutSaving E2E test
  Task T009:  workouts.ts — add X button HTML + wire closeEditModal() + disable during submit

Sequential after both pairs complete:
  Task T010: npm run build — verify zero TypeScript errors
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Foundational CSS (T001–T002)
2. Complete Phase 2: US1 — Edit Muscle (T003–T007)
3. **STOP and VALIDATE**: Open `/muscles`, click any muscle card, confirm ✕ appears and closes modal without saving ✅
4. Ship US1 or continue to US2

### Incremental Delivery

1. Phase 1 complete → CSS ready for all modals
2. Phase 2 complete → Edit Muscle X button working (P1 fix delivered)
3. Phase 3 complete → Edit Exercise and Edit Workout X buttons working (consistency delivered)
4. Phase 4 complete → all tests green, build clean, smoke test verified

---

## Notes

- [P] tasks = different files or non-overlapping code sections with no dependencies on each other
- The X button element ID in `workouts.ts` is `workout-edit-close` (not `edit-modal-close`) to avoid a collision — both `exercises.ts` and `workouts.ts` pages can be mounted and the IDs must be unique per page
- The ✕ character used in button text is `&#x2715;` (U+2715 MULTIPLICATION X) — a well-rendered close symbol across all browsers
- `disabled` attribute (not `aria-disabled`) is used on the X button during submit — this removes it from the `getVisibleModalButtons` focus cycle automatically
- **Delete handler — no change needed**: `openDeleteConfirmModal()` calls `closeEditModal()` before showing the delete confirmation, so the X button in the edit modal is always hidden before any delete request is in-flight. No X button disable logic is required in the delete path.
- **Escape key — already implemented and tested**: `editBackdrop` has a `keydown` listener that calls `closeEditModal()` when `event.key === "Escape"`. This is already verified by the existing `EditMuscle_EscapeDiscardChanges` and `EditWorkout_EscapeClosesModal` E2E tests. No new implementation or tests needed for Escape behaviour.
- Do not add an X button to delete-confirm, discard, effort, or pre-start modals — those already have clear dismiss paths and are out of scope
- Commit after each checkpoint
