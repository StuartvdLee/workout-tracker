# Implementation Plan: Edit Past Workouts

**Branch**: `031-edit-past-workouts` | **Date**: 2026-07-19 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/031-edit-past-workouts/spec.md`

## Summary

Add edit mode to the existing session detail page so users can correct or fill missing historical per-exercise `loggedWeight` and `effort` values, plus session-level `overallEffort`. The existing session model already stores all fields, so no schema migration is needed. Add a new `PUT /api/sessions/{sessionId}` API endpoint and matching web proxy route that updates only editable historical values on an existing completed workout session. The frontend updates `session-detail.ts` to toggle between read-only review and edit mode, reuse the existing 1-10 effort scale/labels, preserve chart/delete behavior after save, and reuse the established unsaved-changes discard-confirmation pattern from feature 023.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~7.0.2 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript, Playwright, Vitest  
**Storage**: PostgreSQL via EF Core — no schema changes; updates existing `workout_session.overall_effort`, `logged_exercise.logged_weight`, and `logged_exercise.effort` columns  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Playwright E2E tests; Vitest only if extractable frontend helpers are added  
**Target Platform**: Web browser (mobile-first responsive UI)  
**Project Type**: Web application (SPA-style frontend served by ASP.NET Core / .NET Aspire orchestration)  
**Performance Goals**: Edit mode usable within 1 second for sessions with up to 25 exercises; save feedback within 2 seconds for up to 25 edited exercise entries; no noticeable regression to history/detail page load times  
**Constraints**: No external JS/CSS frameworks; strict TypeScript; preserve existing session identity/date/exercise order/membership; use existing effort labels and no-data marker; update historical comparison consumers by changing the source data, not by adding cache invalidation or duplicated state  
**Scale/Scope**: Changes touch `WorkoutTracker.Api/Program.cs`, `WorkoutTracker.Web/Program.cs`, `session-detail.ts`, `styles.css`, `SessionApiTests.cs`, and `WorkoutHistoryTests.cs`; no migration or new project

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Keep the edit workflow inside the existing `session-detail.ts` module because the affected UI is the current session detail page. Add one typed update request DTO in `WorkoutTracker.Api/Program.cs`, one proxy route following existing Web proxy patterns, and explicit TypeScript interfaces for editable snapshots and update payloads. No `any`, no duplicated effort-label logic, and no broad catch/silent success paths. Existing `getEffortLabel`, `escapeHtml`, session-detail BEM block, and discard-modal classes must be reused. ✅
- **Testing**: Add backend integration tests for successful update, clearing optional values, invalid effort/weight validation, unknown session, foreign logged exercise ID rejection, and unchanged workout identity/order. Add Playwright coverage for entering edit mode, saving exercise weight/effort edits, editing overall effort, cancelling edits, discard warning with unsaved changes, save failure retaining edits, and updated historical comparisons/chart data after save where practical. Existing session detail and delete tests must continue to pass. ✅
- **Security**: New endpoint accepts a GUID path parameter and JSON body. Validate session existence, ensure every `loggedExerciseId` belongs to the target session, reject malformed/out-of-range effort and over-length weight values server-side, and expose only generic validation/not-found errors. Single-user app authorization assumptions remain consistent with prior workout/session features; no secrets or external services are introduced. ✅
- **User Experience Consistency**: Edit mode appears on the existing session detail page, reuses table/summary-row layout, same "—" no-data marker, same effort scale labels, same BEM naming, and the feature 023 discard modal pattern for unsaved changes. Loading, validation error, save error, success, cancel, and retry states are explicitly designed. ✅
- **Performance**: Updating a session uses one bounded load of the target session and its logged exercises followed by one `SaveChangesAsync`. Frontend edit mode reuses already loaded detail data and requires no fetch before becoming editable. The existing trends endpoint reflects saved values naturally because the source rows are updated. ✅

## Project Structure

### Documentation (this feature)

```text
specs/031-edit-past-workouts/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # Session update endpoint and proxy contract
│   └── ui-contract.md   # Session detail edit-mode UI contract
└── tasks.md             # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                              # MODIFIED: add PUT /api/sessions/{sessionId};
                                            # add SessionUpdateRequest DTO and validation

src/WorkoutTracker.Web/
├── Program.cs                              # MODIFIED: proxy PUT /api/sessions/{sessionId}
└── wwwroot/
    ├── css/
    │   └── styles.css                      # MODIFIED: session-detail edit controls/states
    └── ts/
        └── pages/
            └── session-detail.ts           # MODIFIED: edit mode, snapshots, save/cancel,
                                            # discard warning, updated render after save

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                  # MODIFIED: session update integration tests

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs              # MODIFIED: past-workout edit E2E coverage
```

**Structure Decision**: Preserve the existing .NET Aspire solution and page-module layout. This is an update to the existing completed-session detail surface, so no new page, no schema migration, and no new frontend framework are introduced. The plan follows previous specs 014, 016, 023, 029, and 030.

## Phase 0: Research Outcomes

Research captured in [research.md](./research.md).

Key decisions from previous plans reused here:

1. **Feature 014 (`014-workout-history-detail-page`)** established `/history/session?id=<sessionId>`, `GET /api/sessions/{sessionId}`, the session-detail table, previous-value columns, and session-detail BEM block.
2. **Feature 016 (`016-workout-overall-effort`)** established nullable `OverallEffort`, the 1-10 effort validation rule, and the summary row below the detail table.
3. **Feature 023 (`023-unsaved-changes-warning`)** established original-value snapshots, `hasEditChanges()` checks, and the reusable discard-confirmation modal pattern.
4. **Feature 029 (`029-latest-exercise-data`)** established that historical comparisons should read from source session rows, so editing the saved session values is sufficient for future comparison correctness.
5. **Feature 030 (`030-edit-exercise-order`)** reinforced preserving exercise identity/order and using typed client-side state keyed by stable identifiers.

No `NEEDS CLARIFICATION` markers remain.

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Existing entities and editable-value state transitions
- [contracts/api-contract.md](./contracts/api-contract.md): New `PUT /api/sessions/{sessionId}` endpoint and web proxy behavior
- [contracts/ui-contract.md](./contracts/ui-contract.md): Session detail edit-mode states, accessibility, and unsaved-change behavior
- [quickstart.md](./quickstart.md): Implementation and verification workflow
- `.github/copilot-instructions.md` updated to reference this plan

## Post-Design Constitution Check

- **Code Quality** ✅ — Design modifies existing session-detail and session endpoint surfaces only, adds typed DTOs/interfaces, and reuses existing effort/discard patterns rather than adding parallel concepts.
- **Testing** ✅ — Backend and Playwright coverage is explicitly scoped for save, validation, cancel/discard, clearing optional values, and comparison refresh behavior.
- **Security** ✅ — New write endpoint validates ownership-by-containment in the single-user model, range/length checks all user-edited fields, and uses EF parameterized GUID lookups.
- **User Experience Consistency** ✅ — UI contract keeps the session detail layout, no-data marker, effort labels, button treatment, and discard modal language aligned with existing product patterns.
- **Performance** ✅ — Edit mode has no pre-edit network call; save is one bounded session update; dependent views update from changed source rows without extra cache work.

No constitution violations. Plan is ready for `/speckit.tasks`.

## Complexity Tracking

> No constitution violations or exceptions identified.
