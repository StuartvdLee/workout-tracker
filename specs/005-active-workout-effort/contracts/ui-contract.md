# UI Contract: Active Workout UI — Effort Tracking

**Feature**: `005-active-workout-effort`  
**Date**: 2026-04-27

## Overview

Documents the HTML/CSS/ARIA changes to the active workout session view (`active-session.ts`) and workout history view (`history.ts`). All other pages are unchanged.

---

## Active Session View (`/active-session?id=...`)

### Exercise Card — Updated Structure

Each exercise in the session renders as a `div.active-session__exercise-item`. The reps input group is removed. The weight group gets a KG label. An effort group is added.

```html
<div class="active-session__exercise-item" data-exercise-id="{exerciseId}">
  <div class="active-session__exercise-name">{Exercise Name}</div>

  <!-- Optional target info (unchanged) -->
  <!-- <div class="active-session__exercise-targets">@ {targetWeight}</div> -->

  <div class="active-session__exercise-inputs">

    <!-- REMOVED: reps input group (no longer present) -->

    <!-- Weight (KG) -->
    <div class="active-session__input-group">
      <label class="active-session__input-label" for="weight-{exerciseId}">
        Weight (KG)
      </label>
      <input
        class="active-session__input"
        type="number"
        id="weight-{exerciseId}"
        placeholder="e.g. 80"
        min="0"
        step="0.5"
        aria-label="Weight in KG for {Exercise Name}"
      />
    </div>

    <!-- Effort Slider -->
    <div class="active-session__input-group active-session__input-group--effort">
      <label class="active-session__input-label" for="effort-{exerciseId}">
        Effort
      </label>
      <div class="active-session__effort-slider-wrap">
        <input
          class="active-session__effort-slider"
          type="range"
          id="effort-{exerciseId}"
          min="1"
          max="10"
          step="1"
          aria-label="Effort rating for {Exercise Name}"
          aria-valuemin="1"
          aria-valuemax="10"
          aria-valuetext="Not rated"
          data-touched="false"
        />
        <div
          class="active-session__effort-label"
          id="effort-label-{exerciseId}"
          aria-live="polite"
        >
          Rate effort
        </div>
      </div>
    </div>

    <!-- Notes (unchanged) -->
    <div class="active-session__input-group">
      <label class="active-session__input-label" for="notes-{exerciseId}">Notes</label>
      <input
        class="active-session__notes-input"
        type="text"
        id="notes-{exerciseId}"
        placeholder="Notes (optional)"
        aria-label="Notes for {Exercise Name}"
      />
    </div>

  </div>
</div>
```

### Effort Slider Behaviour

> **Note on `aria-valuenow`**: HTML `<input type="range">` always has an implicit DOM value (browser default = midpoint = 5 for range 1–10). The untouched state is tracked via `data-touched="false"`, not by the DOM value. On first user interaction, JS sets `aria-valuenow` and updates `aria-valuetext` to the selected label. Before interaction, `aria-valuenow` must be **removed** via `removeAttribute("aria-valuenow")` when the slider is first rendered, so screen readers announce "Not rated" rather than a misleading "5".

| State            | `data-touched` | `aria-valuenow`   | `aria-valuetext`  | Label text       | Submitted value |
|------------------|----------------|-------------------|-------------------|------------------|-----------------|
| Not interacted   | `false`        | *(absent)*        | `"Not rated"`     | "Rate effort"    | `null`          |
| Value 1–3        | `true`         | `1`–`3`           | `"1, Easy"` etc.  | "Easy"           | 1–3             |
| Value 4–6        | `true`         | `4`–`6`           | `"4, Moderate"` etc. | "Moderate"    | 4–6             |
| Value 7–8        | `true`         | `7`–`8`           | `"7, Hard"` etc.  | "Hard"           | 7–8             |
| Value 9–10       | `true`         | `9`–`10`          | `"9, All Out"` etc. | "All Out"      | 9–10            |

### CSS Classes (new additions to `styles.css`)

```css
/* Effort slider container */
.active-session__input-group--effort { /* layout modifier */ }

/* Slider wrap: positions slider + label vertically */
.active-session__effort-slider-wrap { display: flex; flex-direction: column; gap: var(--space-xs); }

/* The range input itself */
.active-session__effort-slider { width: 100%; cursor: pointer; }

/* The live label beneath the slider */
.active-session__effort-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  min-height: 1.2em; /* prevent layout shift when text appears */
  text-align: center;
}

/* Intensity band colour modifiers (applied by JS) */
.active-session__effort-label--easy     { color: var(--color-success, green); }
.active-session__effort-label--moderate { color: var(--color-warning, orange); }
.active-session__effort-label--hard     { color: var(--color-danger, red); }
.active-session__effort-label--all-out  { color: var(--color-danger-dark, darkred); }
```

### Weight Input Type Change

The weight input changes from `type="text"` to `type="number"` (with `step="0.5"`) to enable numeric keyboard on mobile and allow basic input validation. The underlying `LoggedWeight` remains a `string?` in the API and DB (the value is sent as a string via `.toString()`).

---

## History View (`/history`)

### Exercise Row — Updated Rendering

The `renderSession` function in `history.ts` changes as follows:

**Before** (feature 004):
```
{loggedReps} reps @ {loggedWeight} — {notes}
```

**After** (feature 005):
```
{loggedWeight} KG  ·  {effort} — {label}  ·  {notes}
```

Each segment is only shown when the value is non-null. All combinations are valid:
- Only weight: `80 KG`
- Only effort: `7 — Hard`
- Weight + effort: `80 KG  ·  7 — Hard`
- Weight + effort + notes: `80 KG  ·  7 — Hard  ·  Great set`
- Nothing (all null): *(empty span — no text shown)*

### HTML Structure (history exercise row)

```html
<div class="history-session__exercise">
  <span class="history-session__exercise-name">{Exercise Name}</span>
  <span class="history-session__exercise-data">{rendered data string}</span>
</div>
```

The `history-session__exercise-data` span content is built from the parts array using `" · "` as separator, consistent with the punctuation style in the existing codebase.

### Interface Change (`history.ts`)

```typescript
// BEFORE (feature 004)
interface LoggedExercise {
  readonly loggedExerciseId: string;
  readonly exerciseId: string;
  readonly exerciseName: string;
  readonly loggedReps: number | null;   // REMOVED
  readonly loggedWeight: string | null;
  readonly notes: string | null;
}

// AFTER (feature 005)
interface LoggedExercise {
  readonly loggedExerciseId: string;
  readonly exerciseId: string;
  readonly exerciseName: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;       // NEW
  readonly notes: string | null;
}
```

---

## Accessibility Notes

- The effort slider uses `aria-valuetext` to expose the intensity label to screen readers (e.g., "7, Hard") rather than just the numeric value.
- In the untouched state, `aria-valuenow` is absent (removed via `removeAttribute`) and `aria-valuetext` is `"Not rated"`. On first interaction, JS sets `aria-valuenow` to the current value and updates `aria-valuetext` to `"{value}, {label}"`.
- The effort label `div` has `aria-live="polite"` so screen reader users hear the label update as the slider moves.
- The weight field `aria-label` is updated to include "in KG" for clarity.
- All interactive elements maintain the existing `--min-touch-target` (44px) minimum.

---

## States

### Active Session — Save States

| State      | Save button text | Save button disabled | API error shown |
|------------|-----------------|----------------------|-----------------|
| Default    | "Save Workout"  | No                   | No              |
| Saving     | "Saving..."     | Yes (aria-disabled)  | No              |
| Success    | — (navigates)   | —                    | No              |
| Error      | "Save Workout"  | No                   | Yes             |

Unchanged from feature 004.
