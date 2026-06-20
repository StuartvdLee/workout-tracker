# Implementation Plan: Workout Overall Effort

**Branch**: `016-workout-overall-effort` | **Date**: 2026-05-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-workout-overall-effort/spec.md`

## Summary

When a user presses "Save Workout", an effort modal appears prompting them to rate the session on the same 1–10 scale used for individual exercises. Confirming or skipping the modal completes the save. The overall effort is stored as a new nullable integer column on `WorkoutSession`. It surfaces on the session detail page, where a summary row below the exercises table shows the current overall effort alongside the previous session's overall effort. The history page was initially updated to show effort per card but this was removed after delivery (too cluttered). The change touches one domain model, one EF migration, one API file, one Web proxy file, two TypeScript page modules, one CSS file, and two test files.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
**Storage**: PostgreSQL via EF Core — adding one nullable integer column (`overall_effort`) to the existing `workout_session` table
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests; Playwright E2E tests
**Target Platform**: Web browser (mobile-first, responsive)
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: Effort modal appears immediately on "Save Workout" click (no network call required before render); save completes within the same latency budget as the current save flow
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; BEM CSS naming; strict TypeScript (`strict: true`, `noUnusedLocals`, `noUnusedParameters`); backward compatibility — sessions without `overall_effort` must display without errors
**Scale/Scope**: Changes touch 6 files (`WorkoutSession.cs`, `WorkoutTrackerDbContext.cs`, `Program.cs` API, `active-session.ts`, `history.ts`, `session-detail.ts`, `styles.css`) + 1 migration + 2 test files

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced — no `any`, all new state variables explicitly typed. CSS follows BEM: new modal uses `.effort-modal__*` block (parallel to existing `.discard-modal__*`). C# follows the existing inline anonymous-type pattern and existing validation style (`if (X is not null && (X < 1 || X > 10))`). The effort modal HTML is scaffolded inline in `active-session.ts` — consistent with how the discard modal is co-located. No speculative abstractions or dead code. ✅

- **Testing**:
  - **Backend integration tests** (`SessionApiTests.cs`): New tests for overall effort — (a) `CreateSession_StoresOverallEffort_WhenProvided`; (b) `CreateSession_StoresNullOverallEffort_WhenNotProvided`; (c) `CreateSession_StoresNullOverallEffort_WhenOverallEffortOmitted`; (d) `CreateSession_Returns400_WhenOverallEffortOutOfRange`; (e) `GetSessionDetail_ReturnsOverallEffort`; (f) `GetSessionDetail_ReturnsPreviousOverallEffort_WhenPriorSessionExists`; (g) `GetSessionDetail_ReturnsNullPreviousOverallEffort_WhenNoPriorSession`; (h) `GetSessions_ReturnsOverallEffort`.
  - **E2E (Playwright / `WorkoutHistoryTests.cs`)**: New tests — `SaveWorkout_EffortModal_AppearsOnSave`, `SaveWorkout_EffortModal_SkipSavesWithoutEffort`, `SaveWorkout_EffortModal_ConfirmSavesWithEffort`, `SessionDetailPage_ShowsOverallEffortSummaryRow`, `SessionDetailPage_ShowsPreviousOverallEffort_WhenPriorSessionExists`. History-page display tests (`HistoryPage_ShowsOverallEffort_WhenSessionHasEffort`, `HistoryPage_NoEffortShown_WhenSessionHasNoEffort`) were implemented then removed when the history page effort display was removed.
  - **Frontend Vitest**: No new Vitest tests — effort modal follows the established DOM-rendering pattern; `getEffortLabel` already tested.
  - Tests treated as mandatory. ✅

- **Security**: `OverallEffort` in `SessionCreateRequest` is validated server-side: if provided, must be an integer in range 1–10, otherwise rejected with 400 — consistent with per-exercise effort validation. EF Core check constraint added for double-safety. Single-user app, no cross-user access control concern (SR-002 exception documented consistently with features 005, 008, 013, 014). No new trust boundaries or secrets. ✅

- **User Experience Consistency**: The effort modal reuses the exact same `alertdialog` + focus-trap + Escape-key-dismiss + backdrop-click-dismiss pattern as the existing discard modal. The effort slider inside the modal is built identically to the per-exercise effort slider (`data-touched`, real-time band label update, "Not rated" initial state). History card layout follows existing `.history-session__exercise-count` right-aligned pattern. Session detail summary row reuses `var(--color-text-light)` for the "Previous" label consistent with existing `--prev` column styling. ✅

- **Performance**: The effort modal appears immediately — no network call before render (modal HTML is pre-scaffolded in the page, shown/hidden via `display`). Saving with `overall_effort` adds one nullable column to an existing INSERT — negligible overhead. History and detail API endpoints are unchanged in query scope; `overall_effort` is a direct column on `workout_session` already fetched. ✅

## Project Structure

### Documentation (this feature)

```text
specs/016-workout-overall-effort/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # Updated session endpoints
│   └── ui-contract.md   # Effort modal + history card + detail summary row
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Infrastructure/
├── Data/
│   ├── Models/
│   │   └── WorkoutSession.cs                       # MODIFIED: add OverallEffort (int?) property
│   ├── WorkoutTrackerDbContext.cs                  # MODIFIED: add check constraint on overall_effort
│   └── Migrations/
│       └── [timestamp]_AddOverallEffortToWorkoutSession.cs  # NEW: nullable int column + check constraint

src/WorkoutTracker.Api/
└── Program.cs                                      # MODIFIED: SessionCreateRequest + overall effort
                                                    #   validation + POST/GET response projections

src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                              # MODIFIED: add .effort-modal__*, 
    │                                               #   .history-session__overall-effort,
    │                                               #   .session-detail__overall-effort-row styles
    └── ts/
        └── pages/
            ├── active-session.ts                  # MODIFIED: add effort modal HTML scaffold,
            │                                      #   intercept save button, modal open/close/
            │                                      #   confirm/skip logic, pass overallEffort in POST
            ├── history.ts                         # NOT MODIFIED: effort display implemented
            │                                      #   then removed (too cluttered)
            └── session-detail.ts                  # MODIFIED: add overallEffort + previousOverallEffort
                                                   #   to interface, render summary row below table

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                         # MODIFIED: add overall effort tests (8 new tests)

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs                     # MODIFIED: add effort modal + display tests
                                                   #   (7 new tests)
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects, no new TypeScript page modules. The effort modal HTML is co-located in `active-session.ts` — consistent with the discard modal. No new utility functions needed; `getEffortLabel` from `utils.ts` is imported by `active-session.ts` already.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts are complete.*

- **Code Quality** ✅ — Six existing files surgically modified. No new abstractions, no `any`, BEM naming, existing discard modal pattern reused for effort modal, existing `getEffortLabel` reused.
- **Testing** ✅ — Backend: 8 new integration tests. E2E: 7 new Playwright tests. Vitest: no new tests (consistent with feature 014 pattern). All mandatory.
- **Security** ✅ — Server-side range validation for `OverallEffort` consistent with per-exercise effort, EF check constraint, single-user SR-002 exception documented.
- **User Experience Consistency** ✅ — Effort modal mirrors discard modal pattern. Slider mirrors per-exercise slider. History card and detail page use established CSS token and BEM patterns.
- **Performance** ✅ — Modal is pre-rendered (no network before show), save adds one nullable column to an existing INSERT, API projections extend existing queries with one direct column.

No violations. Plan is ready for `/speckit.tasks`.
