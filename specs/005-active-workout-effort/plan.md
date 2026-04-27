# Implementation Plan: Active Workout UI — Effort Tracking

**Branch**: `005-active-workout-effort` | **Date**: 2026-04-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-active-workout-effort/spec.md`

## Summary

Refine the active workout session view and history view. The three changes are: (1) remove the reps input from the active session UI and stop displaying reps in history, (2) make weight units explicit by labelling the weight field as "KG", and (3) add a per-exercise Effort slider (1–10, integer steps, with named bands Easy/Moderate/Hard/All Out) that persists to a new `effort` column on the `logged_exercise` table. Backend: one new nullable integer column added via EF Core migration, API updated to accept and return effort. Frontend: `active-session.ts` reworked to remove reps, rename weight label, and add slider; `history.ts` updated to show weight-KG and effort-with-label, never reps.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via EF Core — adding one nullable integer column (`effort`) to the existing `logged_exercise` table  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Active session view loads within 3 seconds on slow 3G; Effort slider label updates within 50ms of interaction; Save feedback visible within 200ms  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; backward compatibility — old sessions without effort data must display without errors  
**Scale/Scope**: Single-user; changes touch 3 files (LoggedExercise.cs, Program.cs, active-session.ts, history.ts, styles.css) + 1 migration + test updates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. CSS follows BEM naming. C# uses `<Nullable>enable</Nullable>` and snake_case DB naming via `UseSnakeCaseNamingConvention()`. The `LogEntry` interface in `active-session.ts` must be updated to remove `loggedReps` and add `effort: number | null`; unused `loggedReps` tracking code must be deleted. ✅ No deviations.

- **Testing**: 
  - **Backend integration tests**: `SessionApiTests.cs` must be updated — add `Effort` to `SessionLoggedExerciseItem`, add `effort` assertions to existing tests, add a new test `CreateSession_StoresEffort_WhenProvided`. The DTO classes used in tests (`SessionDetailDto`, `LoggedExerciseDto`) must include `Effort`.
  - **Frontend unit tests**: The existing Vitest tests cover `router.ts` only and do not test page rendering — no new Vitest tests required for this change, consistent with the pattern established in feature 004.
  - **Regression**: All 45 existing xUnit integration tests must pass unchanged (effort field is nullable and additive). ✅ Mandatory, not optional.

- **Security**: 
  - Effort value must be validated server-side: if provided, must be an integer in the range 1–10; reject with 400 if out of range.
  - Client-side: the slider is a range input constrained to min=1, max=10, step=1 — the browser enforces the range; server-side validation is the final gate.
  - `LoggedWeight` is validated server-side for maximum length (100 chars) before persistence; the field remains a free-form string consistent with research Decision 5.
  - No new trust boundaries or secrets introduced. ✅
  - **SR-002 exception (documented)**: This application is currently single-user with no authentication layer. There is no user identity concept in the data model, so cross-user session leakage is structurally impossible. SR-002 is deferred to a future authentication feature. ✅

- **User Experience Consistency**: 
  - Active session view already uses BEM class `active-session__input-group` for reps/weight/notes — the effort slider follows the same grouping pattern with a new modifier class.
  - The intensity label (Easy/Moderate/Hard/All Out) is always visible beneath the slider thumb, consistent with how target information is shown in the same view.
  - History view follows the same `escapeHtml` pattern for all user-supplied strings. Effort label is static and does not need escaping.
  - Empty/null effort is silently omitted from history display — no placeholder. ✅

- **Performance**: 
  - Active session load: 3 seconds on slow 3G — no additional API calls introduced (effort is part of the existing workout load).
  - Slider label update: 50ms — handled synchronously on the `input` event; no async work.
  - Save feedback: 200ms — no changes to the save path timing.
  - History: no additional queries; `effort` is a column on the already-included `LoggedExercise` table. ✅

## Project Structure

### Documentation (this feature)

```text
specs/005-active-workout-effort/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # Updated session endpoint contracts
│   └── ui-contract.md   # Active session and history UI contracts
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Infrastructure/
├── Data/
│   ├── Models/
│   │   └── LoggedExercise.cs              # MODIFIED: add Effort (int?) property
│   └── Migrations/
│       └── [timestamp]_AddEffortToLoggedExercise.cs   # NEW: nullable int column

src/WorkoutTracker.Api/
└── Program.cs                             # MODIFIED: accept/return Effort in session endpoints
                                           # MODIFIED: add server-side range validation (1–10)

src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                     # MODIFIED: add effort slider styles (BEM)
    └── ts/
        ├── utils.ts                       # NEW: shared getEffortLabel helper (Decision 7)
        └── pages/
            ├── active-session.ts          # MODIFIED: remove reps, add KG label, add effort slider
            └── history.ts                 # MODIFIED: remove reps display, add weight-KG + effort display

src/WorkoutTracker.Tests/
└── Api/
    └── SessionApiTests.cs                 # MODIFIED: add Effort to DTOs, add effort assertion tests
```

**Structure Decision**: Existing .NET Aspire solution structure is preserved. No new projects, no new files in the TypeScript pages directory. All changes are surgical modifications to existing files plus one new EF migration. One new shared TypeScript utility file (`utils.ts`) is added to `wwwroot/ts/` to avoid duplicating `getEffortLabel` across page modules (see research Decision 7).

## Implementation Phases

### Phase 0: Research & Clarification

**Output**: `research.md` with all design decisions documented

**Research tasks** (all resolved — no NEEDS CLARIFICATION markers in spec):
1. EF Core additive migration pattern — adding a nullable column to an existing table without breaking existing data
2. HTML range input accessibility — ARIA attributes, keyboard navigation, label association for effort slider
3. Backward compatibility — how existing `LoggedExercise` rows (with `effort = NULL`) are handled in the history display

### Phase 1: Design & Contracts

**Prerequisites**: `research.md` complete

1. **Data model** (`data-model.md`): Document the `LoggedExercise` entity change and migration
2. **API contract** (`contracts/api-contract.md`): Updated `POST /api/workouts/{id}/sessions` and `GET /api/sessions` to include `effort`
3. **UI contract** (`contracts/ui-contract.md`): Active session exercise card (no reps, KG weight, effort slider) and history exercise row (KG weight + effort label, no reps)
4. **Quickstart** (`quickstart.md`): Updated walkthrough covering the effort slider and new history display
5. **Agent context update**: Run `.specify/scripts/bash/update-agent-context.sh copilot`

**Output**: research.md, data-model.md, contracts/api-contract.md, contracts/ui-contract.md, quickstart.md, updated agent context

### Phase 2: Implementation (Dependency Graph)

**Prerequisites**: Phase 1 complete

**Workstream A: Database + API (backend)**
1. Add `Effort` property to `LoggedExercise.cs` (nullable int, 1–10)
2. Add check constraint in `DbContext.OnModelCreating` (`CHECK (effort IS NULL OR (effort >= 1 AND effort <= 10))`)
3. Generate EF Core migration
4. Update `SessionLoggedExerciseItem` DTO: add `Effort` (int?)
5. Update `POST /api/workouts/{id}/sessions`: validate effort range server-side, persist to `LoggedExercise.Effort`
6. Update `GET /api/sessions` response projection: include `le.Effort` in the anonymous DTO
7. Update `SessionApiTests.cs`: update DTOs, add effort assertions, add `CreateSession_StoresEffort_WhenProvided` test

**Workstream B: Frontend**
1. Update `active-session.ts`:
   - Remove `loggedReps` from `LogEntry` interface
   - Remove reps input group from `renderExerciseInputs()`
   - Update weight label to "KG" (label text and aria-label)
   - Add effort slider per exercise (range 1–10, hidden initially/unset state, label div)
   - Update `handleSave()`: remove `loggedReps`, include `effort`
2. Update `history.ts`:
   - Remove `loggedReps` from `LoggedExercise` interface
   - Update `renderSession()`: remove reps display, add weight-KG display, add effort-with-label display
3. Add BEM CSS classes for effort slider to `styles.css`

**Dependencies**: Workstream A must complete before B submits a session (integration test can run in parallel if effort field is nullable)

## Complexity Tracking

> No constitution violations — no entries required.
