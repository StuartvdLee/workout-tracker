# Research: Unsaved Changes Warning in Edit Modals

## Decision 1: Warning mechanism — alertdialog vs browser confirm()
- **Decision**: Reuse the existing `.discard-modal-backdrop` / `.discard-modal` pattern from `active-session.ts`
- **Rationale**: The pattern (backdrop + modal with "Discard" / "Continue" buttons, `role="alertdialog"`) is already fully implemented and styled in `styles.css`. `browser.confirm()` is not acceptable because it is synchronous, cannot be styled, and the project already uses an accessible custom modal pattern.
- **Alternatives considered**: Inline warning text inside the edit modal (rejected — not consistent with existing discard UX); `window.confirm()` (rejected — unstyled, synchronous, blocks thread, inconsistent).

## Decision 2: CSS — new classes or reuse existing?
- **Decision**: Reuse the existing `.discard-modal-backdrop`, `.discard-modal`, `.discard-modal__title`, `.discard-modal__desc`, `.discard-modal__actions`, `.discard-modal__discard`, `.discard-modal__continue` classes verbatim. No new CSS needed.
- **Rationale**: These classes are already defined globally in `styles.css` (lines 1831–1933) and cover all visual states including dark mode via CSS custom properties.
- **Alternatives considered**: New BEM modifier classes (rejected — adds duplication without benefit).

## Decision 3: DOM layering — how to show discard modal above edit modal
- **Decision**: Add the discard-confirmation backdrop as a sibling element **after** the edit-modal backdrop in each page's HTML, relying on DOM order to paint it on top. Both share z-index 200; later DOM elements paint on top when z-index is equal.
- **Rationale**: No CSS change needed; consistent with how the delete-confirm backdrop in `muscles.ts` layers above the edit-modal backdrop when triggered.
- **Alternatives considered**: Give discard modal z-index 300 (rejected — requires CSS change and creates z-index escalation risk).

## Decision 4: Change detection approach per entity
- **Decision**:
  - **Muscles**: Capture `originalEditName: string` in `openEditModal()`. Compare with `input.value.trim()`.
  - **Exercises**: Capture `originalEditName: string` and `originalEditMuscleIds: ReadonlySet<string>` in `openEditModal()`. Compare name and Set membership.
  - **Workouts**: Capture `originalEditName: string` and `originalEditExerciseIds: readonly string[]` in `fetchAndPopulateEditModal()` (after async load, since that's when field values are set). Compare name and array stringification.
- **Rationale**: Comparison against snapshots captured at open-time is the minimal, zero-overhead approach that matches the spec. The async workouts case naturally falls out "clean" (no changes detected) if close is triggered before the fetch completes, since all originals default to empty.
- **Alternatives considered**: Dirty-tracking via `input` event listeners (rejected — more complex, same outcome, and inconsistent with the muscles case which has no Cancel button and thus close is the only action).

## Decision 5: Default focus in discard confirmation modal
- **Decision**: Focus the "Discard" (destructive) button on open, matching the `openDiscardModal()` pattern already used in `active-session.ts`.
- **Rationale**: Consistency with existing code is more important than the "safer default" UX argument. Users triggering the close action are intentionally closing; "Continue" is the escape hatch.
- **Alternatives considered**: Focus "Keep editing" / Continue button (rejected — inconsistent with existing `openDiscardModal()` implementation).

## Decision 6: Escape key in the discard confirmation modal
- **Decision**: Pressing Escape inside the discard confirmation modal closes the confirmation and returns focus to the edit modal — it does NOT propagate to close the edit modal.
- **Rationale**: The spec requires that the user can "keep editing" without losing changes. If Escape on the confirmation also closed the edit modal, the user would lose changes even via the "keep editing" path.
- **Alternatives considered**: Escape closes both modals (rejected — violates FR-005).

## Decision 7: E2E test coverage
- **Decision**: Add E2E tests (Playwright, `WorkoutsPageTests.cs`, `ExercisesPageTests.cs`, `MusclesPageTests.cs`) covering:
  - (P1) Warning shown when changes exist + discard confirmed → modal closes
  - (P1) Warning shown when changes exist + keep editing → modal stays open with changes intact
  - (P2) No warning when no changes → modal closes immediately
  - Close triggers: Cancel button (where present), X button, backdrop click, Escape
- **Rationale**: All page behavior in this codebase is tested via E2E (Playwright). No unit tests exist for any of the page modules. Change detection is a pure in-memory comparison — the E2E test indirectly exercises it by observing modal visibility and field value outcomes.
- **Alternatives considered**: Vitest unit tests for `hasEditChanges()` helper (rejected — `hasEditChanges()` is a private function; extracting it just for testability adds speculative abstraction; E2E tests already provide sufficient coverage as per all prior features).
