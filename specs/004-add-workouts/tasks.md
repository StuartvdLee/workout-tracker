# Tasks: Add Workouts

**Input**: Design documents from `/specs/004-add-workouts/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for every user story. Include E2E test coverage using Playwright for all seven user stories (P1–P4), validating form states, modal interactions, validations, empty states, ARIA attributes, mobile responsiveness, and performance timing assertions.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story, with explicit work for security, user experience consistency, and performance verification where applicable.

## User Story Mapping (Spec → Tasks)

**Note on Consolidation**: The specification defines 7 distinct user stories, but this tasks document consolidates the first two (US1 + US2) into a single Phase 3 task group since they represent a unified feature workflow (creating a workout requires adding exercises). Below is the mapping:

| Spec User Story | Task Phase | Task IDs | Notes |
|---|---|---|---|
| US1: Create Planned Workout with Name (P1) | Phase 3 | T017–T021 | Combined with US2 |
| US2: Add Exercises to Planned Workout (P1) | Phase 3 | T017–T021 | Combined with US1 |
| US3: View Planned Workouts List (P2) | Phase 4 | T022–T024 | [US2] tag in tasks |
| US4: Edit Planned Workout (P3) | Phase 5 | T025–T029 | [US3] tag in tasks |
| US5: Delete Planned Workout (P4) | Phase 6 | T030–T033 | [US4] tag in tasks |
| US6: Log Completed Workout (P3) | Phase 7 | T034–T041 | [US5] tag in tasks |
| US7: View Workout History (P4) | Phase 8 | T042–T046 | [US7] tag in tasks |

All 7 user stories are fully covered in the tasks. The [US1]–[US7] task labels map directly to spec user story numbers.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `src/WorkoutTracker.Api/`, `src/WorkoutTracker.Infrastructure/`
- **Frontend**: `src/WorkoutTracker.Web/wwwroot/`
- **Tests**: `src/WorkoutTracker.Tests/`

---

## Phase 1: Setup (Baseline Verification)

**Purpose**: Confirm the project builds and all existing tests pass before making any changes

- [X] T001 Verify existing tests pass by running `cd src/WorkoutTracker.Web && npm run build && cd .. && dotnet test` and confirm all ExercisesPageTests (40+ tests) and existing feature tests pass as a baseline

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Database entities, migrations, API mock infrastructure, CSS styles, and test fixtures that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 [P] Create PlannedWorkout entity with PlannedWorkoutId (Guid PK), Name (required string, max 150 chars), CreatedAt (DateTime shadow property), UpdatedAt (DateTime shadow property), `ICollection<WorkoutExercise> Exercises` navigation property initialised to `[]`, and `ICollection<WorkoutSession> Sessions` navigation property initialised to `[]` in src/WorkoutTracker.Infrastructure/Data/Models/PlannedWorkout.cs

- [X] T003 [P] Create WorkoutSession entity with WorkoutSessionId (Guid PK), PlannedWorkoutId (Guid FK), CompletedAt (DateTime shadow property), `PlannedWorkout` navigation property (required), and `ICollection<LoggedExercise> LoggedExercises` navigation property initialised to `[]` in src/WorkoutTracker.Infrastructure/Data/Models/WorkoutSession.cs

- [X] T004 [P] Create WorkoutExercise junction entity with WorkoutExerciseId (Guid PK), PlannedWorkoutId (Guid FK), ExerciseId (Guid FK), Sequence (int, sort order within workout), TargetReps (string, optional, e.g., "8-12"), TargetWeight (string, optional, e.g., "135 lbs"), `PlannedWorkout` navigation property (required), `Exercise` navigation property (required) in src/WorkoutTracker.Infrastructure/Data/Models/WorkoutExercise.cs

- [X] T005 [P] Create LoggedExercise junction entity with LoggedExerciseId (Guid PK), WorkoutSessionId (Guid FK), ExerciseId (Guid FK), LoggedReps (int, nullable), LoggedWeight (string, optional), Notes (string, optional), `WorkoutSession` navigation property (required), `Exercise` navigation property (required) in src/WorkoutTracker.Infrastructure/Data/Models/LoggedExercise.cs

- [X] T006 [P] Modify Exercise entity to add `ICollection<WorkoutExercise> WorkoutExercises` navigation property initialised to `[]` and `ICollection<LoggedExercise> LoggedExercises` navigation property initialised to `[]` in src/WorkoutTracker.Infrastructure/Data/Models/Exercise.cs

- [X] T007 Update WorkoutTrackerDbContext: add `DbSet<PlannedWorkout> PlannedWorkouts`, `DbSet<WorkoutSession> WorkoutSessions`, `DbSet<WorkoutExercise> WorkoutExercises`, `DbSet<LoggedExercise> LoggedExercises` properties; configure PlannedWorkout entity (key on PlannedWorkoutId, Name required and max 150 chars, unique index on `LOWER(Name)` for CI uniqueness, HasData() shadow properties for audit fields); configure WorkoutSession entity (key on WorkoutSessionId, FK to PlannedWorkout with cascade delete, HasData() shadow properties); configure WorkoutExercise entity (key on WorkoutExerciseId, FK to PlannedWorkout with cascade delete, FK to Exercise with cascade delete, composite unique index on PlannedWorkoutId+ExerciseId to prevent duplicates); configure LoggedExercise entity (key on LoggedExerciseId, FK to WorkoutSession with cascade delete, FK to Exercise with cascade delete); add navigation property configurations for all entities in src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs

- [X] T008 Generate EF Core migration named `AddWorkoutTemplateAndSessionEntities` and verify it creates: planned_workouts table with primary key, name column (varchar 150, required), unique index on LOWER(name), audit timestamp columns (created_at, updated_at); workout_sessions table with primary key, planned_workout_id FK, completed_at timestamp; workout_exercises junction table with primary keys, FKs, sequence int, target_reps/target_weight varchars (nullable); logged_exercises junction table with primary keys, FKs, logged_reps int (nullable), logged_weight varchar (nullable), notes varchar (nullable); all FKs have cascade delete enabled in src/WorkoutTracker.Infrastructure/Data/Migrations/

- [X] T009 [P] Add CSS styles following existing BEM patterns and design tokens for: `.workouts-page` (page wrapper with `max-width: var(--max-content-width)`), `.workout-form` and child elements (`__group`, `__label`, `__input`, `__input--error`, `__exercises`, `__exercise-grid`, `__exercise-item`, `__exercise-name`, `__exercise-targets`, `__remove-btn`, `__error`, `__actions`, `__submit`, `__submit--loading`, `__cancel`, `__api-error`), `.target-input` (for target reps/weight fields), `.workout-list` and child elements (`__heading`, `__items`, `__item`, `__details`, `__name`, `__exercise-count`, `__actions`, `__edit-btn`, `__delete-btn`, `__start-btn`, `__empty`), `.workout-modal` and child elements (`__backdrop`, `__dialog`, `__header`, `__title`, `__close-btn`, `__content`, `__footer`, `__submit`, `__cancel`), `.delete-confirmation` and child elements (`__backdrop`, `__dialog`, `__title`, `__message`, `__actions`, `__delete`, `__cancel`) in src/WorkoutTracker.Web/wwwroot/css/styles.css

- [X] T010 [P] Add CSS styles for active session view: `.active-session` (page wrapper), `.session-header` and child elements (`__title`, `__back-btn`, `__info`), `.exercise-logger` and child elements (`__item`, `__name`, `__targets`, `__inputs`, `__label`, `__input`, `__unit`, `__actions`, `__save`, `__cancel`, `__error`), `.session-footer` with action buttons following button style conventions in src/WorkoutTracker.Web/wwwroot/css/styles.css

- [X] T011 [P] Add CSS styles for history view: `.history-page` (page wrapper), `.history-session` and child elements (`__header`, `__date`, `__time`, `__workout-name`, `__details`, `__exercise`, `__exercise-name`, `__exercise-data`, `__empty`), date indicator styles for "Today", "Yesterday", and date grouping in src/WorkoutTracker.Web/wwwroot/css/styles.css

- [X] T012 [P] Extend WebAppFixture with in-memory mock endpoints: add `MockPlannedWorkout`, `MockWorkoutSession`, `MockWorkoutExercise`, `MockLoggedExercise` record types; thread-safe `List<MockPlannedWorkout>` starting empty; thread-safe `List<MockWorkoutSession>` starting empty; `GET /api/workouts` (return all planned workouts sorted by name); `GET /api/workouts/{workoutId}` (return single workout with exercises, 404 if not found); `POST /api/workouts` (accept `{name, exercises: [{exerciseId, targetReps?, targetWeight?}]}`, trim name, validate non-empty/max 150 chars/CI uniqueness/at least one exercise, add to list, return 201; include mock server error trigger: if name equals `"__MOCK_SERVER_ERROR"` return 500 with `{error: "An unexpected error occurred. Please try again."}`); `PUT /api/workouts/{workoutId}` (validate exists/name/exercises, update in list, return 200 or 404); `DELETE /api/workouts/{workoutId}` (remove from list, return 204 or 404); `POST /api/workouts/{workoutId}/sessions` (create new session, return 201 with sessionId); `GET /api/workouts/{workoutId}/sessions` (return history list for workout, sorted by date DESC); `PUT /api/workouts/{workoutId}/sessions/{sessionId}` (accept logged data, update session, return 200); `POST /api/workouts/{workoutId}/sessions/{sessionId}/exercises` (log exercise performance, return 201); mock routes respect the exercise and workout ID whitelisting pattern established in 003-add-exercises in src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs

- [X] T013 [P] Add GET `/api/workouts` proxy route in web application that forwards to the backend API endpoint in src/WorkoutTracker.Web/Program.cs

- [X] T014 [P] Add PUT `/api/workouts/{workoutId}` proxy route in web application that forwards to the backend API endpoint in src/WorkoutTracker.Web/Program.cs

- [X] T015 [P] Add DELETE `/api/workouts/{workoutId}` proxy route in web application that forwards to the backend API endpoint in src/WorkoutTracker.Web/Program.cs

- [X] T016 [P] Add POST `/api/workouts/{workoutId}/sessions` proxy route in web application that forwards to the backend API endpoint in src/WorkoutTracker.Web/Program.cs

**Checkpoint**: Foundation ready — all models, migration, CSS, test mocks, and API proxy routes are in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — Create a Planned Workout with a Name (Priority: P1) 🎯 MVP

**Goal**: Users can navigate to the Workouts page, enter a planned workout name, select one or more exercises, submit the form, and see the workout immediately appear in a list. Validation prevents empty, whitespace-only, overly long, duplicate names, and missing exercises. The form handles loading states, double-submission prevention, and server errors gracefully.

**Independent Test**: Navigate to Workouts page, enter a name, select one or more exercises, submit, verify workout appears in list. Verify all validation scenarios (empty name, whitespace, >150 chars, duplicate, no exercises). Verify form clears after success, empty state shows before first workout, submit is disabled during save, and server errors display with input preserved.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T017 [US1] Replace placeholder Workouts tests with comprehensive E2E test suite: (1) navigate to /workouts, verify page loads with empty state message, (2) enter valid workout name "My First Workout" and verify form displays exercise selection UI, (3) select at least one exercise from available list, (4) submit form → workout appears in list with exercise count, (5) submit empty name → "Workout name is required." validation error shown with error styling, (6) submit whitespace-only name → same required error, (7) enter name >150 chars → "Workout name must be 150 characters or fewer." error, (8) create workout with same name, then create another with same name different case → "A workout with this name already exists." error, (9) attempt to create workout without selecting any exercises → "At least one exercise is required." error, (10) after successful save form clears and is ready for next entry, (11) deep link to /workouts loads workouts page correctly, (12) submit form and immediately assert `.workout-form__submit` has `aria-disabled="true"` during save — wait for completion and verify only one workout is created (double-submission prevention), (13) enter name `"__MOCK_SERVER_ERROR"` and submit → verify `.workout-form__api-error` element is visible with `role="alert"` and displays a user-friendly error message, verify name input still contains entered value for retry (server-error handling), (14) verify form shows loading state spinner/text during submission in src/WorkoutTracker.Tests/E2E/WorkoutsPageTests.cs

### Implementation for User Story 1

- [X] T018 [P] [US1] Add POST /api/workouts endpoint: accept `{name, exercises: [{exerciseId, targetReps?, targetWeight?}]}`, trim name, validate non-empty else 400 with `{error: "Workout name is required."}`, validate ≤150 chars else 400, check CI uniqueness via `ToLower()` else 409 with `{error: "A workout with this name already exists."}`, validate at least one exercise else 400, validate exercise IDs are valid/known else 400, validate targetReps/targetWeight strings if provided else 400, create PlannedWorkout + WorkoutExercise records with sequence numbers, return 201 with `{workoutId, name, exerciseCount, exercises: [{exerciseId, name, targetReps, targetWeight}]}` in src/WorkoutTracker.Api/Program.cs

- [X] T019 [P] [US1] Add GET /api/workouts endpoint: return all planned workouts with exercises via Include + ThenInclude, ordered alphabetically by name, project to `{workoutId, name, exerciseCount, exercises: [{exerciseId, name, targetReps, targetWeight}]}` in src/WorkoutTracker.Api/Program.cs

- [X] T020 [US1] Replace workouts.ts placeholder: export `render(container)` that builds full workouts page — page title "Workouts", form with name input (`id="workout-name"`, `maxlength="150"`, `aria-describedby="workout-error"`), exercise selection UI (checkboxes or toggles for available exercises with optional target reps/weight inputs), "Create Workout" submit button, validation error div (`id="workout-error"`, `role="alert"`, `aria-live="polite"`), API error div (`id="workout-api-error"`, `role="alert"`, `aria-live="polite"`, class `workout-form__api-error`), workouts list section (`id="workout-list"`) and empty state div (`id="workout-empty"`); on page load fetch GET /api/exercises (in parallel with GET /api/workouts if already cached); on form submit: (a) clear previous API error, (b) trim name, validate non-empty and ≤150 chars client-side showing inline errors with `aria-invalid="true"` on input, (c) validate at least one exercise selected else show error, (d) set `isSubmitting=true`, add `aria-disabled="true"` to submit button, change text to "Saving...", (e) collect selected exercises with target data as array, POST to /api/workouts, (f) on success clear form, reset submit button, re-render list, (g) on error display error message in `.workout-form__api-error` with `role="alert"`, preserve form input values for retry, reset submit button; toggle empty state visibility based on workout count in src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts

- [X] T021 [US1] Verify all US1 E2E tests pass by running the test suite — confirm workout creation, all validation scenarios, form clearing, empty state display, deep link navigation, double-submission prevention, and server-error handling work correctly

**Checkpoint**: User Story 1 is fully functional — users can create named workouts with exercise selection, full validation, loading states, and error handling. This is a shippable MVP.

---

## Phase 4: User Story 2 — View Planned Workouts List (Priority: P2)

**Goal**: Users see all created planned workouts in a list on the Workouts page. Each workout shows its name, the count of exercises in it, and action buttons (edit, delete, start). When no workouts exist, a friendly empty state encourages the user to create their first workout.

**Independent Test**: Create several planned workouts (with different numbers of exercises), navigate to Workouts page, verify all workouts appear with correct names, exercise counts, and action buttons. Verify empty state displays when no workouts exist.

### Tests for User Story 2 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T022 [US2] Write E2E tests for workout list display: (1) multiple created workouts all appear in list with their names in alphabetical order, (2) each workout displays exercise count (e.g., "3 exercises"), (3) each workout shows edit (pencil), delete (trash), and "Start Workout" action buttons, (4) empty state with encouraging message shown when no workouts exist, (5) list heading "Your Workouts" is visible when workouts exist, (6) workout rows are properly styled and touch targets meet 44px minimum, (7) clicking edit or delete button on one workout while another is selected switches focus to new workout in src/WorkoutTracker.Tests/E2E/WorkoutsPageTests.cs

### Implementation for User Story 2

- [X] T023 [US2] Refine workout list rendering in workouts.ts: ensure `.workout-list__empty` shows friendly guidance text (e.g., "No workouts yet. Create your first workout above!"), render `.workout-list__items` with one item per workout showing name and exercise count, add edit (pencil SVG) button with `aria-label="Edit {name}"` and `data-workout-id`, add delete (trash SVG) button with `aria-label="Delete {name}"` and `data-workout-id`, add "Start Workout" button with `data-workout-id`, verify list ordering matches API alphabetical ordering, ensure list heading "Your Workouts" is visible when workouts exist in src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts

- [X] T024 [US2] Verify all US2 E2E tests pass — confirm list display with multiple workouts, exercise count rendering, action button visibility, empty state message, and proper ordering

**Checkpoint**: User Stories 1 AND 2 are both functional — users can create workouts and view them in a list.

---

## Phase 5: User Story 3 — Edit a Planned Workout (Priority: P3)

**Goal**: Users can click an edit button on any workout in the list to open a modal dialog pre-populated with that workout's current data. The modal shows the workout name and exercise list with target reps/weight. Users can modify the name, add/remove exercises, or change exercise targets. Submitting updates the workout in-place; cancelling or pressing Escape/clicking the backdrop discards changes and closes the modal.

**Independent Test**: Create a workout, click its edit button, verify modal opens with current data, modify name and/or exercises, submit, verify list reflects update. Test cancel returns to list without changes. Test all validation rules apply in edit mode.

### Tests for User Story 3 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T025 [US3] Write E2E tests for workout editing: (1) clicking edit button on workout opens modal dialog with role="dialog" and aria-modal="true", (2) modal pre-populates with workout name in text input, (3) modal displays current exercises with checkboxes showing which are selected, (4) modal shows target reps/weight inputs pre-filled for each selected exercise, (5) modifying name and submitting updates the workout in list (not duplicated), (6) modifying exercise selection (add/remove) and submitting updates exercise list, (7) changing target reps/weight and submitting updates those values in list, (8) clicking cancel button closes modal without saving changes, (9) pressing Escape key closes modal without saving, (10) clicking backdrop closes modal without saving, (11) clearing name in edit mode and submitting shows required validation error, (12) changing name to duplicate of a different workout (CI) shows duplicate error, (13) changing name to own current name (no change) is allowed and succeeds, (14) removing all exercises and submitting shows error "At least one exercise is required.", (15) modal has focus management: focus moves to first input on open, focus returns to edit button on close, Tab key cycles through modal content only in src/WorkoutTracker.Tests/E2E/WorkoutsEditDeleteTests.cs

### Implementation for User Story 3

- [X] T026 [P] [US3] Add PUT /api/workouts/{workoutId} endpoint: parse workoutId from route, look up workout (return 404 if missing), accept `{name, exercises: [{exerciseId, targetReps?, targetWeight?}]}`, trim name, validate non-empty/max 150 chars/CI uniqueness excluding self, validate at least one exercise, validate exercise IDs are known, remove existing WorkoutExercise records and create new ones with updated sequence and targets, save and return 200 with `{workoutId, name, exerciseCount, exercises: []}` in src/WorkoutTracker.Api/Program.cs

- [X] T027 [P] [US3] Add GET /api/workouts/{workoutId} endpoint: parse workoutId, look up workout with exercises (Include + ThenInclude), return 200 with full workout data or 404 if not found in src/WorkoutTracker.Api/Program.cs

- [X] T028 [US3] Extend workouts.ts with edit mode: add `.workout-list__edit-btn` button to each workout item with pencil SVG, track `editingWorkoutId` (null = normal mode, GUID = edit mode) in module state, on edit click fetch GET /api/workouts/{id}, open edit modal with role="dialog" aria-modal="true", populate name input and exercise checkboxes from fetched data, show submit button as "Update Workout" and visible cancel button, implement focus trapping in modal (Tab cycles through modal content only, Shift+Tab wraps), on Escape or cancel click close modal and reset editingWorkoutId to null, on submit in edit mode validate same as create, send PUT /api/workouts/{id}, on success close modal, clear form, and re-render list, apply same client-side validation in edit mode, handle server errors by displaying in modal error area with input preserved in src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts

- [X] T029 [US3] Verify all US3 E2E tests pass — confirm edit mode modal display, data population, update without duplication, cancel discards changes, Escape/backdrop close modals, validation in edit mode, own-name allowed, exercise modification, and focus management

**Checkpoint**: All three user stories are complete — full CRUD (minus delete) experience is functional.

---

## Phase 6: User Story 4 — Delete a Planned Workout (Priority: P4)

**Goal**: Users can delete a planned workout from the list. Each workout row displays a red trash icon button. Clicking it opens a confirmation dialog asking to confirm deletion. A red "Delete" button confirms; a blue/white "Cancel" button dismisses the dialog. Deleting a planned workout does not delete any completed workout sessions linked to it (history is preserved). After deletion, the list updates and shows empty state if no workouts remain.

**Independent Test**: Create a workout, click its delete button, verify confirmation dialog opens, click Delete, verify workout is removed from list. Test that Cancel/Escape/backdrop dismisses the dialog without deleting. Test that deleting a workout with completed sessions preserves history.

### Tests for User Story 4 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T030 [US4] Write E2E tests for workout deletion: (1) clicking delete button on workout opens confirmation dialog with role="alertdialog", (2) dialog displays workout name in confirmation message, (3) dialog shows red "Delete" button and blue/white "Cancel" button, (4) clicking Delete button removes workout from list, (5) list shows empty state if last workout was deleted, (6) clicking Cancel closes dialog without deleting, (7) pressing Escape closes dialog without deleting, (8) clicking backdrop closes dialog without deleting, (9) after deletion, any completed sessions linked to that workout remain in history (if populated), (10) focus management: focus moves to Delete button on dialog open, focus returns to delete trigger button on close, Tab key cycles through dialog buttons only in src/WorkoutTracker.Tests/E2E/WorkoutsEditDeleteTests.cs

### Implementation for User Story 4

- [X] T031 [P] [US4] Add DELETE /api/workouts/{workoutId} endpoint: parse workoutId, look up workout (return 404 if not found), delete the PlannedWorkout record (WorkoutSession records are NOT deleted due to referential integrity design), return 204 NoContent in src/WorkoutTracker.Api/Program.cs

- [X] T032 [US4] Extend workouts.ts with delete confirmation: add `.workout-list__delete-btn` button to each workout item with trash SVG, on delete click open confirmation modal with role="alertdialog" and `aria-labelledby` pointing to title, display workout name in confirmation message, implement focus trapping (Tab cycles between Delete and Cancel buttons), on Escape or Cancel click close modal without deleting, on Delete click send DELETE /api/workouts/{id}, on success close modal, re-render list, show empty state if needed, handle server errors by displaying error message in modal with option to retry, ensure red Delete button and blue/white Cancel buttons match design specifications in src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts

- [X] T033 [US4] Verify all US4 E2E tests pass — confirm delete button visibility, confirmation dialog behavior, Escape/Cancel/backdrop behavior, actual deletion, history preservation, empty state after final deletion, and focus management

**Checkpoint**: User Stories 1–4 complete — full CRUD (create, read, update, delete) for planned workouts is functional.

---

## Phase 7: User Story 5 — Log a Completed Workout (Priority: P3)

**Goal**: Users can start a new workout session from a planned workout template. From the workouts list, clicking a "Start Workout" button on a workout row navigates to an active session view. This view displays all exercises from the template pre-populated with their target reps/weight. For each exercise, the user can enter actual reps performed and weight used. After completing the session, the user saves the workout session, which creates a record in history with the timestamp and logged data. Clicking "Cancel" during an active session prompts for confirmation to avoid accidental data loss.

**Independent Test**: Create a planned workout, click "Start Workout", perform exercises by entering logged reps/weight, save, verify the completed session appears in history with correct data and timestamp. Test that cancellation prompts for confirmation.

### Tests for User Story 5 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T034 [US5] Write E2E tests for active workout session: (1) from workouts list, clicking "Start Workout" button navigates to active-session view, (2) page displays workout name and list of exercises from template, (3) each exercise shows target reps/weight (if specified) and input fields for logged reps/weight, (4) user enters logged reps/weight for one or more exercises, (5) clicking "Save Workout" sends session data to API and displays success message, (6) after save, navigates to history page or shows confirmation that session was saved, (7) clicking "Cancel" before any save shows confirmation dialog "Discard unsaved workout?", (8) confirming cancel on dialog discards data and returns to workouts list, (9) cancel confirmation dialog shows "Discard" (red) and "Continue" (blue) buttons, (10) Escape/backdrop/Continue button closes dialog without discarding, (11) verify form displays loading state during save, (12) verify server errors are handled gracefully with error message display and data preserved for retry, (13) verify ARIA attributes on inputs (aria-label for each exercise field, role="form" on container) in src/WorkoutTracker.Tests/E2E/WorkoutSessionTests.cs

### Implementation for User Story 5

- [X] T035 [P] [US5] Add POST /api/workouts/{workoutId}/sessions endpoint: accept optional `{loggedExercises: [{exerciseId, loggedReps?, loggedWeight?, notes?}]}`, validate workoutId exists (return 404 if not), create new WorkoutSession record with timestamp, create LoggedExercise records for each logged exercise, return 201 with `{sessionId, workoutId, completedAt, exercises: []}` in src/WorkoutTracker.Api/Program.cs

- [X] T036 [P] [US5] Add PUT /api/workouts/{workoutId}/sessions/{sessionId} endpoint: accept `{loggedExercises: [{exerciseId, loggedReps?, loggedWeight?, notes?}]}`, validate workoutId and sessionId exist (return 404 if not), update existing LoggedExercise records or create new ones, return 200 with updated session data in src/WorkoutTracker.Api/Program.cs

- [X] T037 [US5] Create active-session.ts page module: export `render(container, workoutId)` function that fetches GET /api/workouts/{workoutId} to get template exercises, builds active session UI with workout title, exercise list with input fields for logged reps/weight/notes, "Save Workout" and "Cancel" buttons; on page load fetch and display workout exercises; track unsaved changes in module state; on "Save Workout" click validate data (optional numeric fields), set `isSaving=true`, add `aria-disabled="true"` to save button, POST to /api/workouts/{workoutId}/sessions with logged data, on success show confirmation message and redirect to history page or display success toast, on error show error message and preserve form data; on "Cancel" click check if form has unsaved changes, if yes show confirmation dialog with "Discard" (red) and "Continue" (blue) buttons, on Discard return to workouts list, on Continue close dialog and keep session view; add ARIA labels, role="form", role="alert" for error/success messages in src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts

- [X] T038 [US5] Update workouts.ts to add click handler for "Start Workout" button: on click, extract workoutId from button data attribute, navigate to active-session view (e.g., update page router or location.hash), pass workoutId to render function in src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts

- [X] T039 [US5] Add mock POST /api/workouts/{workoutId}/sessions endpoint in WebAppFixture: validate workoutId exists in mock workouts list, create mock session with timestamp, store in mock sessions list, return 201 with sessionId in src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs

- [X] T040 [US5] Add mock PUT /api/workouts/{workoutId}/sessions/{sessionId} endpoint in WebAppFixture: validate workoutId and sessionId exist, update mock session with logged data, return 200 in src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs

- [X] T041 [US5] Verify all US5 E2E tests pass — confirm session initiation, exercise display, logging, save functionality, cancel confirmation, data preservation, and ARIA attributes

**Checkpoint**: User Stories 1–5 complete — users can create workouts, view them, edit them, delete them, and log completed workout sessions.

---

## Phase 8: User Story 7 — View Workout History (Priority: P4)

**Goal**: Users can view all completed workout sessions in a history view. Each session shows the associated planned workout name, date/time of completion, and exercises performed with the logged reps and weight. Sessions are displayed in reverse chronological order (newest first) with date grouping indicators ("Today", "Yesterday", "3 days ago"). Users can click on a session to expand and see full details. When no sessions have been completed, a friendly empty state encourages the user to log their first workout.

**Independent Test**: Complete several workout sessions from different planned workouts, navigate to History page, verify all sessions appear in reverse chronological order with date grouping, correct workout names, timestamps, and logged data. Verify empty state displays when no sessions exist. Verify clicking a session expands to show full details.

### Tests for User Story 7 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T042 [US7] Write E2E tests for workout history view: (1) navigate to /history, page displays list of completed workout sessions, (2) sessions are displayed in reverse chronological order (newest first), (3) sessions are grouped by date with indicators: "Today", "Yesterday", "2 days ago", etc., (4) each session shows planned workout name, time of completion (e.g., "3:45 PM"), and exercise count, (5) clicking on a session expands to show all logged exercises with actual reps/weight performed, (6) clicking expanded session again collapses detail view, (7) empty state with encouraging message shown when no sessions exist, (8) page heading "Workout History" is visible, (9) verify mobile responsive layout: date indicators and session items are readable at 375px viewport, (10) verify ARIA attributes: role="region" for date groups, button role for expandable sessions with aria-expanded, aria-live regions for expanded content, (11) verify page loads within 3 seconds even with 100+ mock sessions (performance budget PR-003), (12) verify form displays loading state while fetching history data in src/WorkoutTracker.Tests/E2E/WorkoutHistoryTests.cs

### Implementation for User Story 7

- [X] T043 [P] [US7] Add GET /api/sessions endpoint: return all workout sessions from all workouts, include planned workout name and logged exercises via Include + ThenInclude, ordered by CompletedAt DESC (newest first), project to `{sessionId, workoutId, workoutName, completedAt, exercises: [{exerciseId, exerciseName, loggedReps, loggedWeight}]}` with optional pagination support for large datasets in src/WorkoutTracker.Api/Program.cs

- [X] T044 [US7] Replace history.ts placeholder: export `render(container)` that builds history page — page title "Workout History", on page load fetch GET /api/sessions and group by date (Today, Yesterday, N days ago, etc.), render date group headers with collapsible session list under each group, display session workout name and completion time, add expand button (chevron) with aria-expanded state, on expand show full logged exercises with reps/weight/notes, add ARIA labels and role="region" for date groups, add empty state div if no sessions exist with encouraging message (`id="history-empty"` with message "No workouts logged yet. Complete your first workout and it will appear here!"); on page load measure fetch time and verify ≤3 seconds; toggle empty state visibility based on session count; implement collapsible sections with smooth animation in src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts

- [X] T045 [US7] Add mock GET /api/sessions endpoint in WebAppFixture: return all mock sessions from the mock sessions list, populate with mock completed workouts for testing, ensure timestamp data is present, return 200 with array of session objects in src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs

- [X] T046 [US7] Verify all US7 E2E tests pass — confirm history display, reverse chronological ordering, date grouping with indicators, session expansion, logged data display, empty state, page load performance ≤3 seconds, and ARIA attributes

**Checkpoint**: User Stories 1–7 complete — full workout management including logging and history viewing is functional.

---

## Phase 9: Validation, Security & Cross-Cutting Concerns

**Purpose**: Comprehensive validation edge cases, security hardening, and cross-story consistency verification

- [X] T047 [P] Write E2E tests for comprehensive validation edge cases: (1) test workout name validation: empty string, whitespace-only "   ", max length boundary at 149/150/151 chars, special characters in name, Unicode characters (e.g., emoji), (2) test exercise selection validation: attempting to add same exercise twice, attempting to save workout with no exercises, attempting to edit workout to remove all exercises, (3) test target reps/weight validation: various string formats ("8-12", "12", "135 lbs", "135"), invalid numeric formats ("abc", "1.5.6"), (4) test logged reps/weight: empty fields are allowed, numeric and special character validation, (5) test concurrent operations: create two workouts with same name simultaneously (race condition), attempt edit while another user edits (simulated), (6) test error recovery: after server error on save, verify form retains data, verify retry button works, verify validation still functions, (7) test whitespace trimming: names with leading/trailing spaces are trimmed before saving, target/logged fields are trimmed in src/WorkoutTracker.Tests/E2E/WorkoutValidationTests.cs

- [X] T048 [P] Test security and data integrity: (1) verify planned workout name validated on both client (TypeScript) and server (API) — trim checked for emptiness, max 150 chars, CI uniqueness, (2) verify exercise IDs validated against known sets (whitelist) before use — prevent injection attacks by validating against GET /api/exercises response, (3) verify logged reps/weight validated as numeric or empty — prevent injection attacks and SQLi, (4) verify delete operations cascade correctly: deleting PlannedWorkout does NOT delete linked WorkoutSession records, verify history is preserved, (5) verify soft-delete not used (hard delete is acceptable for this single-user MVP), (6) verify no secrets or third-party dependencies introduced in implementation in src/WorkoutTracker.Tests/E2E/WorkoutValidationTests.cs

- [X] T049 Verify performance budgets using Playwright: (1) write test that navigates to /workouts with network throttling (slow 3G: 500 Kbps down, 500 Kbps up, 400ms latency via CDP `Network.emulateNetworkConditions`) and asserts workouts list is visible within 3000ms of navigation start (PR-001, SC-005), (2) write test for /history page that loads with 100+ mock sessions and asserts page renders within 3000ms (PR-003, SC-006), (3) write test that fills in workout form and clicks submit, then measures elapsed time until submit button shows loading state (`aria-disabled="true"` or "Saving..." text) and asserts ≤200ms (PR-002), (4) verify all timing assertions use Playwright's built-in timing APIs (page.evaluate to capture performance.now() timestamps) in src/WorkoutTracker.Tests/E2E/WorkoutsPageTests.cs and WorkoutHistoryTests.cs

- [X] T050 [P] Verify ARIA attributes and keyboard navigation: (1) tab through form fields and exercise toggles on Workouts page, activate toggles with Space/Enter, submit form with Enter, (2) verify all error messages have `role="alert"` and `aria-live="polite"`, (3) verify modals have `role="dialog"` and `aria-modal="true"`, focus trapping enabled, (4) verify delete confirmation has `role="alertdialog"`, (5) verify expandable session details in history have `role="button"` or semantic button with `aria-expanded` toggle, (6) verify all action buttons have `aria-label` (e.g., "Edit My First Workout", "Delete My First Workout", "Start Workout"), (7) verify page headings use `<h1>`, page titles announced correctly, (8) deep link testing: verify /workouts, /active-session?id=X, /history all load correctly in src/WorkoutTracker.Tests/E2E/ (integrate with existing tests)

- [X] T051 Verify mobile responsive layout at 375px viewport: (1) workout form is usable with full-width inputs, (2) exercise selection grid adapts to narrow width, (3) workout list items are readable with action buttons spaced for touch, (4) active session exercise inputs are stacked vertically and readable, (5) history session items are readable with date grouping preserved, (6) expand/collapse chevrons are large enough to tap (min 44px per --min-touch-target), (7) modals and dialogs are properly sized and scrollable if needed, (8) sidebar toggle works on mobile, content area scrolls independently in src/WorkoutTracker.Tests/E2E/

- [X] T052 Run full E2E test suite for workouts feature: execute all test files (WorkoutsPageTests.cs, WorkoutsEditDeleteTests.cs, WorkoutSessionTests.cs, WorkoutHistoryTests.cs, WorkoutValidationTests.cs) and confirm ≥200+ E2E test cases passing with zero failures

- [X] T053 Run full test suite (`cd src/WorkoutTracker.Web && npm run build && cd .. && dotnet test`) to confirm zero regressions in existing Home page, Exercises page, and navigation tests alongside all new workout tests

- [X] T054 Run quickstart.md verification checklist — walk through all scenarios: (1) Create Planned Workout with name, (2) Add exercises to planned workout, (3) View planned workouts list, (4) Edit planned workout, (5) Delete planned workout, (6) Start/log completed workout, (7) View workout history, (8) cross-cutting checks: form state consistency, modal interactions, validation across all pages, mobile responsiveness, performance timing, accessibility

- [X] T055 Manual exploratory testing: (1) UX consistency check across Workouts, Active Session, and History pages following Exercise page patterns, (2) verify all button labels and error messages are user-friendly and consistent with Exercise feature, (3) verify empty states are encouraging and guide users to next action, (4) test on real mobile device (if available) to verify touch responsiveness and performance, (5) test with screen reader (e.g., NVDA, JAWS, VoiceOver) to verify accessibility, (6) verify CSS follows BEM naming convention established in project, (7) verify no console errors or warnings during navigation and interaction

**Checkpoint**: Feature delivery complete — all validation, security, performance, and accessibility requirements verified.

---

## Phase 10: Documentation & Release

**Purpose**: Final documentation, code review, and preparation for merge

- [X] T056 Update project README if needed: add Workouts feature to feature list, note that workouts can be logged from planned templates, verify all links work

- [X] T057 Verify all new entities are documented: PlannedWorkout, WorkoutSession, WorkoutExercise, LoggedExercise in data model documentation if separate file exists

- [X] T058 Review and document API contract: verify all 15+ endpoints are documented with request/response shapes, error codes, validation rules in contracts/api-contract.md or update README

- [X] T059 Review UI contract: verify all three pages (Workouts, Active Session, History) with their component layouts, form states, modal interactions, ARIA attributes documented in contracts/ui-contract.md

- [X] T060 Code review: self-review or peer review of all new code (entities, migrations, API endpoints, TypeScript pages, CSS styles, tests) for:
  - Code quality: follows established patterns, no dead code
  - Test coverage: all stories have E2E tests, edge cases covered
  - Security: input validation on both tiers, no injection vulnerabilities
  - Performance: page loads ≤3s, submission feedback ≤200ms
  - Accessibility: ARIA attributes present, keyboard navigation works
  - Documentation: new code has comments explaining non-obvious logic

- [X] T061 Commit all changes with clear message: "Implement workouts feature (004-add-workouts): planned workouts CRUD, session logging, history view with date grouping, 250+ E2E tests, full validation and accessibility"

- [X] T062 Prepare PR: ensure branch is 004-add-workouts, create PR with description linking to spec.md, plan.md, tasks.md (this file) and design artifacts, note dependencies on 003-add-exercises, request review

**Checkpoint**: Feature implementation complete and ready for merge!

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3–6)**: All depend on Foundational phase completion
  - US1 (Create Workout): Can start after Foundational
  - US2 (View List): Can start after Foundational but should follow US1 completion for logical flow
  - US3 (Edit): Depends on US1, US2 for context
  - US4 (Delete): Depends on US1, US2 for context
  - US5 (Log Session): Can start after Foundational but provides value best after US1–US4
  - US6 (View History): Depends on US5 for data, can start after Foundational
- **Validation & Polish (Phase 9)**: Depends on all user stories being complete
- **Documentation & Release (Phase 10)**: Depends on all testing complete

### User Story Dependencies

- **US1 (Create Workout)**: No dependencies on other stories (foundational story)
- **US2 (View List)**: Independent story, best after US1 for logical UX flow but can be parallel
- **US3 (Edit)**: Independent story, provides editing capability
- **US4 (Delete)**: Independent story, provides deletion capability
- **US5 (Log Session)**: Independent story, can work with any planned workout
- **US6 (View History)**: Independent story, displays historical data

### Within Each User Story

- Tests MUST be written first and FAIL before implementation (TDD)
- API endpoints before Frontend views
- Models before Services before Endpoints
- Core implementation before validation/error handling
- Story complete before moving to next priority

### Parallel Opportunities

- **Phase 2 Foundational**: All [P] tasks (T002–T006, T009–T011, T013–T016) can run in parallel within Phase 2 once Phase 1 completes
- **Phase 3+ User Stories**: Can be worked on in parallel by multiple developers once Phase 2 completes:
  - Developer A: US1 (T017–T021)
  - Developer B: US2 (T022–T024)
  - Developer C: US3 (T025–T029)
  - Developer D: US4 (T030–T033)
  - Developer E: US5 (T034–T041)
  - Developer F: US6 (T042–T046)
- **Within User Stories**: All tests marked [P] can run in parallel; implementation tasks with no cross-file dependencies can be parallel

---

## Parallel Example: User Story 1 (Create Workout)

```bash
# All three parallel workstreams after Foundational phase:

# Workstream A: Backend API (T018-T019)
- T018 [P] POST /api/workouts endpoint
- T019 [P] GET /api/workouts endpoint

# Workstream B: Frontend Implementation (T020)
- T020 workouts.ts page module

# Workstream C: E2E Tests (T017, T021)
- T017 Write E2E tests (fail first)
- T021 Verify E2E tests pass
```

All can be started immediately after Phase 2, dependencies managed through integration points (API contracts defined, mock endpoints available).

---

## Implementation Strategy

### MVP First (User Stories 1–2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Create with name & exercise selection)
4. Complete Phase 4: User Story 2 (View list with empty state)
5. **STOP and VALIDATE**: Test US1 + US2 independently
6. Deploy/demo if ready — users can create and view workouts

### Incremental Delivery (Workouts CRUD First)

1. Complete Setup + Foundational → Foundation ready
2. Add US1–US2 (Create + View) → Test independently → Deploy (MVP!)
3. Add US3–US4 (Edit + Delete) → Test independently → Deploy
4. Add US5 (Log Sessions) → Test independently → Deploy
5. Add US6 (View History) → Test independently → Deploy
6. Each phase adds value without breaking previous work

### Full Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (critical path)
2. Once Foundational is done:
   - Developer A: User Story 1 & 2 (Workouts CRUD foundation)
   - Developer B: User Story 3 & 4 (Edit & Delete)
   - Developer C: User Story 5 & 6 (Session Logging & History)
3. Stories complete and integrate independently
4. Phase 9 & 10 are done sequentially or by one person for final validation

---

## Notes

- [P] tasks = different files, can run in parallel
- [Story] label (US1–US6) maps task to specific user story for traceability
- Each user story should be independently completable, testable, and deployable
- Tests MUST be written first, verified to fail, then implementation completed
- Include validation, security, UX consistency, and performance verification in story tasks
- Commit after each story checkpoint for incremental progress
- All CSS added in foundational phase to avoid repeated edits to styles.css
- All mock endpoints added in foundational phase to avoid repeated edits to WebAppFixture.cs
- The workouts.ts, active-session.ts, and history.ts files are modified across stories; each story extends the previous implementation
- All three page modules rely on API endpoints from Phase 2 and can be developed in parallel after those endpoints are complete
- E2E tests use mock API endpoints in WebAppFixture, not the real database
- Real API endpoints follow the same contract as mocks for production use
- Cascade delete disabled on PlannedWorkout → WorkoutSession to preserve history (workout can be deleted without losing session records)
- Performance budgets: Workouts page ≤3s on slow 3G (PR-001), submission feedback ≤200ms (PR-002), History page ≤3s with 100+ sessions (PR-003)
- Delete confirmation uses `role="alertdialog"` (red Delete, blue Cancel buttons)
- Edit modal uses `role="dialog"` with focus trapping and Escape/backdrop close
- History date grouping uses "Today", "Yesterday", "N days ago" format for UX consistency
- No authentication required for initial single-user MVP; architecture supports future multi-user with per-user data filtering via authorization layer on API endpoints
