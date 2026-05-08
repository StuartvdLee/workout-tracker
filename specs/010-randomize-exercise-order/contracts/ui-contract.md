# UI Contract: Pre-Start Modal — Randomize Exercise Order

**Feature**: `010-randomize-exercise-order`  
**Date**: 2026-05-08

---

## Overview

This document specifies the HTML structure, CSS BEM classes, ARIA attributes, interaction behaviour, and state machine for the new **pre-start modal** introduced in `workouts.ts`.

---

## Trigger

The pre-start modal opens when the user clicks the **"Start"** button on any workout card in the workout list. The direct navigation that previously occurred (`navigate('/active-session?id=...')`) now happens only when the user confirms start inside the modal.

The modal is also present on the **home page** (`home.ts`), where a workout is started via a dropdown form. On the home page, clicking "Start Workout" on the form first fetches `GET /api/workouts/{id}` to retrieve the exercise list, then opens the same pre-start modal. If the fetch fails, it falls back to direct navigation. On cancel, focus returns to the home page's "Start Workout" submit button rather than a workout card button.

---

## HTML Structure

The modal is rendered as part of `workouts.ts`'s static `render()` output, alongside the existing edit modal and delete modal:

```html
<!-- Pre-start modal (rendered inline, hidden by default) -->
<div class="prestart-modal-backdrop" id="workout-prestart-backdrop" style="display:none;">
  <div
    class="prestart-modal"
    role="dialog"
    aria-modal="true"
    aria-labelledby="prestart-modal-title"
  >
    <h2 class="prestart-modal__title" id="prestart-modal-title">Start Workout</h2>

    <!-- Shuffle toggle (hidden when exercise count < 2) -->
    <div class="prestart-modal__shuffle" id="prestart-shuffle-row">
      <label class="prestart-modal__shuffle-label" for="prestart-shuffle-toggle">
        Randomise order
      </label>
      <button
        class="prestart-modal__shuffle-btn"
        type="button"
        id="prestart-shuffle-toggle"
        role="switch"
        aria-checked="false"
      >
        <span class="sr-only">Randomise order</span>
      </button>
    </div>

    <!-- Exercise list preview -->
    <ol class="prestart-modal__exercise-list" id="prestart-exercise-list" aria-label="Exercise order preview">
      <!-- Populated dynamically -->
    </ol>

    <!-- Re-shuffle button (shown only when shuffle is on) -->
    <button
      class="prestart-modal__reshuffle-btn"
      type="button"
      id="prestart-reshuffle"
      style="display:none;"
    >
      Re-shuffle
    </button>

    <div class="prestart-modal__actions">
      <button class="prestart-modal__start-btn" type="button" id="prestart-start">
        Start Workout
      </button>
      <button class="prestart-modal__cancel-btn" type="button" id="prestart-cancel">
        Cancel
      </button>
    </div>
  </div>
</div>
```

---

## BEM Class Reference

| Class                              | Element                              | Notes                                              |
|------------------------------------|--------------------------------------|----------------------------------------------------|
| `prestart-modal-backdrop`          | Full-screen overlay                  | Same dimming as `delete-modal-backdrop`            |
| `prestart-modal`                   | Modal card                           | Same card styling as `delete-modal`, `edit-modal`  |
| `prestart-modal__title`            | `<h2>` heading                       |                                                    |
| `prestart-modal__shuffle`          | Row containing label + toggle button |                                                    |
| `prestart-modal__shuffle-label`    | Label for the toggle                 |                                                    |
| `prestart-modal__shuffle-btn`      | Toggle button (`role="switch"`)      | `aria-checked` flips on click                      |
| `prestart-modal__exercise-list`    | `<ol>` preview list                  | Items are `<li>` with exercise name text           |
| `prestart-modal__reshuffle-btn`    | "Re-shuffle" button                  | Shown only when `aria-checked="true"` on toggle    |
| `prestart-modal__actions`          | Action button row                    |                                                    |
| `prestart-modal__start-btn`        | "Start Workout" confirm button       | Primary action                                     |
| `prestart-modal__cancel-btn`       | "Cancel" button                      | Closes modal, discards shuffle state               |

---

## State Machine

### Modal states

| State         | Condition                          | Toggle visible | Re-shuffle btn visible | Exercise list order |
|---------------|------------------------------------|----------------|------------------------|---------------------|
| **Closed**    | `display:none` on backdrop         | —              | —                      | —                   |
| **Open (off)**| 2+ exercises; shuffle = off        | YES            | NO                     | Template order      |
| **Open (on)** | 2+ exercises; shuffle = on         | YES            | YES                    | Shuffled order      |
| **Open (1ex)**| Exactly 1 exercise                 | NO             | NO                     | Template order      |

### Toggle interaction

| Current `aria-checked` | Action      | New `aria-checked` | Exercise list      | Re-shuffle btn |
|------------------------|-------------|--------------------|--------------------|----------------|
| `"false"`              | Click       | `"true"`           | Shuffled (new)     | Shown          |
| `"true"`               | Click       | `"false"`          | Original order     | Hidden         |

### Re-shuffle interaction

| Condition           | Action        | Effect                                  |
|---------------------|---------------|-----------------------------------------|
| Shuffle is on       | Click         | New shuffle applied; list re-rendered   |
| Shuffle is off      | (not visible) | N/A                                     |

---

## Interaction Behaviour

### Opening the modal

1. User clicks "Start" on a workout card.
2. `openPreStartModal(workout)` is called with the selected `Workout` object.
3. The modal title is set to `"Start Workout"` (no workout name in title — the workout name is implicit context from the list card).
4. The exercise list is rendered in template order (shuffle off by default).
5. If `workout.exercises.length < 2`, the shuffle row is hidden.
6. Focus is moved to the "Start Workout" button.
7. The backdrop is shown (`display: ""`).

### Closing the modal (Cancel / backdrop click / Escape)

1. `closePreStartModal()` is called.
2. Shuffle state is reset: toggle → `aria-checked="false"`, exercise list → template order.
3. Current shuffled order (if any) is discarded.
4. Focus returns to the "Start" button that opened the modal.
5. The backdrop is hidden (`display: "none"`).

### Starting the workout

1. User clicks "Start Workout".
2. If shuffle is **off**: `navigate('/active-session?id=<workoutId>')` — identical to the previous direct navigation.
3. If shuffle is **on**: `navigate('/active-session?id=<workoutId>&order=<guid1>,<guid2>,...')` where the comma-separated IDs reflect the currently displayed shuffled order.

---

## Focus Management

- **On open**: Focus moves to `#prestart-start` (primary action).
- **Focus trap**: Tab/Shift-Tab cycles within the modal's focusable elements only.
- **On close**: Focus returns to the "Start" button (`[data-workout-id="<id>"].workout-list__start-btn`) that triggered the modal.
- **Escape key**: Closes the modal (same as Cancel).

Focusable elements within the modal (in tab order):
1. `#prestart-shuffle-toggle` (when visible)
2. `#prestart-reshuffle` (when visible)
3. `#prestart-start`
4. `#prestart-cancel`

---

## Copy (Text Labels)

| Element                        | Text              | Notes                                     |
|--------------------------------|-------------------|-------------------------------------------|
| Modal title                    | `Start Workout`   |                                           |
| Shuffle label                  | `Randomise order` | British English; consistent with spec     |
| Toggle button (`aria-checked=false`) | *(toggle visual, sr-only text: "Randomise order")* | |
| Re-shuffle button              | `Re-shuffle`      | Hyphenated; consistent with spec          |
| Start confirm button           | `Start Workout`   |                                           |
| Cancel button                  | `Cancel`          |                                           |

---

## CSS Custom Properties Used

Consistent with existing modals (`delete-modal`, `edit-modal`, `discard-modal`):

| Property               | Usage                        |
|------------------------|------------------------------|
| `--color-surface`      | Modal card background        |
| `--color-primary`      | Toggle "on" state, start btn |
| `--color-text`         | Body text                    |
| `--color-text-muted`   | Exercise list items          |
| `--color-border`       | Toggle track border          |

---

## URL Parameter Contract (active-session.ts)

When shuffle is active, the navigation URL includes `?order=<guid1>,<guid2>,...`:

- IDs are comma-separated, no spaces.
- Order corresponds exactly to the visual order shown in the modal's exercise list at the moment the user clicks "Start Workout".
- `active-session.ts` reads this parameter via `params.get("order")` and reorders `workout.exercises` accordingly before rendering.
- If `order` is absent or any listed ID is not found in `workout.exercises`, `active-session.ts` falls back to the API's natural sort order silently.
