# UI Contract: Reorder Exercises in a Workout

**Feature**: `006-reorder-exercises`  
**Date**: 2026-05-02

## Overview

Documents the HTML/CSS/ARIA changes to `workouts.ts` (create form and edit modal). The changes are additive: each exercise item in the selected-exercises lists gains a drag handle and keyboard reorder capability. No other pages are affected.

---

## Affected Surfaces

| Surface | Element ID | File |
|---|---|---|
| Create workout — selected exercises | `#workout-selected-list` | `workouts.ts` |
| Edit workout modal — selected exercises | `#edit-selected-list` | `workouts.ts` |

---

## Selected Exercise Item — Updated Structure

Each exercise in the selected-exercises `<ul>` currently renders as:

```html
<!-- CURRENT (no reorder affordance) -->
<li class="workout-selected__item">
  <span class="workout-selected__name">Bench Press</span>
  <button class="workout-form__remove-btn" type="button" aria-label="Remove Bench Press">✕</button>
</li>
```

The updated structure adds a drag handle button on the left:

```html
<!-- NEW (with drag handle) -->
<li
  class="workout-selected__item"
  draggable="true"
  aria-roledescription="sortable item"
  data-exercise-id="{exerciseId}"
  data-index="{0-based index}"
>
  <button
    class="workout-selected__drag-handle"
    type="button"
    aria-label="Drag to reorder {Exercise Name}"
    tabindex="0"
  >
    <!-- Six-dot grip SVG icon -->
    <svg
      width="16" height="16"
      viewBox="0 0 16 16"
      fill="currentColor"
      aria-hidden="true"
      focusable="false"
    >
      <circle cx="5" cy="3" r="1.5"/>
      <circle cx="5" cy="8" r="1.5"/>
      <circle cx="5" cy="13" r="1.5"/>
      <circle cx="11" cy="3" r="1.5"/>
      <circle cx="11" cy="8" r="1.5"/>
      <circle cx="11" cy="13" r="1.5"/>
    </svg>
  </button>

  <span class="workout-selected__name">{Exercise Name}</span>

  <button
    class="workout-form__remove-btn"
    type="button"
    aria-label="Remove {Exercise Name}"
  >
    <!-- existing ✕ SVG, unchanged -->
  </button>
</li>
```

When the list contains **fewer than two exercises**, `draggable="true"` is omitted from the `<li>` and the `.workout-selected__drag-handle` button is hidden (`display: none` or not rendered), to avoid a no-op interaction.

---

## Drag Handle — CSS

```css
/* Drag handle button */
.workout-selected__drag-handle {
  /* Layout */
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;

  /* Sizing — minimum touch target 44×44px */
  width: 44px;
  height: 44px;
  padding: 0;

  /* Appearance */
  background: none;
  border: none;
  color: var(--color-text-muted);  /* muted/secondary colour, consistent with remove btn */
  cursor: grab;
  border-radius: var(--border-radius-sm);
}

.workout-selected__drag-handle:hover,
.workout-selected__drag-handle:focus-visible {
  color: var(--color-text-primary);
  background-color: var(--color-surface-hover);
}

/* Whilst any drag is in progress */
body.is-dragging .workout-selected__drag-handle {
  cursor: grabbing;
}

/* Dragged item */
.workout-selected__item--dragging {
  opacity: 0.4;
}

/* Drop target indicator */
/* Not used — live DOM reordering during dragover replaces the static border.
   Items shift position in real-time as the user drags, making the drop
   destination immediately visible without a separate indicator state. */
```

### Updated List Item Layout

```css
.workout-selected__item {
  display: flex;
  align-items: center;
  gap: 8px;           /* existing gap between name and remove btn */
  /* drag handle sits at the far left — no layout change to existing elements */
}
```

---

## Accessibility

### ARIA Live Region

A visually hidden live region is added immediately before the `<ul>` in both the create form and edit modal. It announces the result of every reorder to screen readers:

```html
<div
  class="sr-only"
  aria-live="polite"
  aria-atomic="true"
  id="workout-reorder-announce"
></div>
```

After each successful reorder, the live region text is set to:
`"[Exercise Name] moved to position [N] of [total]."`
The text is overwritten on the next move; it is not explicitly cleared.

### Keyboard Interaction on the Drag Handle Button

| Key | Action |
|---|---|
| `Space` | Toggle "picked up" state (`aria-pressed="true"`). While picked up, ↑/↓ move the item. |
| `↑` (Arrow Up) | Move the picked-up item one position earlier in the list. |
| `↓` (Arrow Down) | Move the picked-up item one position later in the list. |
| `Enter` or `Space` (second press) | Drop/confirm the item at its current position. |
| `Escape` | Cancel — return item to its original position before pick-up. |

Focus remains on the drag handle button throughout keyboard reordering so the user can continue pressing ↑/↓.

### ARIA Attributes

| Attribute | Element | Value |
|---|---|---|
| `aria-roledescription` | `<li>` | `"sortable item"` |
| `aria-label` | drag handle `<button>` | `"Drag to reorder [Exercise Name]"` |
| `aria-pressed` | drag handle `<button>` | `"false"` at rest; `"true"` while keyboard-picked-up |
| `aria-live` | announce region | `"polite"` |
| `aria-atomic` | announce region | `"true"` |
| `aria-hidden` | drag handle SVG | `"true"` |
| `focusable` | drag handle SVG | `"false"` |

---

## States

### During Mouse/Touch Drag

| State | Visual |
|---|---|
| Item being dragged | `.workout-selected__item--dragging` — `opacity: 0.4` |
| All other items | Shift live in the DOM as the user moves the dragged item over them — no separate drop-target indicator |
| Body while drag active | `body.is-dragging` — `cursor: grabbing` on drag handles |

### Single-Exercise List (drag disabled)

When `exercises.length < 2`:
- `draggable="true"` is NOT set on `<li>` elements.
- `.workout-selected__drag-handle` buttons are not rendered.
- The list layout is identical to the current design.

### Loading / Saving

No change from existing behaviour. The form submit flow (loading spinner on button, error display) is unchanged.

### Error State (save failure after reorder)

If the API call fails after a reorder, the existing error display (`#workout-api-error` / `#edit-workout-api-error`) shows the error message. The exercise list **retains the reordered state** so the user can retry without losing their changes.

---

## Touch Behaviour

On touch devices:
- `touchstart` on the `<ul>` container begins a touch drag (passive listener — no `preventDefault()` needed at start).
- A **fixed-position clone** of the `<li>` is created and follows the finger; the original `<li>` is faded (`.workout-selected__item--dragging`).
- `touchmove` (non-passive): prevents page scroll via `preventDefault()`; repositions the clone; hides clone with `visibility: hidden` then calls `document.elementFromPoint()` to identify the target `<li>` beneath the finger; live-moves the dragged `<li>` in the DOM using the same midpoint logic as `dragover`.
- `touchend`: reads the final DOM index via `getLiveDomIndex`; calls `reorder(fromIndex, finalIndex)`; removes the clone; removes all visual state.

---

## Files Changed

| File | Change |
|---|---|
| `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` | Primary change: `Set→Array`, drag-and-drop logic, keyboard reorder, ARIA live region |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | Add `.workout-selected__drag-handle`, `.workout-selected__item--dragging`, `body.is-dragging`, `.sr-only` (if not already present) |

No other files are modified.
