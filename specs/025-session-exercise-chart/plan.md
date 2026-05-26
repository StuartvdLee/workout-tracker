# Implementation Plan: Session Exercise Chart

**Branch**: `025-session-exercise-chart` | **Date**: 2026-05-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/025-session-exercise-chart/spec.md`

## Summary

Add a line chart to the session detail page that plots historical workout data for the same planned workout. A dropdown lets the user select one data series at a time: "Overall Session Effort", or per-exercise "ExerciseName – Weight" or "ExerciseName – Effort". The chart renders as an inline SVG (no external library), using data from a new `GET /api/workouts/{workoutId}/session-trends` endpoint that returns up to 50 most-recent sessions. Chart math helpers (coordinate normalisation, tick generation, degenerate-range handling) are extracted into `utils.ts` and unit-tested. The trends fetch starts eagerly as soon as `plannedWorkoutId` is known from the session detail response.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks, no charting library)
**Storage**: PostgreSQL via EF Core — no schema changes; all required data exists in `workout_session` and `logged_exercise`
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests; Playwright E2E tests
**Target Platform**: Web browser (mobile-first, responsive)
**Project Type**: Web application (SPA with .NET Aspire orchestration)
**Performance Goals**: Chart renders within the session page load budget; trends endpoint returns within the same time budget as other session queries; `Take(50)` cap ensures bounded query size regardless of workout history length
**Constraints**: No external JS/CSS frameworks or charting libraries; vanilla TypeScript only; existing tests must continue to pass; BEM CSS naming; strict TypeScript; `CompletedAt` MUST be accessed via `EF.Property<DateTime>(ws, "CompletedAt")` — it is a shadow property with no CLR counterpart on `WorkoutSession`
**Scale/Scope**: Changes touch `Program.cs` (API), `Program.cs` (Web proxy), `session-detail.ts`, `utils.ts`, `styles.css`, `SessionApiTests.cs`, `WorkoutHistoryTests.cs`; no migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced — no `any`, all interfaces explicitly typed. Chart math helpers extracted into `utils.ts` as pure functions (no side-effects, no DOM access). `Number()` used (not `parseFloat`) for weight parsing to catch partial-parse silently wrong inputs. CSS follows BEM: new block is `.session-chart__*`. C# follows existing inline anonymous-type projection pattern. No speculative abstractions or dead code. ✅

- **Testing**:
  - **Backend integration tests** (`SessionApiTests.cs`): New tests for `GET /api/workouts/{workoutId}/session-trends`: (a) returns 404 when workoutId does not exist; (b) returns empty array when no sessions recorded; (c) returns sessions ordered chronologically (oldest first); (d) returns at most 50 sessions for a workout with many sessions; (e) returns correct overallEffort, loggedWeight, and effort per exercise; (f) returns null for fields that were not recorded.
  - **Frontend Vitest** (`utils.test.ts`): New unit tests for the extracted chart math functions: `normaliseValue`, `buildYTicks`, `buildXLabels` — covering degenerate range (min === max, all-zero, single point), negative min, and standard multi-point cases.
  - **E2E (Playwright / `WorkoutHistoryTests.cs`)**: New tests — `SessionDetailPage_ShowsChartSection_WhenWorkoutHasPreviousSession`, `SessionDetailPage_ChartDropdown_SwitchesSeries`, `SessionDetailPage_Chart_ShowsEmptyState_WhenNoHistoricalData`, `SessionDetailPage_NoChartSection_WhenSessionHasNoPlannedWorkout`.
  - Tests treated as mandatory. ✅

- **Security**: New `GET /api/workouts/{workoutId}/session-trends` uses a GUID workoutId — EF Core parameterises it, preventing SQL injection. Response data originates from DB (previously validated on write). Single-user app; cross-user leakage structurally impossible consistent with features 008, 013, 014, 016 (SR-002 exception documented). No new secrets, integrations, or trust boundaries. All exercise names rendered into the SVG via `escapeHtml()` / `textContent` assignment — no innerHTML for user-supplied strings. ✅

- **User Experience Consistency**: Chart section uses existing CSS custom properties (`--color-primary`, `--color-text`, `--color-text-light`, `--color-border`, `--color-white`, `--color-bg`). Dropdown reuses the existing `<select>` styling (`.workout-form__select` pattern). All four states defined: loading ("Loading chart…"), empty ("No data available for this selection"), success (rendered SVG), error ("Could not load chart data"). Y-axis for effort series fixed at 0–10 (effort is bounded 1–10 by DB constraint); Y-axis for weight series uses dynamic min/max. ✅

- **Performance**: `GET /api/workouts/{workoutId}/session-trends` fetches at most 50 sessions (`Take(50)` capped, ordered by `CompletedAt` descending then reversed to chronological) with a bounded join on `logged_exercise`. No N+1 — exercises loaded in one Include. Trends fetch starts eagerly once `plannedWorkoutId` is known from the session detail response (no sequential blocking beyond the existing session fetch). SVG rendering is pure in-memory DOM construction — no layout thrashing, no animation. ✅

## Project Structure

### Documentation (this feature)

```text
specs/025-session-exercise-chart/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # New GET /api/workouts/{workoutId}/session-trends endpoint
│   └── ui-contract.md   # Chart section HTML/CSS/ARIA contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                              # MODIFIED: add GET /api/workouts/{workoutId}/session-trends

src/WorkoutTracker.Web/
└── Program.cs                              # MODIFIED: proxy GET /api/workouts/{workoutId}/session-trends
└── wwwroot/
    ├── css/
    │   └── styles.css                      # MODIFIED: add .session-chart__* BEM block styles
    └── ts/
        ├── utils.ts                        # MODIFIED: add normaliseValue, buildYTicks, buildXLabels
        └── pages/
            └── session-detail.ts           # MODIFIED: add chart section, dropdown, SVG renderer,
                                            #           eager trends fetch after session detail loads

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                  # MODIFIED: add session-trends endpoint tests (6 new tests)

src/WorkoutTracker.Web/
└── __tests__/
    └── utils.test.ts                       # MODIFIED: add chart math function tests

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs              # MODIFIED: add chart section E2E tests (4 new tests)
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects. The chart is rendered inline in `session-detail.ts` using SVG — no new page module needed. Chart math helpers are pure functions extracted into the existing `utils.ts`, keeping them testable without DOM dependencies. The new API endpoint follows the established inline projection pattern in `Program.cs`.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts are complete.*

- **Code Quality** ✅ — No new projects. Surgical modification of existing files. Chart math extracted into `utils.ts` as pure functions (no DOM, no side-effects). No `any`, BEM naming, existing CSS token system. `Number()` used for weight parsing. Shadow property pattern explicitly documented and required.
- **Testing** ✅ — Backend: 6 new integration tests for the trends endpoint. Vitest: new unit tests for `normaliseValue`, `buildYTicks`, `buildXLabels` (arithmetic functions warrant unit coverage). E2E: 4 new Playwright tests. All mandatory.
- **Security** ✅ — GUID parameter, EF parameterisation, exercise names injected via `textContent` (not `innerHTML`), `escapeHtml()` used where innerHTML is unavoidable. Single-user SR-002 exception documented consistently.
- **User Experience Consistency** ✅ — Existing `<select>` styling reused. Existing CSS tokens for all colours. All four states (loading, success, empty, error) defined. No chart section rendered for ad-hoc sessions.
- **Performance** ✅ — `Take(50)` cap. Eager post-session-load fetch. Single bounded EF query. SVG is in-memory DOM — no layout thrashing.

No violations. Plan is ready for `/speckit.tasks`.
