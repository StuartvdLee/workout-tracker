# Implementation Plan: Unsaved Changes Warning in Edit Modals

**Branch**: `023-unsaved-changes-warning` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/023-unsaved-changes-warning/spec.md`

## Summary

When a user has modified fields in an edit modal (workout, exercise, or muscle) and attempts to close it via Cancel, the × button, backdrop click, or Escape key, a discard confirmation modal is shown asking "Discard changes?" with "Discard" and "Keep editing" actions. If no changes have been made, the modal closes immediately. The implementation reuses the existing `.discard-modal-backdrop` / `.discard-modal` CSS classes and follows the `openDiscardModal` / `closeDiscardModal` pattern already established in `active-session.ts`. Change detection captures original field values at modal open time and compares in-memory — no network calls, no rendering overhead.

## Technical Context

**Language/Version**: TypeScript ~6.0.3 (frontend only); C# / .NET 10 (backend — unaffected)  
**Primary Dependencies**: Vanilla TypeScript — no JS frameworks. No new dependencies.  
**Storage**: N/A — no data model changes  
**Testing**: Playwright E2E (`MusclesPageTests.cs`, `ExercisesPageTests.cs`, `WorkoutsPageTests.cs`); existing E2E suite must continue to pass  
**Target Platform**: Web browser (mobile-first, responsive; Chrome 93+, Firefox 92+, Safari 15.4+, Edge 93+)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Change detection is a pure in-memory string / Set / array comparison — imperceptible  
**Constraints**: Strict TypeScript (noUnusedLocals, noUnusedParameters, noImplicitReturns); BEM CSS; existing tests must pass; no JS frameworks  
**Scale/Scope**: 3 TypeScript page files modified + 3 E2E test files modified; no backend, no API, no new CSS

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Each page module (`muscles.ts`, `exercises.ts`, `workouts.ts`) gains: a module-level `originalEdit*` snapshot variable, a private `hasEditChanges()` function, an inline discard-confirm modal in the HTML, and `openEditDiscardModal()` / `closeEditDiscardModal()` functions. All follow existing BEM and function-naming conventions. The existing `closeEditModal()` call sites in all close handlers are updated to route through `hasEditChanges()` first. No speculative abstractions (a shared helper would require cross-module state passing that doesn't simplify the code). `isEditSubmitting` guard remains before the changes check so that in-flight saves are never interrupted. Strict TypeScript constraints are unaffected. ✅

- **Testing**: New E2E tests are mandatory per Constitution II. Each entity type needs tests for:
  1. Warning appears when changes exist and user tries to close (Cancel / × / backdrop / Escape)
  2. Confirming discard → modal closes, original value persisted
  3. Choosing "Keep editing" → modal stays open, changed value preserved in field
  4. No warning when no changes → modal closes immediately
  Tests are added to `MusclesPageTests.cs`, `ExercisesPageTests.cs`, `WorkoutsPageTests.cs` following the existing pattern (`CreatePageAsync` / `CloseAsync` / Playwright locator assertions). Existing close-without-warning tests (`EditWorkout_CancelClosesModal`, `EditWorkout_BackdropClickClosesModal`, etc.) are updated to reflect that these paths now only close immediately when the field contains the **original** value (no changes). ✅

- **Security**: No new inputs, no API calls, no trust boundaries changed. The confirmation modal is purely client-side DOM logic — no data leaves the browser. ✅

- **User Experience Consistency**: The discard confirmation reuses the `.discard-modal-backdrop` / `.discard-modal` pattern verbatim (already used in `active-session.ts`). Button labels follow the existing copy: "Discard" (destructive, red) and "Keep editing" (safe, outlined). `role="alertdialog"` with `aria-labelledby` / `aria-describedby`, focus sent to Discard button on open, Escape closes the confirmation (not the edit modal). This is consistent with how the active-session discard modal handles keyboard events. ✅

- **Performance**: Change detection is an in-memory comparison of at most one string and one small array/set per close action. Zero DOM traversal, zero network calls. Well within the "imperceptible" budget. ✅

## Project Structure

### Documentation (this feature)

```text
specs/023-unsaved-changes-warning/
├── plan.md              # This file
├── research.md          # Phase 0 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

*No `data-model.md`, `quickstart.md`, or `contracts/` — pure frontend UI addition with no API surface or data model changes.*

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    └── ts/
        └── pages/
            ├── muscles.ts          MODIFIED
            │                         + let originalEditName: string = ""
            │                         + openEditModal(): capture originalEditName = muscle.name
            │                         + hasEditChanges(): input.value.trim() !== originalEditName
            │                         + openEditDiscardModal() / closeEditDiscardModal()
            │                         + initEventListeners(): wire discard modal buttons
            │                         + backdrop click, X button, Escape → route through
            │                           hasEditChanges() before calling closeEditModal()
            │                         + add discard-confirm modal HTML block
            │
            ├── exercises.ts        MODIFIED
            │                         + let originalEditName: string = ""
            │                         + let originalEditMuscleIds: ReadonlySet<string> = new Set()
            │                         + openEditModal(): capture both originals
            │                         + hasEditChanges(): name differs OR Set membership differs
            │                         + openEditDiscardModal() / closeEditDiscardModal()
            │                         + initEventListeners(): wire discard modal buttons
            │                         + Cancel, backdrop click, X button, Escape → route through
            │                           hasEditChanges() before calling closeEditModal()
            │                         + add discard-confirm modal HTML block
            │
            └── workouts.ts         MODIFIED
                                      + let originalEditName: string = ""
                                      + let originalEditExerciseIds: string[] = []
                                      + fetchAndPopulateEditModal(): capture both originals after
                                        async load (alongside existing nameInput.value assignment)
                                      + hasEditChanges(): name differs OR exercise array differs
                                      + openEditDiscardModal() / closeEditDiscardModal()
                                      + initEditModal(): wire discard modal buttons
                                      + Cancel, backdrop click, X button, Escape → route through
                                        hasEditChanges() before calling closeEditModal()
                                      + add discard-confirm modal HTML block

src/WorkoutTracker.E2ETests/
└── E2E/
    ├── MusclesPageTests.cs         MODIFIED
    │                                 + EditMuscle_DiscardWarning_ShownWhenChangesExist
    │                                   (change name, click X → discard modal visible)
    │                                 + EditMuscle_DiscardWarning_DiscardConfirmed_ModalCloses
    │                                   (change name, click X, click Discard → edit modal hidden,
    │                                    original name still in grid)
    │                                 + EditMuscle_DiscardWarning_KeepEditing_ModalStaysOpen
    │                                   (change name, click X, click Keep editing → edit modal
    │                                    visible, changed value still in field)
    │                                 + EditMuscle_NoWarning_WhenNoChanges
    │                                   (open modal, click X without changing → modal closes
    │                                    immediately, no discard modal visible)
    │                                 UPDATE EditMuscle_CloseButton_ClosesModalWithoutSaving
    │                                   → rename / adjust: this test opens modal, does NOT change
    │                                    the name field (fill with same original value), clicks X
    │                                    → confirms modal closes without warning
    │
    ├── ExercisesPageTests.cs       MODIFIED
    │                                 + EditExercise_DiscardWarning_ShownWhenNameChanged
    │                                 + EditExercise_DiscardWarning_DiscardConfirmed_ModalCloses
    │                                 + EditExercise_DiscardWarning_KeepEditing_ModalStaysOpen
    │                                 + EditExercise_NoWarning_WhenNoChanges
    │                                 UPDATE EditExercise_CloseButton_ClosesModalWithoutSaving
    │                                   → now only closes immediately if no changes
    │
    └── WorkoutsPageTests.cs        MODIFIED
                                      + EditWorkout_DiscardWarning_ShownWhenNameChanged
                                      + EditWorkout_DiscardWarning_DiscardConfirmed_ModalCloses
                                      + EditWorkout_DiscardWarning_KeepEditing_ModalStaysOpen
                                      + EditWorkout_NoWarning_WhenNoChanges
                                      UPDATE EditWorkout_CancelClosesModal
                                        → now only closes immediately if no changes
                                      UPDATE EditWorkout_CloseButton_ClosesModalWithoutSaving
                                        → update to reflect new behaviour (warning shown, discard)
                                      UPDATE EditWorkout_BackdropClickClosesModal
                                        → now only closes immediately if no changes
                                      UPDATE EditWorkout_EscapeClosesModal
                                        → now only closes immediately if no changes
```

**Structure Decision**: Web application pattern (ASP.NET Core + Aspire + vanilla TypeScript SPA). Consistent with all prior features.

## Key Implementation Details

### `requestCloseEditModal()` — user-initiated close vs force-close

`closeEditModal()` remains a force-close / reset function, called directly from:
- Post-save success path in `handleEditSave()`
- "Discard" button click in the discard confirmation modal

A new wrapper `requestCloseEditModal()` is used for all user-initiated close actions (Cancel button, × button, backdrop click, Escape key):

```typescript
function requestCloseEditModal(): void {
  if (isEditSubmitting) return;
  if (hasEditChanges()) {
    openEditDiscardModal();
  } else {
    closeEditModal();
  }
}
```

This prevents any risk of the discard warning appearing during a save or when "Discard" itself is clicked.

### Set snapshot — copy not alias (exercises.ts)

`originalEditMuscleIds` must be a deep copy, not an alias of `selectedEditMuscleIds`:

```typescript
selectedEditMuscleIds = new Set(exercise.muscles.map(m => m.muscleId));
originalEditMuscleIds = new Set(selectedEditMuscleIds); // separate copy
```

### Stale-response guard (workouts.ts)

`fetchAndPopulateEditModal()` must guard against stale responses when a second edit is opened before the first fetch resolves:

```typescript
async function fetchAndPopulateEditModal(workoutId: string, ...): Promise<void> {
  const response = await fetch(`/api/workouts/${workoutId}`);
  if (editingWorkoutId !== workoutId) return; // stale: another edit opened
  // ... populate form and capture originals
}
```

### Focus management in discard modal

- On `openEditDiscardModal()`: focus the "Discard" button (matching `active-session.ts` convention)
- On "Keep editing" / Escape in discard modal: focus returns to the edit modal's primary text input
- Focus is trapped within the discard modal while it is open (Tab key cycles between "Discard" and "Keep editing"), replicating the pattern from `active-session.ts`

## Discard Confirmation Modal HTML (template for all three pages)

Each page adds a discard-confirm backdrop **after** its edit-modal-backdrop in the DOM (same z-index 200, DOM order ensures it layers on top):

```html
<div class="discard-modal-backdrop" id="{page}-edit-discard-backdrop" style="display:none;">
  <div class="discard-modal" role="alertdialog" aria-modal="true"
       aria-labelledby="{page}-edit-discard-title"
       aria-describedby="{page}-edit-discard-desc">
    <h2 class="discard-modal__title" id="{page}-edit-discard-title">Discard changes?</h2>
    <p class="discard-modal__desc" id="{page}-edit-discard-desc">
      You have unsaved changes. Are you sure you want to discard them?
    </p>
    <div class="discard-modal__actions">
      <button class="discard-modal__discard" type="button"
              id="{page}-edit-discard-confirm">Discard</button>
      <button class="discard-modal__continue" type="button"
              id="{page}-edit-discard-cancel">Keep editing</button>
    </div>
  </div>
</div>
```

IDs used per page:
| Page | backdrop ID | confirm ID | cancel ID |
|---|---|---|---|
| muscles.ts | `muscle-edit-discard-backdrop` | `muscle-edit-discard-confirm` | `muscle-edit-discard-cancel` |
| exercises.ts | `exercise-edit-discard-backdrop` | `exercise-edit-discard-confirm` | `exercise-edit-discard-cancel` |
| workouts.ts | `workout-edit-discard-backdrop` | `workout-edit-discard-confirm` | `workout-edit-discard-cancel` |

## Complexity Tracking

> No constitution violations.
