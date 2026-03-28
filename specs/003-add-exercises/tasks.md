# Tasks: Add Exercises

**Input**: Design documents from `/specs/003-add-exercises/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for every user story and every bug fix.
Include the appropriate unit, integration, contract, or end-to-end coverage
needed to prove behavior before implementation is complete.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story, with explicit work for security, user
experience consistency, and performance verification where applicable.

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

- [X] T001 Verify existing tests pass by running `cd src/WorkoutTracker.Web && npm run build && cd .. && dotnet test` and confirm all ExercisesPageTests (4 placeholder tests) and Home page tests pass as a baseline

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Database entities, migration, CSS styles, and test mock infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 [P] Create Muscle entity with MuscleId (Guid PK), Name (required string), and `ICollection<ExerciseMuscle> ExerciseMuscles` navigation property initialised to `[]` in src/WorkoutTracker.Infrastructure/Data/Models/Muscle.cs
- [X] T003 [P] Create ExerciseMuscle junction entity with ExerciseMuscleId (Guid PK), ExerciseId (Guid FK), MuscleId (Guid FK), and navigation properties `Exercise` (null!) and `Muscle` (null!) in src/WorkoutTracker.Infrastructure/Data/Models/ExerciseMuscle.cs
- [X] T004 [P] Modify Exercise entity to add `[MaxLength(150)]` on Name property and add `ICollection<ExerciseMuscle> ExerciseMuscles` navigation property initialised to `[]` in src/WorkoutTracker.Infrastructure/Data/Models/Exercise.cs
- [X] T005 Update WorkoutTrackerDbContext: add `DbSet<Muscle> Muscles` and `DbSet<ExerciseMuscle> ExerciseMuscles` properties, configure Muscle entity (key on MuscleId, Name required), configure ExerciseMuscle entity (key on ExerciseMuscleId, FK to Exercise with cascade delete, FK to Muscle with cascade delete, composite unique index on ExerciseId+MuscleId), add `HasMaxLength(150)` to Exercise.Name, add unique index on `LOWER(Name)` expression for Exercise, seed 11 predefined muscles (Chest, Back, Shoulders, Biceps, Triceps, Forearms, Core, Quads, Hamstrings, Glutes, Calves) via `HasData()` with stable GUIDs in src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs
- [X] T006 Generate EF Core migration named `AddMusclesAndExerciseConstraints` and verify it creates: muscles table with seed data for 11 muscles, exercise_muscles junction table with foreign keys and composite unique index, varchar(150) constraint on exercises.name, and case-insensitive unique index using LOWER(name) on exercises in src/WorkoutTracker.Infrastructure/Data/Migrations/
- [X] T007 [P] Add CSS styles following existing BEM patterns and design tokens for: `.exercises-page` (page wrapper with `max-width: var(--max-content-width)`), `.exercise-form` and child elements (`__group`, `__label`, `__input`, `__input--error`, `__muscles`, `__error`, `__actions`, `__submit`, `__submit--loading`, `__cancel`, `__api-error`), `.muscle-toggle` and `.muscle-toggle--active` (chip-style toggle buttons in responsive grid with `min-height: var(--min-touch-target)`), `.exercise-list` and child elements (`__heading`, `__items`, `__item`, `__details`, `__name`, `__muscles`, `__muscle-chip`, `__edit-btn`, `__empty`) in src/WorkoutTracker.Web/wwwroot/css/styles.css
- [X] T008 [P] Extend WebAppFixture with in-memory mock endpoints: add `MockExercise` and `MockMuscle` record types, static `Muscles` list (11 muscles with stable GUIDs matching DbContext seed data), thread-safe `List<MockExercise>` starting empty, `GET /api/muscles` (return sorted muscles), `GET /api/exercises` (return list with muscles sorted by name), `POST /api/exercises` (trim name, validate non-empty/max 150 chars/CI uniqueness/valid muscle IDs, add to list, return 201; additionally, if the trimmed name equals exactly `"__MOCK_SERVER_ERROR"` return 500 with `{error: "An unexpected error occurred. Please try again."}` to enable server-error E2E testing), `PUT /api/exercises/{exerciseId}` (validate exists/name/muscles, update in list, return 200 or 404) in src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs

**Checkpoint**: Foundation ready — all models, migration, CSS, and test mocks are in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — Create an Exercise with a Name (Priority: P1) 🎯 MVP

**Goal**: Users can navigate to the Exercises page, enter an exercise name, submit the form, and see the exercise immediately appear in a list. Validation prevents empty, whitespace-only, overly long, and duplicate names. The form handles loading states, double-submission prevention, and server errors gracefully.

**Independent Test**: Navigate to Exercises page, enter a name, submit, verify exercise appears in list. Verify all validation scenarios (empty, whitespace, >150 chars, duplicate). Verify form clears after success, empty state shows before first exercise, submit is disabled during save, and server errors display with input preserved.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T009 [US1] Replace existing 4 placeholder tests with E2E tests for exercise creation: (1) enter valid name and submit → exercise appears in list, (2) submit empty name → "Exercise name is required." validation error shown, input has error styling, (3) submit whitespace-only name → same required error, (4) enter name >150 chars → "Exercise name must be 150 characters or fewer." error, (5) create exercise then create another with same name different case → "An exercise with this name already exists." error, (6) after successful save form clears and is ready for next entry, (7) when no exercises exist empty state message is displayed, (8) deep link to /exercises loads the exercises page correctly, (9) submit form and immediately assert `.exercise-form__submit` has `aria-disabled="true"` during the save — wait for completion and verify only one exercise is created in the list (double-submission prevention per spec edge case 3 and UX-003), (10) enter name `"__MOCK_SERVER_ERROR"` and submit → verify `.exercise-form__api-error` element is visible with `role="alert"` and displays a user-friendly error message, verify the name input still contains the entered value so the user can retry without re-entering data (server-error handling per spec edge case 4 and UX-003) in src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs

### Implementation for User Story 1

- [X] T010 [P] [US1] Add POST /api/exercises endpoint (accept `{name, muscleIds?}`, trim name, validate non-empty else 400 with "Exercise name is required.", validate ≤150 chars else 400, check CI uniqueness via `ToLower()` else 400, validate muscleIds against known muscles if provided else 400, create Exercise + ExerciseMuscle records, return 201 with `{exerciseId, name, muscles}`) and GET /api/exercises endpoint (return all exercises with muscles via Include + ThenInclude, ordered alphabetically by name, project to `{exerciseId, name, muscles: [{muscleId, name}]}`) in src/WorkoutTracker.Api/Program.cs
- [X] T011 [P] [US1] Replace exercises.ts placeholder: export `render(container)` that builds full exercise page — page title "Exercises", form with name input (`id="exercise-name"`, `maxlength="150"`, `aria-describedby="exercise-error"`), "Add Exercise" submit button, validation error div (`id="exercise-error"`, `role="alert"`, `aria-live="polite"`), API error div (`id="exercise-api-error"`, `role="alert"`, `aria-live="polite"`, class `exercise-form__api-error`), exercise list section (`id="exercise-list"`) and empty state div (`id="exercise-empty"`); on page load fetch GET /api/exercises and render list or empty state; on form submit: (a) clear any previous API error, (b) trim name, validate non-empty and ≤150 chars client-side showing inline errors with `aria-invalid="true"` on input, (c) set `isSubmitting=true`, add `aria-disabled="true"` to submit button and set text to "Saving...", (d) POST to /api/exercises, (e) on success clear form, reset submit button, re-render list, (f) on fetch error or non-2xx response display error message in `.exercise-form__api-error` with `role="alert"`, preserve all form input values for retry, reset submit button; toggle empty state visibility in src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts
- [X] T012 [US1] Verify all US1 E2E tests pass by running the test suite — confirm exercise creation, all five validation scenarios, form clearing, empty state display, deep link navigation, double-submission prevention (submit disabled during save), and server-error handling (error message shown, input preserved) work correctly

**Checkpoint**: User Story 1 is fully functional — users can create named exercises with full validation, loading states, and error handling. This is a shippable MVP.

---

## Phase 4: User Story 2 — Assign Targeted Muscles to an Exercise (Priority: P2)

**Goal**: When creating an exercise, users can optionally select one or more muscles from a predefined list of 11 muscle groups. Selected muscles are saved with the exercise and displayed as chips in the exercise list.

**Independent Test**: Create an exercise and select muscles from the toggle grid, submit, verify exercise displays with muscle chips. Create another exercise without selecting muscles, verify it succeeds with no chips. Verify all 11 muscles are displayed as toggles.

### Tests for User Story 2 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T013 [US2] Write E2E tests for muscle assignment: (1) all 11 muscle toggle buttons are displayed in the form, (2) clicking a muscle toggle adds `muscle-toggle--active` class and sets `aria-checked="true"`, clicking again deselects, (3) creating exercise with selected muscles saves them and shows muscle chips in list, (4) creating exercise without selecting any muscles succeeds and shows no muscle chips, (5) selecting multiple muscles saves and displays all of them with the exercise in src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs

### Implementation for User Story 2

- [X] T014 [P] [US2] Add GET /api/muscles endpoint returning all predefined muscles ordered alphabetically by name, projected to `{muscleId, name}` in src/WorkoutTracker.Api/Program.cs
- [X] T015 [P] [US2] Extend exercises.ts: fetch GET /api/muscles on page load (parallel with GET /api/exercises), render muscle toggle button grid inside form (`role="group"`, `aria-label="Targeted muscles"`) with one button per muscle (`role="checkbox"`, `aria-checked="false"`, `data-muscle-id`, class `muscle-toggle`), track `selectedMuscleIds` Set in module state, toggle selection on click (add/remove from Set, toggle `muscle-toggle--active` class, update `aria-checked`), include `muscleIds` array from Set in POST request body, render `exercise-list__muscles` container with `exercise-list__muscle-chip` spans in each list item when exercise has muscles in src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts
- [X] T016 [US2] Verify all US2 E2E tests pass — confirm muscle toggles display, selection/deselection, exercise creation with and without muscles, and muscle chip rendering in list

**Checkpoint**: User Stories 1 AND 2 are both functional — users can create exercises with optional muscle targeting.

---

## Phase 5: User Story 3 — View Exercise List (Priority: P3)

**Goal**: Users see all created exercises in a scrollable list on the Exercises page. Each exercise shows its name and, if assigned, its targeted muscles as chips. When no exercises exist, a friendly empty state encourages the user to add their first exercise.

**Independent Test**: Create several exercises (some with muscles, some without), verify all appear in the list with correct details. Verify empty state appears when no exercises exist.

### Tests for User Story 3 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T017 [US3] Write E2E tests for exercise list display: (1) multiple created exercises all appear in list with their names, (2) exercise with muscles displays muscle chips alongside name, (3) exercise without muscles shows only the name with no muscle container, (4) empty state with encouraging message shown when no exercises exist, (5) list heading "Your Exercises" is visible when exercises exist in src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs

### Implementation for User Story 3

- [X] T018 [US3] Refine exercise list rendering in exercises.ts: ensure `.exercise-list__empty` shows friendly guidance text (e.g., "No exercises yet. Add your first exercise above!"), conditionally render `.exercise-list__muscles` container only when exercise has muscles, ensure list heading "Your Exercises" is visible, verify list ordering matches API alphabetical ordering in src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts
- [X] T019 [US3] Verify all US3 E2E tests pass — confirm list display with multiple exercises, muscle chip rendering, name-only rendering, and empty state message

**Checkpoint**: User Stories 1, 2, AND 3 are all functional — full create and view experience is complete.

---

## Phase 6: User Story 4 — Edit an Existing Exercise (Priority: P4)

**Goal**: Users can click an edit button on any exercise in the list to populate the creation form with that exercise's current data. The form switches to "edit" mode with an "Update Exercise" button and a "Cancel" button. Submitting updates the exercise in-place; cancelling discards changes and returns to create mode.

**Independent Test**: Create an exercise, click its edit button, verify form populates with name and muscles, modify data, submit, verify list reflects update. Test cancel returns to create mode. Test all validation rules apply in edit mode.

### Tests for User Story 4 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T020 [US4] Write E2E tests for exercise editing: (1) clicking edit button on exercise populates form with its name and selected muscles, (2) form shows "Update Exercise" submit label and visible "Cancel" button in edit mode, (3) modifying name and submitting updates the exercise in list (not duplicated), (4) modifying muscle selections and submitting updates muscle chips in list, (5) clicking cancel clears form and returns to "Add Exercise" create mode, (6) clearing name in edit mode and submitting shows required validation error, (7) changing name to duplicate of a different exercise (CI) shows duplicate error, (8) changing name to own current name (no change) is allowed and succeeds, (9) clicking edit on one exercise while editing another switches form to new exercise data in src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs

### Implementation for User Story 4

- [X] T021 [P] [US4] Add PUT /api/exercises/{exerciseId} endpoint: parse exerciseId from route, look up exercise (return 404 `{error: "Exercise not found."}` if missing), accept `{name, muscleIds?}`, trim name, validate non-empty/max 150 chars/CI uniqueness excluding self, validate muscleIds against known muscles, remove existing ExerciseMuscle records and create new ones, save and return 200 with `{exerciseId, name, muscles}` in src/WorkoutTracker.Api/Program.cs
- [X] T022 [P] [US4] Extend exercises.ts with edit mode: add `.exercise-list__edit-btn` button to each list item (`aria-label="Edit {name}"`, `data-exercise-id`), track `editingExerciseId` (null = create mode, GUID = edit mode) in module state, on edit click populate name input and set muscle toggle states from exercise data, change submit button text to "Update Exercise", show `.exercise-form__cancel` button, on submit in edit mode send PUT /api/exercises/{id} with form data, on success clear form and reset to create mode and re-render list, on cancel clear form and reset editingExerciseId to null, on edit of different exercise re-populate form with new exercise data, apply same client-side validation in edit mode in src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts
- [X] T023 [US4] Verify all US4 E2E tests pass — confirm edit mode form population, update without duplication, cancel returns to create mode, validation in edit mode, own-name allowed, and exercise switching

**Checkpoint**: All four user stories are complete — full CRUD (minus delete) experience is functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification across all user stories for regression, accessibility, responsiveness, performance budgets, and resilience

- [ ] T024 [P] Run full test suite (`cd src/WorkoutTracker.Web && npm run build && cd .. && dotnet test`) to confirm zero regressions in existing Home page and navigation tests alongside all new exercise tests
- [ ] T025 [P] Verify ARIA attributes and keyboard navigation on the exercises page: tab through form fields and muscle toggles, activate toggles with Space/Enter, submit form with Enter, verify `role="alert"` error messages are announced, verify `aria-checked` toggles correctly, verify `aria-label` on edit buttons reads "Edit {name}"
- [ ] T026 Verify mobile responsive layout at 375px viewport: exercise form is usable and inputs are full-width, muscle toggle grid wraps to fit, exercise list is scrollable, all touch targets meet `var(--min-touch-target)` (44px) minimum, sidebar toggle works on mobile
- [ ] T027 Run quickstart.md verification checklist — walk through all scenarios for Exercise Creation (US1), Muscle Selection (US2), Exercise List (US3), Exercise Editing (US4), and Cross-Cutting checks to confirm complete feature delivery
- [ ] T028 Verify performance budgets PR-001 and PR-002 using Playwright: (1) write a test that navigates to /exercises with Playwright's network throttling configured to simulate slow 3G (download 500 Kbps, upload 500 Kbps, latency 400ms via CDP `Network.emulateNetworkConditions`) and asserts the exercise list is visible within 3000ms of navigation start (PR-001, SC-004), (2) write a test that fills in an exercise name and clicks submit, then measures the elapsed time until the submit button shows the loading state (`aria-disabled="true"` or "Saving..." text) and asserts it is under 200ms (PR-002) — these tests validate Constitution Principle V (Performance) delivery standards in src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phases 3–6)**: All depend on Foundational phase completion
  - User stories build on each other in priority order (see below)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **User Story 2 (P2)**: Depends on US1 — extends the exercise form with muscle toggles and the list with muscle chips
- **User Story 3 (P3)**: Depends on US2 — verifies list display including muscle chips built in US2
- **User Story 4 (P4)**: Depends on US2 — edit mode must populate muscle toggle states from US2

```text
Phase 1 (Setup) → Phase 2 (Foundational) → US1 → US2 → US3
                                                      → US4
                                                              → Phase 7 (Polish)
```

US3 and US4 can proceed in parallel after US2 is complete.

### Within Each User Story

1. Tests MUST be written first and confirmed to FAIL before implementation
2. API endpoints and frontend changes can proceed in parallel ([P] tasks)
3. Verification task runs after all implementation tasks complete
4. Story must be fully verified before starting the next priority

### Parallel Opportunities

**Phase 2** (Foundational):
- T002, T003, T004 in parallel (different model files)
- T007 and T008 in parallel with each other and with T002–T004 (independent files)
- T005 waits for T002–T004 (needs entity types), T006 waits for T005 (needs DbContext)

**Within each User Story**:
- API endpoint tasks (Program.cs) and frontend tasks (exercises.ts) are marked [P] — different files, no compile-time dependency

**Cross-Story**:
- US3 and US4 can be worked on in parallel after US2 completes

---

## Parallel Example: Phase 2 (Foundational)

```text
# Launch all model files in parallel:
Task: T002 "Create Muscle entity in Models/Muscle.cs"
Task: T003 "Create ExerciseMuscle entity in Models/ExerciseMuscle.cs"
Task: T004 "Modify Exercise entity in Models/Exercise.cs"
Task: T007 "Add CSS styles in styles.css"
Task: T008 "Extend WebAppFixture with mock endpoints"

# After T002–T004 complete, sequentially:
Task: T005 "Update DbContext"
Task: T006 "Generate migration"
```

## Parallel Example: User Story 1

```text
# Write tests first (sequential):
Task: T009 "E2E tests for exercise creation"

# Then launch implementation in parallel:
Task: T010 "POST + GET /api/exercises endpoints in Program.cs"
Task: T011 "Exercise form + list in exercises.ts"

# Then verify:
Task: T012 "Run US1 tests"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (baseline verification)
2. Complete Phase 2: Foundational (models, migration, CSS, mocks)
3. Complete Phase 3: User Story 1 (create exercise with name)
4. **STOP and VALIDATE**: Test US1 independently — users can create named exercises with full validation
5. Deploy/demo if ready — this is a useful, shippable increment

### Incremental Delivery

1. Setup + Foundational → Infrastructure ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (exercises with muscles)
4. Add User Story 3 → Test independently → Deploy/Demo (polished list view)
5. Add User Story 4 → Test independently → Deploy/Demo (edit capability)
6. Polish → Final validation → Feature complete

### Single-Agent Strategy (Recommended)

Since stories build on each other sequentially:

1. Complete Setup + Foundational phases
2. Work through stories in priority order: US1 → US2 → US3 → US4
3. Each story: write tests (fail) → implement → verify (pass)
4. Polish phase for final cross-cutting validation
5. Commit after each story checkpoint for incremental progress

---

## Analysis Issue Resolution

This tasks.md addresses the following cross-artifact analysis findings:

- **C1 (CRITICAL) — Performance verification gap**: T028 added to Phase 7. Uses Playwright CDP `Network.emulateNetworkConditions` to simulate slow 3G and asserts page load ≤ 3s (PR-001) and submission-to-feedback latency ≤ 200ms (PR-002).
- **E1 (HIGH) — Double-submission prevention untested**: T009 test case (9) added. Asserts `aria-disabled="true"` on submit button during save and verifies only one exercise is created. T011 implementation explicitly sets `aria-disabled="true"` and "Saving..." text on the submit button during the API call.
- **E2 (HIGH) — Network/server error handling untested**: T008 mock endpoint includes a `"__MOCK_SERVER_ERROR"` trigger name that returns 500. T009 test case (10) added — submits the trigger name, verifies `.exercise-form__api-error` with `role="alert"` displays a message, and verifies the form input is preserved for retry. T011 implementation handles non-2xx responses by displaying the error and preserving input.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- E2E tests use mock API endpoints in WebAppFixture, not the real database
- Real API endpoints (Program.cs) follow the same contract as mocks for production use
- The exercises.ts file is modified across US1–US4; each story extends the previous implementation
- All CSS is added once in foundational phase to avoid repeated edits to styles.css
- All mock endpoints are added once in foundational phase to avoid repeated edits to WebAppFixture.cs
- The single EF Core migration covers all schema changes (models, constraints, seed data)
- Commit after each task or story checkpoint for incremental progress
