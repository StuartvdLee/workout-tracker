---
description: "Task list for 009-last-workout-hint"
---

# Tasks: Last Workout Hint on Home Page

**Input**: Design documents from `/specs/009-last-workout-hint/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-contract.md ✅, quickstart.md ✅

**Tech stack**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend), ASP.NET Core minimal API, EF Core + Npgsql, vanilla TypeScript, xUnit 3.2.2 integration tests, Vitest  
**No schema changes** — all required data exists in `workout_session`.  
**Files changed**: `WorkoutTracker.Api/Program.cs`, `WorkoutTracker.Web/Program.cs`, `home.ts`, `styles.css`, `SessionApiTests.cs`

---

## Phase 1: Setup

**Purpose**: No new projects or dependencies needed. This phase confirms the working branch and verifies the existing test suite passes before any changes are made.

- [X] T001 Verify existing tests pass before making changes by running `dotnet test src/WorkoutTracker.UnitTests` from repository root

---

## Phase 2: Foundational — Backend Endpoint

**Purpose**: The `GET /api/sessions/latest` endpoint is the single prerequisite for both user stories. Both stories (hint shown / hint absent) are tested against this endpoint.

**⚠️ CRITICAL**: The frontend changes in Phase 3 and Phase 4 depend on this endpoint being complete and proxied.

- [X] T002 Add `GET /api/sessions/latest` handler to `src/WorkoutTracker.Api/Program.cs` — query `WorkoutSessions` ordered by `CompletedAt` shadow property descending, project to `{ workoutName, completedAt }`, return `{ hasSession: false }` when no row exists and `{ hasSession: true, workoutName, completedAt }` otherwise (see contracts/api-contract.md and research.md Decision 3)
- [X] T003 Add proxy route `GET /api/sessions/latest` to `src/WorkoutTracker.Web/Program.cs` — follow the existing try/catch proxy pattern used for all other routes in that file (e.g. `GET /api/sessions`)

**Checkpoint**: `GET /api/sessions/latest` is reachable via the Web project and returns the correct shape.

---

## Phase 3: User Story 1 — See Last Workout on Home Page (Priority: P1) 🎯 MVP

**Goal**: Returning users see `Last workout: [Name] — [Date]` below the "Start Workout" button.

**Independent Test**: Complete one workout session, return to the home page, confirm the hint text appears below the "Start Workout" button with the correct workout name and a human-readable date.

### Tests for User Story 1

- [X] T004 [P] [US1] Add integration test to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — `GetLatestSession_ReturnsHasSessionFalse_WhenNoSessions`: call `GET /api/sessions/latest` with no data, assert `hasSession` is `false`
- [X] T005 [P] [US1] Add integration test to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — `GetLatestSession_ReturnsWorkoutNameAndDate_AfterOneSession`: create a workout + session, call `GET /api/sessions/latest`, assert `hasSession` is `true`, `workoutName` matches, `completedAt` is present
- [X] T006 [P] [US1] Add integration test to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — `GetLatestSession_ReturnsMostRecentSession_WhenMultipleSessionsExist`: create two sessions for different workouts, call `GET /api/sessions/latest`, assert response matches the session with the later `completedAt`

### Implementation for User Story 1

- [X] T007 [US1] Add `LastWorkoutDto` interface and `loadLastWorkoutHint()` async function to `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` — fire `void loadLastWorkoutHint()` at the end of `render()`, fetch `/api/sessions/latest`, on success with `hasSession: true` create a `<p class="workout-form__last-workout">` element and `appendChild` to the `#workout-form` element; on failure or `hasSession: false` do nothing (see research.md Decision 4 and quickstart.md for the full code shape)
- [X] T008 [US1] Add `.workout-form__last-workout` CSS rule to `src/WorkoutTracker.Web/wwwroot/css/styles.css` after the `.workout-form__button:active` block — `font-size: var(--font-size-sm); color: var(--color-text-light); text-align: center;`

**Checkpoint**: User Story 1 independently testable — complete a session, visit home page, hint appears with name and date formatted as "3 May 2026".

---

## Phase 4: User Story 2 — No Hint for First-Time Users (Priority: P1)

**Goal**: Users with no completed sessions see the home page unchanged — no element, no empty space, no error text below the button.

**Independent Test**: Open the app with no sessions. Confirm the home page layout is identical to the pre-feature state — nothing below the "Start Workout" button.

*Note: User Story 2 shares the same implementation path as User Story 1. The `loadLastWorkoutHint()` function already handles the no-session case (`hasSession: false` → do nothing) and the error case (catch → do nothing). This phase validates those code paths are correct with a dedicated test.*

### Tests for User Story 2

- [X] T009 [P] [US2] Add integration test to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` — `GetLatestSession_ReturnsHasSessionFalse_AfterReset`: reset data, call `GET /api/sessions/latest`, assert `hasSession` is `false` and no `workoutName` or `completedAt` fields are present (validates the no-session response shape)

**Checkpoint**: All four backend tests pass. User Story 2 verified — no hint rendered when `hasSession` is `false`.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T010 [P] Run full test suite `dotnet test src/WorkoutTracker.UnitTests` — confirm all existing tests still pass and all four new `GetLatestSession_*` tests pass
- [X] T011 Run TypeScript compilation `cd src/WorkoutTracker.Web && npm run build` (or equivalent) — confirm no TypeScript errors in `home.ts` (strict mode, no `any`, `LastWorkoutDto` typed correctly, `loadLastWorkoutHint` returns `Promise<void>`)
- [ ] T012 [P] Verify the hint layout on a narrow viewport (320 px) — confirm `.workout-form__last-workout` text wraps gracefully and does not cause horizontal scroll or layout shift in the `workout-form` flex column
- [ ] T013 [P] Confirm dark mode compatibility — the `--color-text-light` custom property is already overridden in `[data-theme="dark"]` in `styles.css`; no additional dark mode rules needed; verify visually

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS Phases 3 and 4
- **Phase 3 (US1)**: Depends on Phase 2 (endpoint + proxy must exist for tests)
- **Phase 4 (US2)**: Depends on Phase 2; can run in parallel with Phase 3 (T009 is independent of T007/T008)
- **Phase 5 (Polish)**: Depends on Phases 3 and 4

### Parallel Opportunities

Within Phase 3:
- T004, T005, T006 (backend tests) can be written in parallel with each other
- T007 (frontend `home.ts`) and T008 (CSS) can be written in parallel once T002/T003 are done

Within Phase 4:
- T009 can be written in parallel with T007/T008

Within Phase 5:
- T010, T012, T013 can run in parallel

### Implementation Strategy

**MVP scope**: Phases 1–3 deliver the complete feature (both user stories are covered by the same code path). Phase 4 adds the explicit validation test for the no-session case. Phase 5 is polish and regression confirmation.

Suggested execution order for a single implementer: T001 → T002 → T003 → T004, T005, T006, T009 (tests first) → T007, T008 (implementation) → T010, T011, T012, T013 (polish).
