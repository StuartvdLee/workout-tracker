# UI Contract: Add Exercises

**Feature**: 003-add-exercises
**Date**: 2026-07-15

## Page Route

| Route        | Page      | Description                                  |
| ------------ | --------- | -------------------------------------------- |
| `/exercises` | Exercises | Exercise creation form + exercise list        |

The exercises page replaces the current placeholder content. The route is unchanged.

## HTML Structure Contract

The exercises page renders the following structure inside `main.content`:

```
main.content
└── div.exercises-page
    ├── h1.exercises-page__title                    # "Exercises"
    ├── form.exercise-form                          # Create/edit form
    │   ├── div.exercise-form__group                # Name field group
    │   │   ├── label.exercise-form__label          # "Exercise name"
    │   │   └── input.exercise-form__input          # Text input for name
    │   ├── div.exercise-form__group                # Muscle selection group
    │   │   ├── label.exercise-form__label          # "Targeted muscles (optional)"
    │   │   └── div.exercise-form__muscles          # Muscle toggle grid
    │   │       └── button.muscle-toggle            # ×11, one per muscle
    │   ├── div.exercise-form__error                # Validation error message
    │   ├── div.exercise-form__actions              # Button group
    │   │   ├── button.exercise-form__submit        # "Add Exercise" / "Update Exercise"
    │   │   └── button.exercise-form__cancel        # "Cancel" (edit mode only)
    │   └── div.exercise-form__api-error            # API/network error message
    ├── section.exercise-list                       # Exercise list section
    │   ├── h2.exercise-list__heading               # "Your Exercises"
    │   └── ul.exercise-list__items                 # Exercise list
    │       └── li.exercise-list__item              # ×N, one per exercise
    │           ├── div.exercise-list__details       # Name + muscles
    │           │   ├── span.exercise-list__name     # Exercise name
    │           │   └── div.exercise-list__muscles   # Muscle chips (if any)
    │           │       └── span.exercise-list__muscle-chip  # Individual muscle
    │           └── button.exercise-list__edit-btn   # Edit button
    └── div.exercise-list__empty                    # Empty state (when no exercises)
```

## CSS Class Contract

### Page Layout

| Class                    | Element   | Purpose                             |
| ------------------------ | --------- | ----------------------------------- |
| `.exercises-page`        | `div`     | Page wrapper with max-width constraint |
| `.exercises-page__title` | `h1`      | Page heading                        |

### Exercise Form

| Class                       | Element    | Purpose                                  |
| --------------------------- | ---------- | ---------------------------------------- |
| `.exercise-form`            | `form`     | Exercise creation/edit form              |
| `.exercise-form__group`     | `div`      | Field group (label + input/control)      |
| `.exercise-form__label`     | `label`    | Field label                              |
| `.exercise-form__input`     | `input`    | Exercise name text input                 |
| `.exercise-form__input--error` | `input` | Error state modifier (red border)        |
| `.exercise-form__muscles`   | `div`      | Muscle toggle button grid                |
| `.exercise-form__error`     | `div`      | Validation error message                 |
| `.exercise-form__actions`   | `div`      | Button group for submit + cancel         |
| `.exercise-form__submit`    | `button`   | Submit button (Add/Update Exercise)      |
| `.exercise-form__submit--loading` | `button` | Loading state (disabled, altered label) |
| `.exercise-form__cancel`    | `button`   | Cancel button (edit mode only)           |
| `.exercise-form__api-error` | `div`      | API/network error message                |

### Muscle Toggle

| Class                    | Element  | Purpose                                 |
| ------------------------ | -------- | --------------------------------------- |
| `.muscle-toggle`         | `button` | Individual muscle toggle button (chip)  |
| `.muscle-toggle--active` | `button` | Selected state modifier (primary color) |

### Exercise List

| Class                          | Element  | Purpose                             |
| ------------------------------ | -------- | ----------------------------------- |
| `.exercise-list`               | `section`| List section container              |
| `.exercise-list__heading`      | `h2`     | "Your Exercises" heading            |
| `.exercise-list__items`        | `ul`     | Unordered list of exercises         |
| `.exercise-list__item`         | `li`     | Single exercise row                 |
| `.exercise-list__details`      | `div`    | Name + muscles wrapper              |
| `.exercise-list__name`         | `span`   | Exercise name text                  |
| `.exercise-list__muscles`      | `div`    | Muscle chips container for an exercise |
| `.exercise-list__muscle-chip`  | `span`   | Individual muscle name chip         |
| `.exercise-list__edit-btn`     | `button` | Edit button on each exercise row    |
| `.exercise-list__empty`        | `div`    | Empty state container               |

## Element IDs

| ID                    | Element  | Purpose                                |
| --------------------- | -------- | -------------------------------------- |
| `exercise-form`       | `form`   | Form reference for event listeners     |
| `exercise-name`       | `input`  | Name input for programmatic access     |
| `exercise-error`      | `div`    | Validation error element               |
| `exercise-api-error`  | `div`    | API error element                      |
| `exercise-list`       | `ul`     | Exercise list for DOM updates          |
| `exercise-empty`      | `div`    | Empty state element for show/hide      |

## Data Attributes

| Attribute             | On Element              | Values                | Purpose                                  |
| --------------------- | ----------------------- | --------------------- | ---------------------------------------- |
| `data-muscle-id`      | `.muscle-toggle`        | Muscle UUID           | Identifies which muscle a toggle represents |
| `data-exercise-id`    | `.exercise-list__item`  | Exercise UUID         | Identifies the exercise in a list row    |
| `data-exercise-id`    | `.exercise-list__edit-btn` | Exercise UUID      | Identifies which exercise to edit        |

## ARIA Contract

| Element                       | Attribute           | Value                                  |
| ----------------------------- | ------------------- | -------------------------------------- |
| `.exercise-form__input`       | `aria-describedby`  | `"exercise-error"`                     |
| `.exercise-form__input`       | `aria-invalid`      | `"true"` (when error) / removed        |
| `.exercise-form__input`       | `maxlength`         | `"150"`                                |
| `.exercise-form__error`       | `role`              | `"alert"`                              |
| `.exercise-form__error`       | `aria-live`         | `"polite"`                             |
| `.exercise-form__api-error`   | `role`              | `"alert"`                              |
| `.exercise-form__api-error`   | `aria-live`         | `"polite"`                             |
| `.exercise-form__muscles`     | `role`              | `"group"`                              |
| `.exercise-form__muscles`     | `aria-label`        | `"Targeted muscles"`                   |
| `.muscle-toggle`              | `role`              | `"checkbox"`                           |
| `.muscle-toggle`              | `aria-checked`      | `"true"` / `"false"`                   |
| `.exercise-form__submit`      | `aria-disabled`     | `"true"` (when submitting) / removed   |
| `.exercise-list__edit-btn`    | `aria-label`        | `"Edit {exercise name}"`               |

## Form States

| State    | Visual Indicators                                                          |
| -------- | -------------------------------------------------------------------------- |
| Default  | Empty name input, no muscles selected, "Add Exercise" button enabled       |
| Loading  | Submit button shows "Saving..." text and is disabled                       |
| Success  | Form clears, exercise appears in list, no error messages                   |
| Error    | Inline validation error near name field, input preserved                   |
| API Error| Network/server error message below form, input preserved                   |
| Edit     | Name input pre-filled, muscles pre-selected, "Update Exercise" button label, cancel button visible |

## API Dependencies

| Method | Path                 | Response                                          | Used By          |
| ------ | -------------------- | ------------------------------------------------- | ---------------- |
| GET    | `/api/muscles`       | `[{muscleId, name}, ...]`                         | Exercises page   |
| GET    | `/api/exercises`     | `[{exerciseId, name, muscles: [{muscleId, name}]}, ...]` | Exercises page   |
| POST   | `/api/exercises`     | `{exerciseId, name, muscles}` or `{error}`        | Exercise form    |
| PUT    | `/api/exercises/{id}`| `{exerciseId, name, muscles}` or `{error}`        | Exercise form    |
