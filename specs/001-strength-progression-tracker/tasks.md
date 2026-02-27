# Tasks: Strength Progression Tracker

**Input**: Design documents from `/specs/001-strength-progression-tracker/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize backend/frontend workspaces and quality tooling

- [x] T001 Initialize .NET 10 solution and Web API host in backend/WorkoutTracker.sln and backend/src/Api/Program.cs
- [x] T002 Initialize React TypeScript app shell in frontend/package.json and frontend/src/main.tsx
- [x] T003 [P] Configure backend formatting and analyzers in backend/.editorconfig and backend/Directory.Build.props
- [x] T004 [P] Configure frontend lint/format tooling in frontend/eslint.config.js and frontend/prettier.config.cjs
- [x] T005 [P] Add CI pipeline for backend/frontend checks in .github/workflows/ci.yml

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 Add environment templates and PostgreSQL wiring in backend/src/Api/appsettings.Development.json and backend/.env.example
- [x] T007 Create EF Core DbContext and initial schema migration in backend/src/Infrastructure/Persistence/WorkoutTrackerDbContext.cs and backend/src/Infrastructure/Persistence/Migrations/0001_InitialCreate.cs
- [x] T008 [P] Implement exercise-name normalization utility in backend/src/Application/Exercises/ExerciseNameNormalizer.cs
- [x] T009 [P] Add global validation/error middleware in backend/src/Api/Middleware/ProblemDetailsMiddleware.cs
- [x] T010 Define shared API DTO contracts in backend/src/Api/Contracts/SessionsContracts.cs and backend/src/Api/Contracts/ExercisesContracts.cs
- [x] T011 [P] Create frontend API client and shared models in frontend/src/services/apiClient.ts and frontend/src/services/models.ts
- [x] T012 Create frontend route/page skeleton for sessions, history, and progression in frontend/src/pages/SessionsPage.tsx, frontend/src/pages/HistoryPage.tsx, and frontend/src/pages/ProgressionPage.tsx
- [x] T013 Add request timing instrumentation for performance evidence in backend/src/Api/Middleware/RequestTimingMiddleware.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Log gym exercises in a session (Priority: P1) 🎯 MVP

**Goal**: Let users create a workout session and log/edit/delete exercise entries with sets, reps, weight, and timestamps

**Independent Test**: Create a session, add multiple entries, edit one entry, delete one entry, refresh, and confirm persisted values remain correct

- [x] T014 [US1] Implement create-session endpoint in backend/src/Api/Controllers/SessionsController.cs
- [x] T015 [US1] Implement add/list-session-entries endpoints in backend/src/Api/Controllers/SessionEntriesController.cs
- [x] T016 [US1] Implement update/delete-entry endpoints in backend/src/Api/Controllers/EntriesController.cs
- [x] T017 [P] [US1] Implement session and entry command service in backend/src/Application/Sessions/SessionCommandService.cs
- [x] T018 [P] [US1] Implement session and entry repositories in backend/src/Infrastructure/Repositories/WorkoutSessionRepository.cs and backend/src/Infrastructure/Repositories/ExerciseEntryRepository.cs
- [x] T019 [US1] Register persistence/services in dependency injection in backend/src/Api/Extensions/ServiceCollectionExtensions.cs
- [x] T020 [US1] Build session creation form in frontend/src/features/sessions/SessionCreateForm.tsx
- [x] T021 [US1] Build exercise entry form with numeric validation in frontend/src/features/exercises/ExerciseEntryForm.tsx
- [x] T022 [US1] Build session entry list with edit/delete actions in frontend/src/features/exercises/SessionEntriesList.tsx
- [x] T023 [US1] Connect sessions and entries API calls in frontend/src/services/sessionsApi.ts and frontend/src/services/entriesApi.ts
- [x] T024 [US1] Apply accessibility and inline validation messaging for logging flow in frontend/src/features/exercises/ExerciseEntryForm.tsx
- [x] T025 [US1] Capture save-entry p95 measurement workflow in scripts/perf/save-entry-metrics.md

**Checkpoint**: User Story 1 is independently functional and testable

---

## Phase 4: User Story 2 - Review exercise history (Priority: P2)

**Goal**: Let users view paginated historical exercise entries by exercise name

**Independent Test**: Select an exercise with prior entries and verify paginated chronological history displays date, sets, reps, and weight

- [x] T026 [US2] Implement exercise-history endpoint in backend/src/Api/Controllers/ExerciseHistoryController.cs
- [x] T027 [P] [US2] Implement paginated history query service in backend/src/Application/Exercises/ExerciseHistoryQueryService.cs
- [x] T028 [P] [US2] Add history query indexes migration in backend/src/Infrastructure/Persistence/Migrations/0002_AddExerciseHistoryIndexes.cs
- [x] T029 [US2] Build exercise history page UI in frontend/src/pages/HistoryPage.tsx
- [x] T030 [US2] Build history table/list component with pagination controls in frontend/src/features/exercises/ExerciseHistoryList.tsx
- [x] T031 [US2] Add history API integration and state handling in frontend/src/services/historyApi.ts and frontend/src/features/exercises/useExerciseHistory.ts
- [x] T032 [US2] Apply keyboard/focus/error/empty-state UX consistency in frontend/src/features/exercises/ExerciseHistoryList.tsx
- [x] T033 [US2] Capture history-load p95 measurement workflow in scripts/perf/history-load-metrics.md

**Checkpoint**: User Stories 1 and 2 both work independently

---

## Phase 5: User Story 3 - Compare new performance to prior performance (Priority: P3)

**Goal**: Show progression results (latest-vs-previous and latest-vs-best) for logged entries

**Independent Test**: Log a new entry where prior entries exist and confirm comparison states show improved/unchanged/declined or no-baseline correctly

- [x] T034 [US3] Implement entry-comparison endpoint in backend/src/Api/Controllers/EntryComparisonController.cs
- [x] T035 [P] [US3] Implement progression comparison service in backend/src/Application/Progression/ProgressComparisonService.cs
- [x] T036 [P] [US3] Implement repository queries for previous/best entry retrieval in backend/src/Infrastructure/Repositories/ExerciseEntryRepository.cs
- [x] T037 [US3] Build progression comparison component in frontend/src/features/progression/ProgressComparisonCard.tsx
- [x] T038 [US3] Integrate comparison retrieval after save and from history in frontend/src/features/progression/useProgressComparison.ts
- [x] T039 [US3] Implement comparison status messaging and visual states in frontend/src/features/progression/ProgressComparisonCard.tsx
- [x] T040 [US3] Validate accessibility and terminology consistency in progression UI in frontend/src/features/progression/ProgressComparisonCard.tsx
- [x] T041 [US3] Capture comparison-response p95 measurement workflow in scripts/perf/comparison-metrics.md

**Checkpoint**: All user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T047 [US1] Add backend unit tests for session and entry command validation in backend/tests/unit/Application/Sessions/SessionCommandServiceTests.cs
- [x] T048 [US1] Add backend integration tests for create/list/update/delete entry flows in backend/tests/integration/Api/SessionEntriesApiTests.cs
- [x] T049 [US1] Add frontend integration tests for session + entry logging flow in frontend/tests/integration/logging-flow.test.tsx
- [x] T050 [US2] Add backend integration tests for paginated exercise history endpoint in backend/tests/integration/Api/ExerciseHistoryApiTests.cs
- [x] T051 [US2] Add frontend integration tests for history pagination and empty/error states in frontend/tests/integration/history-flow.test.tsx
- [x] T052 [US3] Add backend unit tests for progression status computation in backend/tests/unit/Application/Progression/ProgressComparisonServiceTests.cs
- [x] T053 [US3] Add backend integration tests for entry comparison endpoint in backend/tests/integration/Api/EntryComparisonApiTests.cs
- [x] T054 [US3] Add frontend integration tests for progression comparison states in frontend/tests/integration/progression-flow.test.tsx
- [x] T055 [US1] [US2] [US3] Add end-to-end critical journey tests for logging, history, and progression in e2e/tests/strength-progression.spec.ts

- [x] T042 [P] Consolidate API validation messages and error mapping in backend/src/Api/Middleware/ProblemDetailsMiddleware.cs and frontend/src/services/apiErrorMapper.ts
- [x] T043 [P] Add structured request logging and correlation IDs in backend/src/Api/Middleware/RequestLoggingMiddleware.cs
- [x] T044 Update quickstart validation steps with final commands/evidence in specs/001-strength-progression-tracker/quickstart.md
- [x] T045 Tune query/projection performance for history and comparison paths in backend/src/Application/Exercises/ExerciseHistoryQueryService.cs and backend/src/Application/Progression/ProgressComparisonService.cs
- [x] T046 Document final API usage examples aligned with contract in specs/001-strength-progression-tracker/contracts/exercise-api.yaml

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 completion; blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2 completion; defines MVP
- **Phase 4 (US2)**: Depends on Phase 2 and uses entities/services from US1
- **Phase 5 (US3)**: Depends on Phase 2 and consumes exercise-history/session-entry data produced in US1/US2
- **Phase 6 (Polish)**: Depends on completion of desired user stories and required automated test implementation

### User Story Dependency Graph

- **US1 (P1)** → enables initial persisted logging workflows
- **US2 (P2)** → can start after foundational work, but practically builds on entry/session data from US1
- **US3 (P3)** → relies on prior entry data and history retrieval from US1/US2

### Within Each User Story

- API/service/repository changes before frontend integration
- Validation and UX consistency updates before performance evidence capture
- Story checkpoint must pass before moving to next priority story

---

## Parallel Execution Examples

### User Story 1

- Run in parallel:
  - T017 and T018 (service + repository in different files)
  - T020 and T021 (session form + entry form)

### User Story 2

- Run in parallel:
  - T027 and T028 (query service + index migration)
  - T029 and T031 (history UI page + API integration hook)

### User Story 3

- Run in parallel:
  - T035 and T036 (comparison service + repository query updates)
  - T037 and T038 (comparison component + integration hook)

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phases 1 and 2
2. Complete Phase 3 (US1)
3. Validate US1 independently and demo persisted logging flow

### Incremental Delivery

1. Deliver US1 (logging)
2. Add US2 (history)
3. Add US3 (progression comparison)
4. Execute Polish phase for cross-cutting quality/performance improvements

### Team Parallelization

1. One engineer completes foundational backend/database work
2. One engineer builds frontend shells and forms in parallel after foundational checkpoints
3. Additional engineers split US2 and US3 once US1 data contracts stabilize
