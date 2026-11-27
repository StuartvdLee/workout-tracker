---
description: "Tasks for resuming an in-progress workout after a refresh"
---

# Tasks: Resume an In-Progress Workout After a Refresh

**Input**: Design documents in `specs/035-resume-active-workout/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Focused storage, PostgreSQL and browser tests were added and run. Actual results and their scope are recorded in [quickstart.md](./quickstart.md).

**Format**: `[ID] [P?] [Story] Description` — `[P]` marks the original opportunities for parallel work.

## Phase 1: Setup

**Purpose**: Prepare the existing project and test environment without adding dependencies.

- [X] T001 Check the existing frontend, PostgreSQL integration and Playwright prerequisites and commands in `specs/035-resume-active-workout/quickstart.md`; record unavailable prerequisites instead of treating skipped checks as passes

## Phase 2: Foundational

**Purpose**: Establish shared draft types.

- [X] T002 Define typed draft, entry and frozen-completion structures in `src/WorkoutTracker.Web/wwwroot/ts/active-workout-draft.ts`, following [data-model.md](./data-model.md) for `Active Workout Draft.version`, `Active Workout Draft.draftId`, `Active Workout Draft.workoutId`, `Active Workout Draft.sets`, `Active Workout Draft.exerciseOrder`, `Active Workout Draft.entries`, `Active Workout Draft.completion`, `Active Workout Draft Entry.loggedWeight` and `Active Workout Draft Entry.loggedEffort`

**Checkpoint**: Shared types, validation, storage and lifecycle behavior are implemented.

## Phase 3: User Story 1 — Restore an Active Workout (Priority: P1)

**Goal**: Automatically save and restore an active workout, its selected sets, entered values and committed exercise order.

**Independent Test**: Select sets, start a workout, save changed weight, effort and order, refresh twice with another edit between refreshes, and verify the saved values, selected sets and existing history.

### Tests for User Story 1

- [X] T003 [P] [US1] Add draft round-trip tests for workout identity, 3/5 sets, weight, untouched/touched effort and committed order in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/active-workout-draft.test.ts`
- [X] T004 [P] [US1] Add browser tests for saved values and sets surviving refresh, a second edit and refresh, no draft on an empty visit, unchanged history, and two-tab draft-lock contention, release and retry in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 1

- [X] T005 [US1] Implement version-1 bounded local-storage serialization and validated read/write in `src/WorkoutTracker.Web/wwwroot/ts/active-workout-draft.ts`; implement [data-model.md](./data-model.md) constraints for `Active Workout Draft.version`, `Active Workout Draft.draftId`, `Active Workout Draft.workoutId`, `Active Workout Draft.sets`, `Active Workout Draft.exerciseOrder`, `Active Workout Draft.entries`, `Active Workout Draft.completion`, `Active Workout Draft Entry.loggedWeight` and `Active Workout Draft Entry.loggedEffort`, including byte and exercise-count limits, exercise-ID uniqueness and membership checks
- [X] T006 [US1] Integrate initial draft creation, hydration after workout loading, weight/effort updates and bounded automatic saves in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`; restore selected sets and values without changing previous-performance or accessible order controls
- [X] T007 [US1] Persist only committed exercise-order changes and preserve the original order when editing is cancelled in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T008 [US1] Implement exclusive Web Lock acquisition, re-read after acquisition, ownership through the active flow, contention retry and explicit unavailable-lock failure in `src/WorkoutTracker.Web/wwwroot/ts/active-workout-draft.ts`, `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` and `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`; prevent overwriting a different draft and provide explicit resume or discard choices

**Checkpoint**: The refresh journey and the completion/discard and failure-safety stories are implemented.

## Phase 4: User Story 2 — Finish or Discard a Workout (Priority: P2)

**Goal**: Clear completed or discarded drafts and prevent duplicate completed sessions after repeated or interrupted completion.

**Independent Test**: Complete an active draft, repeat the completion after a lost response and refresh, and verify exactly one history entry and no restored draft; discard another draft and verify no new history entry.

### Tests for User Story 2

- [X] T009 [P] [US2] Add PostgreSQL-backed create/replay tests for identical and conflicting concurrent requests, ID-only replay after history edits, changed workout, transient commit retry, deleted-session replay, legacy requests and unchanged history in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [X] T010 [P] [US2] Add browser tests for successful completion and discard cleanup, repeat submission, refresh after a committed response is lost, two-tab completion contention, lock release/recovery and exactly one history entry in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 2

- [X] T011 [US2] Add the receipt entity, unique key and additive migration in `src/WorkoutTracker.Infrastructure/Data/Models/WorkoutCompletionReceipt.cs`, `src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs` and `src/WorkoutTracker.Infrastructure/Data/Migrations/`; implement [data-model.md](./data-model.md) constraints for `Completion Receipt.operation UUID`, `Completion Receipt.canonical request fingerprint`, `Completion Receipt.workout ID` and `Completion Receipt.resulting session ID`, including receipt retention after session deletion
- [X] T012 [US2] Extend the existing create DTO and POST in `src/WorkoutTracker.Api/Program.cs` with optional operation ID, deterministic fingerprint, atomic session/receipt transaction, concurrency recovery, ID-only replay after edits, replay-before-workout-validation, conflict and deleted-session responses as defined in `specs/035-resume-active-workout/contracts/api-contract.md`
- [X] T013 [US2] Preserve the create request body and response status/body through `src/WorkoutTracker.Web/Program.cs`, including replay and error responses; enforce the documented request-size limit
- [X] T014 [US2] Extend the browser mock create endpoint and fault injection for ID-only idempotent replay, conflicts and lost committed responses in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs`
- [X] T015 [US2] Persist the frozen completion with operation UUID equal to the existing draft ID before POST, accept both 201 and ID-only 200 responses, and reconcile uncertain or conflicting outcomes without generating another ID in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` and `src/WorkoutTracker.Web/wwwroot/ts/active-workout-draft.ts`; implement [data-model.md](./data-model.md) constraints for `Frozen Completion Operation.operation UUID`, `Frozen Completion Operation.workout ID` and `Frozen Completion Operation.create payload`
- [X] T016 [US2] Remove the draft after confirmed completion or explicit discard, preserve history, and handle failed removal without reporting success in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

**Checkpoint**: Completion, retry and discard can be tested using a saved draft; duplicate protection survives a lost response and deletion of the completed session.

## Phase 5: User Story 3 — Recover from Save or Restore Problems (Priority: P2)

**Goal**: Give actionable feedback and safe recovery for unavailable, invalid or failed draft storage without losing visible input.

**Independent Test**: Force a storage write failure and malformed or unreadable saved data; verify warnings, retry/clear actions, retained in-page input, successful later refresh and unchanged history.

### Tests for User Story 3

- [X] T017 [P] [US3] Add tests for malformed JSON, version 1 and unknown versions, 100/101-code-unit weights, 1,000/1,001 exercises, 1,048,576/1,048,577-byte drafts, invalid fields, duplicate exercise IDs and storage read/write/remove failures in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/active-workout-draft.test.ts`; cover mismatched workouts in the browser integration suite
- [X] T018 [P] [US3] Add browser tests for save failure and retry, invalid, oversized or unavailable restore, unavailable locking, explicit recovery, cleanup failures and preserved history in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 3

- [X] T019 [US3] Return distinct missing, invalid and unavailable storage outcomes without silently replacing data in `src/WorkoutTracker.Web/wwwroot/ts/active-workout-draft.ts`, enforcing the `Active Workout Draft` field definitions in [data-model.md](./data-model.md)
- [X] T020 [US3] Add accessible saved, failure, retry and restore-conflict feedback in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`; preserve live inputs on failure and never claim a failed write succeeded
- [X] T021 [US3] Add retry for unavailable storage and explicit recovery or clearing for invalid, missing-workout and conflicting drafts in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`; retain the frozen operation while completion is uncertain
- [X] T022 [US3] Reuse existing `src/WorkoutTracker.Web/wwwroot/css/styles.css` styles and modal conventions for status, retry and conflict controls; no CSS change was needed

**Checkpoint**: Failed storage and restore operations are visible and recoverable; no failure silently deletes progress or changes completed history.

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T023 [P] Update `README.md` with refresh recovery, explicit discard, retry behavior and the same-browser-profile storage limitation
- [X] T024 [P] Add request-observation and 25-exercise performance verification for PB-01, PB-02 and PB-03 in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`, using the exact 100-trial thresholds and measurement points in `specs/035-resume-active-workout/plan.md`
- [X] T025 Verify the 100 refresh, completion/retry and discard outcomes required by SC-001, SC-003 and SC-004, and record environment, timings and results in `specs/035-resume-active-workout/quickstart.md`
- [X] T026 Run the frontend build and tests plus the session, history and accessible reorder suites listed in `specs/035-resume-active-workout/quickstart.md`; record actual results and any unavailable prerequisites there
- [X] T027 Request **SE: Security** review of the draft storage, untrusted inputs, idempotent API and receipt retention described in `specs/035-resume-active-workout/plan.md`; address findings and record the outcome in `specs/035-resume-active-workout/quickstart.md`
- [X] T028 Record deployment observations and the user-managed SSO assumption in `specs/035-resume-active-workout/quickstart.md`; no application-level SSO configuration was added
- [X] T029 Add durable rejection fences, bounded retention admission, unkeyed versioned fingerprints and the forward-only fingerprint transition in `src/WorkoutTracker.Api/Program.cs` and `src/WorkoutTracker.Infrastructure/Data/`

## Original Dependencies & Execution Order

### Phase Dependencies

- **Setup → Foundation**: T001 precedes T002; no new dependencies or application identity are introduced.
- **US1 → US2**: US2 integrates the draft and active-session lifecycle built in US1; its API tests and database/API work can start separately after T002, but completion integration requires US1.
- **US1 → US3**: US3 extends US1's storage and restore path. Cleanup-failure and uncertain-completion recovery also depend on US2.
- **Polish**: T023–T028 are complete; verification results, review scope and deployment observations are recorded in [quickstart.md](./quickstart.md).
- The original plan avoided concurrent changes to the same file.

### Story Dependency Graph

```text
Setup → Foundation → US1 ─┬→ US2 ─┐
                          └→ US3 ←─┘
                                  ↓
                              Polish/release
```

US3's save/restore test cases depended on US1; its completion-cleanup cases also depended on US2. US2's backend work could proceed alongside US1 after the foundation.

### Original Parallel Opportunities

- **US1**: T003 and T004 targeted separate frontend and browser-test files.
- **US2**: T009 and T010 targeted separate API and browser-test files; T012 and T014 targeted separate implementation files.
- **US3**: T017 and T018 targeted separate test files.
- **Cross-cutting**: T023 and T024 targeted separate files.

## Implementation Strategy

1. Setup, shared types and US1 refresh restoration were completed.
2. US2 completion, discard and idempotent retry were implemented and exercised against PostgreSQL and a browser mock.
3. US3 storage failure and invalid-data recovery were implemented and tested.
4. Cross-cutting documentation, measured performance and security review were completed; SSO remains externally managed.

## Notes

- Every task uses an explicit path and sequential ID; `[US1]`, `[US2]` and `[US3]` apply only within their respective story phases.
- `[P]` records planned parallelism, not a claim that tasks actually ran concurrently.
- Field constraints are canonical in `data-model.md`; implementing tasks cite their exact entity and field rather than duplicating their rules.
