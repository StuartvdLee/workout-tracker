---
description: "Task list for latest available exercise data"
---

# Tasks: Latest Exercise Data

**Input**: Design documents from `/specs/029-latest-exercise-data/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Automated tests are REQUIRED because this is a bug fix and behavior change. Backend integration tests and E2E coverage must prove the skipped/latest-empty regression before implementation is considered complete.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. User Story 1 is the MVP and fixes the issue described in GitHub issue #128.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`)
- Every task includes an explicit file path

---

## Phase 1: Setup (Shared Context)

**Purpose**: Confirm existing implementation points and test fixtures before changing behavior.

- [X] T001 Confirm the existing API route and current single-session query for `GET /api/workouts/{workoutId}/previous-performance` in `src/WorkoutTracker.Api/Program.cs`.
- [X] T002 [P] Confirm the active-session previous data fetch and rendering branches in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [X] T003 [P] Confirm the E2E mock previous-performance route and session data structures in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs`.
- [X] T004 [P] Confirm current session-detail previous-value lookup for `GET /api/sessions/{sessionId}` in `src/WorkoutTracker.Api/Program.cs` and review rendering in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Update shared response shapes used by all user stories before story-specific tests and implementation.

**⚠️ CRITICAL**: User story work depends on these shared contract updates.

- [X] T005 [P] Extend the `PreviousExerciseDataDto` test record to include selected-row `CompletedAt` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T006 [P] Extend the `PreviousExerciseData` TypeScript interface to include optional selected-row `completedAt` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.

**Checkpoint**: Shared DTO/interface shape is ready for story implementation.

---

## Phase 3: User Story 1 - See latest available exercise data during a workout (Priority: P1) 🎯 MVP

**Goal**: Show older meaningful "Last time" data when the immediately previous workout skipped or saved empty data for an exercise.

**Independent Test**: Start a workout that contains an exercise skipped in the immediately previous workout but completed earlier; the active-session exercise card shows the older completed weight or effort.

### Tests for User Story 1 ⚠️

- [X] T007 [P] [US1] Add integration test `GetPreviousPerformance_FallsBackToOlderUsableExerciseData_WhenLatestRowIsEmpty` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T008 [P] [US1] Add integration test `GetPreviousPerformance_ReturnsLatestUsableExerciseData_WhenLatestRowHasWeightOrEffort` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T009 [P] [US1] Add Playwright regression test for active-session "Last time" fallback when the latest session skipped an exercise in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.

### Implementation for User Story 1

- [X] T010 [US1] Replace the single-latest-session previous-performance query with latest-usable-per-exercise selection in `src/WorkoutTracker.Api/Program.cs`.
- [X] T011 [US1] Ensure non-blank `loggedWeight` or non-null `effort` is required for success and sequence-only entries render as no previous data in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [X] T012 [US1] Update the mock previous-performance route to select latest usable data per exercise in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs`.
- [X] T013 [US1] Run targeted backend regression validation with `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj --filter PreviousPerformance` from `specs/029-latest-exercise-data/quickstart.md`.

**Checkpoint**: User Story 1 is complete and independently testable as the MVP.

---

## Phase 4: User Story 2 - Understand when no prior exercise data exists (Priority: P2)

**Goal**: Show a clear no-prior-data state when an exercise has no usable historical completion data.

**Independent Test**: Start a workout containing an exercise with no completed historical data or only skipped/empty entries; the exercise card shows the no-prior-data state and no misleading blank "Last time" value.

### Tests for User Story 2 ⚠️

- [X] T014 [P] [US2] Update `GetPreviousPerformance_ReturnsNoSession_WhenNoSessionsExist` expectations for the latest-available contract in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T015 [P] [US2] Add integration test `GetPreviousPerformance_ReturnsNoUsableData_WhenHistoryOnlyHasEmptyRows` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T016 [P] [US2] Add Playwright coverage for the no-prior-data display when only skipped or empty history exists in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.

### Implementation for User Story 2

- [X] T017 [US2] Ensure exercises with no usable historical rows are omitted and `hasPreviousSession` is false when none remain in `src/WorkoutTracker.Api/Program.cs`.
- [X] T018 [US2] Ensure missing per-exercise entries render `First session — no previous data` without producing blank success text in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [X] T019 [US2] Run targeted active-session E2E validation with `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj --filter WorkoutHistory` from `specs/029-latest-exercise-data/quickstart.md`.

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Keep historical comparisons stable across workout changes (Priority: P3)

**Goal**: Keep "Last time" lookup stable when workouts are reordered, partially completed, or different exercises resolve to different historical sessions.

**Independent Test**: Use history with completed, skipped, reordered, and partially completed exercise entries; each current exercise resolves by exercise identity to its newest usable prior data.

### Tests for User Story 3 ⚠️

- [X] T020 [P] [US3] Add integration test `GetPreviousPerformance_SelectsEachExerciseFromItsOwnLatestUsableSession` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T021 [P] [US3] Add integration test `GetPreviousPerformance_MatchesByExerciseId_NotSequence` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T022 [P] [US3] Add Playwright coverage for reordered active-session exercises retaining correct "Last time" data in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
- [X] T023 [P] [US3] Add integration test `GetSessionDetail_FallsBackToOlderUsableExerciseData_WhenPriorSessionRowIsEmpty` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`.
- [X] T024 [P] [US3] Add Playwright coverage for session-detail `Prev. Weight (kg)` and `Prev. Effort` columns falling back to older usable data in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.

### Implementation for User Story 3

- [X] T025 [US3] Preserve frontend previous-data matching by `exerciseId` and ignore sequence for lookup in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [X] T026 [US3] Ensure API previous-performance selection groups by `ExerciseId` and orders by `CompletedAt` then `WorkoutSessionId` before choosing rows in `src/WorkoutTracker.Api/Program.cs`.
- [X] T027 [US3] Update session-detail previous-value selection to use the same latest-usable per-exercise rule for `PreviousWeight` and `PreviousEffort` in `src/WorkoutTracker.Api/Program.cs`.
- [X] T028 [US3] Update the mock `GET /api/sessions/{sessionId}` route to use latest-usable per-exercise previous values in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs`.
- [X] T029 [US3] Verify session-detail rendering still uses existing escaped `previousWeight` and `previousEffort` cells in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`.
- [X] T030 [US3] Run combined regression validation with `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj --filter "PreviousPerformance|SessionDetail"` and `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj --filter WorkoutHistory` from `specs/029-latest-exercise-data/quickstart.md`.

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Confirm security, UX consistency, performance, and final validation across the complete feature.

- [X] T031 [P] Verify all dynamic "Last time" values still use `textContent` and not `innerHTML` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [X] T032 [P] Verify session-detail previous comparison values remain escaped and no diagnostic details are exposed in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`.
- [X] T033 [P] Verify the previous-performance and session-detail queries remain planned-workout scoped, projection-only, and bounded for long history in `src/WorkoutTracker.Api/Program.cs`.
- [X] T034 [P] Verify the E2E mock contracts match `specs/029-latest-exercise-data/contracts/api-contract.md` in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs`.
- [X] T035 Run full quickstart validation commands from `specs/029-latest-exercise-data/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion — blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational completion — MVP and core bug fix.
- **User Story 2 (Phase 4)**: Depends on Foundational completion and can be implemented after or alongside US1 once the shared endpoint semantics are in place.
- **User Story 3 (Phase 5)**: Depends on Foundational completion; should follow US1 for stable latest-usable semantics.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no dependency on US2 or US3.
- **US2 (P2)**: Can start after Phase 2 — integrates naturally with US1 but remains independently testable.
- **US3 (P3)**: Can start after Phase 2 — depends conceptually on the same latest-usable selection rule from US1.

### Within Each User Story

- Tests must be written before implementation and should fail before the fix.
- API behavior in `src/WorkoutTracker.Api/Program.cs` must be implemented before final E2E validation.
- Mock behavior in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs` must match API behavior for both previous-performance and session-detail routes before Playwright assertions are trusted.
- Validation tasks run last within each story.

---

## Parallel Execution Examples

### User Story 1

```bash
Task T007: Add backend fallback regression in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
Task T009: Add active-session fallback E2E in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T011: Update frontend no sequence-only success rendering in src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts
```

### User Story 2

```bash
Task T015: Add no-usable-data backend regression in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
Task T016: Add no-prior-data E2E coverage in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T018: Confirm missing entries render empty state in src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts
```

### User Story 3

```bash
Task T020: Add per-exercise different-session integration test in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
Task T022: Add reordered active-session E2E in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T024: Add session-detail fallback E2E in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T027: Update session-detail previous-value selection in src/WorkoutTracker.Api/Program.cs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate that a skipped latest session falls back to older usable exercise data.

### Incremental Delivery

1. Setup + Foundational → shared contract ready.
2. US1 → fixes issue #128 for skipped latest workouts.
3. US2 → confirms intentional empty state when no usable prior data exists.
4. US3 → hardens identity matching across reordered and partial histories.
5. Polish → security, UX, performance, and quickstart validation.

### Parallel Team Strategy

After Phase 2, backend regression tests in `SessionApiTests.cs`, Playwright tests in `WorkoutHistoryTests.cs`, frontend rendering checks in `active-session.ts`, and E2E mock updates in `WebAppFixture.cs` can proceed in parallel when coordinated to avoid same-file conflicts.

---

## Summary

| Phase | Tasks | User Story | Parallel Opportunities |
|---|---:|---|---|
| Phase 1: Setup | 4 | n/a | T002, T003, T004 |
| Phase 2: Foundational | 2 | n/a | T005, T006 |
| Phase 3: US1 | 7 | US1 | T007, T008, T009 |
| Phase 4: US2 | 6 | US2 | T014, T015, T016 |
| Phase 5: US3 | 11 | US3 | T020, T021, T022, T023, T024 |
| Phase 6: Polish | 5 | n/a | T031, T032, T033, T034 |

**Total tasks**: 35  
**US1 tasks**: 7  
**US2 tasks**: 6  
**US3 tasks**: 11  
**Suggested MVP scope**: Phases 1-3 (through User Story 1)  
**Format validation**: All tasks use `- [X] T###`, story tasks include `[US#]`, parallel tasks use `[P]`, and each task includes an explicit file path.
