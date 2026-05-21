# Research: Modal Close Button

## Decision 1: X button placement in the DOM

**Decision**: Place the X button as the **last child of `.edit-modal`**, after `</form>`, and position it visually in the top-right corner via `position: absolute`.

**Rationale**: The existing focus traps in all three affected modals (`muscles.ts`, `exercises.ts`, `workouts.ts`) cycle focusable elements from first to last and wrap. Appending the X button last makes it the final focusable element in the tab cycle (input → Save → [Cancel] → X → wraps to input), which requires zero changes to the existing trap logic. Placing it first (inside a header div) would break the Shift+Tab behaviour in `trapEditModalTabKey` (muscles.ts) and the generic selector trap in exercises/workouts.

**Alternatives considered**:
- Header wrapper div (flex, space-between): Cleaner semantics but requires updating `trapEditModalTabKey` to include the X button in the Shift+Tab-at-input branch. More invasive and more prone to regressions.
- `position: absolute` without DOM reordering: Same visual result, same DOM-last approach.

---

## Decision 2: CSS positioning

**Decision**: Add `position: relative` to `.edit-modal` and use `position: absolute; top: var(--spacing-md); right: var(--spacing-md)` on `.edit-modal__close`.

**Rationale**: The existing `.edit-modal` has `padding: var(--spacing-xl)` (2 rem) on all sides. Using `--spacing-md` (1 rem) for top/right offset visually places the X roughly centred with the modal's padding boundary. No need for negative margins or extra wrappers.

**Alternatives considered**:
- `top: var(--spacing-xl); right: var(--spacing-xl)` — too close to the modal's inner edge, crowds the title text.
- Float / margin-left auto — fragile when modal content reflows.

---

## Decision 3: Disabled state during in-flight requests

**Decision**: Use the HTML `disabled` attribute (not `aria-disabled`) on the X button when a save request is in progress.

**Rationale**: The X button doesn't need a text change (unlike the Save button which shows "Saving…"). Using `disabled` both prevents clicks **and** removes the button from `getVisibleModalButtons` (which filters `button:not([disabled])`), automatically collapsing the focus trap back to its pre-X-button state. This is the same approach used for the cancel button in the delete-confirm modal.

**Alternatives considered**:
- `aria-disabled="true"` (matches Save pattern): Keeps button in DOM focus cycle while blocking interaction. Unnecessary complexity for a purely dismissive action.

---

## Decision 4: Scope — which modals get the X button

**Decision**: Edit Muscle (P1), Edit Exercise (P2), Edit Workout (P2). All three share the `.edit-modal` / `.edit-modal-backdrop` CSS pattern, so a single new CSS rule covers all three.

**Out of scope**: Delete modals (Cancel + Delete), Discard modal (Discard + Continue), Effort modal (Save + Skip), Pre-start modal (No + Yes). All have clear action-based dismiss paths.

---

## Decision 5: Dark mode compatibility

**Decision**: Use `var(--color-text)` for icon colour and `var(--color-bg)` for hover background. The project's dark mode uses `[data-theme="dark"]` CSS attribute selectors that redefine these custom properties — no extra dark-mode rules needed for the X button.

---

## Decision 6: Accessibility label

**Decision**: `aria-label="Close"` on the button, with `✕` (U+2715) as the visible character.

**Rationale**: `✕` is a recognisable close symbol; `aria-label="Close"` gives screen readers a clear label. This matches the pattern used across WAI-ARIA dialog examples.

---

## Decision 7: Testing strategy

**Decision**: One new E2E test in `MusclesPageTests.cs` covering the P1 case (Edit Muscle X button closes without saving). Edit Exercise and Edit Workout already have Cancel-button E2E tests and the X button invokes the same code path — a snapshot test would be redundant. If product owners want explicit X-button coverage for exercises/workouts, that can be added in a follow-up.

**No unit tests**: The X button is a simple DOM event wire-up with no logic to isolate. The E2E test proves the full integrated behaviour.
