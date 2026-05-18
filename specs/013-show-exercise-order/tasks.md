# Tasks: Previous Exercise Order Indicator in Active Workout

**Input**: Design documents from `/specs/013-show-exercise-order/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Automated tests are REQUIRED. The existing `PreviousExerciseDataDto` test record is updated to carry `Sequence`; one new integration test covers the happy-path sequence return; one existing test is extended to assert the null-sequence path.

**Organisation**: Single user story. No setup or foundational phase required — the full infrastructure (endpoint, proxy, frontend fetch, CSS, test harness) was established in `008-workout-exercise-history`. All tasks land in the US1 phase followed by a polish phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1)
- Exact file paths are included in descriptions

---

## Phase 1: User Story 1 — See Previous Exercise Position in Active Session (Priority: P1) 🎯 MVP

**Goal**: Each exercise card in an active workout session shows a `#x` (1-based) position indicator from the most recently completed session for the same planned workout, alongside the existing "Last time" weight and effort reference data.

**Independent Test**: Complete a planned workout session for a workout with 3 exercises. Start a new session for the same planned workout. Confirm each exercise shows `#1`, `#2`, `#3` respectively within the "Last time" display. Confirm the position persists correctly even when a new session uses a different (e.g. randomised) order. Confirm exercises with no previous session show no `#x` indicator.

### Tests for User Story 1

> **Write T001 first** — it updates the shared test DTO that T002 and T003 extend. T002 and T003 should initially fail until T004 (backend) is implemented.

- [X] T001 [US1] Update `PreviousExerciseDataDto` record in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` (line ~726) — add `int? Sequence` as the fourth positional parameter: `private sealed record PreviousExerciseDataDto(Guid ExerciseId, string? LoggedWeight, int? Effort, int? Sequence);` — this is a prerequisite for T002 and T003 to compile

- [X] T002 [P] [US1] Add integration test `GetPreviousPerformance_ReturnsSequence_FromLastSession` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — create a planned workout with **three** exercises (Exercise A, B, C); POST a session with `LoggedExercises: [{ ExerciseId: exerciseA, LoggedWeight: "80", Effort: 7, Sequence: 0 }, { ExerciseId: exerciseB, LoggedWeight: "60", Effort: 5, Sequence: 1 }, { ExerciseId: exerciseC, LoggedWeight: null, Effort: null, Sequence: 2 }]`; GET `/api/workouts/{workoutId}/previous-performance`; assert: (1) 200, `hasPreviousSession: true`; (2) Exercise A has `Sequence == 0`, `LoggedWeight == "80"`, `Effort == 7`; (3) Exercise B has `Sequence == 1`; (4) **Exercise C has `Sequence == 2`, `LoggedWeight == null`, `Effort == null`** — this covers the State 5 data path (position recorded, no weight/effort) and proves the projection returns sequence even when weight and effort are absent

- [X] T003 [P] [US1] Update existing test `GetPreviousPerformance_ReturnsWeightAndEffort_FromLastSession` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — the existing POST body does not include a `Sequence` field; add assertion `Assert.Null(result.Exercises[0].Sequence)` to verify that a session saved without a sequence value returns `null` for the `sequence` field (confirms graceful null-handling path)

### Implementation for User Story 1

- [X] T004 [P] [US1] Extend the `previous-performance` endpoint projection in `src/WorkoutTracker.Api/Program.cs` — in the `Select(ws => new { ..., LoggedExercises = ws.LoggedExercises.Select(le => new { le.ExerciseId, le.LoggedWeight, le.Effort, le.Sequence }).ToList() })` projection (around line 277), add `le.Sequence` after `le.Effort`; the final `return Results.Ok(new { hasPreviousSession = true, completedAt = ..., exercises = lastSession.LoggedExercises })` already returns the projected list unchanged, so no further modification is needed

- [X] T005 [P] [US1] Add `readonly sequence: number | null` to the `PreviousExerciseData` interface in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` (line ~22) — place it after `readonly effort: number | null`; the `PreviousPerformance` interface is unchanged

- [X] T006 [US1] Update `renderExerciseInputs()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — in the `parts` array build block (around line 275), add `if (entry.sequence !== null) parts.push(`#${entry.sequence + 1}`);` as the **first line** before the existing `loggedWeight` and `effort` pushes; this single change: (a) prepends `#x` to the value string when position is available (e.g. `#2 · 80 KG · 7 — Hard`); (b) naturally changes State 5 behaviour — when weight and effort are both null but sequence is set, `parts` is non-empty (`["#2"]`), so the "Last time: #2" display is shown instead of "First session — no previous data" (per `specs/013-show-exercise-order/contracts/ui-contract.md` State 5); (c) null sequence (old sessions) leaves the display unchanged from feature 008

- [X] T007 [US1] Run all backend integration tests: `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj` — T002 and T003 must pass; all existing previous-performance tests (T003–T006 from feature 008) must continue to pass; confirms `le.Sequence` flows correctly from the POST session body through the DB to the GET previous-performance response

- [X] T008 [US1] Build TypeScript to confirm no type errors: `cd src/WorkoutTracker.Web && npm run build` — confirms `readonly sequence: number | null` on `PreviousExerciseData` is correctly consumed in `renderExerciseInputs()` with no implicit `any`, no unused-variable violations, and no `noImplicitReturns` failures

**Checkpoint**: Feature is fully functional — each exercise in a repeated active session shows `#x` alongside weight and effort; null-sequence sessions degrade gracefully; all tests pass

---

## Phase 2: Polish & Cross-Cutting Concerns

**Purpose**: Security, UX consistency, and manual verification across all defined display states.

- [X] T009 [P] Verify XSS safety of the `#x` indicator in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — confirm `#${entry.sequence + 1}` is written via `.textContent` assignment on `valueSpan`, never via `.innerHTML` or `insertAdjacentHTML`; `sequence` is a DB integer so injection is structurally impossible, but the textContent usage must be consistent with the existing `loggedWeight` assignment that SR-002 already mandates (per `specs/008-workout-exercise-history/plan.md` T015)

- [ ] T010 [P] Verify all display states render correctly in browser against `specs/013-show-exercise-order/contracts/ui-contract.md` — check: **(0) loading state**: previous section is empty (no child elements) while fetch is in progress — no layout shift when data arrives; (1) first session (no previous session): no `#x`, "First session — no previous data"; (2) position + weight + effort: `Last time: #2 · 80 KG · 7 — Hard`; (3) position + weight only: `Last time: #2 · 80 KG`; (4) position + effort only: `Last time: #2 · 7 — Hard`; (5) position only (weight/effort null): `Last time: #2`; (6) null sequence (old session, weight/effort available): `Last time: 80 KG · 7 — Hard` (no `#x`); (7) error state: "Could not load previous data" unchanged

- [ ] T011 Run manual end-to-end verification per `specs/013-show-exercise-order/quickstart.md` — create a 3-exercise workout, complete a session, start a new session, confirm `#1` / `#2` / `#3` appear correctly in the "Last time" display for each exercise

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (US1)**: No external dependencies — can start immediately; within the phase, T001 must complete before T002 and T003
- **Phase 2 (Polish)**: Depends on Phase 1 complete (T007 and T008 passed)

### Within User Story 1

```
T001 (DTO update — prerequisite)
  ├── T002 [P] (new test — write before T004 passes so it initially fails)
  └── T003 [P] (extend existing test)

T004 [P] (backend endpoint — independent of tests, different file)
T005 [P] (TS interface — independent of tests, different file)
  └── T006 (render update — depends on T005)

T007 (run tests — depends on T002, T003, T004 all complete)
T008 (TS build — depends on T005, T006 complete)
```

### Parallel Opportunities

All [P]-marked tasks within a phase can execute concurrently:

```
# Can start in parallel immediately (Phase 1):
T001 — update DTO in SessionApiTests.cs (prereq for T002/T003)
T004 — add le.Sequence to endpoint in Program.cs
T005 — add sequence to PreviousExerciseData in active-session.ts

# After T001 completes, start in parallel:
T002 — new integration test in SessionApiTests.cs
T003 — update existing test in SessionApiTests.cs

# After T005 completes:
T006 — update renderExerciseInputs in active-session.ts

# After T002, T003, T004 all complete:
T007 — dotnet test (sequential gate)

# After T005, T006 complete:
T008 — npm run build (sequential gate)

# After T007 and T008 (Phase 2):
T009 [P], T010 [P] — can run concurrently
T011 — manual run after T009, T010
```

---

## Implementation Strategy

### MVP (This Feature Has One Story)

1. Write T001 (DTO update)
2. Write T002 and T003 (tests — verify they fail on current endpoint)
3. Implement T004, T005, T006 in parallel
4. Run T007 (all tests pass) and T008 (no TS errors)
5. **STOP and VALIDATE**: Start a session for a previously-completed workout; confirm `#1`, `#2`, `#3` appear in "Last time" display
6. Complete Phase 2 polish

---

## Summary

| Phase | Tasks | User Story | Can Parallelise |
|-------|-------|------------|-----------------|
| Phase 1: US1 (P1) | T001–T008 | US1 | T002 ∥ T003 ∥ T004 ∥ T005; T009 ∥ T010 |
| Phase 2: Polish | T009–T011 | n/a | T009 ∥ T010 |

**Total tasks**: 11  
**US1 tasks**: 8 (T001–T008)  
**Polish tasks**: 3 (T009–T011)  
**Parallel opportunities**: 7 of 11 tasks can run concurrently within their batch  
**Suggested MVP scope**: Phase 1 only (the entire feature is one story)
