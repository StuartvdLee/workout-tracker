# UI Contract: Current Workout Exercise Order Editing

## Overview

Adds an order-editing mode to the current workout (`active-session.ts`) that collapses exercises into a name-only sortable list. The sortable interaction must match the existing "Edit Workout" selected-exercises reorder behavior.

## Affected Surface

| Surface | Element / Area | File |
|---|---|---|
| Current workout header | `.active-session__header` | `active-session.ts` |
| Current workout exercise list | `#session-exercises` | `active-session.ts` |
| Shared sortable behavior | sortable list helper | `sortable-list.ts`, `workouts.ts`, `active-session.ts` |
| Styles | active-session order mode + existing sortable classes | `styles.css` |

## Header and Footer Actions

Normal mode:

```html
<div class="active-session__header">
  <h1 class="active-session__title" id="session-title">Push Day</h1>
  <button class="active-session__edit-order-btn" type="button" id="session-edit-order">
    Edit order
  </button>
</div>
```

Order-editing mode:

```html
<div class="active-session__header">
  <h1 class="active-session__title" id="session-title">Push Day</h1>
  <button class="active-session__edit-order-btn" type="button" id="session-edit-order" style="display:none;">
    Edit order
  </button>
</div>
<div class="active-session__actions">
  <button class="active-session__save-btn" type="button" id="session-save">
    Done
  </button>
  <button class="active-session__cancel-btn" type="button" id="session-cancel">
    Cancel
  </button>
</div>
```

Rules:

- The "Edit order" button is placed in the top-right area of the current workout header in normal mode.
- Selecting "Edit order" switches to order-editing mode without navigation.
- While order-editing mode is active, `#session-edit-order` is hidden and the footer `#session-save` button text changes to:

```html
<button class="active-session__save-btn" type="button" id="session-save">
  Done
</button>
```

- Selecting the footer "Done" action applies the in-memory exercise order and exits order-editing mode, restoring the normal logging view without saving the workout to the server.
- The "Cancel" button (`#session-cancel`) is also available in order-editing mode:
  - If no order changes have been made: exits order-editing mode immediately (equivalent to Done).
  - If order changes have been made: opens the discard confirmation modal with order-specific content (see below).
- For zero exercises, the button may be hidden or disabled.
- For one exercise, the button may enter name-only mode but no drag handle is rendered.

## Collapsed Sortable List

Order-editing mode replaces normal exercise cards with:

```html
<div class="sr-only" aria-live="polite" aria-atomic="true" id="session-reorder-announce"></div>
<ul class="workout-selected__list active-session__order-list" id="session-order-list">
  <li
    class="workout-selected__item active-session__order-item"
    draggable="true"
    aria-roledescription="sortable item"
    data-exercise-id="{exerciseId}"
    data-index="{0-based index}"
  >
    <button
      class="workout-selected__drag-handle"
      type="button"
      aria-label="Drag to reorder {Exercise Name}"
      aria-pressed="false"
    >
      <!-- same six-dot grip SVG used by Edit Workout -->
    </button>
    <span class="workout-selected__name active-session__order-name">{Exercise Name}</span>
  </li>
</ul>
```

Rules:

- Only exercise names and reorder affordances are visible.
- Weight controls, effort sliders, target text, previous-performance text, and exercise-entry controls are not rendered in this mode.
- The normal Save Workout / Cancel actions remain outside the exercise rows; while order-editing mode is active, the primary action is relabeled "Done" and exits the mode instead of saving.
- Reordered rows update the in-memory active workout order immediately.

## Discard Confirmation Modal (Order-Editing Mode)

When the user presses "Cancel" while in order-editing mode and has made order changes, the existing discard modal is re-used with order-specific content:

```html
<div class="discard-modal" ...>
  <h2 class="discard-modal__title" id="discard-title">Discard changes?</h2>
  <p class="discard-modal__desc" id="discard-desc">You have unsaved order changes. Are you sure you want to discard them?</p>
  <div class="discard-modal__actions">
    <button class="discard-modal__discard" type="button" id="discard-confirm">Discard</button>
    <button class="discard-modal__continue" type="button" id="discard-cancel">Keep editing</button>
  </div>
</div>
```

Rules:

- Selecting "Discard" restores the exercise order to what it was when "Edit order" was selected, then exits order-editing mode.
- Selecting "Keep editing" closes the modal and returns the user to order-editing mode with their changes intact.
- In normal (non-order-editing) discard mode the continue button reads "Continue workout"; in order-editing mode it reads "Keep editing".
- The modal is not shown if no order changes were made; "Cancel" exits order-editing mode immediately in that case.

## Reorder Interaction

The shared sortable behavior must match the existing "Edit Workout" screen:

| Input | Required behavior |
|---|---|
| Mouse drag/drop | Drag handle/item moves rows live in the DOM and drops into the target position |
| Touch drag/drop | Touch clone follows the finger; original row fades; rows move live under the finger |
| Keyboard | Space picks up; ArrowUp/ArrowDown moves; Enter/Space drops; Escape cancels |
| Screen reader | Live region announces moves, e.g. `Exercise moved to position 2 of 4` |

## Visual States

| State | Visual / Behavior |
|---|---|
| Normal mode | Full active-session exercise cards with name, target/previous data, weight, and effort |
| Order-editing mode | Name-only sortable rows |
| Dragging | Existing `.workout-selected__item--dragging` opacity and `body.is-dragging` cursor behavior |
| Single exercise | One name-only row, no drag handle |
| Empty workout | Existing empty/loading/error state remains clear; no broken sortable list |
| Reorder cancelled | Original order is restored by the shared keyboard/Escape or drag-cancel behavior |

## Accessibility

- Header button has visible text and keyboard activation.
- Sortable rows reuse `aria-roledescription="sortable item"`.
- Drag handles use `aria-label="Drag to reorder {Exercise Name}"`.
- Keyboard pickup uses `aria-pressed`.
- Reorder result announcements use `.sr-only[aria-live="polite"][aria-atomic="true"]`.
- Focus remains usable after every re-render; after keyboard movement, focus returns to the moved handle.

## Data Integrity

- `logEntries` remain keyed by `exerciseId`.
- Re-rendering normal mode after order editing must restore weight and effort state for the correct exercise.
- No exercise may be added, removed, or duplicated by reordering.
