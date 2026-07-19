# Implementation Plan: Latest Exercise Data

**Branch**: `029-latest-exercise-data` | **Date**: 2026-07-19 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/029-latest-exercise-data/spec.md`

## Summary

Change previous exercise comparisons from "data in the immediately previous session" to "latest usable data for each exercise". The existing `GET /api/workouts/{workoutId}/previous-performance` route remains the active-workout contract surface, but its backend query walks completed sessions for the planned workout in newest-first order and selects the first logged exercise per exercise where comparison data is usable. The session detail review endpoint applies the same per-exercise latest-usable rule when populating `previousWeight` and `previousEffort`. Skipped or empty logged-exercise rows no longer block older completed data.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~7.0.2 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via EF Core — no schema changes; all data exists in `workout_session` and `logged_exercise`  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests where helper logic changes; Playwright E2E tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with .NET Aspire orchestration)  
**Performance Goals**: Active session remains interactive within the existing 3-second slow-3G budget; latest available data appears within 1 second of workout data becoming available for 95% of workout views  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; no schema migration; preserve the existing `/previous-performance` route and active-session BEM classes; `CompletedAt` MUST be accessed via `EF.Property<DateTime>(ws, "CompletedAt")` because it is a shadow property  
**Scale/Scope**: Changes touch `WorkoutTracker.Api/Program.cs`, `active-session.ts`, `session-detail.ts` if review rendering needs copy/state alignment, `SessionApiTests.cs`, `WorkoutHistoryTests.cs`, and E2E mock previous-performance/session-detail data; Web proxy route stays unchanged unless contract forwarding needs no-op documentation

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Keep the existing endpoint and UI structure instead of adding parallel concepts. Extract any non-trivial "usable previous data" predicate into a named helper or clearly bounded local expression, avoiding duplicated null/blank checks between projection and tests. TypeScript interfaces must remain explicit with no `any`; C# must follow the current inline minimal-API projection style. ✅
- **Testing**: Add backend regression tests proving skipped/empty latest rows fall back to older rows, latest usable rows still win, entries are selected independently per exercise, cross-workout data is ignored, and no-data states remain correct. Add or update Playwright coverage for both the active-session "Last time" UI and session-detail review previous columns when the immediately previous comparable session skipped an exercise. Existing previous-performance and session-detail previous-value tests must be updated to reflect "latest available per exercise" semantics. ✅
- **Security**: The route remains read-only with a GUID path parameter and no request body. EF Core parameterisation handles the path value. Single-user authorization assumptions remain unchanged from features 008 and 025; no secrets, external services, or new trust boundaries are introduced. Returned strings are still written via `textContent`, not `innerHTML`. ✅
- **User Experience Consistency**: Preserve existing labels, BEM classes, typography, and "Could not load previous data" error copy. Update the empty state semantics so it means "no usable data for this exercise", not "no previous workout exists". Loading remains implicit while the concurrent fetch resolves. ✅
- **Performance**: Replace the single-session query with a bounded newest-first history lookup scoped to one planned workout. The query must avoid N+1 access and should project only `CompletedAt`, `WorkoutSessionId`, and logged-exercise fields needed to choose latest usable entries. If no hard cap exists, add a documented cap or server-side grouping strategy before implementation. ✅

## Project Structure

### Documentation (this feature)

```text
specs/029-latest-exercise-data/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # Updated previous-performance response semantics
│   └── ui-contract.md   # Active-session Last time display states
└── tasks.md             # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                              # MODIFIED: select latest usable data per exercise

src/WorkoutTracker.Web/
├── Program.cs                              # UNCHANGED: proxy keeps forwarding previous-performance route
└── wwwroot/
    └── ts/
        └── pages/
            ├── active-session.ts           # MODIFIED: consume per-exercise latest data semantics and copy
            └── session-detail.ts           # MODIFIED if needed: keep review previous columns aligned

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                  # MODIFIED: previous-performance and session-detail regression tests

src/WorkoutTracker.E2ETests/
├── Infrastructure/
│   └── WebAppFixture.cs                    # MODIFIED: mock endpoints mirror latest-available semantics
└── E2E/
    └── WorkoutHistoryTests.cs              # MODIFIED: active-session and session-detail fallback coverage
```

**Structure Decision**: Existing .NET Aspire solution structure is preserved. No new projects, migrations, routes, or CSS blocks are required. The existing previous-performance endpoint remains the integration point so the Web proxy and active-session fetch path stay stable.

## Phase 0: Research Outcomes

Research captured in [research.md](./research.md).

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Defines read-only entities and the latest-usable selection rule
- [contracts/api-contract.md](./contracts/api-contract.md): Updated response contract for `GET /api/workouts/{workoutId}/previous-performance`
- [contracts/ui-contract.md](./contracts/ui-contract.md): Active-session "Last time" state and rendering contract
- [quickstart.md](./quickstart.md): Implementation and verification workflow

## Post-Design Constitution Check

- **Code Quality** ✅ — Design keeps the existing endpoint and UI block, avoids schema changes, and centralizes the usable-data rule for implementation.
- **Testing** ✅ — Backend, E2E mock, and Playwright regression coverage are explicitly scoped for skipped latest rows, per-exercise fallback, no-data, cross-workout isolation, and session-detail review consistency.
- **Security** ✅ — No new input surface or trust boundary; response remains user-owned workout history via existing single-user assumptions.
- **User Experience Consistency** ✅ — Existing "Last time:" label, classes, and error treatment remain; only the data selection and empty-state meaning change.
- **Performance** ✅ — Design requires bounded, planned-workout-scoped lookup and projection-only reads, with the 1-second display target retained from the spec.

## Complexity Tracking

> No constitution violations or exceptions identified.
