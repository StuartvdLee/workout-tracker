# Feature Specification: Dedicated Muscles Page

**Feature Branch**: `targeted-muscles-page`  
**Created**: 2026-05-19  
**Status**: Implemented  
**Input**: User description: "I want to change the way to add targeted muscles. Right now, I can add muscles on the Exercises page. I want to create a dedicated 'Muscles' page where I can add, edit and delete the muscles that are targeted by exercises. The flow should be the same as on other pages: text box at the top and a blue 'Add Muscle' button below. Below that button, I want to see the grid of muscles that's now visible on the Exercises page. Simply copy the style and layout from the exercises page"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add a Muscle (Priority: P1)

A user navigates to the new Muscles page from the sidebar. They see a text input at the top and a blue "Add Muscle" button. They type a muscle name and click the button. The new muscle immediately appears in the muscle grid below, sorted alphabetically, without a page reload. The text input is cleared and ready for the next entry.

**Why this priority**: This is the core capability of the dedicated page. Without it, users cannot manage muscles from this new location, and the feature has no value.

**Independent Test**: Can be fully tested by navigating to the Muscles page, typing a new muscle name, clicking "Add Muscle", and verifying it appears in the grid in the correct alphabetical position without a page reload.

**Acceptance Scenarios**:

1. **Given** the user is on the Muscles page, **When** they enter a valid muscle name and click "Add Muscle", **Then** the muscle is saved and immediately appears in the muscle grid in alphabetical order.
2. **Given** the user has just added a muscle successfully, **When** the muscle appears in the grid, **Then** the text input is cleared and ready for the next entry.
3. **Given** the user attempts to add a muscle with no name (empty or whitespace-only), **When** they click "Add Muscle", **Then** a validation message is displayed and no muscle is created.
4. **Given** a muscle named "Biceps" already exists, **When** the user attempts to add "biceps" (case-insensitive match), **Then** an error message indicates the muscle already exists and no duplicate is created.

---

### User Story 2 - Edit a Muscle (Priority: P2)

A user sees an existing muscle in the grid and wants to rename it. They click anywhere on the muscle card. A modal opens pre-populated with the current muscle name and provides Save and Delete actions. They update the name and save. The grid updates immediately to reflect the new name, re-sorted alphabetically.

**Why this priority**: The ability to correct or update muscle names is important for data quality. It depends on the muscle grid being rendered (Story 1) before it can be used.

**Independent Test**: Can be fully tested by clicking any muscle card, modifying the name in the modal, saving, and verifying the grid reflects the change in correct alphabetical order. Also test that dismissal via Escape key or backdrop click discards the change.

**Acceptance Scenarios**:

1. **Given** the user clicks anywhere on a muscle card, **When** the modal opens, **Then** it is pre-populated with the current muscle name.
2. **Given** the edit modal is open, **When** the user enters a valid new name and saves, **Then** the muscle is renamed and the grid updates immediately with the new name in the correct alphabetical position.
3. **Given** the edit modal is open, **When** the user clears the name and attempts to save, **Then** a validation message is shown and the save is rejected.
4. **Given** the edit modal is open, **When** the user enters a name that already belongs to a different muscle (case-insensitive), **Then** a duplicate-name validation message is shown and the update is rejected.
5. **Given** the edit modal is open, **When** the user presses Escape or clicks the backdrop, **Then** no changes are made and the modal closes.

---

### User Story 3 - Delete a Muscle (Priority: P3)

A user wants to remove a muscle they no longer need. They click a muscle card to open the edit modal, then click the Delete button. A separate confirmation modal appears matching the style of the exercise delete modal. Upon confirmation, the muscle is removed from the grid immediately.

**Why this priority**: Deletion is the final CRUD operation completing the full lifecycle. It is less frequently used than add/edit but essential for housekeeping. It depends on the grid and edit capability being in place first.

**Independent Test**: Can be fully tested by clicking a muscle card, clicking Delete in the edit modal, confirming the deletion in the separate confirmation modal, and verifying the card is removed from the grid. Also test that cancelling the confirmation leaves the muscle in place.

**Acceptance Scenarios**:

1. **Given** the user clicks a muscle card and then clicks Delete in the edit modal, **When** the separate confirmation modal appears, **Then** the muscle is NOT yet removed and deletion is not triggered by a trash icon on the card.
2. **Given** the separate confirmation modal is shown, **When** the user confirms, **Then** the muscle is removed from the grid immediately.
3. **Given** the separate confirmation modal is shown, **When** the user cancels, **Then** the muscle remains in the grid unchanged.

---

### User Story 4 - View the Muscles Grid (Priority: P1)

A user navigates to the Muscles page and sees a grid of all existing muscles. The layout and visual style match the muscle grid currently shown on the Exercises page. When no muscles exist, an appropriate empty state message is shown.

**Why this priority**: The grid is the primary way users see and interact with muscles. Without it, add, edit, and delete interactions have no surface to operate on.

**Independent Test**: Can be fully tested by navigating to the Muscles page and verifying the grid renders correctly with existing muscles, sorted alphabetically, using the same layout as the Exercises page muscle grid. Also verify the empty state.

**Acceptance Scenarios**:

1. **Given** muscles exist in the system, **When** the user navigates to the Muscles page, **Then** all muscles are displayed in a grid sorted alphabetically.
2. **Given** no muscles exist, **When** the user navigates to the Muscles page, **Then** a friendly empty-state message is shown.
3. **Given** the page is loaded, **When** the user views the muscle grid, **Then** the layout and visual style matches the existing muscle grid on the Exercises page.

---

### User Story 5 - Remove Muscle Management from the Exercises Page (Priority: P2)

The inline "add muscle" control currently on the Exercises page is removed. The Exercises page muscle section becomes a read-only display (for selecting targeted muscles for an exercise), since muscle management is now handled on its own dedicated page.

**Why this priority**: Moving muscle management to its own page means removing it from the Exercises page to avoid duplication and inconsistency. This depends on the dedicated page (Stories 1–4) being in place first.

**Independent Test**: Can be fully tested by verifying the Exercises page no longer shows a "create muscle" input or button, while still showing the list of muscles as selectable options when creating or editing an exercise.

**Acceptance Scenarios**:

1. **Given** the Muscles page exists, **When** the user navigates to the Exercises page, **Then** there is no "add muscle" input or button on the Exercises page.
2. **Given** the Exercises page is open, **When** the user creates or edits an exercise, **Then** the existing muscles are still shown as selectable options (the muscle selection capability is unchanged — only the creation capability is removed).

---

### Edge Cases

- What happens if a muscle is deleted while it is still associated with existing exercises? The deletion permanently removes all associated `ExerciseMuscle` rows via cascade delete (configured at the database level). Exercise records will no longer reference the deleted muscle — there is no soft-delete or history retention.
- What happens when the network request to add, edit, or delete a muscle fails? A clear error message is shown and the grid reflects no change.
- What happens when the user submits a muscle name with leading/trailing whitespace? The system should trim the input before validation and saving.
- What is the maximum length for a muscle name? The system should enforce a reasonable limit (consistent with the existing behaviour) and show a message if exceeded.
- What does the page look like during the initial data load? A loading indicator should be shown while muscles are being fetched.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST provide a dedicated "Muscles" page accessible from the sidebar navigation.
- **FR-002**: The Muscles page MUST display a text input at the top and a blue "Add Muscle" button below it, matching the layout and interaction pattern of other pages (Exercises, Workouts).
- **FR-003**: The Muscles page MUST display all existing muscles in a grid, sorted alphabetically, using the same visual style as the muscle grid currently on the Exercises page.
- **FR-004**: Users MUST be able to add a new muscle by entering a name in the text input and clicking "Add Muscle"; the new muscle MUST appear in the grid immediately without a page reload.
- **FR-005**: Users MUST be able to edit the name of an existing muscle via a modal dialog opened from the muscle's card in the grid by clicking anywhere on the muscle card (the entire card is a clickable button). The modal MUST provide Save and Delete buttons and MUST be dismissible via Escape key or backdrop click without a separate Cancel button.
- **FR-006**: Users MUST be able to delete a muscle from within the edit modal via a Delete button. Clicking Delete MUST open a separate confirmation modal matching the exercise page delete confirmation modal, and the muscle MUST only be removed after explicit confirmation.
- **FR-007**: The system MUST prevent muscles with duplicate names from being created or renamed to an existing name (comparison MUST be case-insensitive).
- **FR-008**: The system MUST reject muscle names that are empty or contain only whitespace.
- **FR-009**: Muscle name input MUST be trimmed of leading/trailing whitespace before validation and saving.
- **FR-010**: The "add muscle" input and button on the Exercises page MUST be removed; muscle management is now exclusively on the Muscles page.
- **FR-011**: The muscle selection capability on the Exercises page (selecting existing muscles as targets for an exercise) MUST remain fully functional and unchanged.
- **FR-012**: The muscle grid MUST display an empty state message when no muscles exist.
- **FR-013**: The page MUST display a loading indicator while muscles are being fetched from the API, consistent with UX-004.

### Security & Privacy Requirements

- **SR-001**: All muscle name inputs MUST be validated and sanitized to prevent injection or malformed data.
- **SR-002**: Muscle create, edit, and delete operations MUST be subject to the same authorization rules as other data-write operations in the app.

### User Experience Consistency Requirements

- **UX-001**: The Muscles page layout MUST match the pattern of existing pages: text input at top, action button below, content grid beneath.
- **UX-002**: The muscle grid visual style and card layout MUST match the `.exercise-form__muscles` layout from the Exercises page, using `repeat(auto-fill, minmax(6rem, 1fr))`.
- **UX-003**: The edit modal MUST follow the same pattern as edit modals on other pages (e.g., pre-populated fields, save and delete actions, Escape key and backdrop dismissal), with no separate Cancel button.
- **UX-004**: Loading, empty, success, and error states MUST be handled consistently with existing pages.
- **UX-005**: The "Add Muscle" button MUST use the same blue primary button style used by equivalent action buttons on other pages.
- **UX-006**: Terminology MUST remain consistent: "Muscles" as the page title, "muscle" as the entity name.

### Performance Requirements

- **PR-001**: A newly added muscle MUST appear in the grid within 1 second of a successful save under normal network conditions.
- **PR-002**: Edit and delete operations MUST be reflected in the grid within 1 second of confirmation under normal network conditions.
- **PR-003**: Performance will be verified manually and via integration tests confirming round-trips complete promptly.

### Key Entities

- **Muscle**: A named body area or muscle group. Has a unique name (case-insensitive). Shared across all users of the app. Can be associated with exercises as a targeted muscle.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can navigate to the Muscles page from the sidebar and see the full muscle grid.
- **SC-002**: A user can add a new muscle and see it appear in the alphabetically sorted grid within 1 second, without a page reload.
- **SC-003**: A user can edit a muscle name via the modal and see the updated name reflected in the grid within 1 second.
- **SC-004**: A user can delete a muscle with confirmation and see it removed from the grid within 1 second.
- **SC-005**: 100% of add/edit attempts with a duplicate muscle name (case-insensitive) are rejected with a visible error message.
- **SC-006**: 100% of add/edit attempts with an empty or whitespace-only name are rejected with a visible validation message.
- **SC-007**: The Exercises page no longer shows a "create muscle" input or button; exercise muscle selection continues to work correctly.
- **SC-008**: The Muscles page layout, grid style, and interaction patterns are visually consistent with other pages in the app.

## Assumptions

- Muscles are shared across all users of the app (there is no per-user muscle list).
- The maximum muscle name length follows the existing behaviour established in feature 015 (assumed 100 characters).
- The sidebar navigation already supports adding a new page link; no significant navigation restructuring is required.
- The confirmation step for deletion uses a separate modal dialog matching the exercise page delete modal rather than an inline confirmation on the card.
- Deleting a muscle that is still associated with exercises is allowed; all associated `ExerciseMuscle` join rows are permanently removed via cascade delete (configured on the database FK). Exercise records will no longer reference the deleted muscle.
