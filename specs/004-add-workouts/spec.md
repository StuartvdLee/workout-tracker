# Feature Specification: Add Workouts

**Feature Branch**: `004-add-workouts`  
**Created**: 2025-03-29  
**Status**: Draft  
**Input**: User description: "Create workouts - collections of exercises with a name. Add exercises to workouts (similar to how exercises have muscles). Edit workouts (with edit button, similar to exercises feature). Delete workouts (with delete button, similar to exercises feature). View past/completed workouts (history view). Need to decide architecture: planned workouts (templates users create for recurring workouts) vs past/history workouts (completed workout sessions)"

## Architecture Decision

**Resolved**: This feature uses a **related entities pattern** where:

1. **PlannedWorkout** (Workout Template) — A user-created template representing a planned workout structure (exercises, target rep ranges, intended weight)
2. **WorkoutSession** (Completed History) — A record of an actual completed workout, linked to a PlannedWorkout, capturing what was actually performed (actual reps, weight, duration)

**Rationale**: 
- Separating planned from completed workouts allows users to reuse the same PlannedWorkout template multiple times while maintaining distinct history records
- Users can track progress by comparing completed sessions against their planned template
- This mirrors real-world fitness tracking patterns (many apps use this model)
- Enables future analytics features (progress tracking, personal records, volume trends) without data model changes

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a Planned Workout with a Name (Priority: P1)

A user navigates to the Workouts page and creates a new planned workout by entering its name. The workout is saved and immediately appears in the planned workouts list. This is the core action — every planned workout must have a name, and users need a straightforward way to create a reusable workout template.

**Why this priority**: Without the ability to create a named planned workout, no other feature in the workouts domain can function. This is the foundational building block for the entire feature.

**Independent Test**: Can be fully tested by navigating to the Workouts page, entering a planned workout name, submitting the form, and verifying the workout appears in the planned workouts list. Delivers value by allowing users to build their personal workout library.

**Acceptance Scenarios**:

1. **Given** the user is on the Workouts page, **When** they enter a valid planned workout name and submit the form, **Then** the workout is saved and appears in the planned workouts list.
2. **Given** the user is on the Workouts page, **When** they attempt to submit the form without entering a name, **Then** a validation message is displayed indicating the name is required, and no workout is created.
3. **Given** the user is on the Workouts page, **When** they enter a name that already exists (case-insensitive), **Then** a validation message is displayed indicating a workout with that name already exists, and no duplicate is created.
4. **Given** the user has just successfully created a planned workout, **When** the workout is saved, **Then** the form is cleared and ready for another entry.

---

### User Story 2 - Add Exercises to a Planned Workout (Priority: P1)

When creating or editing a planned workout, the user can add one or more exercises from their exercise library to the workout. Each exercise in the workout can have optional target rep and weight information. This builds out the structure of the workout template and allows users to plan their training sessions.

**Why this priority**: A planned workout without exercises is incomplete and not useful. The ability to add exercises is essential to making planned workouts functional and provides immediate value.

**Independent Test**: Can be fully tested by creating a planned workout, adding one or more exercises from the exercise library with optional rep/weight targets, submitting, and verifying the exercises are saved and displayed with the workout. Also test that creating a planned workout without exercises is rejected (at least one exercise required).

**Acceptance Scenarios**:

1. **Given** the user is creating or editing a planned workout, **When** they add one or more exercises from the predefined exercise library, **Then** the exercises are associated with the workout and saved.
2. **Given** an exercise is added to a workout, **When** the user optionally specifies target rep range (e.g., "8-12" reps) and/or target weight, **Then** these values are saved with the exercise assignment.
3. **Given** the user is creating a planned workout, **When** they do not add any exercises, **Then** a validation message is displayed indicating at least one exercise is required.
4. **Given** the user has added multiple exercises to a workout, **When** they view or edit the workout, **Then** all exercises are displayed in the order they were added, with their target reps and weight (if specified).
5. **Given** an exercise is already in a workout, **When** the user attempts to add the same exercise again, **Then** a validation message indicates the exercise is already included and the duplicate is not added.

---

### User Story 3 - View Planned Workouts List (Priority: P2)

The user can view all planned workouts they have created. Each workout displays its name, the count of exercises in it, and action buttons (edit, delete). This list replaces the current placeholder content on the Workouts page and lets users see their workout library at a glance.

**Why this priority**: Viewing planned workouts is essential for users to confirm what they've added and to reference their library, but it supports the creation stories rather than standing alone as the primary value.

**Independent Test**: Can be fully tested by creating several planned workouts (with different numbers of exercises), navigating to the Workouts page, and verifying all workouts are displayed with their exercise counts and action buttons.

**Acceptance Scenarios**:

1. **Given** the user has created one or more planned workouts, **When** they view the Workouts page, **Then** all planned workouts are listed showing their name, exercise count, and edit/delete action buttons.
2. **Given** no planned workouts have been created yet, **When** the user views the Workouts page, **Then** a friendly empty state message is displayed indicating no workouts exist and encouraging the user to create their first workout.
3. **Given** a planned workout has three exercises in it, **When** it is displayed in the list, **Then** the exercise count is shown (e.g., "3 exercises").
4. **Given** a planned workout has been created, **When** the user views it in the list, **Then** they can see which exercises it contains, their target reps, and their target weight (if specified).

---

### User Story 4 - Edit a Planned Workout (Priority: P3)

A user can edit an existing planned workout from the workouts list. Clicking a pencil icon on a workout row opens a modal dialog pre-populated with that workout's name and current exercises. The user modifies the name, adds/removes exercises, or changes exercise targets. Saves within the modal apply the updates. A cancel button or pressing Escape/clicking the backdrop closes the modal without saving.

**Why this priority**: Editing planned workouts allows users to correct mistakes, adjust exercises, or refine targets after creation, but it depends on the creation and listing stories being in place first.

**Independent Test**: Can be fully tested by creating a planned workout, clicking its edit (pencil) button, verifying the modal opens with the workout's data, modifying the name or exercises, saving, and verifying the updated workout appears correctly in the list. Also test that cancellation/Escape/backdrop click discards changes and closes the modal.

**Acceptance Scenarios**:

1. **Given** the user views the planned workouts list, **When** they click the pencil icon on a workout row, **Then** a modal dialog opens pre-populated with that workout's name and current exercises (with their targets).
2. **Given** the edit modal is open, **When** the user modifies the workout name and/or adds/removes exercises and submits, **Then** the workout is updated (not duplicated) and the list reflects the changes.
3. **Given** the edit modal is open, **When** the user clicks the cancel button, presses Escape, or clicks the backdrop, **Then** all unsaved changes are discarded and the modal closes.
4. **Given** the edit modal is open, **When** the user clears the name and attempts to submit, **Then** the same validation rules apply (name required, no duplicates) and the update is rejected.
5. **Given** the edit modal is open, **When** the user removes all exercises and attempts to submit, **Then** a validation message indicates at least one exercise is required and the update is rejected.

---

### User Story 5 - Delete a Planned Workout (Priority: P4)

A user can delete a planned workout from the workouts list. Each workout row displays a red trash icon button to the right of the edit button. Clicking it opens a confirmation dialog. A red "Delete" button confirms the action, and a blue/white "Cancel" button dismisses the dialog without deleting. Deleting a planned workout does not delete any completed workout sessions linked to it (history is preserved).

**Why this priority**: Deleting planned workouts lets users keep their library clean, but it depends on creation, listing, and editing stories.

**Independent Test**: Can be fully tested by creating a planned workout, clicking its delete (trash) button, verifying the confirmation dialog opens, clicking Delete, and verifying the workout is removed from the list. Also test that clicking Cancel/Escape/backdrop dismisses the dialog without deleting.

**Acceptance Scenarios**:

1. **Given** the user views the planned workouts list, **When** they click the red trash icon on a workout row, **Then** a confirmation dialog opens asking whether they want to delete the workout.
2. **Given** the delete confirmation dialog is open, **When** the user clicks the "Delete" button, **Then** the planned workout is permanently deleted and the list is updated.
3. **Given** the delete confirmation dialog is open, **When** the user clicks "Cancel", presses Escape, or clicks the backdrop, **Then** the dialog closes and the workout is not deleted.
4. **Given** the user deletes a planned workout that had completed workout sessions, **When** the deletion completes, **Then** the completed sessions are NOT deleted and remain in the history.
5. **Given** only one planned workout exists and the user deletes it, **When** the deletion completes, **Then** the empty state message is displayed.

---

### User Story 6 - Log a Completed Workout (Priority: P3)

A user can start a new workout session from a planned workout template. From the planned workouts list, clicking a "Start Workout" button on a workout row opens a new view where the user performs the exercises. For each exercise, the user logs the actual reps performed and weight used. After completing (or stopping) the session, the user can save the workout session. Saving creates a completed workout record in history with the timestamp and logged data.

**Why this priority**: Logging completed workouts is essential for progress tracking and history, but it depends on planned workouts existing first. This enables the history feature.

**Independent Test**: Can be fully tested by creating a planned workout, clicking "Start Workout", performing exercises with logging inputs, saving, and verifying the completed session appears in the history with the correct data and timestamp.

**Acceptance Scenarios**:

1. **Given** the user views a planned workout, **When** they click "Start Workout", **Then** a new workout session view opens with all exercises from the template pre-populated and ready for logging.
2. **Given** the user is in an active workout session, **When** they complete an exercise and enter the actual reps and weight performed, **Then** the session captures and stores this data.
3. **Given** the user is in an active workout session, **When** they click "Save Workout", **Then** a completed workout record is created in history with the timestamp, linked to the planned workout template.
4. **Given** the user is in an active workout session, **When** they click "Cancel" or navigates away, **Then** the user is prompted to confirm (to avoid accidental data loss), and if confirmed, the session is discarded without saving.
5. **Given** a completed workout has been saved, **When** the user views the history, **Then** the completed workout appears with the planned workout name, timestamp, and the actual reps/weight logged.

---

### User Story 7 - View Workout History (Priority: P4)

The user can view all completed workout sessions in a history view. Each session shows the associated planned workout name, date/time of completion, and exercises performed with the logged reps and weight. This list helps users track their training progress over time and see patterns in their workout performance.

**Why this priority**: Viewing workout history is essential for progress tracking and motivation, but it depends on the ability to log completed workouts first. This provides the primary value for retention and engagement.

**Independent Test**: Can be fully tested by completing several workout sessions (logged from different planned workouts), navigating to the History page, and verifying all completed sessions are displayed with correct timestamps and logged data.

**Acceptance Scenarios**:

1. **Given** the user has completed one or more workouts, **When** they view the History page, **Then** all completed workouts are listed in reverse chronological order (newest first) showing planned workout name, date/time, and exercise details.
2. **Given** no workouts have been completed yet, **When** the user views the History page, **Then** a friendly empty state message is displayed indicating no history exists and encouraging the user to complete their first workout.
3. **Given** a completed workout has multiple exercises logged, **When** the user views the history entry, **Then** all exercises are displayed with their logged reps and weight.
4. **Given** a completed workout session is displayed, **When** the user clicks on it, **Then** they can view full details including date, time, exercises, and logged data.

---

### Edge Cases

- What happens when the user enters a workout name with only whitespace? The system should treat it as empty and show the required-name validation message.
- What happens when the user enters a very long workout name? The system should enforce a reasonable maximum length (150 characters, matching exercises) and show a validation message if exceeded.
- What happens when the user submits the form while a previous save is still in progress? The system should prevent duplicate submissions by disabling the submit action until the current save completes.
- What happens when the save fails due to a network error or server issue? The system should display a user-friendly error message and preserve the user's input so they can retry without re-entering data.
- What happens when the workout list is empty? The page should display a clear empty state with guidance on how to add the first workout.
- What happens when the user clicks edit on one workout while already editing another? The form should switch to the newly selected workout's data, discarding any unsaved changes from the previous edit.
- What happens when the user tries to add an exercise that has already been removed from the Exercise library? If the exercise no longer exists in the library, display a warning that the exercise is no longer available and either remove it from the workout or allow users to keep the historical reference.
- What happens when a user is mid-workout session and their network connection drops? The app should detect the disconnection, pause the workout, and allow the user to resume or save a partial workout when reconnected.
- What happens when the user is viewing a planned workout that has no exercises assigned (data inconsistency)? Show a warning that the workout is incomplete and prevent them from starting it until at least one exercise is added.
- What happens when the user tries to log reps/weight as non-numeric values? The system should validate numeric input and show a validation message indicating only numbers are allowed.

## Requirements *(mandatory)*

### Functional Requirements

**Planned Workouts (Core)**

- **FR-001**: System MUST allow users to create a planned workout by providing a name.
- **FR-002**: System MUST require a non-empty, non-whitespace planned workout name for every workout.
- **FR-003**: System MUST enforce a maximum planned workout name length of 150 characters.
- **FR-004**: System MUST prevent duplicate planned workout names (case-insensitive comparison).
- **FR-005**: System MUST require at least one exercise to be added to a planned workout; a workout without exercises cannot be saved.
- **FR-006**: System MUST allow users to add one or more exercises from the exercise library to a planned workout.
- **FR-007**: System MUST allow users to specify optional target rep range (e.g., "8-12") and/or target weight for each exercise in a planned workout.
- **FR-008**: System MUST prevent the same exercise from being added to a planned workout more than once.
- **FR-009**: System MUST persist planned workouts and their associated exercises so they are available across sessions.
- **FR-010**: System MUST display all created planned workouts in a list on the Workouts page, showing each workout's name and exercise count.
- **FR-011**: System MUST display a clear empty state when no planned workouts exist, guiding the user to create their first workout.
- **FR-012**: System MUST clear the creation form after a successful save, ready for the next entry.
- **FR-013**: System MUST allow users to edit an existing planned workout by clicking a pencil icon on each workout row in the list.
- **FR-014**: System MUST open a modal dialog for editing, pre-populated with the workout's current name and exercises.
- **FR-015**: System MUST update (not duplicate) the planned workout when the user submits the edit modal form.
- **FR-016**: System MUST apply the same validation rules (non-empty name, max 150 characters, no duplicate names for a different workout, at least one exercise) when editing as when creating.
- **FR-017**: System MUST allow closing the edit modal via a cancel button, pressing Escape, or clicking the backdrop, discarding unsaved changes.
- **FR-018**: System MUST allow users to delete a planned workout by clicking a red trash icon on each workout row.
- **FR-019**: System MUST show a confirmation dialog before deleting, with a red "Delete" button and a blue/white "Cancel" button.
- **FR-020**: System MUST permanently delete the planned workout and update the list when the user confirms deletion.
- **FR-021**: System MUST allow dismissing the delete confirmation via Cancel, Escape, or backdrop click without deleting.
- **FR-022**: When a planned workout is deleted, any linked completed workout sessions MUST remain in history and continue to display the workout name (even though the template is gone).

**Workout Sessions (History)**

- **FR-023**: System MUST allow users to start a new workout session from a planned workout, accessed via a "Start Workout" button on the planned workouts list.
- **FR-024**: System MUST open a dedicated workout session view with all exercises from the planned workout template pre-populated and ready for logging.
- **FR-025**: System MUST allow users to log actual reps and weight performed for each exercise in the active session.
- **FR-026**: System MUST validate that logged reps and weight values are numeric (or empty/optional).
- **FR-027**: System MUST allow users to save a completed workout session via a "Save Workout" button, creating a persistent history record with timestamp.
- **FR-028**: System MUST allow users to cancel an active workout session without saving, with a confirmation prompt to prevent accidental loss.
- **FR-029**: System MUST display all completed workout sessions in a History view in reverse chronological order (newest first).
- **FR-030**: System MUST display the associated planned workout name, completion date/time, and logged exercise details for each completed session.
- **FR-031**: System MUST display a clear empty state when no completed workouts exist in the history.
- **FR-032**: System MUST allow users to view full details of a completed workout session by clicking on a history entry.

### Security & Privacy Requirements

- **SR-001**: System MUST validate and sanitize all external inputs (workout names, rep/weight values) to prevent injection attacks.
- **SR-002**: System MUST enforce authorization rules so users can only view and modify their own planned workouts and workout history.
- **SR-003**: System MUST not expose sensitive workout data in URLs or client-side storage without encryption (e.g., do not store workout IDs in plaintext in the URL).

### User Experience Consistency Requirements

- **UX-001**: The Workouts page and History page MUST follow the existing layout patterns established by the app (sidebar navigation, content area, mobile-responsive design).
- **UX-002**: Form validation messages MUST appear inline near the relevant field, consistent with the existing validation pattern from the Exercises feature.
- **UX-003**: The planned workout creation form MUST define the following states: default (ready for input), loading (save in progress with submit disabled), success (form cleared, workout added to list), and error (user-friendly message displayed, input preserved).
- **UX-003a**: Editing MUST use a modal dialog (not a separate page). The modal has ARIA attributes (role="dialog", aria-modal="true"), focus trapping, and can be dismissed via Cancel, Escape, or backdrop click.
- **UX-003b**: The delete confirmation MUST use a modal dialog with role="alertdialog", a red Delete button, and a blue/white Cancel button.
- **UX-004**: Touch targets for all interactive elements (buttons, exercise selections, input fields) MUST meet the existing minimum size standard used in the app for mobile usability.
- **UX-005**: The exercise addition workflow MUST be intuitive, allowing easy addition/removal of exercises from the workout, with clear visual feedback for exercises already in the workout.
- **UX-006**: The active workout session view MUST display exercise name, target reps/weight (from the template), and input fields for logging actual performance. Loading and error states MUST be clearly communicated.
- **UX-007**: The History view MUST display completed workouts in a clear, scannable list format with date indicators (e.g., "Today", "Yesterday", "3 days ago") for quick time reference.

### Performance Requirements

- **PR-001**: The Workouts page MUST load and display the planned workouts list within 3 seconds on a slow 3G connection, consistent with the existing Home page target.
- **PR-002**: Saving a new planned workout or completed workout session MUST provide visual feedback (loading state) within 200 milliseconds of submission so the user knows the action was received.
- **PR-003**: The History page MUST load and display the workout history within 3 seconds on a slow 3G connection, even if there are 100+ completed sessions (pagination or lazy loading may be required).

### Key Entities

- **PlannedWorkout**: Represents a user-created workout template. Has a required name (unique, up to 150 characters), a collection of zero or more associated exercises with optional target reps/weight, and metadata (created date, updated date). Is independent of specific completed sessions.
- **WorkoutExercise**: A join entity representing an exercise assigned to a planned workout. Contains references to both the exercise and the planned workout, along with optional target rep range and target weight. Maintains order (sequence) within the workout.
- **WorkoutSession**: Represents a completed workout session logged by the user. Has a reference to the planned workout template (for context, even if the template is later deleted), a completion timestamp, and a collection of logged exercise data (actual reps and weight performed). Immutable once saved.
- **LoggedExercise**: A join entity representing an exercise performed during a completed workout session. Contains references to the exercise and workout session, along with the actual reps and weight logged by the user.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a new planned workout with exercises in under 2 minutes, including selecting exercises and setting targets.
- **SC-002**: 100% of created planned workouts persist and are visible upon returning to the Workouts page.
- **SC-003**: 100% of completed workout sessions persist and are visible in the History page.
- **SC-004**: 100% of validation errors (empty name, duplicate name, no exercises, invalid rep/weight input) are caught and communicated before any save is attempted.
- **SC-005**: The Workouts page loads and is interactive within 3 seconds on a slow 3G connection.
- **SC-006**: The History page loads and is interactive within 3 seconds on a slow 3G connection, even with 100+ completed sessions.
- **SC-007**: Users successfully create a planned workout on their first attempt at least 95% of the time (no confusion about required fields or form behavior).
- **SC-008**: Users successfully log and save a completed workout session on their first attempt at least 90% of the time.
- **SC-009**: All form states (default, loading, success, error, edit mode) and the empty-list state are visually distinct and consistent with the rest of the application.
- **SC-010**: Users can edit an existing planned workout (modify name, exercises, or targets) and see the updated data in the list immediately after saving.
- **SC-011**: Users can cancel an in-progress edit and return to create mode without any data loss to the original workout.
- **SC-012**: Users can view their complete workout history with timestamps and logged data for all completed sessions.
- **SC-013**: Deleting a planned workout does not delete any linked completed workout sessions; history is fully preserved and remains viewable.

## Assumptions

- This feature manages only the user's own workouts. Multi-user sharing of workouts is out of scope for this release.
- Planned workouts are templates; they are not themselves "performed." Users perform workout sessions based on those templates. This separation is intentional to support recurring workouts and progress tracking.
- The predefined exercise list (created in the previous 003-add-exercises feature) is populated and available for selection when building planned workouts.
- The initial release does not include features like planned workout copying, scheduled workout plans, or recurring weekly plans. Templates are created once and performed as needed.
- This feature does not include workout-specific exercises or exercise variants (e.g., "Barbell Bench Press" vs. "Dumbbell Bench Press"). Exercises from the library are reused as-is in workouts.
- The History page displays completed sessions in simple reverse chronological order. Advanced filtering, searching, or aggregation (e.g., "total volume per week") is out of scope for this release.
- No authentication or multi-user support is required; the app is assumed to be single-user.
- Rep and weight tracking are optional fields for both planned workouts (targets) and logged sessions (actuals). A user can log a workout with just exercise names if desired.
- The app does not enforce rep/weight ranges (e.g., "reps must be 1-100"). Validation is basic: numeric or empty.

## Architectural Notes

### Separation of Concerns: PlannedWorkout vs. WorkoutSession

This feature establishes a clear separation between **workout templates** (PlannedWorkout) and **workout instances** (WorkoutSession):

1. **PlannedWorkout** is created once and can be performed many times. It is the template.
2. **WorkoutSession** is created each time the user performs a workout. It captures what actually happened during that session.

This design:
- Enables progress tracking (compare logged sessions against the planned template)
- Supports recurring workouts (same template, different sessions with different results)
- Preserves history even if a planned workout is deleted
- Aligns with industry-standard fitness tracking patterns

**Alternative rejected**: Using a single "Workout" entity with a Status field (Draft/Planned/Completed). This would complicate the data model because:
- A single workout entity would need to store both target AND logged data
- Recurring workouts would require duplicating the template multiple times
- Deleting a workout template might require deleting all instances, which loses history

### UI Architecture

- **Workouts Page**: Lists all planned workout templates with create form, edit/delete actions, and "Start Workout" button for each.
- **Active Workout Session View**: Displays exercises from the selected template with input fields for logging actual performance. Separate from planned view to reduce cognitive load.
- **History Page**: Lists all completed workout sessions in reverse chronological order, with drill-in capability to view session details.

This three-view architecture keeps concerns clear and aligns with fitness app conventions (e.g., Fitbod, Strong App, etc.).
