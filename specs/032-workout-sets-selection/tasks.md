# Tasks: Workout Sets Selection

**Input**: Design documents from `/specs/032-workout-sets-selection/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/
**Delivery**: PR #159; implementation tasks complete, with one deferred pre-release measurement task

**Tests**: Automated tests are required for every user story. Write each story's tests first and confirm they fail for the intended missing behavior before implementation.

**Organization**: Tasks are grouped by user story so each increment can be implemented and tested independently. Sets is always stored once on `WorkoutSession`, never on individual logged exercises.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it changes a different file and has no dependency on unfinished work
- **[Story]**: Maps the task to a user story in `spec.md`
- Every task names the exact file or files it changes or validates

## Phase 1: Setup (Shared Test Infrastructure)

**Purpose**: Prepare existing test fixtures and unrelated regression journeys for the new required active-session Sets value and additive response fields.

- [X] T001 Extend mocked create-request capture, previous-performance responses, session-detail responses, and update responses with nullable `sets` and `previousSets` data in `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs`
- [X] T002 [P] Add `sets=3` to pre-existing direct `/active-session` navigations so unrelated workout-history regression scenarios retain valid setup in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`
- [X] T003 [P] Add `sets=3` to the direct `/active-session` navigation so reorder regression coverage retains valid setup in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the shared session-level persistence invariant required by every user story.

**⚠️ CRITICAL**: No user story implementation can be completed until this phase is finished.

- [X] T004 Add nullable `int? Sets` to `WorkoutSession` in `src/WorkoutTracker.Infrastructure/Data/Models/WorkoutSession.cs`
- [X] T005 Configure the `ck_workout_session_sets_allowed` constraint with `sets IS NULL OR sets IN (3, 5)` in `src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs`
- [X] T006 Generate the `AddSetsToWorkoutSession` EF Core migration under `src/WorkoutTracker.Infrastructure/Data/Migrations/` and update `src/WorkoutTracker.Infrastructure/Data/Migrations/WorkoutTrackerDbContextModelSnapshot.cs`

**Checkpoint**: PostgreSQL can store one nullable, constrained Sets value per workout session without backfilling legacy rows.

---

## Phase 3: User Story 1 - Choose Sets Before Starting a Workout (Priority: P1) 🎯 MVP

**Goal**: Require the user to choose 3 or 5 Sets on the "Let's go!" page, carry that value into the active workout, and persist it once when the session is saved.

**Independent Test**: Select a workout and 3 or 5 Sets, start and save the workout, and verify the created session stores the selected value; verify missing or invalid Sets cannot start or create a session.

### Tests for User Story 1 ⚠️

- [X] T007 [P] [US1] Add API integration tests for create persistence of 3 and 5, create/list response fields, rejection of missing/null/out-of-range Sets, and database-constraint enforcement in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [X] T008 [P] [US1] Add start-page E2E tests for the `Sets` label, placeholder, exact 3/5 options, field order, and reuse of workout-select CSS classes in `src/WorkoutTracker.E2ETests/E2E/HomeLandingPageSelectionTests.cs`
- [X] T009 [P] [US1] Add E2E tests for missing-Sets validation, focus and error recovery, unchanged workout-first validation, failed workout-list loading, and loading/disabled behavior in `src/WorkoutTracker.E2ETests/E2E/HomeLandingPageValidationTests.cs`
- [X] T010 [US1] Add E2E tests proving start navigation retains `sets` alongside `id` and optional `order`, save forwards one top-level Sets value, and malformed or missing direct query values cannot save in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 1

- [X] T011 [P] [US1] Render the required `Sets` dropdown directly below workout selection, offer only 3 and 5 with no default, reuse loading/disabled and error styles, validate the selection, and append `sets` alongside `id` and optional `order` in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts`
- [X] T012 [P] [US1] Parse `sets` into validated session-level state, reject malformed or missing direct-entry values, retain the value through the active workout, and send it once as a top-level create field in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T013 [US1] Extend `SessionCreateRequest`, server validation, `WorkoutSession` creation, the create response, and session-list projection with Sets in `src/WorkoutTracker.Api/Program.cs`
- [X] T014 [US1] Run and fix the US1 tests in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`, `src/WorkoutTracker.E2ETests/E2E/HomeLandingPageSelectionTests.cs`, `src/WorkoutTracker.E2ETests/E2E/HomeLandingPageValidationTests.cs`, and `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

**Checkpoint**: User Story 1 is independently usable as the MVP, and every newly saved workout contains exactly 3 or 5 Sets.

---

## Phase 4: User Story 2 - See Latest Comparable Session's Sets During the Current Workout (Priority: P2)

**Goal**: Add `3 sets` or `5 sets` to each exercise's existing `Last time` information using Sets from the same historical session selected for that exercise's usable weight or effort.

**Independent Test**: Seed prior sessions with different Sets values, open an active workout, and verify each exercise displays Sets from its own latest usable comparison session; legacy null Sets is omitted, and a newer Sets-only row does not suppress older usable weight or effort.

### Tests for User Story 2 ⚠️

- [X] T015 [P] [US2] Add `InternalsVisibleTo("WorkoutTracker.UnitTests")` without widening production types in `src/WorkoutTracker.Api/WorkoutTracker.Api.csproj`
- [X] T016 [US2] Create pure in-memory selector tests for all nine cases in `plan.md`—source Sets, fallback Sets, Sets-only regression, legacy null, per-exercise selection, ignored targets, deterministic ties, 200-session bound, and empty history—plus weight-only, effort-only, both-present, whitespace-weight, and absent predicate cases in `src/WorkoutTracker.UnitTests/Api/PreviousExerciseDataSelectorTests.cs`
- [X] T017 [P] [US2] Add API integration tests for previous-performance Sets, per-exercise source-session association, legacy nulls, and proof that a newer Sets-only row does not suppress older usable weight or effort in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [X] T018 [P] [US2] Add Playwright tests for `Last time` formatting with 3/5 Sets, middle-dot separation, legacy-null omission, preserved older-data fallback, and unchanged first-session/loading/error states in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Extend `HistoricalSessionData`, `LatestExerciseComparison`, bounded historical projection, and previous-performance response so each exercise carries Sets from its selected source session while `HasUsableComparisonData` remains weight-or-effort only in `src/WorkoutTracker.Api/Program.cs`
- [X] T020 [US2] Extend previous-performance TypeScript types and append `${sets} sets` to the existing separated `Last time` parts only when Sets is present in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T021 [US2] Run and fix the selector, previous-performance API, and active-session E2E tests in `src/WorkoutTracker.UnitTests/Api/PreviousExerciseDataSelectorTests.cs`, `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`, and `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

**Checkpoint**: User Story 2 independently displays temporally consistent historical Sets without changing latest-usable weight/effort selection semantics.

---

## Phase 5: User Story 3 - See and Edit Sets in Workout History (Priority: P3)

**Goal**: Show current and previous Sets in the historical session table and edit the one session-wide value through synchronized row controls.

**Independent Test**: Open a completed session, verify `Sets` and `Prev. Sets`, enter edit mode, change any Sets control, verify all controls synchronize, save, reload, and confirm one session-level value persisted.

### Tests for User Story 3 ⚠️

- [X] T022 [P] [US3] Add API integration tests for detail `sets` and per-exercise `previousSets`, different source sessions, legacy nulls, omitted-update preservation, explicit-null clearing, 3/5 replacement, invalid rejection, and one top-level Sets field in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [X] T023 [US3] Add Playwright tests for view-mode `Sets` and `Prev. Sets` headers, required column order, repeated session value, per-exercise previous values, and standard no-data markers in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`
- [X] T024 [US3] Add Playwright tests for synchronized Sets selects, accessible exercise-specific labels and shared-value description, dirty/discard behavior, save success, save-failure retention, and reload persistence in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 3

- [X] T025 [US3] Add current Sets and per-exercise `previousSets` to session-detail projections, and return the extended detail shape from GET and successful PUT requests in `src/WorkoutTracker.Api/Program.cs`
- [X] T026 [US3] Implement presence-aware update binding so omitted `sets` preserves, explicit null clears, 3/5 replaces, and all other values return the existing validation response in `src/WorkoutTracker.Api/Program.cs`
- [X] T027 [US3] Extend detail interfaces and view-mode rendering with columns ordered Exercise, Weight, Prev. Weight, Sets, Prev. Sets, Effort, Prev. Effort; repeat current Sets per row and use existing no-data markup in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`
- [X] T028 [US3] Add session-level Sets to the edit snapshot and dirty check, render synchronized `Not recorded`/3/5 row selects, send one top-level presence-aware field, and preserve save/cancel/discard/error state in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`
- [X] T029 [US3] Add only the table width, nowrap, and accessible horizontal-overflow rules required for the two new columns in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [X] T030 [US3] Run and fix the US3 detail/update API and history E2E tests in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` and `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

**Checkpoint**: All three user stories are independently functional; history displays and edits one session-wide Sets value consistently.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Complete accessibility, documentation, security, deterministic performance gates, comparative measurements, and full regression validation.

- [X] T031 [P] Add keyboard, label, focus, `role="alert"`/`aria-live`, and shared-session-value accessibility regression coverage in `src/WorkoutTracker.E2ETests/E2E/HomeLandingPageAccessibilityTests.cs` and `src/WorkoutTracker.E2ETests/E2E/WorkoutAccessibilityTests.cs`
- [X] T032 [P] Document required Sets selection, active-session `Last time`, history columns/editing, and legacy-session behavior in `README.md`
- [X] T033 Verify client, API, and database validation boundaries; omitted/null update semantics; parameterized data access; and unchanged session access rules in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts`, `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`, `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`, `src/WorkoutTracker.Api/Program.cs`, and `src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs`
- [X] T034 [P] Implement PB-01 and PB-02 Playwright request-count assertions and retain the PB-08 start-page `< 5000 ms` smoke ceiling in `src/WorkoutTracker.E2ETests/E2E/HomeLandingPagePerformanceTests.cs`
- [X] T035 [P] Implement PB-03 session-detail request-count and PB-09 25-exercise `< 5000 ms` smoke assertions in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`
- [X] T036 Add an EF Core command-counting test interceptor to `src/WorkoutTracker.UnitTests/Infrastructure/ApiFixture.cs`, then assert PB-04/PB-05 exactly-two-query budgets and PB-07 one-top-level-Sets contract in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [X] T037 Assert PB-06 by proving the selector ignores usable data beyond `MaxSessionsToScan = 200`, and add the PB-10 25-exercise session-create `< 2000 ms` local-host smoke assertion in `src/WorkoutTracker.UnitTests/Api/PreviousExerciseDataSelectorTests.cs` and `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [ ] T038 **Deferred pre-release follow-up**: Execute the Tier 3 procedure from `specs/032-workout-sets-selection/quickstart.md` after establishing a separate pre-feature baseline commit; seed 25 exercises and 10 historical sessions, discard 5 warm-ups, time 20 iterations per flow on both commits in the same environment/session, compute p95, verify delta is within the greater of 10% or 100 ms, and record both p95 values, delta, machine, and commit SHAs in the pull request description
- [X] T039 Run all available automated validation from `specs/032-workout-sets-selection/quickstart.md`: Release build, TypeScript build, 92 frontend tests, 167 backend tests, 280 Playwright tests, automated performance budgets, manual Sets flows, and whitespace validation. The repository has no npm `lint` script, and T038 is explicitly deferred.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Starts immediately; T002 and T003 can run in parallel after the fixture contract in T001 is understood.
- **Phase 2 — Foundational**: Depends on Phase 1 and blocks production completion of every story; T006 depends on T004 and T005.
- **Phase 3 — US1**: Depends on Phase 2. Write and observe T007-T010 failing before T011-T013; T014 validates the story.
- **Phase 4 — US2**: Depends on Phase 2. T016 depends on T015; T017 and T018 can be written in parallel. Integrated delivery should follow US1 so users can create Sets data.
- **Phase 5 — US3**: Depends on Phase 2. T023 precedes T024 because both edit the same E2E file; T025 and T026 precede frontend integration validation.
- **Phase 6 — Polish**: Depends on all stories selected for delivery. T036 depends on the completed API paths; T039 completed against the delivered implementation. T038 is a deferred pre-release follow-up because no separate pre-feature baseline commit exists.

### User Story Dependency Graph

```text
Setup → Foundational ─┬→ US1 (P1 / MVP)
                      ├→ US2 (P2)
                      └→ US3 (P3)

Recommended integrated delivery: US1 → US2 → US3 → Polish
```

US2 and US3 are independently testable after Foundational by seeding historical sessions directly, even though normal product usage creates their Sets data through US1.

### Parallel Opportunities

- T002 and T003 change separate regression files.
- T007-T009 write tests in separate files; T010 follows T002 because both touch `WorkoutHistoryTests.cs`.
- T011, T012, and T013 change separate production files after the request contract is fixed.
- T015 and T017 can proceed in parallel; T016 follows T015, while T018 changes a separate E2E file.
- T022 can proceed in parallel with T023; T024 follows T023 in the shared E2E file.
- T025/T026 backend work can proceed in parallel with the initial T027 frontend view implementation if the API contract is held fixed.
- T031, T032, T034, and T035 change separate files and can proceed in parallel.
- Different user stories can be assigned concurrently after Phase 2 because fixtures can seed each story's required state.

---

## Parallel Example: User Story 1

```text
Task T007: Session create/list API tests in SessionApiTests.cs
Task T008: Start-page field and option tests in HomeLandingPageSelectionTests.cs
Task T009: Start-page validation/state tests in HomeLandingPageValidationTests.cs

Then in parallel:
Task T011: Start-page Sets UI and query handoff in home.ts
Task T012: Active-session query validation and save payload in active-session.ts
Task T013: Session create validation and persistence in Program.cs
```

## Parallel Example: User Story 2

```text
Task T015: Grant unit-test access in WorkoutTracker.Api.csproj
Task T017: Previous-performance API tests in SessionApiTests.cs
Task T018: Active-session Last time E2E tests in WorkoutHistoryTests.cs

After T015:
Task T016: Selector matrix in PreviousExerciseDataSelectorTests.cs

Then:
Task T019: Historical selection and response projection in Program.cs
Task T020: Last time rendering in active-session.ts
```

## Parallel Example: User Story 3

```text
Task T022: Detail/update API tests in SessionApiTests.cs
Task T023: History table view tests in WorkoutHistoryTests.cs

Then:
Task T025: Detail response projection in Program.cs
Task T027: View-mode table rendering in session-detail.ts

Sequential follow-ups:
Task T026: Presence-aware update implementation in Program.cs
Task T024: Synchronized edit E2E tests in WorkoutHistoryTests.cs
Task T028: Synchronized edit implementation in session-detail.ts
Task T029: Responsive table styling in styles.css
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 shared test preparation.
2. Complete Phase 2 session-level storage and constraint.
3. Write and observe failing US1 tests T007-T010.
4. Implement T011-T013.
5. Run T014, then stop and validate User Story 1 independently.

### Incremental Delivery

1. **Foundation**: Nullable constrained session Sets storage.
2. **US1 / MVP**: Explicitly select and persist 3 or 5 Sets for every new workout.
3. **US2**: Display the selected comparison session's Sets in active-workout `Last time`.
4. **US3**: Display and edit current/previous Sets in workout history.
5. **Polish**: Accessibility, documentation, security, deterministic budgets, comparative p95 evidence, and regression validation.

### Parallel Team Strategy

1. Complete Setup and Foundational work together.
2. After Phase 2, assign US1, US2, and US3 to separate developers using seeded fixtures.
3. Merge in priority order US1 → US2 → US3.
4. Complete shared performance and regression tasks after the integrated feature stabilizes.

## Delivery Record

- PR #159 contains the delivered implementation.
- T001-T037 and T039 are complete.
- T038 remains intentionally open as a pre-release comparative p95 measurement, not as an implementation blocker.
- Delivered follow-up fixes include missing/undefined legacy Sets rendering and explicit detail-table column widths to prevent header overlap.

## Notes

- `[P]` means the task changes a different file and has no dependency on unfinished work.
- Confirm story tests fail for the intended reason before production implementation.
- Keep Sets as one nullable `WorkoutSession` property; never add it to `LoggedExercise`.
- New sessions require 3 or 5; null exists only for legacy and explicit historical-edit compatibility.
- Preserve the weight-or-effort usability predicate; Sets alone must never select a historical row.
- Reuse existing routes, styles, errors, loading states, and no-data markup rather than adding parallel patterns.
