# UI Contract: Workout Sets Selection

**Status**: Implemented in PR #159

## Start Page (`Let's go!`)

Add a field directly below `Select your workout`:

```html
<label class="workout-form__label" for="sets-select">Sets</label>
<select class="workout-form__select" id="sets-select" required>
  <option value="3" selected>3</option>
  <option value="5">5</option>
</select>
```

Rules:

- Reuse the existing workout form label, select, spacing, focus, disabled, and error styles.
- `3` is preselected by default; `5` is the only other option and there is no placeholder option.
- Start requires a valid workout; sets is always valid because of the default, and an invalid value still shows `Please select sets` using the existing form error treatment and focuses/marks the sets control.
- Valid navigation includes `sets=3` or `sets=5` together with existing `id` and optional randomized `order` parameters.
- No additional fetch is introduced.
- The Workouts-page Start modal uses the same options and default of 3, resetting to 3 each time it closes, for both original-order and randomized navigation, including single-exercise workouts.

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

## Session Detail Stats Graph

The existing `.session-chart` section gains a background bar series for session sets.

Rules:

- Plot sets for every selection offered by `#session-chart-select`: `Overall Session Effort` and each individual exercise.
- Render sets as one vertical bar per session, rising from the plot floor (`y=220`) to a height proportional to the session's set count, using `.session-chart__bar--sets` with the `.session-chart__bar` base class.
- No sets axis, tick labels, or numeric value labels are rendered; the count is conveyed by bar height alone. Bars are measured from zero against a reference of at least `6` sets, so a given count always produces the same height and higher counts raise the reference instead of overflowing the plot area. Weight and effort scales are unchanged.
- The chart `viewBox` stays `0 0 600 260` with the plot area spanning `x=50` to `x=580`, because the bars sit inside the existing plot area. Bars are centred on their session's x position, clamped to the plot edges, and drawn before the axes, gridlines, and data lines so weight and effort remain legible on top.
- Add a `.session-chart__legend-swatch--sets` entry labelled `Sets`. The overall-effort selection, which previously had no legend, gains one listing `Overall Effort` and `Sets`.
- The bars use a distinct colour at reduced opacity and are a different shape from the weight and effort lines, so sets is distinguishable without relying on colour alone.
- A session with no recorded sets renders no bar, leaving a gap rather than a zero-height bar or an error.
- When no session in the loaded range recorded sets, the bars and the sets legend entry are omitted and the chart renders exactly as before.
- The chart `aria-label` names the sets series when it is plotted (for example `Weight, effort and sets for Bench Press`) and omits it when it is not.
- Existing loading, empty, and error states for the chart are unchanged, and no additional fetch is introduced.

## Accessibility

- The start control has an associated `Sets` label.
- Edit-mode row controls use exercise-specific labels such as `Sets for Bench Press`, while conveying that the value applies to the whole workout through visible supporting copy or an accessible description.
- Validation errors retain `role="alert"`/`aria-live` behavior already used by the affected forms.
- Keyboard navigation and focus behavior match existing selects and edit controls.
- The stats graph legend remains `aria-hidden`; the chart's `role="img"` label conveys which series are plotted, including sets.

## Responsive Behavior

The chart keeps its existing `.session-chart__container` horizontal-overflow behavior and its original `viewBox`, which scales with the container rather than forcing a fixed width. Bar width is derived from the spacing between sessions (capped at 28 units), so dense ranges render thin bars instead of overlapping ones. The wider detail table keeps the established responsive table behavior. Column headings remain unambiguous, values do not wrap unnecessarily, and horizontal overflow (if already used) remains accessible without hiding controls. Delivered CSS assigns explicit widths to all seven columns so `Prev. Weight (kg)` does not overlap `Sets`.
