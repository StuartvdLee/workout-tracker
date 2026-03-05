# Tasks: Simplified Homepage Session Start

**Input**: Design documents from `/specs/002-simplify-homepage-workout-type/`
**Prerequisites**: `plan.md` (required), `spec.md` (required for user stories), `research.md`, `data-model.md`, `contracts/`

**Tests**: Automated tests are REQUIRED for all behavior changes in this feature (backend integration/unit, frontend integration, and E2E regression for primary flow).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare test and contract scaffolding for this feature.

- [ ] T001 Add feature API contract file at specs/002-simplify-homepage-workout-type/contracts/session-start-api.yaml
- [ ] T002 Add quickstart validation steps for this feature in specs/002-simplify-homepage-workout-type/quickstart.md
- [ ] T003 [P] Add backend integration test scaffold for session start in backend/tests/integration/Api/SessionsControllerTests.cs
- [ ] T004 [P] Add frontend integration test scaffold for homepage flow in frontend/tests/integration/simplified-homepage.test.tsx
- [ ] T005 [P] Add Playwright E2E spec scaffold in e2e/tests/simplified-homepage-session-start.spec.ts

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core shared model/contract changes that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T006 Update session request/response contracts for workout type and server timestamp in backend/src/Api/Contracts/SessionsContracts.cs
- [ ] T007 Update session command model for required workout type in backend/src/Application/Sessions/SessionCommandService.cs
- [ ] T008 Persist workout type and server-generated startedAt in backend/src/Infrastructure/Persistence/WorkoutTrackerDbContext.cs
- [ ] T009 [P] Add backend validation helper for allowed workout types in backend/src/Application/Sessions/SessionCommandService.cs
- [ ] T010 [P] Add frontend workout type enum/model in frontend/src/services/models.ts
- [ ] T011 [P] Update frontend session API payload mapping for workout type in frontend/src/services/sessionsApi.ts
- [ ] T012 Add API error mapping for missing workout type validation in frontend/src/services/apiErrorMapper.ts

**Checkpoint**: Foundation ready — user stories can now be implemented independently.

---

## Phase 3: User Story 1 - Start Session From Minimal Homepage (Priority: P1) 🎯 MVP

**Goal**: User can select workout type and start a session from a minimal homepage.

**Independent Test**: Load homepage, verify only title/dropdown/button are shown, select valid workout type, start session, verify created session includes workout type and startedAt.

### Tests for User Story 1 (REQUIRED) ✅

- [ ] T013 [P] [US1] Add backend integration test for successful session creation with workout type in backend/tests/integration/Api/SessionsControllerTests.cs
- [ ] T014 [P] [US1] Add backend unit test for startedAt server-side assignment in backend/tests/unit/Application/Sessions/SessionCommandServiceTests.cs
- [ ] T015 [P] [US1] Add frontend integration test for minimal homepage controls and successful submit in frontend/tests/integration/simplified-homepage.test.tsx
- [ ] T016 [P] [US1] Add E2E test for select workout type + start session flow in e2e/tests/simplified-homepage-session-start.spec.ts

### Implementation for User Story 1

- [ ] T017 [US1] Replace homepage content with title, workout type dropdown, and start button in frontend/src/App.tsx
- [ ] T018 [US1] Implement homepage session-start UI state and submit handler in frontend/src/App.tsx
- [ ] T019 [US1] Support workout type + server-generated startedAt creation path in backend/src/Api/Controllers/SessionsController.cs
- [ ] T020 [US1] Ensure session persistence includes workoutType and startedAt in backend/src/Infrastructure/Persistence/WorkoutTrackerDbContext.cs
- [ ] T021 [US1] Return created session payload with workoutType and startedAt in backend/src/Api/Contracts/SessionsContracts.cs
- [ ] T022 [US1] Validate accessibility for label, keyboard flow, and focus visibility in frontend/src/App.tsx
- [ ] T023 [US1] Record US1 timing evidence for homepage render and start-session latency in scripts/perf/save-entry-metrics.md

**Checkpoint**: US1 is fully functional and independently testable (MVP).

---

## Phase 4: User Story 2 - Prevent Invalid Session Start (Priority: P2)

**Goal**: User gets clear validation error and no session is created when workout type is not selected.

**Independent Test**: Attempt Start Session with no selected workout type; verify visible error and no backend session creation.

### Tests for User Story 2 (REQUIRED) ✅

- [ ] T024 [P] [US2] Add backend integration test rejecting missing workout type in backend/tests/integration/Api/SessionsControllerTests.cs
- [ ] T025 [P] [US2] Add backend unit test for allowed workout type validation in backend/tests/unit/Application/Sessions/SessionCommandServiceTests.cs
- [ ] T026 [P] [US2] Add frontend integration test for required-field error and clear-on-select in frontend/tests/integration/simplified-homepage.test.tsx
- [ ] T027 [P] [US2] Add E2E regression test for invalid submit error state in e2e/tests/simplified-homepage-session-start.spec.ts

### Implementation for User Story 2

- [ ] T028 [US2] Enforce required workout type validation in backend session command handling in backend/src/Application/Sessions/SessionCommandService.cs
- [ ] T029 [US2] Return consistent validation problem response for missing workout type in backend/src/Api/Controllers/SessionsController.cs
- [ ] T030 [US2] Show field-level error on submit without selection and clear on valid selection in frontend/src/App.tsx
- [ ] T031 [US2] Map backend validation errors to homepage form message in frontend/src/services/apiErrorMapper.ts
- [ ] T032 [US2] Validate accessibility association of error text to workout type field in frontend/src/App.tsx
- [ ] T033 [US2] Record validation-flow timing/regression evidence in scripts/perf/save-entry-metrics.md

**Checkpoint**: US2 is independently testable and does not require US3.

---

## Phase 5: User Story 3 - Remove Legacy Homepage Elements (Priority: P3)

**Goal**: Homepage no longer displays Session/History/Progression links or Add Exercise Entry section/content.

**Independent Test**: Open homepage and confirm legacy navigation and Add Exercise Entry section are absent.

### Tests for User Story 3 (REQUIRED) ✅

- [ ] T034 [P] [US3] Add frontend integration test asserting legacy links/sections are removed in frontend/tests/integration/simplified-homepage.test.tsx
- [ ] T035 [P] [US3] Add E2E test asserting legacy homepage elements are absent in e2e/tests/simplified-homepage-session-start.spec.ts

### Implementation for User Story 3

- [ ] T036 [US3] Remove Session/History/Progression homepage links and Add Exercise Entry section in frontend/src/App.tsx
- [ ] T037 [US3] Remove homepage-only obsolete composition related to Add Exercise Entry in frontend/src/App.tsx (no changes to non-homepage pages)
- [ ] T038 [US3] Remove unused imports/services now detached from homepage in frontend/src/App.tsx
- [ ] T039 [US3] Validate homepage copy consistency and keyboard navigation after element removal in frontend/src/App.tsx
- [ ] T040 [US3] Record before/after homepage screenshot evidence in specs/002-simplify-homepage-workout-type/quickstart.md

**Checkpoint**: US3 is independently testable and preserves non-homepage flows.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification across all stories.

- [ ] T041 [P] Run backend full test suite for this feature in backend/tests/integration/Backend.IntegrationTests.csproj
- [ ] T042 [P] Run frontend integration suite for homepage changes in frontend/tests/integration/simplified-homepage.test.tsx
- [ ] T043 [P] Run E2E suite for homepage session start in e2e/tests/simplified-homepage-session-start.spec.ts
- [ ] T044 Verify quickstart end-to-end walkthrough accuracy in specs/002-simplify-homepage-workout-type/quickstart.md
- [ ] T045 Update feature notes with final evidence links in specs/002-simplify-homepage-workout-type/plan.md
- [ ] T046 [P] Add frontend regression tests for direct routes to Sessions/History/Progression pages to confirm unchanged behavior in frontend/tests/integration/simplified-homepage-regression.test.tsx
- [ ] T047 [P] Add backend regression tests confirming existing non-homepage sessions/history/progression endpoint behavior remains unchanged in backend/tests/integration/Api/Regression/ExistingFlowsRegressionTests.cs
- [ ] T048 Verify and document FR-012 regression evidence for non-homepage flows in specs/002-simplify-homepage-workout-type/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1 completion; blocks all user story work.
- **Phase 3+ (User Stories)**: Depend on Phase 2 completion.
- **Phase 6 (Polish)**: Depends on completion of desired user stories.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; no dependency on other stories.
- **US2 (P2)**: Starts after Phase 2; can run independently of US3.
- **US3 (P3)**: Starts after Phase 2; functionally independent of US1/US2 but typically merged after US1 for MVP flow stability.

### Within Each User Story

- Tests first and expected to fail before implementation.
- Backend contract/validation before frontend integration behaviors where applicable.
- Core behavior implementation before accessibility/performance evidence tasks.

---

## Parallel Opportunities

- Setup tasks marked `[P]`: T003, T004, T005.
- Foundational tasks marked `[P]`: T009, T010, T011.
- US1 parallel test tasks: T013, T014, T015, T016.
- US2 parallel test tasks: T024, T025, T026, T027.
- US3 parallel test tasks: T034, T035.
- Polish parallel verification tasks: T041, T042, T043.
- Polish parallel verification tasks: T041, T042, T043, T046, T047.

---

## Parallel Example: User Story 1

```bash
# Run US1 tests in parallel workstreams:
Task T013: backend integration test in backend/tests/integration/Api/SessionsControllerTests.cs
Task T014: backend unit test in backend/tests/unit/Application/Sessions/SessionCommandServiceTests.cs
Task T015: frontend integration test in frontend/tests/integration/simplified-homepage.test.tsx
Task T016: e2e flow test in e2e/tests/simplified-homepage-session-start.spec.ts
```

## Parallel Example: User Story 2

```bash
# Implement backend/frontend validation tracks in parallel after tests:
Task T028: backend validation logic in backend/src/Application/Sessions/SessionCommandService.cs
Task T030: frontend validation rendering in frontend/src/App.tsx
Task T031: frontend API error mapping in frontend/src/services/apiErrorMapper.ts
```

## Parallel Example: User Story 3

```bash
# Remove legacy homepage surfaces in parallel where files do not overlap:
Task T036: remove legacy homepage links/sections in frontend/src/App.tsx
Task T037: remove homepage-only obsolete composition related to Add Exercise Entry in frontend/src/App.tsx
Task T040: update screenshot evidence in specs/002-simplify-homepage-workout-type/quickstart.md
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Validate US1 independently via T013–T023.
4. Demo/deploy MVP if acceptable.

### Incremental Delivery

1. Deliver US1 (MVP).
2. Deliver US2 validation hardening.
3. Deliver US3 content removal and cleanup.
4. Run Phase 6 full verification.

### Parallel Team Strategy

1. Team completes Setup + Foundational together.
2. After Phase 2:
   - Dev A: US1
   - Dev B: US2
   - Dev C: US3
3. Consolidate with Polish phase and release evidence.
