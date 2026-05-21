---

description: "Task list template for feature implementation"
---

# Tasks: Fix Effort Modal Outside-Click Behaviour

**Input**: Design documents from `/specs/022-fix-modal-outside-click/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅

**Tests**: One new Playwright E2E regression test is REQUIRED (Constitution II). The test MUST fail before the fix is applied and pass after it. No Vitest unit tests needed — the fix is a DOM event handler change, not isolated logic.

**Organization**: Single user story — one fix, one regression test, one verification pass.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1)
- Exact file paths included in all descriptions

---

## Phase 1: Regression Test (Write First — Must Fail)

**Purpose**: Establish the failing regression test that proves the bug exists before any code changes.

**⚠️ CRITICAL**: Write and run this test FIRST to confirm it fails. Do not fix the production code until T001 is red.

- [x] T001 [US1] Add test method `SaveWorkout_EffortModal_BackdropClick_DismissesWithoutSaving` to `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`, immediately after the existing `SaveWorkout_EffortModal_CloseButton_DismissesWithoutSaving` test (around line 728). The test must: (1) seed workout data and navigate using the existing helpers: `var page = await CreatePageAsync();`, `var (workoutId, _) = await CreateWorkoutAndSessionViaApiAsync(page);`, `await page.GotoAsync($"{_webApp.BaseUrl}/active-session?id={workoutId}");`, `await page.WaitForLoadStateAsync(LoadState.NetworkIdle);` — this matches the pattern used by `SaveWorkout_EffortModal_CloseButton_DismissesWithoutSaving`; (2) click `#session-save` to trigger the effort modal; (3) assert `await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync()` — modal is open; (4) click the backdrop edge using `await page.Locator("#effort-backdrop").ClickAsync(new LocatorClickOptions { Position = new Position { X = 5, Y = 5 } })` (matches the pattern used in `WorkoutsPageTests.cs` line 641); (5) assert `await Expect(page.Locator("#effort-backdrop")).ToBeHiddenAsync()` — modal is closed; (6) assert the session was NOT saved by checking the user remained on the active-session page without navigating to history: `await Expect(page.Locator("#session-save")).ToBeVisibleAsync()` — this matches the exact pattern used in `SaveWorkout_EffortModal_CloseButton_DismissesWithoutSaving`; (7) assert the effort modal can be re-opened by clicking `#session-save` again and asserting `await Expect(page.Locator("#effort-backdrop")).ToBeVisibleAsync()`. Wrap in try/finally with `await page.CloseAsync()`.

**Checkpoint**: `SaveWorkout_EffortModal_BackdropClick_DismissesWithoutSaving` exists, compiles, and FAILS (backdrop click currently saves session via `handleEffortSkip()`).

---

## Phase 2: User Story 1 — Fix Backdrop Click Handler (Priority: P1) 🎯

**Goal**: Clicking the backdrop (outside the modal card) on the Overall Workout Effort modal closes the modal without saving the active session.

**Independent Test**: Open effort modal → click backdrop edge → modal closes → session not saved → effort modal can be re-opened.

### Implementation for User Story 1

- [x] T002 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`, inside `initEffortModal()`, locate the `backdrop.addEventListener("click", ...)` handler (around line 213). Change the handler body from `handleEffortSkip()` to `closeEffortModal()`. The corrected block must read:

  ```typescript
  backdrop.addEventListener("click", (event: Event) => {
    if (event.target === backdrop) {
      closeEffortModal();
    }
  });
  ```

  No other changes to `initEffortModal()` are needed. `closeEffortModal()` is already defined in the same file (line 281); `handleEffortSkip()` retains its existing wiring to the Skip button (line 202) and Escape key (line 221).

- [x] T003 [US1] Run `cd src/WorkoutTracker.Web && npm run build` from the repository root and confirm zero TypeScript compilation errors. The change touches one function call inside a callback — no type signatures change, so no new strict-mode violations are expected.

**Checkpoint**: `active-session.ts` compiles cleanly. Backdrop click now calls `closeEffortModal()`. The regression test from T001 should now pass.

---

## Phase 3: Polish & Verification

**Purpose**: Confirm the full build and all pre-existing tests remain green.

- [x] T004 [P] Run `cd src/WorkoutTracker.Web && npm test` — confirm all Vitest tests pass (no new unit tests added by this fix, but existing tests must remain green).
- [x] T005 [P] Run `dotnet build src/WorkoutTracker.slnx` — confirm the .NET solution builds with no errors or warnings.
- [ ] T006 Manual smoke test in a browser — verify: (1) complete a workout and click Save Workout to open the effort modal; (2) click the semi-transparent backdrop area (outside the modal card) — the modal closes and the user remains on the active session page; (3) the workout is NOT saved (no navigation to history, no session entry created); (4) clicking Save Workout again re-opens the effort modal normally; (5) the Save and Skip buttons inside the modal still work as before; (6) Escape key still skips (saves with null effort) — unchanged behaviour.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Regression test)**: No dependencies — start immediately
- **Phase 2 (Fix)**: Depends on T001 existing and confirmed failing
- **Phase 3 (Verification)**: Depends on Phase 2 complete

### Within Phase 2

- T002 (code fix) must complete before T003 (build check)

### Parallel Opportunities

- T004 and T005 in Phase 3 are independent and can run in parallel

---

## Parallel Example: Phase 3

```bash
# Launch in parallel after T002 and T003 are complete:
Task: "cd src/WorkoutTracker.Web && npm test"       # T004
Task: "dotnet build src/WorkoutTracker.slnx"        # T005
```

---

## Implementation Strategy

### MVP (Single Story — Only One Story Exists)

1. Complete Phase 1: Write regression test (must be red)
2. Complete Phase 2: Apply one-line fix + build check
3. Complete Phase 3: Full verification
4. **VALIDATE**: Regression test (T001) is green; all other tests remain green

### TDD Flow

1. T001: Write test → run → confirm RED (backdrop click saves session)
2. T002: Change `handleEffortSkip()` → `closeEffortModal()` in backdrop handler
3. T003: Build — confirm green
4. T001: Run test again → confirm GREEN
5. T004 + T005: Full suite — confirm no regressions
6. T006: Manual smoke test

---

## Notes

- [P] tasks = different files, no dependencies between them
- [US1] label maps each task to User Story 1 (the only story)
- The Escape key behaviour (`handleEffortSkip()` on Escape) is intentionally left unchanged — out of scope per `research.md` Decision 2
- `closeEffortModal()` already exists at line 281 of `active-session.ts` — no new function needed
- The Skip button retains its `handleEffortSkip()` wiring (line 202) — unchanged
