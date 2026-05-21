# Research: Fix Effort Modal Outside-Click Behaviour

No unknowns require external research. All decisions are grounded in the existing codebase.

---

## Decision 1: Which handler to use for backdrop click

**Decision**: Replace `handleEffortSkip()` with `closeEffortModal()` in the backdrop `click` handler inside `initEffortModal()` (`active-session.ts`).

**Rationale**: `handleEffortSkip()` calls `closeEffortModal()` then `handleSave(null)` — it closes the modal **and** saves the session. The intended behaviour for a backdrop click is dismiss-only (no save), matching the X button (`closeEffortModal()` only). The function already exists and is already wired to the X button in the same `initEffortModal()` block.

**Alternatives considered**:
- Keep `handleEffortSkip()` — the current behaviour; rejected because it silently saves the session, which is the reported bug.
- Introduce a new function — unnecessary; `closeEffortModal()` already does exactly what is needed.

---

## Decision 2: Escape key behaviour — in scope or out?

**Decision**: Out of scope for this fix. Leave Escape wired to `handleEffortSkip()`.

**Rationale**: The user only reported clicking outside the modal. Escape is a keyboard shortcut that intentionally skips effort (analogous to the Skip button). Changing Escape to close-only would be a separate behaviour change with its own spec and tests. The constitution (Principle I) requires changes to be intentional and documented; tackling Escape here without a spec would be speculative scope creep.

---

## Decision 3: E2E test approach

**Decision**: New test `SaveWorkout_EffortModal_BackdropClick_DismissesWithoutSaving` in `WorkoutHistoryTests.cs`, modelled on the existing `WorkoutsPageTests.cs` backdrop pattern and the existing `SaveWorkout_EffortModal_CloseButton_DismissesWithoutSaving` test from feature 021.

**Rationale**: This is a regression test that must fail before the fix and pass after. The `WorkoutHistoryTests.cs` file already contains all effort-modal E2E coverage; adding the new test here is consistent. The test clicks the backdrop edge (position `{ X: 5, Y: 5 }`), asserts the modal is hidden, then asserts no session was saved (via `WebAppFixture.GetLatestSessionAsync()`), then asserts the effort modal can be re-opened by clicking Save Workout again.

**Alternatives considered**:
- Vitest unit test — not suitable; the behaviour is a DOM event interaction on a live page, not a pure function.
- New test file — unnecessary; this test belongs with the existing effort-modal E2E coverage in `WorkoutHistoryTests.cs`.
