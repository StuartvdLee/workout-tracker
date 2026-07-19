# UI Contract: Session Detail Past Workout Editing

## Overview

Adds edit mode to the existing session detail page (`/history/session?id=<sessionId>`). The user can edit per-exercise weight and effort values in the detail table and edit overall workout effort in the existing summary row.

## Affected Surface

| Surface | Element / Area | File |
|---|---|---|
| Session detail header | Edit action near title/back controls | `session-detail.ts` |
| Exercise table | Editable weight and effort cells | `session-detail.ts` |
| Overall effort summary | Editable session-level effort | `session-detail.ts` |
| Unsaved change protection | Discard confirmation modal | `session-detail.ts`, `styles.css` |
| Styles | Session detail edit controls and states | `styles.css` |

## View Mode

Existing view-mode behavior remains:

- Back button navigates to History.
- Header shows workout name and date.
- Exercise table shows Exercise, Weight, Prev. Weight, Effort, Prev. Effort.
- Overall effort summary row shows current and previous effort.
- Chart section and delete-session section remain available.

Add an edit action:

```html
<button class="session-detail__edit" id="session-detail-edit" type="button">
  Edit
</button>
```

Rules:

- The Edit button is visible after session data loads successfully.
- The Edit button is not shown in loading, not-found, or error states.
- The Delete session action remains a destructive action and is not available while edit mode is active.

## Edit Mode

When the user selects Edit:

- Original editable values are captured in a snapshot.
- The table re-renders with editable controls for current Weight and Effort cells.
- Previous Weight and Previous Effort remain read-only.
- Overall Effort becomes editable.
- Save and Cancel actions become visible.
- The chart section is hidden or disabled while editing to avoid showing stale derived data.

Example row:

```html
<tr class="session-detail__row session-detail__row--editing">
  <td class="session-detail__cell session-detail__cell--exercise">Bench Press</td>
  <td class="session-detail__cell">
    <input
      class="session-detail__weight-input"
      type="text"
      maxlength="100"
      value="82.5"
      aria-label="Weight for Bench Press"
    />
  </td>
  <td class="session-detail__cell session-detail__cell--prev">80</td>
  <td class="session-detail__cell">
    <select class="session-detail__effort-select" aria-label="Effort for Bench Press">
      <option value="">Not rated</option>
      <option value="1">1 · Easy</option>
      <option value="2">2 · Easy</option>
      <option value="3">3 · Easy</option>
      <option value="4">4 · Moderate</option>
      <option value="5">5 · Moderate</option>
      <option value="6">6 · Moderate</option>
      <option value="7">7 · Hard</option>
      <option value="8">8 · Hard</option>
      <option value="9">9 · All Out</option>
      <option value="10">10 · All Out</option>
    </select>
  </td>
  <td class="session-detail__cell session-detail__cell--prev">7</td>
</tr>
```

Overall effort edit control:

```html
<div class="session-detail__overall-effort-row session-detail__overall-effort-row--editing">
  <label class="session-detail__overall-effort-label" for="session-detail-overall-effort">
    Overall Effort
  </label>
  <select class="session-detail__effort-select" id="session-detail-overall-effort">
    <option value="">Not rated</option>
    <option value="1">1 · Easy</option>
    ...
    <option value="10">10 · All Out</option>
  </select>
  <span class="session-detail__overall-effort-prev-label">Previous</span>
  <span class="session-detail__overall-effort-prev-value">6 · Moderate</span>
</div>
```

Actions:

```html
<div class="session-detail__edit-actions">
  <button class="session-detail__save" id="session-detail-save" type="button">Save changes</button>
  <button class="session-detail__cancel" id="session-detail-cancel" type="button">Cancel</button>
  <div class="session-detail__save-error" id="session-detail-save-error" role="alert" aria-live="polite"></div>
</div>
```

## Unsaved Changes

Reuse the feature 023 discard modal language and classes:

```html
<div class="discard-modal-backdrop" id="session-edit-discard-backdrop" style="display:none;">
  <div class="discard-modal" role="alertdialog" aria-modal="true"
       aria-labelledby="session-edit-discard-title"
       aria-describedby="session-edit-discard-desc">
    <h2 class="discard-modal__title" id="session-edit-discard-title">Discard changes?</h2>
    <p class="discard-modal__desc" id="session-edit-discard-desc">
      You have unsaved changes. Are you sure you want to discard them?
    </p>
    <div class="discard-modal__actions">
      <button class="discard-modal__discard" type="button" id="session-edit-discard-confirm">Discard</button>
      <button class="discard-modal__continue" type="button" id="session-edit-discard-cancel">Keep editing</button>
    </div>
  </div>
</div>
```

Rules:

- Cancel with no changes exits edit mode immediately.
- Cancel with changes opens the discard modal.
- Back navigation while editing with changes opens the discard modal before leaving.
- Discard restores original values and exits edit mode.
- Keep editing closes the discard modal and preserves current input values.
- Escape closes the discard modal, returning focus to edit mode.

## Save Behavior

On Save:

1. Disable Save and Cancel controls.
2. Clear previous validation messages.
3. Build a `PUT /api/sessions/{sessionId}` payload from the current editable controls.
4. If the response is `200`, replace the in-memory session detail with the response and return to view mode.
5. If the response is validation/error, keep edit mode open, restore enabled controls, and display the returned error.

## States

| State | Required behavior |
|---|---|
| Loading | Existing loading state; no edit controls |
| View | Existing table, chart, delete section, and new Edit button |
| Editing clean | Save/Cancel visible; no discard needed on cancel |
| Editing dirty | Cancel/back prompts before discarding |
| Saving | Save/Cancel disabled; status visible to assistive tech |
| Validation error | Error shown near edit actions; inputs remain editable |
| Save failure | Error shown; edited values remain in controls for retry |
| Save success | Updated read-only values displayed; chart/trends reloaded or refreshed |
| Empty exercises | Exercise table empty row remains; overall effort remains editable |

## Accessibility

- Edit, Save, and Cancel are keyboard-operable buttons.
- Every editable weight input has an exercise-specific accessible label.
- Every effort control includes "Not rated" plus 1-10 options with labels from `getEffortLabel`.
- Error messages use `role="alert"` and `aria-live="polite"`.
- Discard modal uses `role="alertdialog"` and traps focus between actions.
- Focus returns to the Edit button after successful save or discard, if it remains in the DOM.

## Data Integrity

- Inputs are keyed by `loggedExerciseId`.
- Previous-value cells are never editable.
- The UI does not expose controls for changing date, workout name, exercise order, adding exercises, removing exercises, or deleting through edit mode.
- Clearing a weight or effort is intentional and saved as `null`, so view mode displays the existing no-data indicator consistently.
