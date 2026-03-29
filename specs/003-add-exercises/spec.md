# Feature Specification: Add Exercises

**Feature Branch**: `003-add-exercises`  
**Created**: 2025-07-15  
**Status**: Implemented  
**Input**: User description: "I want to add exercises through the app. An exercise should have a name (required) and which muscles are targeted by it (optional)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create an Exercise with a Name (Priority: P1)

A user navigates to the Exercises page and creates a new exercise by entering its name. The exercise is saved and immediately appears in the exercise list. This is the core action — every exercise must have a name, and users need a straightforward way to add one.

**Why this priority**: Without the ability to create a named exercise, no other feature in the exercises domain can function. This is the foundational building block.

**Independent Test**: Can be fully tested by navigating to the Exercises page, entering an exercise name, submitting the form, and verifying the exercise appears in the list. Delivers value by allowing users to build their personal exercise library.

**Acceptance Scenarios**:

1. **Given** the user is on the Exercises page, **When** they enter a valid exercise name and submit the form, **Then** the exercise is saved and appears in the exercise list.
2. **Given** the user is on the Exercises page, **When** they attempt to submit the form without entering a name, **Then** a validation message is displayed indicating the name is required, and no exercise is created.
3. **Given** the user is on the Exercises page, **When** they enter a name that already exists (case-insensitive), **Then** a validation message is displayed indicating an exercise with that name already exists, and no duplicate is created.
4. **Given** the user has just successfully created an exercise, **When** the exercise is saved, **Then** the form is cleared and ready for another entry.

---

### User Story 2 - Assign Targeted Muscles to an Exercise (Priority: P2)

When creating an exercise, the user can optionally select one or more muscles that the exercise targets. This enriches the exercise data and helps users understand what each exercise works. Muscles are selected from a predefined list of common muscle groups.

**Why this priority**: Muscle targeting adds meaningful context to exercises but is not required for the exercise to be useful. An exercise with just a name is still a valid, functional exercise.

**Independent Test**: Can be fully tested by creating an exercise, selecting one or more muscles from the predefined list, submitting, and verifying the exercise displays with its associated muscles. Also test that creating an exercise without selecting any muscles succeeds.

**Acceptance Scenarios**:

1. **Given** the user is creating a new exercise, **When** they select one or more muscles from the predefined list, **Then** the exercise is saved with the selected muscles associated to it.
2. **Given** the user is creating a new exercise, **When** they do not select any muscles, **Then** the exercise is saved successfully with no muscles associated.
3. **Given** a predefined list of muscles exists, **When** the user views the muscle selection options, **Then** all available muscles are displayed for selection.
4. **Given** the user is creating an exercise, **When** they select multiple muscles, **Then** all selected muscles are saved and displayed with the exercise.

---

### User Story 3 - View Exercise List (Priority: P3)

The user can view all exercises they have created. Each exercise displays its name and, if assigned, the muscles it targets. This list replaces the current placeholder content on the Exercises page and lets users see their exercise library at a glance.

**Why this priority**: Viewing exercises is essential for users to confirm what they've added and to reference their library, but it supports the creation stories rather than standing alone as the primary value.

**Independent Test**: Can be fully tested by creating several exercises (some with muscles, some without), navigating to the Exercises page, and verifying all exercises are displayed with correct details.

**Acceptance Scenarios**:

1. **Given** the user has created one or more exercises, **When** they view the Exercises page, **Then** all exercises are listed showing their name and any associated muscles.
2. **Given** no exercises have been created yet, **When** the user views the Exercises page, **Then** a friendly empty state message is displayed indicating no exercises exist and encouraging the user to add one.
3. **Given** an exercise has muscles assigned, **When** it is displayed in the list, **Then** the targeted muscles are shown alongside the exercise name.
4. **Given** an exercise has no muscles assigned, **When** it is displayed in the list, **Then** only the exercise name is shown without any muscle information.

---

### User Story 4 - Edit an Existing Exercise (Priority: P4)

A user can edit an existing exercise from the exercise list. Clicking a pencil icon on an exercise row opens a modal dialog pre-populated with that exercise's current data (name and muscles). The user modifies the data and saves within the modal. A cancel button or pressing Escape/clicking the backdrop closes the modal without saving.

**Why this priority**: Editing exercises allows users to correct mistakes or update muscle associations after creation, but it depends on the creation and listing stories being in place first.

**Independent Test**: Can be fully tested by creating an exercise, clicking its edit (pencil) button, verifying the modal opens with the exercise's data, modifying the name or muscles, saving, and verifying the updated exercise appears correctly in the list. Also test that cancellation/Escape/backdrop click discards changes and closes the modal.

**Acceptance Scenarios**:

1. **Given** the user views the exercise list, **When** they click the pencil icon on an exercise row, **Then** a modal dialog opens pre-populated with that exercise's current name and selected muscles.
2. **Given** the edit modal is open, **When** the user modifies the exercise name and/or muscle selections and submits, **Then** the exercise is updated (not duplicated) and the list reflects the changes.
3. **Given** the edit modal is open, **When** the user clicks the cancel button, presses Escape, or clicks the backdrop, **Then** all unsaved changes are discarded and the modal closes.
4. **Given** the edit modal is open, **When** the user clears the name and attempts to submit, **Then** the same validation rules apply (name required, max length, no duplicates) and the update is rejected with an appropriate message.
5. **Given** the edit modal is open, **When** the user changes the name to one that already exists (case-insensitive) for a different exercise, **Then** a duplicate-name validation message is displayed and the update is rejected.

---

### User Story 5 - Delete an Exercise (Priority: P5)

A user can delete an exercise from the exercise list. Each exercise row displays a red trash icon button to the right of the edit button. Clicking it opens a confirmation dialog asking whether the user is sure. A red "Delete" button confirms the action, and a blue/white "Cancel" button dismisses the dialog without deleting.

**Why this priority**: Deleting exercises lets users keep their library clean, but it depends on creation, listing, and editing stories.

**Independent Test**: Can be fully tested by creating an exercise, clicking its delete (trash) button, verifying the confirmation dialog opens, clicking Delete, and verifying the exercise is removed from the list. Also test that clicking Cancel/Escape/backdrop dismisses the dialog without deleting.

**Acceptance Scenarios**:

1. **Given** the user views the exercise list, **When** they click the red trash icon on an exercise row, **Then** a confirmation dialog opens asking whether they want to delete the exercise.
2. **Given** the delete confirmation dialog is open, **When** the user clicks the "Delete" button, **Then** the exercise is permanently deleted and the list is updated.
3. **Given** the delete confirmation dialog is open, **When** the user clicks "Cancel", presses Escape, or clicks the backdrop, **Then** the dialog closes and the exercise is not deleted.
4. **Given** only one exercise exists and the user deletes it, **When** the deletion completes, **Then** the empty state message is displayed.

---

### Edge Cases

- What happens when the user enters an exercise name with only whitespace? The system should treat it as empty and show the required-name validation message.
- What happens when the user enters a very long exercise name? The system should enforce a reasonable maximum length (150 characters) and show a validation message if exceeded.
- What happens when the user submits the form while a previous save is still in progress? The system should prevent duplicate submissions by disabling the submit action until the current save completes.
- What happens when the save fails due to a network error or server issue? The system should display a user-friendly error message and preserve the user's input so they can retry without re-entering data.
- What happens when the exercise list is empty? The page should display a clear empty state with guidance on how to add the first exercise.
- What happens when the user clicks edit on one exercise while already editing another? The form should switch to the newly selected exercise's data, discarding any unsaved changes from the previous edit (the form represents one edit at a time).
- What happens when the user is in edit mode and changes the name to the exercise's own current name (no actual change)? The system should allow the save since the name is not a duplicate of a *different* exercise.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to create an exercise by providing a name.
- **FR-002**: System MUST require a non-empty, non-whitespace exercise name for every exercise.
- **FR-003**: System MUST enforce a maximum exercise name length of 150 characters.
- **FR-004**: System MUST prevent duplicate exercise names (case-insensitive comparison).
- **FR-005**: System MUST allow users to optionally select one or more targeted muscles from a predefined list when creating an exercise.
- **FR-006**: System MUST persist exercises and their muscle associations so they are available across sessions.
- **FR-007**: System MUST display all created exercises in a list on the Exercises page, showing each exercise's name and any associated muscles.
- **FR-008**: System MUST display a clear empty state when no exercises exist, guiding the user to create their first exercise.
- **FR-009**: System MUST clear the creation form after a successful save, ready for the next entry.
- **FR-010**: System MUST provide a predefined list of muscles consisting of: Chest, Back, Shoulders, Biceps, Triceps, Forearms, Core, Quads, Hamstrings, Glutes, Calves.
- **FR-011**: System MUST allow users to edit an existing exercise by clicking a pencil icon on each exercise row in the list.
- **FR-012**: System MUST open a modal dialog for editing, pre-populated with the exercise's current name and muscle selections.
- **FR-013**: System MUST update (not duplicate) the exercise when the user submits the edit modal form.
- **FR-014**: System MUST apply the same validation rules (non-empty name, max 150 characters, no duplicate names for a different exercise) when editing as when creating.
- **FR-015**: System MUST allow closing the edit modal via a cancel button, pressing Escape, or clicking the backdrop, discarding unsaved changes.
- **FR-016**: System MUST allow users to delete an exercise by clicking a red trash icon on each exercise row.
- **FR-017**: System MUST show a confirmation dialog before deleting, with a red "Delete" button and a blue/white "Cancel" button.
- **FR-018**: System MUST permanently delete the exercise and update the list when the user confirms deletion.
- **FR-019**: System MUST allow dismissing the delete confirmation via Cancel, Escape, or backdrop click without deleting.

### User Experience Consistency Requirements

- **UX-001**: The Exercises page MUST follow the existing layout patterns established by the app (sidebar navigation, content area, mobile-responsive design).
- **UX-002**: Form validation messages MUST appear inline near the relevant field, consistent with the existing validation pattern on the Home page.
- **UX-003**: The exercise creation form MUST define the following states: default (ready for input), loading (save in progress with submit disabled), success (form cleared, exercise added to list), and error (user-friendly message displayed, input preserved).
- **UX-003a**: Editing MUST use a modal dialog (not the creation form). The modal has ARIA attributes (role="dialog", aria-modal="true"), focus trapping, and can be dismissed via Cancel, Escape, or backdrop click.
- **UX-003b**: The delete confirmation MUST use a modal dialog with role="alertdialog", a red Delete button, and a blue/white Cancel button.
- **UX-004**: Touch targets for all interactive elements (buttons, muscle selections) MUST meet the existing minimum size standard used in the app for mobile usability.
- **UX-005**: Muscle toggle buttons MUST clearly show their selected/deselected state on both desktop and touch devices, without hover state interference.

### Performance Requirements

- **PR-001**: The Exercises page MUST load and display the exercise list within 3 seconds on a slow 3G connection, consistent with the existing Home page target.
- **PR-002**: Saving a new exercise MUST provide visual feedback (loading state) within 200 milliseconds of submission so the user knows the action was received.

### Key Entities

- **Exercise**: Represents a single exercise in the user's library. Has a required name (unique, up to 150 characters) and an optional association with one or more muscles.
- **Muscle**: Represents a body muscle that can be targeted by exercises. Drawn from a predefined list. An exercise can target zero or more muscles, and a muscle can be targeted by zero or more exercises.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a new exercise in under 30 seconds, including selecting muscles.
- **SC-002**: 100% of created exercises persist and are visible upon returning to the Exercises page.
- **SC-003**: 100% of validation errors (empty name, duplicate name, name too long) are caught and communicated before any save is attempted.
- **SC-004**: The Exercises page loads and is interactive within 3 seconds on a slow 3G connection.
- **SC-005**: Users successfully create an exercise on their first attempt at least 95% of the time (no confusion about required fields or form behavior).
- **SC-006**: All form states (default, loading, success, error, edit mode) and the empty-list state are visually distinct and consistent with the rest of the application.
- **SC-007**: Users can edit an existing exercise (modify name or muscles) and see the updated data in the list immediately after saving.
- **SC-008**: Users can cancel an in-progress edit and return to create mode without any data loss to the original exercise.

## Assumptions

- The predefined muscle list (Chest, Back, Shoulders, Biceps, Triceps, Forearms, Core, Quads, Hamstrings, Glutes, Calves) covers the needs of typical users. Custom muscles are out of scope for this feature.
- This feature does not include searching, filtering, or sorting the exercise list. A simple alphabetically-ordered list is sufficient for the initial version.
- No authentication or multi-user support is required; the app is assumed to be single-user.
- The Exercises page currently exists as a placeholder in the sidebar navigation; this feature replaces that placeholder with functional content.

## Clarifications

### Session 2026-03-28

- Q: What interaction pattern should editing use — a separate edit page, reuse the creation form, or inline editing in the list? → A: Initially reuse creation form; later changed to modal dialog for better UX separation between create and edit flows.
- Q: How does the user trigger an edit — edit icon/button on each row, clicking the exercise name, or a context/long-press menu? → A: Pencil icon on each exercise row in the list.
- Q: What happens when the user cancels an edit — cancel button discards changes and returns to create mode, or browser back navigation? → A: Cancel button in the edit modal; also dismissible via Escape or backdrop click.

### Session 2026-03-29

- Q: Should delete be supported? → A: Yes. Red trash icon button to the right of the edit button, with a confirmation dialog before deletion.
- Q: What should the edit button look like? → A: Pencil SVG icon with a blue outline, matching the delete button's visual style.
- Bug fix: Muscle toggle hover state was overriding the active (selected) state, making selected buttons appear all-blue on hover and permanently on touch devices. Fixed with `.muscle-toggle--active:hover` override and `@media (hover: none)` for touch devices.
