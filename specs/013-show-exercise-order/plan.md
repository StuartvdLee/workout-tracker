# Implementation Plan: Previous Exercise Order Indicator in Active Workout

**Branch**: `013-show-exercise-order` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/013-show-exercise-order/spec.md`

## Summary

When a user is in an active workout session, display the position (`#1`, `#2`, `#3`, …) that each exercise occupied in the previous session alongside the existing "Last time" weight and effort data. This is a **read-only display enhancement** with no schema changes — `LoggedExercise.Sequence` already exists and is saved on every session save (0-based index). The backend `GET /api/workouts/{workoutId}/previous-performance` endpoint is extended by one field (`sequence`) in its projection; the frontend `PreviousExerciseData` interface and rendering logic are updated to prepend `#x` (1-based) to the "Last time" value display.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via EF Core — no schema changes; `Sequence` already exists on `logged_exercise`  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Active session view loads within 3 seconds on slow 3G (unchanged from `008`); `sequence` is retrieved in the same bounded query — no additional round-trip  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; `sequence` is nullable to handle sessions saved before `Sequence` was tracked  
**Scale/Scope**: Changes touch 3 files (`Program.cs`, `active-session.ts`, `SessionApiTests.cs`); no migration; no new CSS required

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. The new `sequence: number | null` field on `PreviousExerciseData` is explicitly typed — no `any`. The C# projection adds one field inline using the existing anonymous-type pattern; no new classes needed. BEM CSS classes are unchanged — no new CSS. ✅ No deviations required.

- **Testing**:
  - **Backend integration tests**: Update `PreviousExerciseDataDto` in `SessionApiTests.cs` to add `int? Sequence`. Add one new test: `GetPreviousPerformance_ReturnsSequence_FromLastSession` — posts a session with explicit `Sequence` values and asserts they are returned correctly. Update `GetPreviousPerformance_ReturnsWeightAndEffort_FromLastSession` to assert `Sequence` is also present. Existing tests that post without `Sequence` verify the null-handling path.
  - **Frontend Vitest tests**: No new Vitest tests — the rendering logic is DOM manipulation, consistent with the established no-Vitest-for-DOM pattern (features 004, 005, 008, 011). **State 5 exception**: State 5 is a behaviour change from feature 008 (previously "First session — no previous data", now "Last time: #2"). Its data layer is covered by the extended T002 backend integration test (Exercise C with `Sequence == 2`, `LoggedWeight == null`, `Effort == null`). The DOM rendering of State 5 follows deterministically from that data via the `parts` array logic — when `sequence != null` and `loggedWeight/effort == null`, `parts` has exactly one element — which is verifiable by code inspection and confirmed by the manual T010 State 5 check.
  - **Regression**: All existing xUnit integration tests must pass unchanged. ✅ Tests treated as mandatory.

- **Security**: The feature only adds one already-stored integer field (`Sequence`) to an existing read-only endpoint. The `workoutId` is a GUID — EF Core parameterises it. No user-supplied string input is introduced. SR-001 (cross-user) exception documented identically to features 008 and 011 — single-user app, no auth layer. No new trust boundaries. ✅

- **User Experience Consistency**:
  - The `#x` indicator is added to the existing `active-session__exercise-previous` section alongside weight and effort — same visual style, same `--color-text-muted` colour, same `0.85rem` font size.
  - All four states remain: first-session (no indicator), data with position (indicator shown), data without position (null Sequence → no indicator, graceful fallback), error (unchanged).
  - Copy: `#1`, `#2`, `#3` — hash symbol followed by the 1-based integer. Consistent across all exercises. ✅

- **Performance**:
  - `Sequence` is an integer column on `logged_exercise`. It is already retrieved as part of the `Include(ws => ws.LoggedExercises)` row fetch — no additional query, no additional join.
  - The previous-performance fetch remains concurrent with the workout fetch via `Promise.allSettled` (established in `008`).
  - Page load budget remains 3 seconds on slow 3G — no new network round-trips introduced. ✅

## Project Structure

### Documentation (this feature)

```text
specs/013-show-exercise-order/
├── plan.md              # This file
├── research.md          # Phase 0 output ✅
├── data-model.md        # Phase 1 output ✅
├── quickstart.md        # Phase 1 output ✅
├── contracts/
│   ├── api-contract.md  # Updated previous-performance endpoint (adds `sequence` field)
│   └── ui-contract.md   # Updated active session exercise card (adds `#x` indicator)
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                          # MODIFIED: add `le.Sequence` to previous-performance projection

src/WorkoutTracker.Web/
└── wwwroot/
    └── ts/
        └── pages/
            └── active-session.ts       # MODIFIED: add `sequence` to PreviousExerciseData; render `#x` in previous display

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs             # MODIFIED: add Sequence to PreviousExerciseDataDto; new sequence test; update existing test
```

**Structure Decision**: Existing project structure preserved. No new files, no new routes, no migration. All changes are surgical additions to 3 existing files. The feature reuses the full infrastructure established by `008-workout-exercise-history` — same endpoint, same fetch strategy, same rendering container, same state handling.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts (research.md, data-model.md, contracts/, quickstart.md) are complete.*

- **Code Quality** ✅ — One field added to one anonymous-type projection in C#. One field added to one TypeScript interface. One `if` branch updated in rendering logic. No speculative abstractions, no dead code, no `any`.
- **Testing** ✅ — One new integration test for the happy-path sequence return; `PreviousExerciseDataDto` updated; existing tests verify null-Sequence path implicitly (sessions posted without `Sequence`). Vitest unchanged per established pattern.
- **Security** ✅ — No new trust boundaries. Single-user exception documented. Integer field, no injection risk.
- **User Experience Consistency** ✅ — `#x` indicator slots into the existing `active-session__exercise-previous` section with no CSS changes. All four states handled. Copy format `#x` applied uniformly.
- **Performance** ✅ — No new query, no new round-trip. `Sequence` retrieved as part of the existing `LoggedExercises` include.

No violations. Plan is ready for `/speckit.tasks`.
