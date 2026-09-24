# Implementation Plan: Workout Sets Selection

**Branch**: `032-workout-sets-selection` | **Date**: 2026-09-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/032-workout-sets-selection/spec.md`

## Summary

Add an explicit 3-or-5 Sets selection below the workout selector before an active workout begins. Carry the selected session-wide value through the existing active-session query-string flow and persist it as a nullable, constrained `WorkoutSession.Sets` column when the session is saved. Extend the current previous-performance and session-detail contracts so active workouts show `3 sets` or `5 sets` in `Last time`, and historical workout tables show current and previous Sets columns. Historical edit mode updates one synchronized session-level value. The history detail stats graph also shows each session's sets as background bars sized by set count, with no sets axis, for both the overall-effort and per-exercise selections. Existing routes, proxy behavior, UI classes, latest-usable historical selection, and legacy-null rendering are reused.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~7.0.2 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript, Playwright, Vitest
**Storage**: PostgreSQL via EF Core — add one nullable integer `sets` column and `NULL/3/5` check constraint to `workout_sessions`
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); dedicated selector unit tests; Playwright E2E tests; Vitest where frontend helper logic is extracted
**Target Platform**: Web browser (mobile-first responsive UI)
**Project Type**: Web application (SPA-style frontend served by ASP.NET Core / .NET Aspire orchestration)
**Performance Goals**: Automated request-count, query-count, selector-bound, and local latency budgets passed; the manual 20-warm-iteration comparative p95 baseline remains a pre-release follow-up because this branch has no separate pre-feature baseline commit. See **Performance Budgets & Measurement Design**.
**Constraints**: No external JS/CSS frameworks; strict TypeScript; values restricted to 3 or 5 for new sessions; legacy rows remain nullable; one sets value per session despite repeated table and per-data-point chart display; preserve existing random-order query flow, BEM classes, no-data marker, edit/discard behavior, hand-rolled SVG charting, and latest-usable historical comparison semantics
**Scale/Scope**: One model/configuration/migration, existing API session and session-trends routes and selector records, start/active/detail TypeScript pages, limited responsive and chart-series CSS, API/unit/E2E tests; no new project or endpoint

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Add one typed nullable property to the established session model and extend existing DTO/projection records rather than introducing parallel routes or per-exercise persistence. TypeScript remains strict with explicit `3 | 5`-style validation/state, no `any`, and existing page-module ownership. Current form, previous-data, table, and edit patterns are reused. ✅
- **Testing**: Add API integration tests for create validation/persistence, nullable legacy reads, previous-performance source-session association, session-detail current/previous values, omitted-versus-null historical updates, and invalid values. Add dedicated selector unit coverage for source-session Sets propagation, ordering ties, legacy nulls, and preservation of older weight/effort fallback when a newer row contains Sets only. Add Playwright journeys for required start selection, query handoff/save, active-session `Last time`, history columns, synchronized editing, and legacy no-data rendering. Existing random-order, save, history, and edit tests remain regression gates. ✅
- **Security**: Treat query parameters and JSON fields as untrusted. Validate 3 or 5 in the start UI, active-session parser, create endpoint, update endpoint, and database constraint. Existing GUID route handling, EF parameterization, single-user authorization assumptions, proxy boundaries, and generic error responses remain unchanged. No secrets or third-party integrations are introduced. ✅
- **User Experience Consistency**: Reuse `workout-form` select/error styles, active-session `Last time` separators/classes, session-detail table/no-data/edit controls, and discard modal. Copy is consistently `Sets`, `3 sets`, `5 sets`, and `Prev. Sets`. Loading, empty, validation, error, success, and legacy states are specified. ✅
- **Performance**: Sets travels in existing requests/responses and one additional database column. Historical queries project sets with already-loaded session rows, the selector remains bounded to 200 sessions, and no N+1 query or new fetch is added. Budgets are tiered into CI-blocking request/query-count gates, relaxed absolute latency ceilings, and a pre-release comparative p95 procedure. ✅

## Project Structure

### Documentation (this feature)

```text
specs/032-workout-sets-selection/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── api-contract.md
│   └── ui-contract.md
└── tasks.md             # Executed task list and delivery record
```

### Source Code (repository root)

```text
src/WorkoutTracker.Infrastructure/
├── Data/
│   ├── Models/
│   │   └── WorkoutSession.cs                       # MODIFIED: nullable Sets property
│   ├── WorkoutTrackerDbContext.cs                  # MODIFIED: NULL/3/5 constraint
│   └── Migrations/
│       └── [timestamp]_AddSetsToWorkoutSession.cs  # NEW: column and constraint

src/WorkoutTracker.Api/
├── WorkoutTracker.Api.csproj                       # MODIFIED: InternalsVisibleTo WorkoutTracker.UnitTests
└── Program.cs                                      # MODIFIED: create/update validation,
                                                    # projections, comparison records/selector

src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                              # MODIFIED for detail-table responsiveness and
    │                                                # the Sets chart series colour/legend
    └── ts/
        └── pages/
            ├── home.ts                             # MODIFIED: Sets select/validation/query param
            ├── active-session.ts                   # MODIFIED: parse/save sets and render Last time
            └── session-detail.ts                   # MODIFIED: Sets/Prev. Sets view, synchronized edit,
                                                    # and the stats-graph Sets series

src/WorkoutTracker.UnitTests/
└── Api/
    ├── SessionApiTests.cs                          # MODIFIED: session API integration coverage
    └── PreviousExerciseDataSelectorTests.cs        # NEW: comparison-selection unit coverage

src/WorkoutTracker.E2ETests/
├── Infrastructure/
│   └── WebAppFixture.cs                            # MODIFIED: mocks include sets fields; adds the
│                                                    # missing session-trends mock route
└── E2E/
    ├── WorkoutHistoryTests.cs                      # MODIFIED: start, active, history/edit and
    │                                                # stats-graph Sets-series journeys
    └── HomeLandingPagePerformanceTests.cs          # MODIFIED: request-count and latency budgets
```

**Structure Decision**: Preserve the existing .NET Aspire solution and co-located vanilla TypeScript page modules. This follows plans 016, 029, and 031: feature 016 established a nullable constrained session-level value and migration; feature 029 established bounded per-exercise latest-usable comparisons; feature 031 established session-detail editing and the top-level session update payload. No new route, frontend module, or storage abstraction is needed.

## Phase 0: Research Outcomes

Research is captured in [research.md](./research.md).

Key decisions reused from previous specs:

1. **Feature 016 (`016-workout-overall-effort`)** establishes the model/configuration/migration/API pattern for a nullable session-level integer and backward-compatible historical rows.
2. **Feature 029 (`029-latest-exercise-data`)** establishes bounded newest-first history scans and per-exercise weight-or-effort comparison selection; Sets travels with the chosen source session without changing the existing usability predicate.
3. **Feature 031 (`031-edit-past-workouts`)** establishes the session detail update endpoint, typed edit snapshot, save/error behavior, and discard-warning flow; Sets extends the top-level update state.
4. The existing `home.ts` → `active-session.ts` `id`/`order` query pattern carries Sets without an extra request or premature session creation.
5. Session-level persistence plus synchronized row controls maintains one source of truth while meeting the requested table presentation.

No `NEEDS CLARIFICATION` markers remain.

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Session-level Sets property, constraint, migration, invariants, validation, and state transitions
- [contracts/api-contract.md](./contracts/api-contract.md): Additive session create/read/update and historical-comparison contract changes
- [contracts/ui-contract.md](./contracts/ui-contract.md): Start selector, active `Last time`, history columns, synchronized edit, states, and accessibility
- [quickstart.md](./quickstart.md): Implementation order and verification commands
- `.github/copilot-instructions.md` updated to reference this plan

## Performance Budgets & Measurement Design

### Why this section exists

`HomeLandingPagePerformanceTests.cs` currently asserts a single relaxed absolute ceiling and explicitly documents that it is a regression smoke check rather than a spec validation. The repository has no baseline-capture mechanism, so a p95-versus-baseline comparison cannot be a CI assertion. Budgets are therefore split into deterministic automated gates and a documented comparative procedure.

### Tier 1 — Deterministic automated gates (CI-blocking)

These are environment-independent and MUST be asserted in tests.

| Budget ID | Metric | Target | Where asserted |
|---|---|---|---|
| PB-01 | Web requests issued while loading the start page | Unchanged from baseline count; Sets adds 0 | `HomeLandingPagePerformanceTests.cs` |
| PB-02 | Web requests issued from start-page submit to active-session interactive | Unchanged; Sets adds 0 | `HomeLandingPagePerformanceTests.cs` |
| PB-03 | Web requests issued while loading session detail | Unchanged; Sets adds 0 | `WorkoutHistoryTests.cs` |
| PB-04 | Database round trips in `previous-performance` | Exactly 2 (workout-exercise ids, bounded session scan); no per-exercise query | `SessionApiTests.cs` |
| PB-05 | Database round trips in session detail | Exactly 2 (session projection, bounded prior-session scan) | `SessionApiTests.cs` |
| PB-06 | Historical sessions scanned | Capped at the existing `MaxSessionsToScan` value of 200 | `PreviousExerciseDataSelectorTests.cs` |
| PB-07 | Sets values transported per session | Exactly 1 top-level field; 0 per logged exercise | `SessionApiTests.cs` |
| PB-11 | Sets values transported per `session-trends` data point | Exactly 1 top-level field; 0 per exercise | `SessionApiTests.cs` |

PB-01 through PB-03 are implemented with Playwright's `page.Request` counter, following the existing `HomePage_NoExternalNetworkRequests` pattern. PB-04 and PB-05 are asserted with an EF Core command interceptor or logged-command counter registered in the test host.

### Tier 2 — Absolute latency ceilings (CI smoke)

These extend the existing relaxed-ceiling convention and catch gross regressions only.

| Budget ID | Flow | Ceiling | Notes |
|---|---|---|---|
| PB-08 | Start page DOM-content-loaded | < 5000 ms | Existing ceiling retained unchanged |
| PB-09 | Session detail interactive, 25 exercises | < 5000 ms | New assertion, same relaxed convention |
| PB-10 | Session create request, 25 exercises | < 2000 ms | New assertion against the local test host |

These ceilings are deliberately loose. They are regression tripwires, not the spec target.

### Tier 3 — Comparative baseline procedure (pre-release, manual; not run for PR #159)

This is how PR-001 and SC-006 are actually satisfied. It is run once before release and recorded as evidence, not asserted in CI.

1. Check out the pre-feature commit and build in `Release`.
2. Seed a workout with 25 exercises and 10 completed historical sessions.
3. Discard 5 warm-up iterations, then record 20 timed iterations of workout start and of session-detail load.
4. Compute p95 for each flow; this is the baseline.
5. Repeat steps 2-4 on the feature branch in the same environment and session.
6. Pass condition: feature p95 minus baseline p95 must not exceed the greater of 10% of baseline p95 or 100 ms, for both flows.
7. Record both p95 values, the delta, the machine, and the commit SHAs in the pull request description.

Same-environment, same-session execution is mandatory because absolute timings on a developer machine are not comparable across runs.

## Selector Test Design

### Access constraint

`PreviousExerciseDataSelector`, `HistoricalSessionData`, `HistoricalExerciseData`, and `LatestExerciseComparison` are `internal` in `WorkoutTracker.Api`, and no `InternalsVisibleTo` attribute exists today. Unit-testing the selector directly therefore requires adding `InternalsVisibleTo("WorkoutTracker.UnitTests")` to `WorkoutTracker.Api`. This is preferred over widening the types to `public`, which would expose internal projection records as API surface for no product reason.

### Test placement and shape

The new file is `src/WorkoutTracker.UnitTests/Api/PreviousExerciseDataSelectorTests.cs` in namespace `WorkoutTracker.UnitTests.Api`, matching the existing test layout. Unlike `SessionApiTests`, it takes no `[Collection("Api")]` attribute and no fixture: the selector is a pure in-memory function, so these tests need no PostgreSQL instance, run in parallel, and stay fast.

### Test matrix

Each case constructs `HistoricalSessionData` values newest-first and asserts on the returned `Dictionary<Guid, LatestExerciseComparison>`.

| Case | Setup | Assertion |
|---|---|---|
| SelectsSetsFromSourceSession | Newest session has usable weight and Sets 5 | Comparison carries Sets 5 and that session's `CompletedAt` |
| PropagatesSetsFromFallbackSession | Newest row unusable; older usable row has Sets 3 | Comparison carries Sets 3 from the older session, not the newer |
| SetsOnlyRowDoesNotSuppressOlderData | Newest row has Sets 5 but null weight and null effort; older row has usable weight and Sets 3 | Comparison resolves to the older row with weight and Sets 3; proves the I2 regression is absent |
| ReturnsNullSetsForLegacySession | Selected usable session has null Sets | Comparison carries null Sets without error |
| SelectsIndependentlyPerExercise | Exercise A usable in newest session, exercise B only in an older session with different Sets | Each exercise carries the Sets of its own source session |
| IgnoresExercisesOutsideTargetSet | History contains an exercise id absent from the target set | That exercise is not present in the result |
| BreaksTiesByDeterministicOrder | Two sessions share `CompletedAt`, differing Sets | Caller-supplied newest-first order is respected without exception |
| StopsAtMaxSessionsToScan | More than `MaxSessionsToScan` sessions supplied; only the oldest has usable data | Selector honours the bound and does not scan unbounded history |
| ReturnsEmptyForNoUsableHistory | All rows have null weight and null effort | Result is empty, preserving the first-session state |

### Predicate coverage

`HasUsableComparisonData` is asserted directly for the weight-only, effort-only, both-present, whitespace-weight, and both-absent cases, pinning the invariant that Sets is deliberately excluded from usability.

## Post-Design Constitution Check

- **Code Quality** ✅ — Design extends existing session storage, endpoints, selector records, page modules, and typed edit state. It avoids duplicated per-exercise persistence and new abstractions.
- **Testing** ✅ — API, dedicated selector-unit, and Playwright coverage explicitly proves allowed values, legacy nulls, source-session comparison consistency, older weight/effort fallback preservation, omitted-versus-null updates, required start selection, and synchronized historical editing. The selector test matrix and its `InternalsVisibleTo` prerequisite are specified in **Selector Test Design**.
- **Security** ✅ — Client and server validate untrusted values, the database enforces the invariant, and no authorization/trust boundary changes are introduced.
- **User Experience Consistency** ✅ — Existing form, summary, table, chart, no-data, error, and discard interactions are preserved; all new copy and states are defined. The Sets bars reuse the established legend classes and differ from the weight and effort lines in shape as well as colour, so they do not rely on colour alone.
- **Performance** ✅ — All data is added to existing bounded queries and payloads with no new round trips or N+1 access. The chart Sets series reuses the single existing trends response and adds no request when the selection changes. PB-01 to PB-07 were verified by automated tests and PB-08 to PB-10 passed as local regression tripwires. The Tier 3 comparative p95 procedure remains explicitly unverified and is documented as a pre-release follow-up.

No constitution violations. Implementation is complete for PR #159; only the optional pre-release comparative baseline remains.

## Extension: Sets on the history detail stats graph

Added after PR #159 to satisfy User Story 4 and FR-012 to FR-017.

### Design

- `GET /api/workouts/{workoutId}/session-trends` projects the already-loaded session-level `Sets` value into each data point. No new query, join, or endpoint.
- `session-detail.ts` renders `Sets` bars in both chart renderers using shared geometry constants (`CHART_VIEW_WIDTH`, `CHART_PLOT_LEFT`, `CHART_PLOT_RIGHT`, `CHART_BASELINE_Y`, `CHART_SETS_BAR_MAX_WIDTH`) and a shared `buildSetsBars` helper, so the two renderers cannot drift.
- No sets axis is drawn. `computeSetsBarMax` in `utils.ts` returns the value a full-height bar represents: a fixed baseline of `6`, raised only when a session recorded more, so identical counts always render at identical heights and no bar overflows the plot area. Bars are drawn first in the SVG so the weight and effort lines stay readable on top, and the chart keeps its original `0 0 600 260` `viewBox` and `x=50`-`x=580` plot area.
- `buildSetsBars` returns an empty string when every data point has a null sets value, which removes the bars and the legend entry together; individual null values simply render no bar.
- `styles.css` adds `--color-chart-sets` (with a dark-theme override) plus `.session-chart__bar--sets` and `.session-chart__legend-swatch--sets`. The bars are filled at reduced opacity so the lines plotted over them stay legible.
- Changing the chart selection issues no request: the change handler re-renders from the already-fetched in-memory `currentTrends` value, so PB-01 to PB-03 are unaffected.

### Test-infrastructure prerequisite

`WebAppFixture.cs` had no `session-trends` mock route, so the chart silently fell back to single-session data in every E2E test and multi-session chart behavior was untested. The extension adds the mock route, which is required for any assertion about a plotted line rather than a lone point.

### Verification

Frontend tests 96 passed; backend tests 168 passed; Playwright E2E tests 290 passed; TypeScript build passed.

## Complexity Tracking

> No constitution violations or exceptions identified. The implementation follows this plan; the only deferred item is the manual comparative p95 baseline.
