# UI Contract: Muscles Page

**Feature**: `017-targeted-muscles-page`

---

## Page: /muscles

### Layout (mirrors Exercises and Workouts pages)

```
┌─────────────────────────────┐
│ h1: Muscles                  │
│                              │
│  [Text input: Muscle name ]  │
│  [Add Muscle btn          ]  │
│  [error container (hidden) ] │
│                              │
│ ─── section ─────────────── │
│  h2: Your Muscles            │
│  [grid of muscle cards]      │
│  [empty state (hidden)]      │
│                              │
│  [Edit modal (hidden)]       │
│  [Delete confirm modal]      │
└─────────────────────────────┘
```

---

## HTML Structure

### Add Form

```html
<div class="muscles-page">
  <h1 class="muscles-page__title">Muscles</h1>
  <form class="muscle-form" id="muscle-form" novalidate>
    <div class="muscle-form__group">
      <label class="muscle-form__label" for="muscle-name">Muscle name</label>
      <input
        class="muscle-form__input"
        type="text"
        id="muscle-name"
        name="muscle-name"
        maxlength="100"
        aria-describedby="muscle-error"
        autocomplete="off"
      />
    </div>
    <div class="muscle-form__error" id="muscle-error" role="alert" aria-live="polite"></div>
    <div class="muscle-form__actions">
      <button class="muscle-form__submit" type="submit">Add Muscle</button>
    </div>
    <div class="muscle-form__api-error" id="muscle-api-error" role="alert" aria-live="polite"></div>
  </form>
  <section class="muscle-list">
    <h2 class="muscle-list__heading">Your Muscles</h2>
    <div class="muscle-list__loading" id="muscle-loading">Loading...</div>
    <div class="muscle-list__empty" id="muscle-empty" style="display:none;">
      No muscles yet. Add your first muscle above!
    </div>
    <div class="muscle-list__grid" id="muscle-grid"></div>
  </section>
  <!-- Edit modal and delete confirm modal are shown/hidden via display -->
  ...
</div>
```

### Muscle Card (in grid)

Each muscle is rendered as a clickable button. There are no separate edit or delete icon buttons; clicking anywhere on the card opens the edit modal.

```html
<button class="muscle-card" type="button" data-muscle-id="{muscleId}" aria-label="Edit {name}">
  <span class="muscle-card__name">{name}</span>
</button>
```

**Visual style**: `.muscle-card` takes visual inspiration from `.muscle-toggle` (pill shape, same background/border/font) but is a full-width interactive button rather than a toggle. The entire card is the interactive target.

### Edit Modal

```html
<div class="edit-modal-backdrop" id="edit-modal-backdrop" style="display:none;">
  <div class="edit-modal" id="edit-modal" role="dialog" aria-modal="true" aria-labelledby="edit-modal-title">
    <h2 class="edit-modal__title" id="edit-modal-title">Edit Muscle</h2>
    <form class="edit-modal__form" id="edit-modal-form" novalidate>
      <div class="muscle-form__group">
        <label class="muscle-form__label" for="edit-muscle-name">Muscle name</label>
        <input
          class="muscle-form__input"
          type="text"
          id="edit-muscle-name"
          maxlength="100"
          autocomplete="off"
          aria-describedby="edit-muscle-error"
        />
      </div>
      <div class="edit-modal__error" id="edit-muscle-error" role="alert" aria-live="polite"></div>
      <div class="edit-modal__actions">
        <button class="edit-modal__submit-btn" type="submit">Save</button>
        <button class="edit-modal__delete-btn" type="button" id="edit-modal-delete-btn">Delete</button>
      </div>
      <div class="edit-modal__api-error" id="edit-modal-api-error" role="alert" aria-live="polite"></div>
    </form>
  </div>
</div>
```

**Modal behaviour**:
- Opens with current muscle name pre-filled
- `Escape` key dismisses without saving
- Backdrop click dismisses without saving
- No separate Cancel button; the modal is dismissed via `Escape` key or backdrop click
- Focus is trapped within modal while open
- On open, focus moves to the name input

### Delete Confirm Modal

```html
<div class="delete-modal-backdrop" id="delete-confirm-backdrop" style="display:none;">
  <div class="delete-modal" role="alertdialog" aria-modal="true" aria-labelledby="delete-confirm-title" aria-describedby="delete-confirm-desc">
    <h2 class="delete-modal__title" id="delete-confirm-title">Delete Muscle</h2>
    <p class="delete-modal__desc" id="delete-confirm-desc">Are you sure you want to delete "{name}"? This action cannot be undone.</p>
    <div class="delete-modal__actions">
      <button class="delete-modal__delete" type="button" id="delete-confirm-btn">Delete</button>
      <button class="delete-modal__cancel" type="button" id="delete-confirm-cancel">Cancel</button>
    </div>
    <div class="delete-modal__error" id="delete-confirm-error" role="alert" aria-live="polite"></div>
  </div>
</div>
```

This modal reuses the existing `.delete-modal-backdrop`, `.delete-modal`, `.delete-modal__title`, `.delete-modal__desc`, `.delete-modal__actions`, `.delete-modal__delete`, `.delete-modal__cancel`, and `.delete-modal__error` classes from the Exercises page.

---

## Interaction States

### Add form submit button

| State       | Text          | `aria-disabled` | Behaviour           |
|-------------|---------------|-----------------|---------------------|
| Default     | "Add Muscle" | —               | submits form        |
| Submitting  | "Adding..."  | `"true"`        | click has no effect |
| After save  | "Add Muscle" | —               | input cleared       |

### Edit modal submit button

| State       | Text        | `aria-disabled` | Behaviour           |
|-------------|-------------|-----------------|---------------------|
| Default     | "Save"      | —               | submits form        |
| Submitting  | "Saving..." | `"true"`        | click has no effect |

### Delete button (in edit modal)

| State   | Text     | Style                                   | Behaviour                                  |
|---------|----------|-----------------------------------------|--------------------------------------------|
| Default | "Delete" | Red text, white background, red outline | Closes edit modal and opens confirm modal  |

Deletion loading state is handled in the confirmation modal, not on the edit modal button.

### Delete confirmation modal

| State       | Delete button text | Cancel button | Behaviour                                      |
|-------------|--------------------|---------------|------------------------------------------------|
| Default     | "Delete"          | Enabled       | Confirms deletion; Cancel closes the modal     |
| Submitting  | "Deleting..."     | Disabled      | Waits for DELETE response; no duplicate submit |

On success, the muscle is removed from the grid immediately.

---

## CSS BEM Blocks (new)

| Block              | Description                                                         |
|--------------------|---------------------------------------------------------------------|
| `.muscles-page`    | Page wrapper                                                        |
| `.muscles-page__title` | `<h1>` page title                                             |
| `.muscle-form`     | Add-muscle form                                                     |
| `.muscle-form__group` | Label + input group                                            |
| `.muscle-form__label` | Form field label                                               |
| `.muscle-form__input` | Text input                                                     |
| `.muscle-form__error` | Inline validation error (role=alert)                           |
| `.muscle-form__actions` | Button row                                                   |
| `.muscle-form__submit` | Blue primary submit button                                    |
| `.muscle-form__api-error` | Network error display                                      |
| `.muscle-list`     | Section wrapper for the grid                                       |
| `.muscle-list__heading` | `<h2>` section heading                                       |
| `.muscle-list__grid` | Grid container for muscle cards using `repeat(auto-fill, minmax(6rem, 1fr))` |
| `.muscle-list__empty` | Empty state message                                            |
| `.muscle-list__loading` | Loading state message                                         |
| `.muscle-card`     | Clickable button; opens edit modal when clicked                    |
| `.muscle-card__name` | Muscle name display within the button                           |
| `.edit-modal__submit-btn` | Save button in the edit modal                              |
| `.edit-modal__delete-btn` | Delete button in the edit modal                            |

`.delete-modal-backdrop` and the related `.delete-modal__*` classes are reused from the Exercises page; no new delete-confirmation CSS block is introduced for this page.

---

## Sidebar Navigation Entry

```html
<a class="sidebar__link" href="/muscles" data-page="muscles">
  <svg class="sidebar__icon" ...><!-- biceps-flexed (Lucide) --></svg>
  <span class="sidebar__label">Muscles</span>
</a>
```

Positioned after the "Exercises" link. Uses the same `data-page` → active-link convention as all existing nav items.

---

## Exercises Page Changes

- Remove from create-exercise form: the entire `.muscle-add` block (`#add-muscle-form`)
- Remove from edit-exercise modal: the entire `.muscle-add` block (`#edit-add-muscle-form`)
- The `.exercise-form__muscles` toggle group and all toggle interaction logic remains untouched
- Remove from `exercises.ts`: `isAddingMuscle`, `isEditAddingMuscle` state; `handleAddMuscle`, `insertMuscleAlphabetically` functions and their event bindings
