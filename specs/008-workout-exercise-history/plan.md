# Implementation Plan: Previous Exercise Performance in Active Workout

**Branch**: `008-workout-exercise-history` | **Date**: 2026-05-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/008-workout-exercise-history/spec.md`

## Summary

When a user starts an active workout session from a planned workout, surface the weight and effort from the most recently completed session for that same planned workout. This is a **read-only display enhancement** requiring one new API endpoint (`GET /api/workouts/{workoutId}/previous-performance`) and frontend changes to `active-session.ts` to fetch and display the data. No schema changes are needed — the data already exists in `logged_exercise`. The previous data is scoped strictly to the planned workout; the same exercise in a different planned workout contributes no data.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via EF Core — no schema changes; all required data is in the existing `workout_session` and `logged_exercise` tables  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Active session view loads within 3 seconds on slow 3G; previous performance data loaded concurrently with workout data — no sequential blocking  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; previous-performance fetch failure MUST NOT block the session from loading  
**Scale/Scope**: Single-user; changes touch 4 files (`WorkoutTracker.Api/Program.cs`, `WorkoutTracker.Web/Program.cs`, `active-session.ts`, `styles.css`) + backend test additions; no migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. CSS follows BEM naming — new classes MUST use `active-session__exercise-previous` and sub-elements naming. C# uses `<Nullable>enable</Nullable>` and snake_case DB naming via `UseSnakeCaseNamingConvention()`. The new `PreviousExerciseData` interface in `active-session.ts` must be typed explicitly; no `any`. ✅ No deviations required.

- **Testing**:
  - **Backend integration tests**: New tests required in `SessionApiTests.cs` for `GET /api/workouts/{workoutId}/previous-performance`: (a) returns empty/no-previous when no sessions exist; (b) returns correct weight and effort from the single most recent session; (c) returns only data scoped to the specified planned workout (not from sessions for other workouts); (d) returns 404 when workoutId does not exist; (e) returns correct data when most recent session has partial data (some exercises missing weight or effort). All existing tests must continue to pass.
  - **Frontend Vitest tests**: No new Vitest tests — the previous-performance display logic is DOM rendering, consistent with the pattern from features 004 and 005 where page rendering is not covered by Vitest. The `getEffortLabel` call is already unit-tested via the shared utility.
  - **Regression**: All existing xUnit integration tests must pass unchanged. ✅ Tests treated as mandatory, not optional.

- **Security**:
  - The new endpoint is `GET /api/workouts/{workoutId}/previous-performance`. The `workoutId` is a GUID — EF Core parameterises the value preventing SQL injection.
  - No user-supplied string input is involved in this endpoint. Response data originates from the database (previously validated on write) and is returned as JSON — no additional sanitisation needed.
  - **SR-001 (cross-user)**: Same documented exception as `005-active-workout-effort` — the app is currently single-user with no authentication layer; cross-user leakage is structurally impossible. Deferred to future auth feature.
  - No new secrets, third-party integrations, or trust boundaries introduced. ✅

- **User Experience Consistency**:
  - The "Last time" display uses `--color-text-muted` CSS custom property (established in feature 001) to visually distinguish read-only reference data from the active input fields.
  - All four states defined: loading (skeleton placeholder using existing `--color-surface-hover`), empty/first-time ("First session — no previous data"), success (weight + effort label), error ("Could not load previous data").
  - Effort labels in the previous display use the same `getEffortLabel()` function from `utils.ts` used in history and active session views — consistent terminology.
  - The previous data section layout follows the `active-session__exercise-item` BEM structure established in feature 004. ✅

- **Performance**:
  - Previous performance fetch is initiated concurrently with the workout fetch using `Promise.allSettled` — no sequential blocking of the page load.
  - The `GET /api/workouts/{workoutId}/previous-performance` query touches a single `workout_session` row (most recent, via `OrderByDescending(CompletedAt).Take(1)`) joined to `logged_exercise` for that session. This is a bounded, indexed query — no full table scan. ✅
  - Page load budget remains 3 seconds on slow 3G (consistent with feature 005). The additional concurrent fetch adds no sequential latency. ✅

## Project Structure

### Documentation (this feature)

```text
specs/008-workout-exercise-history/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # New previous-performance endpoint
│   └── ui-contract.md   # Active session exercise card with "Last time" display
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                          # MODIFIED: add GET /api/workouts/{workoutId}/previous-performance

src/WorkoutTracker.Web/
└── Program.cs                          # MODIFIED: proxy GET /api/workouts/{workoutId}/previous-performance
└── wwwroot/
    ├── css/
    │   └── styles.css                  # MODIFIED: add .active-session__exercise-previous styles
    └── ts/
        └── pages/
            └── active-session.ts       # MODIFIED: fetch previous performance, render "Last time" per exercise

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs             # MODIFIED: tests for new GET previous-performance endpoint
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects. All changes are surgical modifications to existing files. No new EF migration. The previous-performance endpoint is a new read-only query added inline in `WorkoutTracker.Api/Program.cs`, consistent with all other endpoints.

## Complexity Tracking

> No constitution violations. No complexity justification required.

