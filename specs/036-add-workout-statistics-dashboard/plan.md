# Implementation Plan: Workout Statistics Dashboard

**Feature**: `036-add-workout-statistics-dashboard` | **Git branch**: `stuartvdlee-workout-statistics-dashboard` | **Date**: 2026-11-27 | **Spec**: [spec.md](./spec.md)

## Summary

Add a read-only dashboard for the current or a past local week, month or year. Count completed workouts and distinct per-workout exercise appearances, and show an ordered trend of valid session-wide overall effort with its contributing count. Compute calendar boundaries in the browser, query current completed history by UTC instant, and return a compact period summary. No new persistence, external service, editing, sharing, or export is proposed.

## Technical Context

**Language/Version**: C#/.NET 10; TypeScript ~7.0.2.
**Primary Dependencies**: ASP.NET Core minimal APIs, EF Core/Npgsql, .NET Aspire, vanilla TypeScript; existing CSS and accessible controls. No new runtime dependency.
**Storage**: Existing PostgreSQL completed sessions, shadow `CompletedAt`, overall effort, logged exercises and exercise names; no migration or persisted aggregate.
**Testing**: xUnit/WebApplicationFactory and PostgreSQL API integration; Vitest for calendar and state helpers; Playwright E2E for real interactions, accessibility and layout.
**Target Platform**: Responsive web browsers and existing .NET API/Web deployment.
**Project Type**: Web application.
**Performance Goals**: At least 95% of period changes show the correct updated summary within 2 seconds with up to 1,000 completed workouts; measurable procedure in [quickstart.md](./quickstart.md).
**Constraints**: Local calendar and DST correctness; read latest source on every period selection/re-entry; no future period navigation; bounded period query; reuse the existing workout-history dataset and access path without adding identity or sharing behavior.
**Scale/Scope**: One dashboard and one read-only summary endpoint, one selected week/month/year per request; 1,000 completed workouts is the specified measurement workload, not a hard data limit.

## Constitution Check — Pre-Research

| Principle | Gate and evidence planned | Status |
|---|---|---|
| I. Code quality | Follow existing API, Web proxy, TS page, CSS and strict build conventions; avoid speculative frameworks and new projects. | PASS, subject to implementation review |
| II. Tests prove behavior | Unit-test period/DST logic and aggregation; API integration and browser journeys cover acceptance, regressions and mutation refresh. | PASS, subject to tests |
| III. Security by default | Treat existing workout history as the product's single-user dataset and preserve its current access scope; do not add sharing or a new identity model. Validate and bound inputs, use parameterized queries, and safely render names. No new secrets or third parties are introduced. | PASS for this feature's scope: the endpoint returns only the same completed-workout records available through existing history, restricted to the requested period. Existing deployment access controls are unchanged; this plan does not claim that they provide SSO or user-level isolation. |
| IV. Consistent UX | Reuse history navigation, buttons, typography, no-data conventions and error handling; define loading, empty, unavailable and failure states; keyboard and narrow-screen tests. | PASS, subject to verification |
| V. Performance | Bound the time range and projections, avoid lifetime-history downloads, measure end-to-end p95 and correct results. | PASS, subject to measurement |

This feature reuses the existing single-user history scope and does not add identity or sharing behavior. Existing deployment access controls remain unchanged; this plan does not claim SSO or user-level isolation has been verified.

## Project Structure

### Documentation

```text
specs/036-add-workout-statistics-dashboard/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── contracts/
│   ├── api-contract.md
│   └── ui-contract.md
└── quickstart.md
```

`tasks.md` is a later `/speckit.tasks` output, not created in this phase.

### Source Code

```text
src/WorkoutTracker.Api/Program.cs                   # summary endpoint and validation
src/WorkoutTracker.Infrastructure/Data/             # existing session/exercise entities and EF mapping
src/WorkoutTracker.Web/Program.cs                   # corresponding API proxy
src/WorkoutTracker.Web/wwwroot/ts/main.ts           # route registration/navigation
src/WorkoutTracker.Web/wwwroot/ts/pages/            # statistics page and focused helpers
src/WorkoutTracker.Web/wwwroot/css/                 # responsive dashboard styles
src/WorkoutTracker.Web/wwwroot/ts/__tests__/        # calendar/state tests
src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs  # API/aggregation coverage
src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs # dashboard journeys
src/WorkoutTracker.E2ETests/Infrastructure/         # update mocks if used
```

**Structure Decision**: Extend existing Aspire projects and use source records as the single source of truth. Keep the specific dashboard page and route within existing navigation patterns. Follow 014's history/proxy/test architecture, 016's nullable overall effort, 029's usable-value handling, 031's source-based refresh, 032's existing chart conventions if a chart is used, and 033's real-browser layout testing.

## Phase 0: Research

Decisions and rejected alternatives are recorded in [research.md](./research.md). Technical choices are resolved; deployment access controls are outside this feature and are not represented as verified.

## Phase 1: Design

- [Data model](./data-model.md): source and derived entities, validation and state.
- [API contract](./contracts/api-contract.md): bounded UTC range, aggregation, response and error behavior.
- [UI contract](./contracts/ui-contract.md): periods, accessible states, rendering and refresh.
- [Quickstart](./quickstart.md): functional, security, accessibility and performance verification.

## Post-Design Constitution Check

| Principle | Design result |
|---|---|
| I. Code quality | PASS: small typed endpoint and page with existing conventions; no schema or dependency changes. |
| II. Tests prove behavior | PASS in design: unit, API and browser cases cover period edges, duplicates, partial effort, edits/deletes, errors and accessibility; execution is required before release. |
| III. Security by default | PASS for this feature's scope: the endpoint uses the same single-user history dataset and Web/API access path as existing history, applies a bounded parameterized query, and adds no sharing or identity behavior. Deployment SSO or user-level isolation is not introduced or claimed by this feature. |
| IV. Consistent UX | PASS in design: labeled controls and text values, keyboard and responsive states, plus explicit loading/empty/error handling. |
| V. Performance | PASS in design: bounded period query and defined p95 measurement; actual performance remains unmeasured and must pass before release. |

## Complexity Tracking and Exceptions

No new architectural complexity or constitution exception is requested. The single-user dataset is an explicit feature assumption; existing deployment access controls remain unchanged and are not represented as verified by this plan. No cross-user or sharing behavior is introduced.
