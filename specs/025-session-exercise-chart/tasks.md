---

description: "Task list for 025-session-exercise-chart"
---

# Tasks: Session Exercise Chart

**Input**: Design documents from `/specs/025-session-exercise-chart/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅ | quickstart.md ✅

**Tests**: Automated tests are REQUIRED for every user story and every bug fix.
Include the appropriate unit, integration, contract, or end-to-end coverage
needed to prove behavior before implementation is complete.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story, with explicit work for security, user
experience consistency, and performance verification where applicable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Foundational — Backend, Utilities & Styling (Blocking Prerequisites)

**Purpose**: All three user story phases depend on the trends API endpoint, chart math helpers, and the CSS block. None of the chart section work can begin until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T001 Write 6 integration tests for `GET /api/workouts/{workoutId}/session-trends` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`: (a) `GetSessionTrends_Returns404_WhenWorkoutNotFound` — GET with a random GUID, assert 404 + `{ "error": "Workout not found." }`; (b) `GetSessionTrends_ReturnsEmptyArray_WhenNoSessions` — create a planned workout with no sessions, assert response is `[]`; (c) `GetSessionTrends_ReturnsSessionsInChronologicalOrder` — create the same planned workout's sessions on different dates (out of insertion order), assert the response array is ordered by `completedAt` ascending; (d) `GetSessionTrends_ReturnsCappedAt50Sessions` — create 55 sessions for the same planned workout, assert response array length is 50 and the 50 returned are the most-recent 50; (e) `GetSessionTrends_ReturnsCorrectSessionData` — create one session with one exercise (loggedWeight "80", effort 6, overallEffort 7), assert response contains `completedAt`, `overallEffort: 7`, and `exercises[0]` with `loggedWeight: "80"` and `effort: 6`; (f) `GetSessionTrends_ReturnsNullForUnrecordedFields` — create one session with `overallEffort: null` and one exercise with `loggedWeight: null` and `effort: null`, assert the response fields are null; (g) **`GetSessionTrends_LoggedWeightIsAlwaysSingleNumericStringOrNull`** — verify that `loggedWeight` values in the response are either null or parseable by `double.TryParse` — this asserts the invariant that weight is stored as a single numeric string (it is entered via `<input type="number">` in the UI, so compound strings like "80/85/90" are impossible). Tests must fail before T002 is implemented.

- [ ] T002 Add `GET /api/workouts/{workoutId:guid}/session-trends` endpoint to `src/WorkoutTracker.Api/Program.cs` (after the existing `GET /api/workouts/{workoutId}/previous-performance` handler): (1) check workout exists via `db.PlannedWorkouts.AnyAsync(pw => pw.PlannedWorkoutId == workoutId)` — return `Results.Json(new { error = "Workout not found." }, statusCode: 404)` if false; (2) query `db.WorkoutSessions.Where(ws => ws.PlannedWorkoutId == workoutId).OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt")).ThenByDescending(ws => ws.WorkoutSessionId).Take(50)` — **must use `EF.Property<DateTime>(ws, "CompletedAt")` (shadow property, no CLR counterpart)**; (3) project each session to `{ WorkoutSessionId, CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"), OverallEffort, Exercises = ws.LoggedExercises.OrderBy(le => le.Sequence).Select(le => new { le.ExerciseId, ExerciseName = le.Exercise.Name, le.LoggedWeight, le.Effort }).ToList() }`; (4) call `.ToListAsync()`, then `sessions.Reverse()` to convert to chronological order; (5) return `Results.Ok(sessions)`. **Note**: the currently-viewed session is intentionally included — it appears as the rightmost data point and shows the user where they stand now, consistent with `GET /api/workouts/{workoutId}/previous-performance` which does not filter out the current session. Run all tests from T001 — confirm they pass.

- [ ] T003 [P] Add proxy route to `src/WorkoutTracker.Web/Program.cs` following the existing try/catch + `WebProxyLog.ProxyError` pattern for `GET /api/workouts/{workoutId}/session-trends`: place it adjacent to the existing `GET /api/workouts/{workoutId}/previous-performance` proxy block. The handler signature is `(string workoutId, ILogger<Program> logger, IHttpClientFactory httpClientFactory)`. Run `cd src/WorkoutTracker.Web && npm run build` and `dotnet build src/WorkoutTracker.slnx` to confirm no compilation errors.

- [ ] T004 [P] Add chart math functions to `src/WorkoutTracker.Web/wwwroot/ts/utils.ts`: (a) `export function normaliseValue(value: number, min: number, max: number): number` — if `min === max`, return `120` (flat-line midpoint in the 20–220 plot area); otherwise return `220 - ((value - min) / (max - min)) * 200`; (b) `export function buildYTicks(min: number, max: number, tickCount: number): number[]` — returns `tickCount` evenly spaced values from `min` to `max` inclusive; when `tickCount < 2`, return `[min]`; (c) `export function buildXLabels(dates: readonly string[], maxLabels: number): (string | null)[]` — returns same-length array where labels are shown at evenly spaced indices up to `maxLabels` (always include last index); format each shown date as `"DD MMM"` using `toLocaleDateString("en-GB", { day: "2-digit", month: "short" })`; null for all non-shown positions. Strict TypeScript: no `any`, explicit return types.

- [ ] T005 [P] Write Vitest unit tests for the new chart math functions in `src/WorkoutTracker.Web/__tests__/utils.test.ts` (add to existing file): **normaliseValue** — (a) returns 120 when min === max; (b) returns 220 (bottom) when value === min; (c) returns 20 (top) when value === max; (d) returns correct midpoint for value between min and max; (e) handles negative min correctly. **buildYTicks** — (f) returns `[min, max]` for tickCount 2; (g) returns 6 evenly spaced ticks for effort axis (min 0, max 10, tickCount 6): `[0, 2, 4, 6, 8, 10]`; (h) handles tickCount < 2 by returning `[min]`. **buildXLabels** — (i) returns all non-null labels when dates.length ≤ maxLabels; (j) returns null for intermediate dates when dates.length > maxLabels; (k) always includes the last date as non-null; (l) returns empty array for empty input. Run `cd src/WorkoutTracker.Web && npm test` — all new tests must pass.

- [ ] T006 [P] Add `.session-chart__*` CSS block to `src/WorkoutTracker.Web/wwwroot/css/styles.css` (after the `.session-detail__*` block): `.session-chart` (margin-top `var(--spacing-lg)`, padding `var(--spacing-md)`, background `var(--color-white)`, border `1px solid var(--color-border)`, border-radius `var(--radius)`); `.session-chart__header` (display flex, align-items center, gap `var(--spacing-sm)`, margin-bottom `var(--spacing-md)`, flex-wrap wrap); `.session-chart__label` (font-size `var(--font-size-sm)`, color `var(--color-text-light)`, white-space nowrap); `.session-chart__select` (flex 1, min-width 0 — inherits existing `<select>` token styles via the existing `.workout-form__select` or equivalent selector; if no shared selector exists, add: border `1px solid var(--color-border)`, border-radius `var(--radius)`, padding `var(--spacing-xs)`, font-size `var(--font-size-sm)`, color `var(--color-text)`, background `var(--color-white)`); `.session-chart__loading`, `.session-chart__empty` (font-size `var(--font-size-sm)`, color `var(--color-text-light)`, padding `var(--spacing-md) 0`); `.session-chart__error` (font-size `var(--font-size-sm)`, color `var(--color-error)`, padding `var(--spacing-md) 0`); `.session-chart__container` (width 100%, overflow-x auto); `.session-chart__svg` (width 100%, height auto, display block); `.session-chart__axis-line` (stroke `var(--color-border)`, stroke-width 1); `.session-chart__gridline` (stroke `var(--color-border)`, stroke-width 1, opacity 0.5); `.session-chart__tick-label`, `.session-chart__date-label` (fill `var(--color-text-light)`, font-size 11px, font-family `var(--font-family)`); `.session-chart__line` (stroke `var(--color-primary)`, stroke-width 2, fill none); `.session-chart__point` (fill `var(--color-primary)`). No hardcoded colours or pixel values where tokens exist.

**Checkpoint**: `dotnet test src/WorkoutTracker.UnitTests` — 6 new `GetSessionTrends_*` tests pass, zero regressions. `cd src/WorkoutTracker.Web && npm test` — all new `normaliseValue`, `buildYTicks`, `buildXLabels` tests pass, zero regressions. `dotnet build src/WorkoutTracker.slnx` — no compile errors.

---

## Phase 2: User Story 1 — View Exercise Weight History (Priority: P1) 🎯 MVP

**Goal**: A user viewing a session detail page linked to a planned workout sees a chart section below the overall effort row. The default "Overall Session Effort" option is selected. Switching the dropdown to an exercise's "Weight" option shows a line chart of that exercise's logged weight across historical sessions.

**Independent Test**: Complete two sessions of the same planned workout — first session records "70" kg for Bench Press, second records "80" kg. Navigate to the second session's detail page. The chart section loads. Change the dropdown to "Bench Press – Weight". Verify a line chart appears with two data points, the first lower than the second.

### Tests for User Story 1

- [ ] T007 [P] [US1] Write E2E test `SessionDetailPage_ShowsChartSection_WhenWorkoutHasPreviousSession` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: (1) seed a planned workout + 2 sessions with one exercise (loggedWeight "70" then "80") via API; (2) navigate to History, click the most-recent session entry; (3) wait for `.session-detail` to be visible; (4) assert `.session-chart` element exists in DOM; (5) select the "Bench Press – Weight" option from `#session-chart-select`; (6) wait for `#session-chart-container` to be visible; (7) assert one `<polyline>` element exists inside `.session-chart__svg`. Test must fail before T008 is implemented.

- [ ] T008 [P] [US1] Write E2E test `SessionDetailPage_NoChartSection_WhenSessionHasNoPlannedWorkout` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: POST a session directly to `/api/sessions` (ad-hoc, no `workoutId`) — navigate to its detail page — assert `.session-chart` does not exist in DOM. Test must fail before T009 is implemented.

### Implementation for User Story 1

- [ ] T009 [US1] Add chart section rendering to `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (1) Add new interfaces `SessionTrendExercise` and `SessionTrendItem` per `data-model.md`. (2) After the session detail response resolves successfully, check `session.plannedWorkoutId` — if null, do not append chart section (return early from chart init). (3) Append chart section HTML inside `contentEl` after the overall effort row: use the DOM structure from `contracts/ui-contract.md` including `#session-chart-loading`, `#session-chart-error`, `#session-chart-container`, `#session-chart-empty`, `#session-chart-select`. Populate the `<select>` with option `value="overall-effort"` labelled "Overall Session Effort" first, then one pair per exercise from `session.exercises` ordered by their appearance in the response: `value="exercise:{exerciseId}:weight"` labelled `"{exerciseName} – Weight"` and `value="exercise:{exerciseId}:effort"` labelled `"{exerciseName} – Effort"`. Exercise names must be escaped via `escapeHtml()` before being set as option text content (use `option.textContent = ...` not `innerHTML`). (4) Start trends fetch eagerly: `fetch(\`/api/workouts/${encodeURIComponent(session.plannedWorkoutId)}/session-trends\`)` in a `Promise` stored in a local variable — do not `await` it yet. (5) Attach `change` event listener on `#session-chart-select`: when fired, resolve the trends Promise (if still pending, show loading state; when resolved, call `renderChartForSelection`). (6) Immediately after attaching the listener, call the same `renderChartForSelection` logic for the initial selection ("overall-effort") — so the first chart renders as soon as trends data arrives without requiring user interaction. (7) Implement `renderChartForSelection(selection: string, trends: SessionTrendItem[])`: parse `selection` — if `"overall-effort"`, collect points from `trends` where `overallEffort != null`; if `"exercise:{id}:weight"`, collect from `trends.exercises` where `exerciseId` matches and `Number(loggedWeight)` is not `NaN`; label x-axis with `buildXLabels`, y-axis: for effort fixed 0–10, for weight dynamic min/max (with degenerate-range handling: if all values equal, use `[value-1, value+1]`). If zero points: show empty state. Else: render SVG via `renderLineSvg(points, yMin, yMax, dateLabels)`. (8) Implement `renderLineSvg`: construct SVG string with viewBox `"0 0 600 260"`, y-axis grid using `buildYTicks` + `normaliseValue`, x-axis date labels using `buildXLabels`, `<polyline>` for the data line, `<circle r="4">` per point. All SVG text content set via `textContent` (not innerHTML injection). Set `aria-label` on `<svg>` to the selected series label. Set on `#session-chart-container` `aria-label` attribute likewise.

- [ ] T010 [US1] Handle trends fetch loading and error states in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (1) While the trends Promise is pending and the user selects an option, show `#session-chart-loading` and hide other chart states. (2) If the trends fetch returns non-200 or throws, show `#session-chart-error` with text "Could not load chart data." and hide other states. (3) On success, hide `#session-chart-loading` and proceed to render. Ensure all four states (loading, success, empty, error) are mutually exclusive — toggle `display:none` consistently. Add `role="alert"` and `aria-live="polite"` to `#session-chart-error` per `contracts/ui-contract.md`. Run `cd src/WorkoutTracker.Web && npm run build` — zero type errors.

**Checkpoint**: T007 E2E test passes (`SessionDetailPage_ShowsChartSection_WhenWorkoutHasPreviousSession`). T008 E2E test passes (`SessionDetailPage_NoChartSection_WhenSessionHasNoPlannedWorkout`). `npm run build` clean.

---

## Phase 3: User Story 2 — View Exercise Effort History (Priority: P2)

**Goal**: Selecting `"{exerciseName} – Effort"` from the dropdown renders a line chart of that exercise's effort scores over historical sessions. The Y-axis is fixed at 0–10.

**Independent Test**: Complete two sessions of the same planned workout — first records effort 5 for Bench Press, second records effort 8. Navigate to the second session's detail page. Change the dropdown to "Bench Press – Effort". Verify a line chart appears with two data points, the second higher than the first, and the Y-axis spans 0–10.

### Tests for User Story 2

- [ ] T011 [P] [US2] Write E2E test `SessionDetailPage_ChartDropdown_SwitchesSeries` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: (1) seed 2 sessions with one exercise having effort 5 then effort 8; (2) navigate to the second session's detail page; (3) confirm `#session-chart-select` contains options matching "– Weight" and "– Effort" for the exercise; (4) select the "– Effort" option; (5) wait for `#session-chart-container` to be visible; (6) assert one `<polyline>` is present in `.session-chart__svg`; (7) select the "– Weight" option; (8) wait for container to update; (9) assert `<polyline>` is still present (chart re-rendered for new series). Test must fail before T012 is implemented.

### Implementation for User Story 2

- [ ] T012 [US2] Verify `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` handles exercise effort series correctly — this is a verification task because the series rendering logic built in T009 already handles `"exercise:{id}:effort"` selections: (a) confirm `collection` logic filters `trends` entries where `effort != null` for the matching `exerciseId`; (b) confirm Y-axis uses fixed range `[0, 10]` for effort series (not dynamic); (c) confirm `buildYTicks(0, 10, 6)` is called for effort, producing ticks `[0, 2, 4, 6, 8, 10]`; (d) confirm `normaliseValue` is called with `min=0, max=10` for each point. If any of these conditions are not met, fix them in `session-detail.ts`. Run T011 E2E test to confirm pass.

**Checkpoint**: T011 E2E test passes (`SessionDetailPage_ChartDropdown_SwitchesSeries`). All US1 E2E tests continue to pass.

---

## Phase 4: User Story 4 — Switch Between Chart Data (Priority: P2)

**Goal**: The dropdown lists all available series. Switching selection updates the chart without a page reload. The `aria-label` on the chart container reflects the active series name.

**Independent Test**: On a session detail page with chart data, select one option, confirm chart renders. Select a different option, confirm chart updates to the new series. Inspect the DOM — `#session-chart-container[aria-label]` contains the currently selected series name.

### Tests for User Story 4

- [ ] T013 [P] [US4] Write E2E test `SessionDetailPage_Chart_ShowsEmptyState_WhenNoHistoricalData` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: (1) seed a planned workout with exactly one session (no prior sessions); (2) navigate to that session's detail page; (3) assert `.session-chart` is present; (4) wait for `#session-chart-loading` to disappear; (5) assert `#session-chart-empty` is visible and contains "No data available for this selection." (the single session produces data points, so test "Overall Session Effort" when `overallEffort` was not recorded — create the session without overall effort to trigger the empty state). Test must fail before T014 is verified.

### Implementation for User Story 4

- [ ] T014 [US4] Verify dropdown completeness and aria updates in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (a) confirm the `<select>` renders exactly `1 + (exercises.length * 2)` options (one "Overall Session Effort" + two per exercise); (b) confirm exercises appear in the same order as `session.exercises` (i.e., `sequence` order from the API); (c) confirm that after `renderChartForSelection` succeeds, `document.getElementById("session-chart-container")?.setAttribute("aria-label", selectedLabel)` is called with the human-readable series label (e.g., "Bench Press – Weight over time"); (d) confirm the `<select>` is not disabled during the trends fetch — user can pre-select before data arrives, and the selection is honoured once data resolves. Fix any issues found in `session-detail.ts`. Run T013 E2E test to confirm pass.

**Checkpoint**: T013 E2E test passes. All previous E2E tests continue to pass.

---

## Phase 5: User Story 3 — View Overall Session Effort History (Priority: P3)

**Goal**: The "Overall Session Effort" option (default selection) renders a line chart of overall session effort scores across historical sessions of the same workout. The Y-axis is fixed at 0–10.

**Independent Test**: Complete two sessions of the same planned workout with overall efforts 6 and 8. Navigate to either session's detail page. The chart section loads and automatically renders the "Overall Session Effort" line with two data points.

### Tests for User Story 3

- [ ] T015 [P] [US3] Write E2E test `SessionDetailPage_Chart_OverallEffortSeriesRendersOnLoad` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: (1) seed 2 sessions with `overallEffort` 6 and 8; (2) navigate to the second session's detail page; (3) wait for `#session-chart-loading` to disappear; (4) assert `#session-chart-container` is visible (default "Overall Session Effort" selected); (5) assert one `<polyline>` inside `.session-chart__svg`. Test must fail before T016 is confirmed.

### Implementation for User Story 3

- [ ] T016 [US3] Verify `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` handles the overall session effort series correctly — this is a verification task because the series was already wired in T009: (a) confirm `"overall-effort"` is the default selected value in the `<select>` on initial render; (b) confirm the initial `renderChartForSelection("overall-effort", trends)` call fires automatically once trends data resolves, without user interaction; (c) confirm `overallEffort` series uses fixed Y-axis `[0, 10]`; (d) confirm sessions where `overallEffort === null` are excluded from the series (not plotted as zero). If any issue found, fix in `session-detail.ts`. Run T015 E2E test to confirm pass.

**Checkpoint**: T015 E2E test passes. All previous user story tests continue to pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Build validation, type safety, UX consistency, security, accessibility, and full regression.

- [ ] T017 Build TypeScript and run all frontend tests: `cd src/WorkoutTracker.Web && npm run build` — confirm zero strict-mode type errors in `session-detail.ts` and `utils.ts`; `npm test` — confirm all Vitest tests pass including new chart math tests and all existing tests (router, utils, theme, prestart-modal).

- [ ] T018 [P] Run full backend integration test suite: `dotnet test src/WorkoutTracker.UnitTests` — confirm all 6 new `GetSessionTrends_*` tests pass; confirm zero regressions in existing session, workout, exercise, and muscle tests.

- [ ] T019 [P] Run full Playwright E2E test suite: `dotnet test src/WorkoutTracker.E2ETests` — confirm all 4 new chart tests pass: `SessionDetailPage_ShowsChartSection_WhenWorkoutHasPreviousSession`, `SessionDetailPage_NoChartSection_WhenSessionHasNoPlannedWorkout`, `SessionDetailPage_ChartDropdown_SwitchesSeries`, `SessionDetailPage_Chart_ShowsEmptyState_WhenNoHistoricalData`, `SessionDetailPage_Chart_OverallEffortSeriesRendersOnLoad`; confirm all existing session detail, history, and workout E2E tests pass without modification.

- [ ] T020 UX consistency and performance review in `src/WorkoutTracker.Web/wwwroot/css/styles.css` and `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (a) confirm all `.session-chart__*` CSS rules use only existing CSS custom properties (no hardcoded hex colours or pixel values where tokens exist); (b) confirm the chart section visually matches the card style of the session detail table (same background, border, border-radius tokens); (c) confirm the dropdown label text "Show history for" is consistent with copy used in other selector UI in the app; (d) confirm the empty state message "No data available for this selection." uses `--color-text-light` consistent with other empty states in the app; (e) **SC-001 / PR-001 performance validation** — open a session detail page with prior sessions in the browser, measure the time from navigation-complete to chart section being interactive (dropdown responsive + initial series rendered): this MUST be under 2 seconds. Record the observed time in a code comment near the trends fetch in `session-detail.ts` (e.g. `// Performance observed: ~Xms initial chart render (SC-001: must be <2s)`); if the budget cannot be met, record a mitigation plan before merging; (f) verify chart section appears below the overall effort row on viewport widths of **320px**, 375px, and 1280px using browser dev tools — all three widths must render without horizontal overflow or text truncation that impairs usability.

- [ ] T021 Security review in `src/WorkoutTracker.Api/Program.cs`, `src/WorkoutTracker.Web/Program.cs`, and `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (a) confirm `workoutId` uses `:guid` route constraint on both API and proxy endpoints — non-GUID inputs are rejected before the handler runs; (b) confirm EF Core query uses parameterised `Where(ws => ws.PlannedWorkoutId == workoutId)` — no string interpolation; (c) confirm exercise names in dropdown `<option>` elements are set via `option.textContent = escapeHtml(name)` (not `option.innerHTML`); (d) confirm SVG `<text>` element content (tick labels, date labels) is assigned via `textContent` (not innerHTML) for all user-data-derived strings; (e) add comment to the new API endpoint: `// Single-user app: cross-user access not possible; no auth layer (consistent with features 008, 013, 014, 016)`.

- [ ] T022 [P] Accessibility verification in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (a) confirm `<svg role="img" aria-label="...">` is present on the rendered SVG; (b) confirm `<label for="session-chart-select">` is correctly associated with `<select id="session-chart-select">`; (c) confirm `#session-chart-error` has `role="alert"` and `aria-live="polite"`; (d) confirm the SVG `aria-label` is updated on each series switch to reflect the new series name; (e) confirm no interactive elements inside the chart section are keyboard-inaccessible.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Foundational): No dependencies — start immediately
Phase 2 (US1):          Depends on Phase 1 completion — BLOCKS on T002, T004, T006
Phase 3 (US2):          Depends on Phase 2 completion — effort series built on top of US1 chart infrastructure
Phase 4 (US4):          Depends on Phase 3 completion — dropdown switching requires ≥2 working series
Phase 5 (US3):          Depends on Phase 1 completion — can begin after Foundational; does not depend on US2
Phase 6 (Polish):       Depends on all user story phases completing
```

### Within Each Phase

- Integration tests (T001) → endpoint implementation (T002) → proxy (T003)
- Chart math tests (T005) → chart math functions (T004) are parallel: write tests and functions simultaneously (TDD-style: tests validate, functions implement)
- CSS (T006) is independent of T001–T005 and can be written in parallel with any foundational task

### Parallel Opportunities

- T003 (web proxy), T004 (chart math functions), T005 (chart math tests), T006 (CSS) are all fully independent of each other and of T001/T002 — they can be written simultaneously once T002 is underway
- T007 and T008 (US1 E2E tests) can be written in parallel while T009–T010 (US1 implementation) are being worked
- T015 (US3 E2E test) can be written and will fail long before T016 is verified — write it early
- T017, T018, T019, T021, T022 (Polish) can all run in parallel once implementation is complete

---

## Parallel Example: Foundational Phase

```
Simultaneously:
  T001: Write GetSessionTrends_* integration tests
  T004: Add normaliseValue/buildYTicks/buildXLabels to utils.ts
  T005: Write Vitest tests for chart math functions
  T006: Add .session-chart__* CSS block

Then sequentially:
  T002: Implement GET /api/workouts/{workoutId}/session-trends (confirm T001 passes)
  T003: Add web proxy (independent, can overlap with T002)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Foundational (T001–T006)
2. Complete Phase 2: User Story 1 (T007–T010)
3. **STOP and VALIDATE**: Navigate to a session detail page with prior sessions — confirm chart section, weight series, empty state, loading/error states all work
4. Demo/review before continuing to US2–US4

### Incremental Delivery

1. Foundation → MVP chart with weight series (US1) → Add effort series (US2 + US4) → Add overall effort default (US3) → Polish
2. Each phase adds a working increment without breaking previous behaviour

---

## Notes

- [P] tasks = different files, no shared state dependencies — safe to run in parallel
- `CompletedAt` is a shadow property: **always** use `EF.Property<DateTime>(ws, "CompletedAt")` — never `ws.CompletedAt`
- `Number()` not `parseFloat()` for weight strings — `Number("60abc")` returns `NaN`; `parseFloat("60abc")` silently returns `60`
- `loggedWeight` is always a single numeric string or null — entered via `<input type="number" step="0.5">` in the active session UI; compound strings are impossible; `NaN` from `Number()` means genuinely missing data, not a multi-set format
- Effort Y-axis is **always fixed 0–10**; weight Y-axis is **always dynamic**
- Degenerate range (all values equal): use `[value - 1, value + 1]` before calling `normaliseValue` to avoid division-by-zero
- No chart section rendered when `session.plannedWorkoutId === null` (ad-hoc sessions)
- Trends fetch is eager (starts immediately when `plannedWorkoutId` is known) but rendering waits for user/auto selection
- The **currently-viewed session is intentionally included** as the rightmost chart data point — shows the user where they stand now, consistent with `previous-performance` endpoint behaviour
