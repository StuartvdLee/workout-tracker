# UI Contract: Workout Overall Effort

**Feature**: `016-workout-overall-effort`

---

## 1. Effort Modal (active-session.ts)

### DOM Structure

The modal is injected into the page alongside the existing discard modal scaffold. It starts hidden with `display:none` and is revealed on save-button press.

```html
<div class="effort-modal-backdrop" id="effort-backdrop" style="display:none;">
  <div class="effort-modal"
       role="alertdialog"
       aria-modal="true"
       aria-labelledby="effort-modal-title"
       aria-describedby="effort-modal-desc">

    <h2 class="effort-modal__title" id="effort-modal-title">Overall Workout Effort</h2>
    <p class="effort-modal__desc" id="effort-modal-desc">How hard was this workout overall?</p>

    <div class="effort-modal__slider-group">
      <div class="effort-modal__label-row">
        <label class="effort-modal__label" for="overall-effort-slider">Effort</label>
        <span class="effort-modal__value" id="overall-effort-value">Not rated</span>
      </div>
      <input class="effort-modal__slider"
             type="range"
             id="overall-effort-slider"
             min="1" max="10" step="1"
             data-touched="false"
             aria-label="Overall workout effort"
             aria-valuemin="1"
             aria-valuemax="10"
             aria-valuetext="Not rated" />
      <span class="effort-modal__band" id="overall-effort-band"></span>
    </div>

    <div class="effort-modal__actions">
      <button class="effort-modal__save" type="button" id="effort-modal-save">Save</button>
      <button class="effort-modal__skip" type="button" id="effort-modal-skip">Skip</button>
    </div>

  </div>
</div>
```

### Behaviour

| Trigger | Action |
|---|---|
| "Save Workout" button click | Resets slider to untouched state, opens modal (sets `display:block` on `#effort-backdrop`), moves focus to `#effort-modal-save` |
| Slider `input` event | Sets `data-touched="true"` on slider, updates `#overall-effort-value` with numeric value, updates `#overall-effort-band` with label from `getEffortLabel()`, sets `aria-valuetext` |
| "Save" button (`#effort-modal-save`) click | Closes modal, calls `handleSave(pendingOverallEffort)` where `pendingOverallEffort` is null if `data-touched="false"`, else the slider's current integer value |
| "Skip" button (`#effort-modal-skip`) click | Closes modal, calls `handleSave(null)` |
| Escape key (while modal open) | Closes modal, calls `handleSave(null)` |
| Backdrop click (`#effort-backdrop`) | Closes modal, calls `handleSave(null)` |
| Modal inner area click | Does NOT close modal (event stops at modal div, does not reach backdrop) |

### Slider Untouched State

- `data-touched="false"` on the `<input>` element
- `#overall-effort-value` shows "Not rated"
- `#overall-effort-band` is empty / hidden
- `aria-valuetext` is "Not rated"
- The slider position is visually centred (or at min) — but since `data-touched="false"`, the value is ignored and treated as null

### CSS Classes (BEM)

| Class | Element |
|---|---|
| `.effort-modal-backdrop` | Full-screen overlay (same role as `.discard-backdrop`) |
| `.effort-modal` | Modal dialog box |
| `.effort-modal__title` | `<h2>` heading |
| `.effort-modal__desc` | Description paragraph |
| `.effort-modal__slider-group` | Wrapper for label + band + slider |
| `.effort-modal__label` | `<label>` element |
| `.effort-modal__value` | Inline value display (e.g., "Not rated", "7") |
| `.effort-modal__band` | Effort band label (e.g., "Hard") |
| `.effort-modal__slider` | `<input type="range">` |
| `.effort-modal__actions` | Button row wrapper |
| `.effort-modal__save` | Save / confirm button (primary style) |
| `.effort-modal__skip` | Skip button (secondary / ghost style) |

### TypeScript Changes (active-session.ts)

- Add `import { getEffortLabel } from "../utils.js";` (if not already imported)
- Add `let pendingOverallEffort: number | null = null;` at module level
- `handleSave(overallEffort: number | null): Promise<void>` — updated signature
- POST body: `JSON.stringify({ loggedExercises, overallEffort })`
- New functions: `openEffortModal()`, `closeEffortModal()`, `handleEffortSliderInput()`, `handleEffortSave()`, `handleEffortSkip()`

---

## 2. History Card — Overall Effort *(Not implemented — removed post-delivery)*

> The overall effort is **not** displayed on the Workout History page. This was implemented initially (inline text below exercise count, right-aligned) but removed at the user's request after delivery — the card was considered too cluttered.
>
> The API (`GET /api/sessions`) still returns the `overallEffort` field for forward compatibility, but the frontend ignores it.

---

## 3. Session Detail — Overall Effort Summary Row (session-detail.ts)

### Affected DOM (renderDetailTable)

After the closing `</table>` tag and outside the table-wrapper (always rendered):

```html
<div class="session-detail__overall-effort-row">
  <span class="session-detail__overall-effort-label">Overall Effort</span>
  <!-- When overallEffort is not null: -->
  <span class="session-detail__overall-effort-value">8 · All Out</span>
  <!-- When overallEffort is null: -->
  <span class="session-detail__overall-effort-value"><span class="session-detail__no-data">—</span></span>
  <span class="session-detail__overall-effort-prev-label">Previous</span>
  <!-- When previousOverallEffort is not null: -->
  <span class="session-detail__overall-effort-prev-value">7 · Hard</span>
  <!-- When previousOverallEffort is null: -->
  <span class="session-detail__overall-effort-prev-value"><span class="session-detail__no-data">—</span></span>
</div>
```

### Rendering Rule

- The summary row is **always rendered**, regardless of whether either value is null.
- `overallEffort != null`: render `"{value} · {label}"` — e.g., `"8 · All Out"`
- `overallEffort == null` (or absent): render `<span class="session-detail__no-data">—</span>`
- Same rule applies to `previousOverallEffort`

### CSS

| Class | Notes |
|---|---|
| `.session-detail__overall-effort-row` | Flex row below the exercises table; padding/spacing consistent with card interior |
| `.session-detail__overall-effort-label` | "Overall effort" label; bold or dark text |
| `.session-detail__overall-effort-value` | Current effort value text |
| `.session-detail__overall-effort-prev-label` | "Previous" text label; colour `var(--color-text-light)` |
| `.session-detail__overall-effort-prev-value` | Previous effort value text; colour `var(--color-text-light)` |

### TypeScript Changes (session-detail.ts)

- Add `import { getEffortLabel } from "../utils.js";`
- Add `readonly overallEffort: number | null;` and `readonly previousOverallEffort: number | null;` to `SessionDetailWithPrevious` interface
