# Implementation Plan: Randomise Exercise Order UX Simplification

**Branch**: `011-randomise-exercise-order` | **Date**: 2026-05-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-randomise-exercise-order/spec.md`

## Summary

Simplify the randomise-exercise-order UX introduced in feature 010. On the **homepage**, the pre-start modal is removed entirely and replaced with an iOS-style toggle ("Randomise exercise order") displayed inline in the workout form; clicking "Start Workout" honours the toggle state directly. On the **Workouts page**, the complex pre-start modal (toggle + exercise preview + re-shuffle + Start/Cancel) is replaced with a minimal confirmation modal that asks "Randomise exercise order?" with only "Yes" and "No" buttons. The "Re-shuffle" button is removed from both pages. No backend changes, no new API endpoints, no database migration.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: No changes — no schema changes, no migration required  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Homepage "Start Workout" navigates directly (no extra API call) when toggle is off; API call only happens when toggle is on (to fetch exercise IDs for `?order=`)  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; `?order=` URL parameter encoding is unchanged; no new files required  
**Scale/Scope**: Changes touch 4 existing files (`home.ts`, `workouts.ts`, `prestart-modal.ts`, `styles.css`); no new files; no backend changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`). Removing `renderPrestartExercisePreview` from `prestart-modal.ts` requires removing all imports of it in `home.ts` and `workouts.ts` to avoid `noUnusedLocals` violations. CSS follows BEM — the new homepage toggle row uses `workout-form__randomise*` following the established `workout-form__*` block. The simplified workouts modal retains the `prestart-modal__*` BEM block. No `any` types introduced. ✅ No deviations required.

- **Testing**: This feature is a pure UI refactoring with no changes to the `shuffle()`, `applyOrder()`, or backend logic that is already under test. Per the established project pattern (features 001, 004, 005, 008, 009), DOM rendering logic is not covered by Vitest. The existing Vitest unit tests for `shuffle()`, `reorder()`, and `applyOrder()` are unaffected and must continue to pass. Existing xUnit backend integration tests are unaffected. **Two new E2E tests are required** (Constitution II — new user journeys MUST have integration or end-to-end coverage): one covering the homepage toggle-on path (verifies `?order=` appears in navigation URL), and one covering the Workouts page Yes-button path (verifies `?order=` appears in navigation URL). These are added as T024 and T025 in Phase 5 of `tasks.md`. ✅

- **Security**: No new trust boundaries. The `?order=` URL parameter is already validated in `active-session.ts` against the API-fetched exercise list (feature 010). Removing the Re-shuffle button introduces no security consideration. SR-001 (cross-user) exception documented identically to features 005, 008, 009, 010 — single-user app, no auth layer. ✅

- **User Experience Consistency**:
  - Homepage inline toggle uses the same iOS-style `role="switch"` button pattern already defined in `prestart-modal__shuffle-btn` CSS; new class name `workout-form__randomise-btn` reuses the same geometry and transition values.
  - Workouts page modal retains `prestart-modal-backdrop` / `prestart-modal` BEM structure, focus trap (`trapModalTabKey`), backdrop click to close, and Escape key handling — consistent with all other modals in the app.
  - "Yes"/"No" button styles follow the existing primary/secondary pairing (`prestart-modal__start-btn` / `prestart-modal__cancel-btn`) via new `prestart-modal__yes-btn` / `prestart-modal__no-btn` aliases.
  - States defined: (homepage) toggle visible ≥2 exercises / hidden <2 exercises; (workouts modal) open / closed via Yes, No, or Escape.
  - Copy: "Randomise exercise order" (label on homepage toggle), "Randomise exercise order?" (modal title on Workouts page), "Yes", "No". British English throughout. ✅

- **Performance**:
  - Homepage with toggle off → direct `navigate()` call, **zero API round-trips** vs. one in feature 010. This is a performance improvement.
  - Homepage with toggle on → one `GET /api/workouts/{id}` call (same as feature 010).
  - Workouts page Yes/No → exercises already in memory from page load, no extra API call (same as feature 010).
  - Removing the exercise preview list (`renderPrestartExercisePreview`) eliminates the DOM list render on modal open — minor improvement.
  - Budget verification: manual smoke test of the workout start flow; no new perf test harness needed for sub-millisecond DOM changes. ✅

## Project Structure

### Documentation (this feature)

```text
specs/011-randomise-exercise-order/
├── plan.md              # This file
├── research.md          # Phase 0 output ✅
├── data-model.md        # Phase 1 output ✅
├── quickstart.md        # Phase 1 output ✅
├── contracts/
│   └── ui-contract.md   # Updated UI modal and toggle contracts ✅
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                        # MODIFIED: remove reshuffle/exercise-list/shuffle-modal CSS; add workout-form__randomise* + yes/no btn styles
    └── ts/
        ├── prestart-modal.ts                 # MODIFIED: remove PrestartExercisePreview interface + renderPrestartExercisePreview export
        └── pages/
            ├── home.ts                       # MODIFIED: remove modal; add inline toggle; update PlannedWorkout interface; simplify start flow
            └── workouts.ts                   # MODIFIED: replace complex modal with Yes/No modal; remove reshuffle/toggle/preview logic
```

**Structure Decision**: Existing project structure preserved. No new files, no new routes, no backend changes, no migration. All changes are surgical deletions and additions within 4 existing files.

## Complexity Tracking

> No constitution violations — no entries required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts (research.md, data-model.md, contracts/, quickstart.md) are complete.*

- **Code Quality** ✅ — Pure refactoring within existing BEM/TypeScript patterns. Dead code (`renderPrestartExercisePreview`, reshuffle functions, modal state vars) is fully removed. New `workout-form__randomise*` BEM elements follow the block convention. No `any`, no speculative abstractions.
- **Testing** ✅ — No logic changes to tested functions (`shuffle`, `applyOrder`). Existing Vitest and xUnit suites cover all unchanged behaviour. DOM rendering changes follow the established no-Vitest-for-DOM pattern. Two new E2E tests added (T024, T025) to satisfy Constitution II for the new toggle-on and Yes-path user journeys.
- **Security** ✅ — No new trust boundaries. `?order=` validation in `active-session.ts` is unchanged. Single-user exception documented.
- **User Experience Consistency** ✅ — iOS toggle reuses existing CSS geometry. Workouts modal retains established modal pattern. All states explicitly defined. British English copy.
- **Performance** ✅ — Homepage direct navigation (toggle off) eliminates one API call vs. feature 010. Workouts page in-memory shuffle unchanged. Exercise preview list render eliminated.

No violations. Plan is ready for `/speckit.tasks`.
