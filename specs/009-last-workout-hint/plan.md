# Implementation Plan: Last Workout Hint on Home Page

**Branch**: `009-last-workout-hint` | **Date**: 2026-05-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-last-workout-hint/spec.md`

## Summary

Display a small read-only hint below the "Start Workout" button on the home page showing the name and date of the most recently completed workout session. The hint is loaded asynchronously after the page renders and is silently absent on failure or when no sessions exist. This requires one new API endpoint (`GET /api/sessions/latest`), a corresponding proxy route in the Web project, a non-blocking fetch in `home.ts`, and a new BEM class in `styles.css`. No schema changes are needed.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via EF Core — no schema changes; all required data is in the existing `workout_session` table  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Home page remains interactive within 3 seconds on slow 3G; last workout hint fetch is non-blocking and does not delay the dropdown or "Start Workout" button  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; hint fetch failure MUST NOT block the home page; no layout shift when hint loads  
**Scale/Scope**: Single-user; changes touch 5 files (`WorkoutTracker.Api/Program.cs`, `WorkoutTracker.Web/Program.cs`, `home.ts`, `styles.css`, `SessionApiTests.cs`) + no migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. CSS follows BEM naming — the new hint element MUST use `workout-form__last-workout` following the established `workout-form__*` block. C# uses `<Nullable>enable</Nullable>` and snake_case DB naming via `UseSnakeCaseNamingConvention()`. The new `LastWorkoutDto` interface in `home.ts` must be typed explicitly; no `any`. ✅ No deviations required.

- **Testing**:
  - **Backend integration tests**: New tests required in `SessionApiTests.cs` for `GET /api/sessions/latest`: (a) returns `{ hasSession: false }` when no sessions exist; (b) returns workout name and completedAt after one session is created; (c) returns only the most recent session when multiple sessions exist; (d) returns the correct workout name when the most recent session differs from earlier ones. All existing tests must continue to pass.
  - **Frontend Vitest tests**: No new Vitest tests — the hint display logic is DOM rendering, consistent with the pattern from features 001, 004, 005, and 008 where page rendering is not covered by Vitest. Async fetch behaviour and error swallowing are integration/E2E concerns.
  - **Regression**: All existing xUnit integration tests must pass unchanged. ✅ Tests treated as mandatory, not optional.

- **Security**:
  - `GET /api/sessions/latest` is a read-only endpoint with no request body and no user-controlled query parameters — zero injection surface.
  - Response data originates entirely from the database (previously validated on write); no re-sanitisation needed on the read path.
  - **SR-001 (cross-user)**: Same documented exception as `005-active-workout-effort` and `008-workout-exercise-history` — single-user app with no authentication layer; cross-user leakage is structurally impossible. Deferred to future auth feature.
  - No new secrets, third-party integrations, or trust boundaries introduced. ✅

- **User Experience Consistency**:
  - The hint text uses `--color-text-light` CSS custom property (established in feature 001 as `#6b7280`) to visually distinguish read-only reference text from active form controls.
  - All four states defined: loading (element not rendered / absent), empty/no-sessions (element not rendered), success (hint text shown), error (silent fail — element not rendered). No visible loading spinner; no error label.
  - Date displayed in the same format used on the history page — human-readable, unambiguous (e.g., "3 May 2026") using `toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })`.
  - The hint lives inside the existing `workout-form` block, following the `workout-form__*` BEM pattern established in feature 001. ✅

- **Performance**:
  - The hint fetch (`GET /api/sessions/latest`) is fired after `render()` completes using an immediately-invoked async IIFE — the page is fully interactive before the fetch begins.
  - `GET /api/sessions/latest` is a single-row read: `OrderByDescending(CompletedAt).Select(minimal DTO).FirstOrDefaultAsync()`. Bounded, indexed query. ✅
  - No layout shift on hint load — the hint element is inserted into the DOM only when data is available; the surrounding layout uses `gap` spacing that absorbs its absence/presence without reflow of other controls. ✅

## Project Structure

### Documentation (this feature)

```text
specs/009-last-workout-hint/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-contract.md  # GET /api/sessions/latest endpoint
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                          # MODIFIED: add GET /api/sessions/latest

src/WorkoutTracker.Web/
└── Program.cs                          # MODIFIED: proxy GET /api/sessions/latest
└── wwwroot/
    ├── css/
    │   └── styles.css                  # MODIFIED: add .workout-form__last-workout style
    └── ts/
        └── pages/
            └── home.ts                 # MODIFIED: fetch hint data, render conditionally

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs             # MODIFIED: tests for GET /api/sessions/latest
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects. All changes are surgical modifications to existing files. No EF migration. The `GET /api/sessions/latest` endpoint is a new read-only query added inline in `WorkoutTracker.Api/Program.cs`, consistent with all other endpoints in the project.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts (data-model.md, contracts/, quickstart.md) are complete.*

- **Code Quality** ✅ — Design confirms no new abstractions, patterns, or projects. One new BEM class (`workout-form__last-workout`), one new TypeScript interface (`LastWorkoutDto`), one new C# endpoint — all inline with existing conventions.
- **Testing** ✅ — Four new backend integration tests identified and scoped in the Constitution Check above. No frontend unit tests required (consistent with established pattern). Regression coverage: full existing xUnit suite.
- **Security** ✅ — No request body, no path parameters, no user-controlled input. Read-only endpoint. Single-user exception documented and consistent with features 005 and 008.
- **User Experience Consistency** ✅ — Hint uses `--color-text-light`, `var(--font-size-sm)`, and `workout-form__*` BEM naming. All four states (loading, empty, success, error) are defined. No new design tokens.
- **Performance** ✅ — Non-blocking async fetch; single-row DB query; no layout shift. Home page interactive before hint loads. Budget (interactive within 3s on slow 3G) unaffected.

No violations. Plan is ready for `/speckit.tasks`.

