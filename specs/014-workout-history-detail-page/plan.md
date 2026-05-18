# Implementation Plan: Workout History Detail Page

**Branch**: `014-workout-history-detail-page` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/014-workout-history-detail-page/spec.md`

## Summary

Replace the inline expand-collapse behaviour on the History page with navigation to a dedicated session detail page. Clicking a history entry navigates to `/history/session?id=<sessionId>`. The detail page renders a five-column table (Exercise, Weight, Previous Weight, Effort, Previous Effort) where the "previous" columns are sourced from the most recent prior completed session of the same planned workout. This requires one new backend endpoint (`GET /api/sessions/{sessionId}`), a new proxy route in the web layer, a new `session-detail.ts` frontend page, CSS for the detail table, a new route registration in `main.ts`, and updates to two existing E2E tests that tested the old expand/collapse interaction.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
**Storage**: PostgreSQL via EF Core — no schema changes; all required data is in the existing `workout_session` and `logged_exercise` tables
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests; Playwright E2E tests
**Target Platform**: Web browser (mobile-first, responsive)
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: Detail page loads within the standard page transition time; session exercises and previous-session data fetched in a single API call — no sequential blocking
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass (or be replaced where behaviour deliberately changes); router uses exact path matching — session ID passed via `?id=` query param, consistent with `active-session?id=` pattern
**Scale/Scope**: Changes touch 6 files (`Program.cs` API, `Program.cs` Web, `history.ts`, `session-detail.ts` new, `styles.css`, `main.ts`) + E2E test file + backend test additions; no migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. All new interfaces explicitly typed — no `any`. CSS follows BEM naming: new page uses `session-detail__*` block. C# uses `<Nullable>enable</Nullable>` and snake_case DB naming via `UseSnakeCaseNamingConvention()`. New endpoint projection uses the existing anonymous-type inline pattern. No speculative abstractions or dead code. ✅

- **Testing**:
  - **Backend integration tests** (xUnit / `SessionApiTests.cs`): New tests for `GET /api/sessions/{sessionId}`: (a) returns 404 when session does not exist; (b) returns correct session name, date, and exercise data; (c) returns empty exercises array when session has no logged exercises; (d) returns previous weight and effort per exercise when a prior session exists for the same planned workout; (e) returns `null` for previous weight/effort when no prior session exists; (f) returns `null` for previous fields when prior session exists but did not include that exercise.
  - **E2E (Playwright / `WorkoutHistoryTests.cs`)**: `HistoryPage_SessionExpandCollapse` and `HistoryPage_SessionDetails_ShowsExerciseData` test the old expand behaviour — both MUST be replaced by: `HistoryPage_SessionEntry_NavigatesToDetailPage` and `SessionDetailPage_ShowsExerciseTable`. Add `SessionDetailPage_BackNavigation_ReturnsToHistory` and `SessionDetailPage_ShowsPreviousData_WhenPriorSessionExists`.
  - **Frontend Vitest**: No new Vitest tests — the session-detail page rendering follows the established DOM-rendering pattern (features 004, 005, 008, 012). Existing router tests must continue to pass.
  - Tests treated as mandatory, not optional. ✅

- **Security**: The new `GET /api/sessions/{sessionId}` endpoint uses a GUID session ID — EF Core parameterises it, preventing SQL injection. No user-supplied string input is introduced. SR-001 (cross-user) exception documented identically to features 008 and 013 — single-user app, no auth layer. Escaping via `escapeHtml()` must be applied to all exercise names, weights, and effort labels rendered in the table. ✅

- **User Experience Consistency**: Session detail table uses `--color-white` card background, `--color-border`, `--color-text`, `--color-text-light` — same tokens used across all card/table components. "Previous" column values use `--color-text-light` to distinguish read-only comparison data from the current session's values, consistent with the `active-session__exercise-previous` pattern from feature 008. Back navigation uses the existing `← Back` link pattern. All four states (loading, empty, success, error) are defined. ✅

- **Performance**: The new endpoint retrieves session + prior session exercises in a single bounded EF Core query — two `FirstOrDefaultAsync` calls within one handler, no full-table scans, no N+1. No new frontend network round-trips beyond the single `GET /api/sessions/{sessionId}` call. ✅

## Project Structure

### Documentation (this feature)

```text
specs/014-workout-history-detail-page/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # New GET /api/sessions/{sessionId} endpoint
│   └── ui-contract.md   # Session detail page HTML/CSS/ARIA contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                              # MODIFIED: add GET /api/sessions/{sessionId}

src/WorkoutTracker.Web/
└── Program.cs                              # MODIFIED: add proxy GET /api/sessions/{sessionId}
└── wwwroot/
    ├── css/
    │   └── styles.css                      # MODIFIED: add .session-detail__* table styles
    └── ts/
        ├── main.ts                         # MODIFIED: import + register /history/session route
        └── pages/
            ├── history.ts                  # MODIFIED: replace expand logic with navigate()
            └── session-detail.ts           # NEW: renders session detail page with 5-column table

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                  # MODIFIED: add GET /api/sessions/{sessionId} tests

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs              # MODIFIED: replace expand/collapse tests;
                                            #           add detail page navigation + table tests
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects. The session detail page is added as a new TypeScript page module (`session-detail.ts`) consistent with all other pages in `wwwroot/ts/pages/`. Route registration follows the exact-path pattern in `main.ts`. Session ID is passed as `?id=<sessionId>` query param, consistent with the `active-session?id=` pattern established in feature 004.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts are complete.*

- **Code Quality** ✅ — One new page module added, two existing files surgically modified (history.ts and main.ts), one C# endpoint added inline. No speculative abstractions, no dead code, no `any`.
- **Testing** ✅ — Backend: six new integration tests. E2E: two old tests replaced by four new tests. Vitest: unchanged per established pattern.
- **Security** ✅ — GUID parameter, EF parameterisation, `escapeHtml()` applied throughout, single-user exception documented.
- **User Experience Consistency** ✅ — BEM naming, existing CSS tokens, all four states defined, back navigation matches existing pattern.
- **Performance** ✅ — Single endpoint, two bounded queries, no N+1, no new round-trips.

No violations. Plan is ready for `/speckit.tasks`.
