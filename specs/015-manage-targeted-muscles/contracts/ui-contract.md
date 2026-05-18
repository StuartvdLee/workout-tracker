# UI Contract: Add-Muscle Inline Form

**Feature**: `015-manage-targeted-muscles`

---

## Overview

An inline mini-form is added immediately below the muscle toggle group in both the **create exercise form** and the **edit exercise modal**. It consists of a text input, an "Add" button, and an error container.

---

## HTML Structure

### Create form addition (inside `#exercise-form`)

```html
<div class="muscle-add" id="add-muscle-form">
  <input
    class="muscle-add__input exercise-form__input"
    type="text"
    id="add-muscle-name"
    placeholder="New muscle name"
    maxlength="100"
    autocomplete="off"
    aria-label="New muscle name"
    aria-describedby="add-muscle-error"
  />
  <button
    class="muscle-add__btn"
    type="button"
    id="add-muscle-btn"
  >Add</button>
  <div
    class="muscle-add__error"
    id="add-muscle-error"
    role="alert"
    aria-live="polite"
  ></div>
</div>
```

### Edit modal addition (inside `#edit-modal-form`)

Same structure with IDs prefixed `edit-`:
- `#edit-add-muscle-name`
- `#edit-add-muscle-btn`
- `#edit-add-muscle-error`

---

## Interaction States

| State    | Input             | Button                            | Error container      |
|----------|-------------------|-----------------------------------|----------------------|
| Idle     | Empty, enabled    | "Add", enabled                    | Empty                |
| Loading  | Disabled          | "Adding...", `aria-disabled=true` | Empty                |
| Success  | Cleared, enabled  | "Add", re-enabled                 | Empty                |
| Error    | Retains value     | "Add", re-enabled                 | Error message shown  |

---

## CSS Classes

| Class              | Element  | Purpose                                              |
|--------------------|----------|------------------------------------------------------|
| `.muscle-add`      | `div`    | Container; `display: flex; gap: var(--spacing-xs); align-items: center; flex-wrap: wrap; margin-top: var(--spacing-xs)` |
| `.muscle-add__input` | `input` | Reuses `exercise-form__input` base + compact width   |
| `.muscle-add__btn` | `button` | Compact secondary button; reuses button token colours|
| `.muscle-add__error` | `div`  | Inline error text; `color: var(--color-error); font-size: var(--font-size-sm)` |

---

## Behaviour

1. User types a name and clicks "Add" (or presses Enter in the input).
2. Frontend trims input:
   - If empty → show "Muscle name is required." in error container; do not call API.
   - If > 100 chars → show "Muscle name must be 100 characters or fewer."; do not call API.
3. Button shows "Adding..." and is `aria-disabled="true"`. Input is disabled.
4. `POST /api/muscles` called with `{ name }`.
5. On 201:
   - New muscle inserted into `muscles[]` at correct alphabetical position.
   - `renderMuscleToggles()` and `renderEditMuscleToggles()` called.
   - New muscle auto-selected in the active context (`selectedMuscleIds` or `selectedEditMuscleIds`).
   - Input cleared.
   - Error container cleared.
6. On 400 or network error:
   - Error message shown in error container.
   - Input value retained.
7. Input and button re-enabled after API response (success or failure).

---

## Accessibility

- Error containers use `role="alert"` + `aria-live="polite"` — consistent with existing `#exercise-error` and `#exercise-api-error`.
- Input uses `aria-describedby` pointing at its error container.
- "Add" button is `type="button"` to avoid triggering the parent form submission.
- Focus remains in the input after a successful add so the user can add another muscle without re-tabbing.
