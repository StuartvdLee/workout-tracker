# UI Contract: Accessible Exercise Ordering While Scrolling

## Normal workout view

- A single native button with accessible name **Edit order** and a decorative reorder icon replaces the former visible text. The button remains `#session-edit-order` so existing behavior remains connected.
- When available, the button is visible, in the unobscured viewport, and directly operable at the top, middle, and bottom of the current workout. Supported scroll methods do not alter this behavior.
- At widths of at least 768 CSS pixels, its dedicated rail reserves layout space without covering workout controls. Below 768 pixels, the fixed blue button with white icon and border floats over the workout without narrowing its content; fields and footer actions remain reachable by scrolling. It stays below the fixed top bar, and navigation and modal backdrops remain above it.
- The target is at least 44 by 44 CSS pixels, shows a visible keyboard focus indication, and supports pointer, touch, Enter, and Space. The icon is hidden from the accessibility tree.
- Both themes, the viewport and zoom verification matrix in [spec.md](../spec.md), and long workout/exercise names remain readable and operable without horizontal clipping.

## Existing states and transitions

- The action is hidden while loading, on blocking workout-load failure, and for empty workouts; it remains available for a loaded workout with exercises after non-blocking previous-performance or session-save failures. One-exercise and short workouts retain existing behavior.
- Activating the action opens the existing collapsed, name-only sortable editor without changing routes or exercise-associated log values. The action is hidden in editing mode.
- Done applies the edited sequence and returns to the normal view. Cancel without edits exits immediately; Cancel after edits asks for confirmation, and only confirmation restores the original sequence. Declining continues editing.
- Modals retain their focus handling and appear above the control. After activation hides the button, focus moves to the first reorder handle, or Done if there is no handle.

## Verification

At 0%, 50%, and 100% scroll in a 50-exercise workout, measure the button within the unobscured viewport, check hit-testing, desktop content clearance and full mobile content width, and activate it by measured coordinates without automatic scrolling. Verify mobile fields remain reachable, plus the matrix, themes, accessible semantics, keyboard activation, current editor behaviors and the 20-position scroll and editor-transition performance budgets specified in [plan.md](../plan.md).

The automated checks and remaining manual verification are recorded in [tasks.md](../tasks.md); the 640×480 CSS-pixel viewport is an approximation, not an actual 200% zoom test.
