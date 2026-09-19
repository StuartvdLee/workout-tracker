# UI Contract: Workout Sets Selection

**Status**: Implemented in PR #159

## Start Page (`Let's go!`)

Add a field directly below `Select your workout`:

```html
<label class="workout-form__label" for="sets-select">Sets</label>
<select class="workout-form__select" id="sets-select" required>
  <option value="" disabled selected>Select sets</option>
  <option value="3">3</option>
  <option value="5">5</option>
</select>
```

Rules:

- Reuse the existing workout form label, select, spacing, focus, disabled, and error styles.
- No value is preselected.
- Start requires both a valid workout and sets value.
- Missing sets shows `Please select sets` using the existing form error treatment and focuses/marks the sets control.
- Valid navigation includes `sets=3` or `sets=5` together with existing `id` and optional randomized `order` parameters.
- No additional fetch is introduced.
- The Workouts-page Start modal also requires Sets before either original-order or randomized navigation, including single-exercise workouts.

## Active Session (`Last time`)

Extend the existing previous-value parts list:

```text
#1 · 80 KG · 5 sets · 7 — Hard
```

Rules:

- Render exactly `3 sets` or `5 sets`.
- Use the existing middle-dot separator and `.active-session__previous-*` classes.
- Omit sets when the previous value is null.
- Sets is shown only when the selected latest usable weight-or-effort comparison carries a value; a newer Sets-only row does not suppress fallback to older usable weight or effort.
- Existing loading, error, and first-session messages remain unchanged.

## Session Detail View

Add columns in this order:

1. Exercise
2. Weight (kg)
3. Prev. Weight (kg)
4. Sets
5. Prev. Sets
6. Effort
7. Prev. Effort

Rules:

- Current Sets repeats the session-level value in each exercise row.
- Prev. Sets displays the per-exercise comparison value returned by the API.
- Null values render the existing `—` no-data element.
- Use existing table classes; add only width/responsive rules needed to keep the table usable on small screens.
- Empty-session, loading, not-found, and error states continue to use existing treatments.

## Session Detail Edit Mode

- Current Sets cells become selects using `.session-detail__select`, with options `Not recorded`, `3`, and `5`.
- Every visible Sets select represents the same session-level draft value.
- Changing any Sets select immediately synchronizes the value of all other Sets selects.
- Prev. Sets remains read-only.
- The update request sends one top-level `sets` value, never one value per logged exercise.
- Sets participates in the existing dirty-state snapshot and discard-warning flow.
- Save errors preserve the selected sets value for retry.
- On success, returned data replaces in-memory detail and all rows render the saved value.

## Accessibility

- The start control has an associated `Sets` label.
- Edit-mode row controls use exercise-specific labels such as `Sets for Bench Press`, while conveying that the value applies to the whole workout through visible supporting copy or an accessible description.
- Validation errors retain `role="alert"`/`aria-live` behavior already used by the affected forms.
- Keyboard navigation and focus behavior match existing selects and edit controls.

## Responsive Behavior

The wider detail table keeps the established responsive table behavior. Column headings remain unambiguous, values do not wrap unnecessarily, and horizontal overflow (if already used) remains accessible without hiding controls. Delivered CSS assigns explicit widths to all seven columns so `Prev. Weight (kg)` does not overlap `Sets`.
