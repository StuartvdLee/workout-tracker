# Tasks: Previous Exercise Performance in Active Workout

**Input**: Design documents from `/specs/008-workout-exercise-history/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Automated tests are REQUIRED for every user story and every bug fix.
Include the appropriate unit, integration, contract, or end-to-end coverage
needed to prove behavior before implementation is complete.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story, with explicit work for security, user
experience consistency, and performance verification where applicable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- Exact file paths are included in every description

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Add the backend endpoint and web proxy that all frontend and test work depends on. No migrations needed — this feature reads existing data only.

**⚠️ CRITICAL**: No frontend or integration test work for either user story can complete end-to-end until this phase is done.

- [X] T001 Add `GET /api/workouts/{workoutId}/previous-performance` endpoint to `src/WorkoutTracker.Api/Program.cs` — validate workoutId exists (return `404 { "error": "Workout not found." }` if not); query `WorkoutSessions` filtered by `PlannedWorkoutId == workoutId`, ordered by `EF.Property<DateTime>(ws, "CompletedAt")` descending, `Include(ws => ws.LoggedExercises)`, `FirstOrDefaultAsync()`; return `{ hasPreviousSession: bool, completedAt: DateTime?, exercises: [{ exerciseId, loggedWeight, effort }] }` — `hasPreviousSession: false` and empty `exercises` array when no session found
- [X] T002 [P] Add proxy route `GET /api/workouts/{workoutId:guid}/previous-performance` to `src/WorkoutTracker.Web/Program.cs` — forward to API backend using `httpClientFactory.CreateClient("api").GetAsync($"/api/workouts/{workoutId}/previous-performance")`; return content with original status code; log errors with `WebProxyLog.ProxyError` on exception (same pattern as `GET /api/workouts/{workoutId}` directly above in the file)

**Checkpoint**: API endpoint and proxy are live — frontend and test streams can proceed in parallel

---

## Phase 2: User Story 1 — See Previous Weight and Effort on Workout Start (Priority: P1) 🎯 MVP

**Goal**: When a user starts an active session from a planned workout they have completed before, each exercise card shows "Last time: 80 KG · 7 — Hard" (or partial data, or "First session — no previous data") above the input fields. If the fetch fails, the session still loads with "Could not load previous data" shown per exercise.

**Independent Test**: Complete a planned workout session with weight and effort recorded for all exercises. Start a new session for the same planned workout. Confirm each exercise row shows the previously recorded weight and effort. Confirm input fields are unaffected and the session can be saved normally.

### Tests for User Story 1

- [X] T003 [P] [US1] Add integration test `GetPreviousPerformance_ReturnsNoSession_WhenNoSessionsExist` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — create a planned workout (with one exercise), do NOT create any session, GET `/api/workouts/{workoutId}/previous-performance`; assert 200, `hasPreviousSession: false`, `exercises` is empty array
- [X] T004 [P] [US1] Add integration test `GetPreviousPerformance_ReturnsWeightAndEffort_FromLastSession` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — create a planned workout, POST one session with `loggedWeight: "80"` and `effort: 7` for one exercise, GET `/api/workouts/{workoutId}/previous-performance`; assert 200, `hasPreviousSession: true`, `exercises[0].loggedWeight == "80"`, `exercises[0].effort == 7`, `exercises[0].exerciseId` matches the logged exercise
- [X] T005 [P] [US1] Add integration test `GetPreviousPerformance_Returns404_WhenWorkoutNotFound` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — GET `/api/workouts/{randomGuid}/previous-performance`; assert 404 with `{ "error": "Workout not found." }`
- [X] T006 [P] [US1] Add integration test `GetPreviousPerformance_HandlesPartialData_WhenFieldsAreNull` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — POST a session with `loggedWeight: "60"` but `effort: null` for exercise A, and `loggedWeight: null` and `effort: null` for exercise B; GET previous-performance; assert exercise A has `loggedWeight: "60"` and `effort: null`; assert exercise B has both null

### Implementation for User Story 1

- [X] T007 [P] [US1] Add `PreviousExerciseData` and `PreviousPerformance` TypeScript interfaces near the top of `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — `PreviousExerciseData: { readonly exerciseId: string; readonly loggedWeight: string | null; readonly effort: number | null; }` and `PreviousPerformance: { readonly hasPreviousSession: boolean; readonly completedAt: string | null; readonly exercises: PreviousExerciseData[]; }`
- [X] T008 [US1] Refactor `loadWorkout()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` to use `Promise.allSettled` — fetch `GET /api/workouts/${workoutId}` and `GET /api/workouts/${workoutId}/previous-performance` concurrently; if workout fetch fails or returns non-ok, show existing error state; if previous-performance fetch fails or returns non-ok, pass `'error'` sentinel to `renderExerciseInputs()`; if both succeed, parse previous performance and build a `Map<string, PreviousExerciseData>` keyed by `exerciseId`, pass to `renderExerciseInputs()`
- [X] T009 [US1] Update `renderExerciseInputs()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` to accept a second parameter `previousData: Map<string, PreviousExerciseData> | 'error' | null` — for each exercise, insert `<div class="active-session__exercise-previous" id="previous-${exercise.exerciseId}">` **before** the inputs div; populate with the correct state: (a) if `previousData === 'error'`: `<span class="active-session__previous-error">Could not load previous data</span>`; (b) if `previousData === null` or `hasPreviousSession` was false or no map entry for this exerciseId: `<span class="active-session__previous-empty">First session — no previous data</span>`; (c) if map has entry: build value string from non-null weight/effort using `textContent` (not `innerHTML`) — weight: `"${weight} KG"`, effort: `"${value} — ${getEffortLabel(value)}"`, joined with ` · ` if both present; **if the resulting value string is empty (both `loggedWeight` and `effort` were null on the map entry), render the first-session indicator instead** (same as branch (b) — this is ui-contract State 5); otherwise wrap non-empty string in `<span class="active-session__previous-label">Last time:</span>` + `<span class="active-session__previous-value">…</span>`
- [X] T010 [P] [US1] Add CSS rules to `src/WorkoutTracker.Web/wwwroot/css/styles.css` for the previous-performance display — `.active-session__exercise-previous`: `margin-bottom: 0.5rem; font-size: 0.85rem; min-height: 1.2em;` (min-height prevents layout shift); `.active-session__previous-label`: `color: var(--color-text-muted); margin-right: 0.25rem;`; `.active-session__previous-value`: `color: var(--color-text-muted);`; `.active-session__previous-empty`: `color: var(--color-text-muted); font-style: italic;`; `.active-session__previous-error`: `color: var(--color-error);`
- [X] T011 [US1] Run all integration tests: `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj` — all existing tests plus T003–T006 must pass; confirm no regressions

**Checkpoint**: User Story 1 is fully functional — previous performance displays above inputs, all four states (first-session, data, partial data, error) work correctly, session saving is unaffected

---

## Phase 3: User Story 2 — Most Recent Session Semantics (Priority: P2)

**Goal**: When multiple prior sessions exist for a planned workout, the active session view always shows values from the **single most recently completed** session — not an average, not an older session. The most recent session's nulls take precedence over older sessions' non-null values.

**Independent Test**: Complete a planned workout twice with different weights and efforts (e.g., first session: 60 KG / effort 5; second session: 80 KG / effort 7). Start a third session. Confirm the view shows 80 KG and effort 7 (from the second session, not the first).

### Tests for User Story 2

- [X] T012 [P] [US2] Add integration test `GetPreviousPerformance_ReturnsMostRecentSession_WhenMultipleSessionsExist` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — POST two sessions for the same planned workout with different weights (first: `"60"`, second: `"80"`); GET previous-performance; assert `loggedWeight == "80"` (most recent session only) and `hasPreviousSession: true`
- [X] T013 [P] [US2] Add integration test `GetPreviousPerformance_ReturnsOnlyDataFromSpecifiedWorkout` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — create two planned workouts (Workout A and Workout B) each containing the same exercise; POST a session for Workout B with `loggedWeight: "100"` and `effort: 9`; GET previous-performance for Workout A (which has no sessions); assert `hasPreviousSession: false` and exercises array is empty (Workout B's session data MUST NOT appear)

### Implementation for User Story 2

- [X] T014 [US2] Run all integration tests: `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj` — T012 and T013 plus all prior tests must pass; confirm the `OrderByDescending(CompletedAt).FirstOrDefaultAsync()` in T001 correctly satisfies both test cases

**Checkpoint**: User Stories 1 and 2 are both independently verified — most-recent semantics confirmed, cross-workout isolation confirmed

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Security, UX consistency, performance, and regression verification across the complete feature.

- [X] T015 [P] Verify XSS safety of the "Last time" display in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — confirm `loggedWeight` value is assigned via `.textContent`, never via `.innerHTML` or `insertAdjacentHTML` (SR-002; `loggedWeight` is a free-form user-supplied string from the database)
- [X] T016 [P] Verify all four UI states are reachable and render correctly per `specs/008-workout-exercise-history/contracts/ui-contract.md` — review `renderExerciseInputs()` implementation against: (1) first-session state, (2) full data state (weight + effort), (3) weight-only state, (4) effort-only state, (5) both-null state (treated same as first-session), (6) error state
- [X] T017 Verify active session page loads within 3s end-to-end — using browser DevTools Network throttle (Slow 3G preset), start a session for a workout with prior data and confirm the previous-performance display appears without observable sequential delay (both fetches are concurrent per T008)
- [X] T018 [P] Run the complete user flow from `specs/008-workout-exercise-history/quickstart.md` — verify the first-session flow, the returning-session flow, and the error-state flow match the documented behaviour

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: No dependencies — start immediately; T001 and T002 can run in parallel
- **User Story 1 (Phase 2)**: Depends on Phase 1 complete — tests (T003–T006) and implementation (T007–T010) can all start in parallel once T001 and T002 are done; T011 runs last within the phase
- **User Story 2 (Phase 3)**: Depends on Phase 2 complete (T011 passed) — T012 and T013 can run in parallel; T014 runs last within the phase
- **Polish (Phase 4)**: Depends on Phase 3 complete — T015, T016, T018 can run in parallel; T017 runs sequentially

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational (Phase 1) — no dependency on US2
- **US2 (P2)**: Depends on US1 complete (endpoint and frontend must exist to demonstrate "most recent" semantics end-to-end)

### Within Each User Story

- Tests (T003–T006, T012–T013) must be written before final verification
- Interfaces (T007) can be added in parallel with backend work (different files)
- T008 depends on T007 (needs the interface types)
- T009 depends on T007 and T008 (needs interfaces and updated loadWorkout signature)
- T010 (CSS) is independent of all TypeScript changes
- T011 and T014 (test runs) are the final gate for each phase

---

## Parallel Execution Examples

### Phase 1

```
Parallel start:
  T001 — Add API endpoint in WorkoutTracker.Api/Program.cs
  T002 — Add proxy route in WorkoutTracker.Web/Program.cs
```

### Phase 2 (after Phase 1 complete)

```
Parallel start:
  T003 — Integration test: no sessions exist
  T004 — Integration test: returns weight and effort
  T005 — Integration test: 404 on missing workout
  T006 — Integration test: handles partial null data
  T007 — TypeScript interfaces in active-session.ts
  T010 — CSS rules in styles.css

Sequential after T007:
  T008 — Refactor loadWorkout() to Promise.allSettled
  T009 — Update renderExerciseInputs() with previous data rendering

Sequential after T003–T009:
  T011 — Run all integration tests
```

### Phase 3 (after Phase 2 complete)

```
Parallel start:
  T012 — Integration test: most recent session wins
  T013 — Integration test: cross-workout isolation

Sequential after T012–T013:
  T014 — Run all integration tests
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1: Foundational (endpoint + proxy)
2. Complete Phase 2: User Story 1 (tests + frontend + CSS)
3. **STOP and VALIDATE**: Start a fresh session for a previously-completed workout and confirm "Last time" data appears correctly
4. Deploy/demo if ready

### Full Feature

1. Phase 1 → Phase 2 → Phase 3 → Phase 4
2. Each phase adds verifiable value without breaking the previous

---

## Summary

| Phase | Tasks | User Story | Can Parallelise |
|-------|-------|------------|-----------------|
| Phase 1: Foundational | T001–T002 | n/a | T001 ∥ T002 |
| Phase 2: US1 (P1) | T003–T011 | US1 | T003–T007, T010 all ∥ |
| Phase 3: US2 (P2) | T012–T014 | US2 | T012 ∥ T013 |
| Phase 4: Polish | T015–T018 | n/a | T015, T016, T018 ∥ |

**Total tasks**: 18  
**US1 tasks**: 9 (T003–T011)  
**US2 tasks**: 3 (T012–T014)  
**Parallel opportunities**: 12 of 18 tasks can run in parallel within their phase  
**Suggested MVP scope**: Phases 1 + 2 (User Story 1)
