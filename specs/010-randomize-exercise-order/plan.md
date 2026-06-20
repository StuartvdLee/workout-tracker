# Implementation Plan: Randomize Workout Exercise Order

**Branch**: `010-randomize-exercise-order` | **Date**: 2026-05-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/010-randomize-exercise-order/spec.md`

## Summary

Add a pre-start modal to `workouts.ts` that gives users an optional "Randomise order" toggle before beginning a workout session. When enabled, exercises are shuffled client-side using Fisher-Yates, previewed in the modal, and the shuffled order is passed to the active session page via a `?order=` URL parameter. `active-session.ts` reorders exercises in memory and records each exercise's display position as a `sequence` value in the session save request. A single nullable `int` column (`sequence`) is added to `logged_exercise` via a new EF Core migration. The workout template (`PlannedWorkoutExercise.Sequence`) is never modified.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via EF Core — one new nullable `int` column on `logged_exercise`; migration required  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Pre-start modal opens and renders within the same time budget as the existing "Start" button response; shuffle of up to 50 exercises introduces no perceptible delay  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; `PlannedWorkoutExercise.Sequence` MUST NOT be modified by any part of this feature  
**Scale/Scope**: Single-user; changes touch 7 files + 1 new migration file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. CSS follows BEM naming — the new modal elements MUST use `prestart-modal-backdrop` / `prestart-modal` / `prestart-modal__*` following the established modal BEM pattern (consistent with `delete-modal-backdrop`, `edit-modal-backdrop`, `discard-modal-backdrop`). C# uses `<Nullable>enable</Nullable>` and snake_case DB naming via `UseSnakeCaseNamingConvention()`. The new `shuffle<T>()` function in `utils.ts` must be typed generically with no `any`. ✅ No deviations required.

- **Testing**:
  - **Frontend Vitest unit tests**: The `shuffle<T>()` function in `utils.ts` MUST have unit tests covering: array of length 0, array of length 1 (returns same element), array of length 2 (returns both elements), array of length n returns all original elements (no duplicates, no missing items), type preservation (generic). Note: randomness itself is not asserted; statistical correctness of Fisher-Yates is a known property of the algorithm.
  - **Backend integration tests**: New tests required in `SessionApiTests.cs` (or a new `SessionCreateWithSequenceTests.cs`) for `POST /api/workouts/{workoutId}/sessions` with `Sequence` values: (a) session saved with `sequence` values in non-template order is accepted and returns 201; (b) session saved with `sequence: null` for all items (backward-compatible path) returns 201; (c) the stored `Sequence` on `LoggedExercise` matches the submitted values.
  - **Regression**: All existing xUnit integration tests and Vitest tests must pass unchanged. ✅ Tests treated as mandatory, not optional.

- **Security**:
  - The `?order=` URL parameter contains only exercise IDs (GUIDs) from the workout already loaded for the user. `active-session.ts` matches them against the API-returned `workout.exercises` — any unrecognised IDs are silently ignored. No user-controlled string is rendered as HTML. No injection risk.
  - `Sequence` in `SessionLoggedExerciseItem` is a nullable `int`. The backend stores it as-is without validation (range enforcement is a frontend concern). No injection surface.
  - **SR-001 (cross-user)**: Same documented exception as features 005, 008, 009 — single-user app; no authentication layer; cross-user leakage is structurally impossible. Deferred to future auth feature.
  - No new secrets, third-party integrations, or trust boundaries introduced. ✅

- **User Experience Consistency**:
  - The pre-start modal uses the same modal pattern as `delete-modal` and `edit-modal`: a `[name]-modal-backdrop` div wrapping a `[name]-modal` div with `role="dialog"`, `aria-modal="true"`, `aria-labelledby`.
  - The shuffle toggle uses `role="switch"` with `aria-checked` toggling between `"true"` / `"false"` — the standard ARIA pattern for toggle buttons.
  - Focus management (trap + return on close) follows the exact pattern from `initDiscardModal()` in `active-session.ts` and the edit/delete modals in `workouts.ts`.
  - Copy: "Randomise order" (British English, matching the spec and the rest of the app). "Re-shuffle" (hyphenated). "Start Workout". "Cancel".
  - All states defined: modal closed (hidden), modal open shuffle-off (toggle visible, list in template order), modal open shuffle-on (toggle + re-shuffle visible, list in shuffled order), single-exercise workout (toggle hidden).
  - ✅

- **Performance**:
  - The shuffle operation is O(n) synchronous array manipulation. For ≤ 50 exercises this is < 1 ms. The modal opens and renders its exercise list from the already-loaded `workouts[]` module state — no new API call is made on modal open.
  - The `?order=` URL parameter parsing and in-memory reorder in `active-session.ts` adds < 1 ms to the existing `loadWorkout()` path.
  - No layout shift: the pre-start modal is in the DOM (hidden) before the user clicks "Start"; no DOM insertion on click.
  - Budget verification: existing manual smoke test of the workout start flow covers this (no new performance test harness needed for sub-millisecond operations). ✅

## Project Structure

### Documentation (this feature)

```text
specs/010-randomize-exercise-order/
├── plan.md              # This file
├── research.md          # Phase 0 output ✅
├── data-model.md        # Phase 1 output ✅
├── quickstart.md        # Phase 1 output ✅
├── contracts/
│   ├── api-contract.md  # POST /api/workouts/{id}/sessions extension ✅
│   └── ui-contract.md   # Pre-start modal HTML/CSS/ARIA/state machine ✅
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                        # MODIFIED: add prestart-modal-* styles
    └── ts/
        ├── utils.ts                          # MODIFIED: add shuffle<T>() and applyOrder<T>() functions
        ├── pages/
        │   ├── home.ts                       # MODIFIED: pre-start modal (fetches workout detail before navigating)
        │   ├── workouts.ts                   # MODIFIED: pre-start modal, shuffle, navigation
        │   └── active-session.ts             # MODIFIED: read ?order= param, send sequence
        └── __tests__/
            └── utils.test.ts                 # MODIFIED: add shuffle() tests

src/WorkoutTracker.Api/
└── Program.cs                                # MODIFIED: SessionLoggedExerciseItem + Sequence

src/WorkoutTracker.Infrastructure/
├── Data/
│   ├── Models/
│   │   └── LoggedExercise.cs                 # MODIFIED: add int? Sequence property
│   └── Migrations/
│       └── …_AddSequenceToLoggedExercise.cs  # NEW: EF Core migration

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                    # MODIFIED: tests for Sequence field on session create
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects. All changes are surgical modifications to existing files plus one new migration. No new routes, no new pages, no new API endpoints.

> **Note**: `home.ts` was also modified to surface the pre-start modal from the home page (where a workout can also be started via a dropdown). This was discovered during implementation — the original plan only identified `workouts.ts` as the trigger entry point.

## Complexity Tracking

> No constitution violations — no entries required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts (research.md, data-model.md, contracts/, quickstart.md) are complete.*

- **Code Quality** ✅ — Design confirms no new abstractions or projects. One new BEM block (`prestart-modal-backdrop` / `prestart-modal`), one new TypeScript function (`shuffle<T>()`), one new C# nullable field (`Sequence`), one new migration — all inline with established conventions.
- **Testing** ✅ — Vitest `shuffle()` unit tests specified. Backend integration tests for `Sequence` field on session creation specified. No new page-level E2E tests required (the modal is a variation of the existing start flow, not a new route). Regression: full existing xUnit and Vitest suites.
- **Security** ✅ — No new trust boundaries. `?order=` IDs are validated client-side against the already-fetched exercise list. `Sequence` is a nullable int stored as-is. Single-user exception documented consistently with prior features.
- **User Experience Consistency** ✅ — Pre-start modal follows the three established modal patterns in the app. ARIA `role="switch"` for toggle. Focus trap and focus return implemented. Four modal states explicitly defined. British English copy.
- **Performance** ✅ — Shuffle is O(n) synchronous in < 1 ms for ≤ 50 exercises. No new API calls on modal open. No layout shift. Existing manual smoke test covers the start flow timing.

No violations. Plan is ready for `/speckit.tasks`.
