# UI Contract: Previous Exercise Performance in Active Workout

**Feature**: `008-workout-exercise-history`  
**Date**: 2026-05-06

## Overview

This contract documents the change to the active session exercise card. A new "previous performance" section is added to each exercise row. All other UI elements in the active session view (title, save/cancel buttons, discard modal) are unchanged.

---

## Active Session Exercise Card — Updated Structure

### HTML Structure (per exercise)

```html
<div class="active-session__exercise-item" data-exercise-id="{exerciseId}">

  <!-- Exercise name (unchanged) -->
  <div class="active-session__exercise-name">{exercise.name}</div>

  <!-- Target weight, if set on the planned workout (unchanged) -->
  <div class="active-session__exercise-targets">@ {targetWeight} KG</div>  <!-- only if targetWeight set -->

  <!-- NEW: Previous performance section -->
  <div class="active-session__exercise-previous" id="previous-{exerciseId}">
    <!-- Content determined by state — see State Variants below -->
  </div>

  <!-- Input fields (unchanged) -->
  <div class="active-session__exercise-inputs">
    <div class="active-session__input-group">
      <label class="active-session__input-label" for="weight-{exerciseId}">Weight (KG)</label>
      <input class="active-session__input" type="number" id="weight-{exerciseId}" ... />
    </div>
    <div class="active-session__input-group active-session__effort-group">
      <label class="active-session__input-label" for="effort-{exerciseId}">Effort</label>
      <span class="active-session__effort-value" id="effort-value-{exerciseId}">Not rated</span>
      <input class="active-session__effort-slider" type="range" id="effort-{exerciseId}" ... />
      <span class="active-session__effort-band" id="effort-band-{exerciseId}"></span>
    </div>
  </div>

</div>
```

---

## State Variants for `active-session__exercise-previous`

### State 1: First Session (no previous data for this planned workout)

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-empty">First session — no previous data</span>
</div>
```

### State 2: Previous Data Available — Weight and Effort

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">80 KG · 7 — Hard</span>
</div>
```

### State 3: Previous Data Available — Weight Only (effort was null)

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">80 KG</span>
</div>
```

### State 4: Previous Data Available — Effort Only (weight was null)

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">7 — Hard</span>
</div>
```

### State 5: Previous Data Available — Neither Weight nor Effort Recorded

This occurs when the exercise exists in the most recent session's `logged_exercise` rows but both `loggedWeight` and `effort` were null (user saved without entering anything). Treated identically to State 1 (no useful reference data).

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-empty">First session — no previous data</span>
</div>
```

### State 6: Error (fetch failed)

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-error">Could not load previous data</span>
</div>
```

---

## CSS Classes

All new classes follow BEM: `active-session__exercise-previous` is the block element, sub-elements use `active-session__previous-*` naming.

| Class | Description |
|-------|-------------|
| `active-session__exercise-previous` | Container for previous performance section within each exercise card |
| `active-session__previous-label` | Read-only label text ("Last time:") — uses `--color-text-muted` |
| `active-session__previous-value` | The actual previous values — uses `--color-text-muted`; slightly smaller than the input label text |
| `active-session__previous-empty` | "First session" message — uses `--color-text-muted`; italic |
| `active-session__previous-error` | Error message — uses `--color-error` (matches existing error colour in the app) |

---

## Visual Design

- The entire `active-session__exercise-previous` section is visually muted (lighter than primary text) using `--color-text-muted`.
- Font size: `0.85rem` — secondary information, clearly smaller than the exercise name and input labels.
- No border, no card — rendered as plain inline text between the target weight (if shown) and the input fields.
- Layout is not a table; it is a single line of text per exercise, collapsing gracefully on narrow viewports.

---

## Interaction Rules

- The previous data display is **entirely read-only**. No click, focus, or edit interaction is defined on this section.
- The previous data section does NOT auto-populate the input fields — the user enters their current session values from scratch (may choose to match, exceed, or adjust the previous values).
- The previous data section has no `tabindex` and is not in the tab order.

---

## Accessibility

- `active-session__exercise-previous` is a `<div>` with no role — it is presentational only and does not require ARIA.
- Error state (`active-session__previous-error`) is static text rendered at page load (after `Promise.allSettled` resolves); it does not need `aria-live` because it is present in the DOM from initial render, not injected later.
- The existing `role="form"` on `#session-exercises` and `aria-label` on each input remain unchanged.

---

## TypeScript Interface

```typescript
// New interface in active-session.ts
interface PreviousExerciseData {
  readonly exerciseId: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
}

interface PreviousPerformance {
  readonly hasPreviousSession: boolean;
  readonly completedAt: string | null;
  readonly exercises: PreviousExerciseData[];
}
```

---

## Unchanged UI Elements

The following active session UI elements are not modified by this feature:
- Session title bar
- Save Workout button and saving state
- Cancel button
- Discard modal (backdrop, confirm, cancel)
- Weight input (number field, KG label, validation)
- Effort slider (range, value display, band label)
- Error and API error message areas
