# Tasks: Edit Past Workouts

**Input**: Design documents from `/specs/031-edit-past-workouts/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for every user story. Write the listed tests before implementation and confirm they fail for the missing behavior.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested independently. User Story 1 is the MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task includes exact file paths

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the existing session-detail and session API surfaces for past-workout editing.

- [X] T001 Inspect existing session update-adjacent tests and helper DTO patterns in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T002 [P] Inspect current session-detail edit/delete/chart rendering structure in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T003 [P] Inspect existing session-detail and discard-modal styles in src/WorkoutTracker.Web/wwwroot/css/styles.css
- [X] T004 [P] Inspect existing Web proxy route patterns for sessions in src/WorkoutTracker.Web/Program.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the shared API contract and proxy needed by every edit story.

**CRITICAL**: No user story implementation should begin until this phase is complete.

- [X] T005 Add failing API integration tests for PUT /api/sessions/{sessionId} validation errors in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T006 Add SessionUpdateRequest and SessionUpdateLoggedExerciseItem DTOs in src/WorkoutTracker.Api/Program.cs
- [X] T007 Implement PUT /api/sessions/{sessionId} route skeleton with session lookup and 404 response in src/WorkoutTracker.Api/Program.cs
- [X] T008 Implement shared update validation for overallEffort, loggedExercises, duplicate loggedExerciseId values, loggedWeight length, effort range, and session-row containment in src/WorkoutTracker.Api/Program.cs
- [X] T009 Return the existing session-detail response shape after updates by reusing or extracting the GET /api/sessions/{sessionId} projection in src/WorkoutTracker.Api/Program.cs
- [X] T010 Add PUT /api/sessions/{sessionId:guid} proxy route using the existing proxy forwarding/error pattern in src/WorkoutTracker.Web/Program.cs

**Checkpoint**: The API and proxy foundation exists; user story work can begin.

---

## Phase 3: User Story 1 - Edit Exercise Values in a Past Workout (Priority: P1) MVP

**Goal**: Users can correct, add, or clear per-exercise weight and effort values for a completed workout.

**Independent Test**: Open a completed workout from history, edit one exercise's weight and effort, save, reopen the workout, and verify the corrected values are displayed and persisted.

### Tests for User Story 1

- [X] T011 [P] [US1] Add API integration test for updating loggedWeight and effort on existing logged exercises in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T012 [P] [US1] Add API integration test for clearing optional loggedWeight and effort values to null in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T013 [P] [US1] Add API integration test proving workout identity, completedAt, exerciseId, and sequence are preserved after exercise value edits in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T014 [P] [US1] Add Playwright test for editing and saving a past exercise weight and effort from the session detail page in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
- [X] T015 [P] [US1] Add Playwright test proving corrected exercise values still appear after reopening the session detail page in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs

### Implementation for User Story 1

- [X] T016 [US1] Update PUT /api/sessions/{sessionId} to persist per-exercise LoggedWeight and Effort changes in src/WorkoutTracker.Api/Program.cs
- [X] T017 [US1] Add typed editable session state, editable exercise row payload interfaces, and original-value snapshot support in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T018 [US1] Add Edit button and edit-mode state transition for the loaded session detail page in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T019 [US1] Render editable weight inputs and effort controls for current exercise cells while keeping previous-value cells read-only in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T020 [US1] Build and send PUT /api/sessions/{sessionId} payloads for edited exercise values from src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts, normalizing cleared weight inputs to null
- [X] T021 [US1] Re-render view mode from the 200 OK update response and refresh chart data after exercise edits in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T022 [US1] Add session-detail edit input, action, disabled, and error styles for exercise editing in src/WorkoutTracker.Web/wwwroot/css/styles.css
- [X] T023 [US1] Verify exercise-edit security validation and source-data comparison behavior in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs and src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs

**Checkpoint**: User Story 1 is independently functional and testable as the MVP.

---

## Phase 4: User Story 2 - Edit Overall Workout Effort (Priority: P2)

**Goal**: Users can correct, add, or clear the overall effort value for a completed workout.

**Independent Test**: Open a completed workout with missing or incorrect overall effort, edit the overall effort, save, and verify the session detail summary and comparison context use the updated value.

### Tests for User Story 2

- [X] T024 [P] [US2] Add API integration test for updating WorkoutSession.OverallEffort through PUT /api/sessions/{sessionId} in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T025 [P] [US2] Add API integration test for clearing WorkoutSession.OverallEffort through PUT /api/sessions/{sessionId} in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
- [X] T026 [P] [US2] Add Playwright test for editing and saving overall workout effort in the session detail summary row in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
- [X] T027 [P] [US2] Add Playwright or API regression coverage proving previous overall effort comparisons reflect corrected source data in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs

### Implementation for User Story 2

- [X] T028 [US2] Update PUT /api/sessions/{sessionId} to persist OverallEffort changes together with exercise edits in src/WorkoutTracker.Api/Program.cs
- [X] T029 [US2] Render the overall effort summary row as an editable effort control in edit mode in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T030 [US2] Include overallEffort in edit snapshots, dirty checks, request payloads, and post-save re-rendering in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T031 [US2] Add responsive styling for editable overall effort summary row states in src/WorkoutTracker.Web/wwwroot/css/styles.css
- [X] T032 [US2] Verify overall effort validation, clearing, and comparison refresh behavior in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs and src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs

**Checkpoint**: User Stories 1 and 2 work independently and together.

---

## Phase 5: User Story 3 - Avoid Accidental Historical Changes (Priority: P3)

**Goal**: Users have clear save/cancel controls, discard protection, and retry behavior so historical data is only changed intentionally.

**Independent Test**: Enter edit mode, change values, cancel and discard to verify original values remain; then trigger save failure and verify unsaved edits stay available for retry.

### Tests for User Story 3

- [X] T033 [P] [US3] Add Playwright test for Cancel with no changes exiting edit mode without a discard prompt in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
- [X] T034 [P] [US3] Add Playwright test for Cancel with unsaved changes showing the discard modal, Keep editing preserving values, and Discard restoring originals in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
- [X] T035 [P] [US3] Add Playwright test for Back navigation with unsaved changes using the discard modal before leaving session detail in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
- [X] T036 [P] [US3] Add Playwright test for failed save showing an error while keeping edited values available in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs

### Implementation for User Story 3

- [X] T037 [US3] Add hasEditChanges, requestCloseEditMode, cancel, discard, and keep-editing behavior in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T038 [US3] Add accessible session edit discard modal markup and focus handling in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T039 [US3] Route Back button behavior through unsaved-change protection while edit mode is dirty in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T040 [US3] Implement saving state, disabled actions, validation error display, API failure display, and retry behavior in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T041 [US3] Hide or disable delete and chart interactions while edit mode is active in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts
- [X] T042 [US3] Add CSS for session edit discard modal integration, saving state, and validation/save error messages in src/WorkoutTracker.Web/wwwroot/css/styles.css
- [X] T043 [US3] Verify keyboard and focus behavior for Edit, Save, Cancel, discard modal, and Back interactions in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the full feature across security, UX consistency, performance, and documentation.

- [X] T044 [P] Run targeted backend tests with dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj --filter Session
- [X] T045 [P] Run targeted E2E tests with dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj --filter WorkoutHistory
- [X] T046 [P] Run frontend tests from src/WorkoutTracker.Web with npm test if TypeScript helper logic was extracted or changed
- [X] T047 Review quickstart workflow and update specs/031-edit-past-workouts/quickstart.md if implementation details changed
- [X] T048 Verify PR-001, PR-002, and PR-003 performance budgets for edit-mode entry, save feedback, and no noticeable history/session-detail load regression with a representative large-history dataset using src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs or documented manual timing
- [X] T049 Perform security review of PUT /api/sessions/{sessionId} validation and error messages in src/WorkoutTracker.Api/Program.cs and src/WorkoutTracker.Web/Program.cs
- [X] T050 Perform UX consistency review of session-detail edit controls against existing modal, effort, no-data, loading, and error patterns in src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts and src/WorkoutTracker.Web/wwwroot/css/styles.css

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational and can share the US1 edit-mode UI, but remains independently testable through overall-effort-only edits.
- **User Story 3 (Phase 5)**: Depends on edit mode from US1/US2; protects all edit flows.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other stories after Foundational.
- **US2 (P2)**: Can be implemented after Foundational, but is most efficient after US1 establishes edit-mode rendering and save plumbing.
- **US3 (P3)**: Depends on edit-mode state from US1/US2 because it protects pending edits.

### Within Each User Story

- Tests must be written and fail before implementation.
- API persistence and validation should land before frontend save wiring.
- Frontend edit rendering should land before save/cancel interaction wiring.
- Each checkpoint should be validated before continuing to the next priority story.

### Parallel Opportunities

- T002, T003, and T004 can run in parallel after T001 starts.
- T011, T012, T013, T014, and T015 can be authored in parallel because they target separate test scenarios.
- T024, T025, T026, and T027 can be authored in parallel.
- T033, T034, T035, and T036 can be authored in parallel.
- T044, T045, and T046 can run in parallel during polish.

---

## Parallel Example: User Story 1

```bash
# Write US1 tests in parallel:
Task: "T011 [US1] Add API integration test for updating loggedWeight and effort"
Task: "T012 [US1] Add API integration test for clearing optional loggedWeight and effort"
Task: "T013 [US1] Add API integration test proving identity/order preservation"
Task: "T014 [US1] Add Playwright test for editing and saving exercise values"
Task: "T015 [US1] Add Playwright test proving values persist after reopen"

# Then implement the API and UI sequence:
Task: "T016 [US1] Update PUT endpoint to persist per-exercise values"
Task: "T017 [US1] Add editable session state and snapshots"
Task: "T018-T022 [US1] Add edit button, editable rows, save payload, re-render, and styles"
```

## Parallel Example: User Story 2

```bash
# Write US2 tests in parallel:
Task: "T024 [US2] Add API integration test for updating OverallEffort"
Task: "T025 [US2] Add API integration test for clearing OverallEffort"
Task: "T026 [US2] Add Playwright test for editing overall effort"
Task: "T027 [US2] Add regression coverage for previous overall effort comparisons"
```

## Parallel Example: User Story 3

```bash
# Write US3 tests in parallel:
Task: "T033 [US3] Add no-change cancel E2E coverage"
Task: "T034 [US3] Add dirty cancel discard modal E2E coverage"
Task: "T035 [US3] Add dirty back-navigation discard modal E2E coverage"
Task: "T036 [US3] Add failed-save retry E2E coverage"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 tests T011-T015 and confirm they fail.
3. Complete US1 implementation T016-T023.
4. Validate that per-exercise weight and effort edits persist and reappear after reopening session detail.

### Incremental Delivery

1. Deliver US1 to support correcting exercise history values.
2. Deliver US2 to support correcting session-level overall effort.
3. Deliver US3 to add full accidental-change protection and save-failure resilience.
4. Complete Phase 6 validation before release.

### Notes

- [P] tasks are parallelizable because they target different files or independent scenarios.
- All user-story tasks include [US1], [US2], or [US3] labels for traceability.
- Setup, foundational, and polish tasks intentionally omit story labels.
- The feature must not introduce database migrations, new routes beyond PUT /api/sessions/{sessionId}, or controls for changing workout date/name/exercise membership/order.
