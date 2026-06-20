# UI Contract: Randomise Exercise Order UX Simplification

**Feature**: `011-randomise-exercise-order`  
**Date**: 2026-05-09

---

## 1. Homepage — Inline Randomise Toggle

### HTML Structure

Inserted inside `#workout-form`, between the select group (`#workout-select`) and the error `<div>`:

```html
<div class="workout-form__randomise" id="home-randomise-row" style="display:none;">
  <label class="workout-form__randomise-label" for="home-randomise-toggle">
    Randomise exercise order
  </label>
  <button
    class="workout-form__randomise-btn"
    type="button"
    id="home-randomise-toggle"
    role="switch"
    aria-checked="false"
  ><span class="sr-only">Randomise exercise order</span></button>
</div>
```

### States

| State | `aria-checked` | `style.display` (row) | Description |
|-------|----------------|-----------------------|-------------|
| Hidden | `"false"` | `"none"` | Default; shown only when selected workout has ≥ 2 exercises |
| Visible / Off | `"false"` | `""` | Toggle visible, inactive |
| Visible / On | `"true"` | `""` | Toggle visible, active (blue fill + knob right) |

### Behaviour

- Row is **hidden** on page load and whenever the workout select resets.
- Row becomes **visible** when the user selects a workout with `exerciseCount >= 2`.
- Row becomes **hidden** again if the user selects a workout with `exerciseCount < 2`.
- Toggle `aria-checked` resets to `"false"` whenever the row is hidden.
- On "Start Workout" submit:
  - Toggle **off** → `navigate('/active-session?id=<workoutId>')` (no API call).
  - Toggle **on** → `fetch('/api/workouts/<workoutId>')` → shuffle exercises → `navigate('/active-session?id=<workoutId>&order=<comma-separated-ids>')`.
  - API call failure when toggle on → fallback to direct navigation without `?order=`.

### CSS Classes

| Class | Element | Description |
|-------|---------|-------------|
| `workout-form__randomise` | Row `<div>` | Flex row, `justify-content: space-between`, `align-items: center` |
| `workout-form__randomise-label` | `<label>` | Same typography as other `workout-form__*` labels |
| `workout-form__randomise-btn` | `<button>` | iOS-style toggle: 2.75 rem × 1.5 rem, pill border-radius, animated knob |
| `workout-form__randomise-btn[aria-checked="true"]` | Active state | Background changes to `--color-primary`; knob translates right |

---

## 2. Workouts Page — Simplified Randomise Modal

### HTML Structure

Replaces the existing `#workout-prestart-backdrop` content in `workouts.ts`:

```html
<div class="prestart-modal-backdrop" id="workout-prestart-backdrop" style="display:none;">
  <div class="prestart-modal" role="dialog" aria-modal="true" aria-labelledby="prestart-modal-title">
    <h2 class="prestart-modal__title" id="prestart-modal-title">Randomise exercise order?</h2>
    <div class="prestart-modal__actions">
      <button class="prestart-modal__no-btn" type="button" id="prestart-no">No</button>
      <button class="prestart-modal__yes-btn" type="button" id="prestart-yes">Yes</button>
    </div>
  </div>
</div>
```

### States

| State | `style.display` (backdrop) | Focus target on open |
|-------|----------------------------|----------------------|
| Closed | `"none"` | — |
| Open | `""` | `#prestart-yes` button |

### Behaviour

- Modal opens when the user clicks a workout's Start button.
- Modal is **skipped** (navigate directly) when the workout has fewer than 2 exercises.
- **Yes** → shuffle `prestartWorkout.exercises` in memory → `navigate('/active-session?id=<workoutId>&order=<comma-separated-ids>')`.
- **No** → `navigate('/active-session?id=<workoutId>')`.
- **Escape** → close modal, return focus to trigger button, do not start.
- **Backdrop click** → close modal, return focus to trigger button, do not start.
- Tab key is trapped within the modal (via `trapModalTabKey` from `prestart-modal.ts`).
- On close, focus returns to `prestartTriggerBtn` (the Start button that opened the modal).

### CSS Classes

| Class | Element | Description |
|-------|---------|-------------|
| `prestart-modal__no-btn` | No `<button>` | Secondary button — same visual style as existing `prestart-modal__cancel-btn`; `flex: 1` for equal width |
| `prestart-modal__yes-btn` | Yes `<button>` | Primary button — same visual style as existing `prestart-modal__start-btn`; `flex: 1` for equal width |

Existing `prestart-modal-backdrop`, `prestart-modal`, `prestart-modal__title`, and `prestart-modal__actions` styles are **unchanged**.

---

## 3. Removed Elements

The following HTML elements and their associated CSS classes are **removed** and must not appear in any page or flow after this feature ships:

| Element | CSS class(es) | Present in |
|---------|--------------|------------|
| Re-shuffle button | `prestart-modal__reshuffle-btn` | `home.ts`, `workouts.ts` |
| Exercise preview list | `prestart-modal__exercise-list`, `prestart-modal__exercise-item`, `prestart-modal__exercise-empty` | `home.ts`, `workouts.ts` |
| Shuffle toggle row (modal) | `prestart-modal__shuffle`, `prestart-modal__shuffle-label`, `prestart-modal__shuffle-btn` | `home.ts`, `workouts.ts` |

---

## 4. `prestart-modal.ts` Exports After This Feature

| Export | Status | Notes |
|--------|--------|-------|
| `getVisibleModalButtons` | **Retained** | Used internally by `trapModalTabKey` |
| `trapModalTabKey` | **Retained** | Used by `workouts.ts` simplified modal |
| `PrestartExercisePreview` | **Removed** | No longer has any callers |
| `renderPrestartExercisePreview` | **Removed** | No longer has any callers |
