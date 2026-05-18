# Tasks: Workout History Detail Page

**Input**: Design documents from `/specs/014-workout-history-detail-page/`
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

## Phase 1: Foundational — New API Endpoint (Blocking Prerequisite)

**Purpose**: Add the `GET /api/sessions/{sessionId}` endpoint and its web proxy — the single data source that all three user stories depend on. No frontend work can meaningfully proceed until this endpoint exists and its integration tests pass.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T001 Write 6 integration tests for `GET /api/sessions/{sessionId}` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`: (a) `GetSessionDetail_Returns404_WhenSessionNotFound` — POST to non-existent GUID, assert 404 + `{ "error": "Session not found." }`; (b) `GetSessionDetail_ReturnsSessionData_WithExercises` — create workout + session with one exercise (weight + effort), GET by sessionId, assert workoutName, completedAt, exerciseName, loggedWeight, effort; (c) `GetSessionDetail_ReturnsEmptyExercises_WhenNoneLogged` — create session with no loggedExercises, assert exercises is `[]`; (d) `GetSessionDetail_ReturnsPreviousData_WhenPriorSessionExists` — complete same planned workout twice with different weights/efforts, GET second sessionId, assert previousWeight and previousEffort match first session's values; (e) `GetSessionDetail_ReturnsNullPreviousData_WhenNoPriorSession` — GET first (only) session, assert previousWeight and previousEffort are both `null` for every exercise row; (f) `GetSessionDetail_ReturnsNullPreviousFields_WhenExerciseMissingFromPriorSession` — prior session exists but does not include the exercise that is in the viewed session; assert previousWeight and previousEffort are `null` for that exercise row. Add `SessionDetailWithPreviousDto` record with fields matching the API contract response shape.

- [ ] T002 Add `GET /api/sessions/{sessionId:guid}` endpoint to `src/WorkoutTracker.Api/Program.cs`: (1) look up the session by `sessionId` via `db.WorkoutSessions.Include(ws => ws.PlannedWorkout).Include(ws => ws.LoggedExercises).ThenInclude(le => le.Exercise)` — return `Results.Json(new { error = "Session not found." }, statusCode: 404)` if null; (2) if `session.PlannedWorkoutId` is not null, find the prior session — `db.WorkoutSessions.Where(ws => ws.PlannedWorkoutId == session.PlannedWorkoutId && ws.WorkoutSessionId != sessionId && EF.Property<DateTime>(ws, "CompletedAt") <= EF.Property<DateTime>(session, "CompletedAt")).OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt")).ThenByDescending(ws => ws.WorkoutSessionId).Select(ws => ws.LoggedExercises.Select(le => new { le.ExerciseId, le.LoggedWeight, le.Effort }).ToList()).FirstOrDefaultAsync()`; (3) merge per exercise — for each `LoggedExercise` in session, find the **first matching entry** in the prior session list by `ExerciseId` (i.e., `priorExercises.FirstOrDefault(le => le.ExerciseId == currentExercise.ExerciseId)`); extract `previousWeight` and `previousEffort` (null if not found or no prior session). When the same exercise appears multiple times in a session, each row in the current session independently looks up its prior match — all duplicate rows will map to the same single prior row. This is the accepted "first-match" behaviour documented in `research.md` R-007; (4) return `Results.Ok` with shape from `contracts/api-contract.md`: `{ workoutSessionId, plannedWorkoutId, workoutName, completedAt, exercises: [{ loggedExerciseId, exerciseName, loggedWeight, effort, previousWeight, previousEffort }] }`. `workoutName` = `session.WorkoutName ?? session.PlannedWorkout?.Name`. Run all 6 integration tests from T001 and confirm they pass.

- [ ] T003 [P] Add proxy route to `src/WorkoutTracker.Web/Program.cs` following the existing try/catch + `WebProxyLog.ProxyError` pattern: `app.MapGet("/api/sessions/{sessionId:guid}", async (Guid sessionId, ILogger<Program> logger, IHttpClientFactory httpClientFactory) => { try { var client = httpClientFactory.CreateClient("api"); var response = await client.GetAsync($"/api/sessions/{sessionId}"); var content = await response.Content.ReadAsStringAsync(); return Results.Content(content, "application/json", statusCode: (int)response.StatusCode); } catch (Exception ex) { WebProxyLog.ProxyError(logger, $"GET /api/sessions/{sessionId}", ex); return Results.Json(new { error = "API unavailable." }, statusCode: 502); } });` Place it immediately after the existing `GET /api/sessions/latest` proxy block.

**Checkpoint**: `dotnet test src/WorkoutTracker.UnitTests` — all 6 new integration tests pass, no regressions on existing tests.

---

## Phase 2: User Story 1 — Session Detail Navigation & Page (Priority: P1) 🎯 MVP

**Goal**: Clicking a history entry navigates to a dedicated detail page that shows the workout name, date, and a five-column exercise table. The back button returns to History. The inline expand-collapse is replaced entirely.

**Independent Test**: (1) Navigate to History with at least one session. (2) Click any entry — confirm no inline expansion occurs and the URL changes to `/history/session?id=<sessionId>`. (3) Confirm the detail page shows the workout name, date, and exercise table. (4) Click back — confirm return to `/history`.

### Tests for User Story 1

- [ ] T004 [P] [US1] Write E2E tests in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: (a) **REMOVE** `HistoryPage_SessionExpandCollapse` — this test asserts `aria-expanded="true"` and `.history-session__details` visibility, both of which are being deleted; (b) **REMOVE** `HistoryPage_SessionDetails_ShowsExerciseData` — this test expands the entry and checks `.history-session__exercise-name`, which no longer applies; (c) **ADD** `HistoryPage_SessionEntry_NavigatesToDetailPage` — seed workout + session via API, navigate to History, click `.history-session__header`, wait for URL to contain `/history/session`, assert page contains `.session-detail`; (d) **ADD** `SessionDetailPage_ShowsExerciseTable` — after navigating to detail page, assert `.session-detail__table` is visible, `.session-detail__title` contains the workout name, at least one `.session-detail__row` is visible; (e) **ADD** `SessionDetailPage_BackNavigation_ReturnsToHistory` — navigate to detail page, click `.session-detail__back`, assert URL is `/history` and `.history-page` is visible. All three new tests must FAIL before T005–T008 are implemented.

### Implementation for User Story 1

- [ ] T005 [US1] Register the `/history/session` route in `src/WorkoutTracker.Web/wwwroot/ts/main.ts`: add `import { render as renderSessionDetail } from "./pages/session-detail.js";` after the history import; add `registerRoute("/history/session", renderSessionDetail);` after the history route registration.

- [ ] T006 [US1] Create `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`: (1) Import `{ navigate }` from `"../router.js"`. (2) Define interfaces `SessionExercise { loggedExerciseId, exerciseName, loggedWeight: string | null, effort: number | null, previousWeight: string | null, previousEffort: number | null }` and `SessionDetail { workoutSessionId, plannedWorkoutId: string | null, workoutName, completedAt, exercises: SessionExercise[] }`. (3) `export async function render(container: HTMLElement)`: read `?id=` param; if missing, render error state "Invalid session." with back button. Set `container.innerHTML` to the root skeleton from `contracts/ui-contract.md` (back button, header with title + date, loading div, error div, table-wrapper div, empty div). Wire back button to `navigate('/history')`. (4) Fetch `GET /api/sessions/{id}` in try/catch: on 404 → show error "Session not found."; on other error → show "Could not load session details."; on success with zero exercises → show empty state. (5) On success with exercises: format date using `Intl.DateTimeFormat` with `{ day: "numeric", month: "long", year: "numeric" }` + time via `Intl.DateTimeFormat` with `{ hour: "numeric", minute: "2-digit", hour12: true }` joined by " · " (consistent with `history.ts`); populate `session-detail__title` and `session-detail__date`; render `<tbody>` rows — each exercise gets `loggedWeight ?? "—"`, `previousWeight ?? "—"`, effort rendered as numeric `${effort}` or `"—"`, same for previousEffort; apply `escapeHtml()` to all string fields. (6) Include `escapeHtml` helper identical to the one in `history.ts`.

- [ ] T007 [US1] Update `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts`: (1) Add `import { navigate } from "../router.js";` at top. (2) In `renderSession()`: remove the entire `exercisesHtml` block (exercises loop and empty-state fallback); change `<button class="history-session__header" type="button" aria-expanded="false" aria-controls="session-details-...">` to `<button class="history-session__header" type="button">`; remove `<span class="history-session__toggle">▸</span>`; remove the `<div class="history-session__details" ...>` block entirely. (3) In `renderSessions()`: replace the `toggleSession` event listener loop with `container.querySelectorAll<HTMLButtonElement>(".history-session__header").forEach((btn) => { const sessionId = btn.closest(".history-session")?.getAttribute("data-session-id") ?? ""; btn.addEventListener("click", () => { navigate("/history/session?id=" + sessionId); }); });`. (4) Delete the entire `toggleSession()` function. Confirm `renderSession()` returns only the header button (name, date, exercise count) with no expandable section.

- [ ] T008 [P] [US1] Update `src/WorkoutTracker.Web/wwwroot/css/styles.css`: (1) **Remove** the following CSS rules entirely: `.history-session__toggle { … }`, `.history-session--expanded .history-session__toggle { … }`, `.history-session__details { … }`, `.history-session--expanded .history-session__details { … }`. (2) **Add** the `.session-detail__*` CSS block after the history section, per `contracts/ui-contract.md`: `.session-detail`, `.session-detail__back` (styled as text link using `--color-primary`; background transparent, border none), `.session-detail__header`, `.session-detail__title`, `.session-detail__date`, `.session-detail__loading`, `.session-detail__error`, `.session-detail__empty`, `.session-detail__table-wrapper` (`background-color: var(--color-white)`, border, border-radius, overflow hidden), `.session-detail__table` (`width: 100%`, `border-collapse: collapse`), `.session-detail__th` (padding, `font-size: var(--font-size-sm)`, `color: var(--color-text-light)`, `font-weight: 600`, text-align left, border-bottom, `background-color: var(--color-bg)`), `.session-detail__th--exercise` (`width: 35%`), `.session-detail__td` (padding, font-size, `color: var(--color-text)`, border-bottom using `var(--color-bg)`), `.session-detail__td--exercise` (`font-weight: 500`), `.session-detail__td--prev` (`color: var(--color-text-light)`, `font-size: var(--font-size-sm)`).

**Checkpoint**: Run T004 E2E tests — all three new tests pass. `HistoryPage_SessionExpandCollapse` and `HistoryPage_SessionDetails_ShowsExerciseData` have been removed. All other retained history E2E tests pass.

---

## Phase 3: User Story 2 — Previous Session Comparison (Priority: P2)

**Goal**: The "Prev. Weight" and "Prev. Effort" columns on the detail page display real data from the most recently completed prior session of the same planned workout — not just "—".

**Independent Test**: Complete two sessions of the same planned workout with different weights/efforts. Navigate to the detail page of the **second** session. Verify that "Prev. Weight" and "Prev. Effort" cells contain the values from the first session.

### Tests for User Story 2

- [ ] T009 [P] [US2] Add E2E test `SessionDetailPage_ShowsPreviousData_WhenPriorSessionExists` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: (1) seed exercise + workout via API; (2) POST first session with `loggedWeight: "70 KG"` and `effort: 6`; (3) POST second session with `loggedWeight: "75 KG"` and `effort: 7`; (4) GET `/api/sessions` to find the second session's `workoutSessionId`; (5) navigate to History, click the first entry (most recent), wait for `.session-detail`; (6) assert a `.session-detail__td--prev` cell containing `"70 KG"` is visible, and a `.session-detail__td--prev` cell containing `"6"` is visible. Test must fail before Phase 1 (T002) is implemented.

### Implementation for User Story 2

- [ ] T010 [US2] Verify `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` handles all previous-data edge cases correctly (no code changes expected if T006 was implemented correctly — this is an explicit verification task): (a) first session of a planned workout — all prev cells show "—"; (b) ad-hoc session (`plannedWorkoutId == null`) — all prev cells show "—" (API returns null, frontend renders "—"); (c) exercise not in prior session — that row's prev cells show "—". Run T009 E2E test to confirm pass. If any rendering defect is found, fix it in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`.

**Checkpoint**: T009 E2E test passes. All US1 E2E tests continue to pass.

---

## Phase 4: User Story 3 — Remove Inline Expand-Collapse Affordances (Priority: P3)

**Goal**: The History page entries contain no expand-collapse visual affordances — no toggle icon, no `aria-expanded`, no expandable section. Entries are clean tappable cards leading to the detail page.

**Independent Test**: Open the History page. Confirm no `.history-session__toggle` elements exist in the DOM and no `.history-session__details` sections are present.

### Tests for User Story 3

- [ ] T011 [P] [US3] Add E2E test `HistoryPage_NoExpandCollapseAffordance` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`: seed one session via API, navigate to History, assert `page.Locator(".history-session__toggle").CountAsync()` equals 0, assert `page.Locator(".history-session__details").CountAsync()` equals 0, assert `page.Locator(".history-session__header[aria-expanded]").CountAsync()` equals 0. Test should already pass if T007 and T008 were implemented correctly — this confirms no regressions.

### Implementation for User Story 3

- [ ] T012 [US3] Audit `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts` and `src/WorkoutTracker.Web/wwwroot/css/styles.css` for any remaining references to the removed expand-collapse behaviour: confirm zero references to `aria-expanded`, `aria-controls`, `history-session--expanded`, `history-session__details`, `history-session__toggle`, `toggleSession`. Fix any remaining references found. Run T011 to confirm pass.

**Checkpoint**: T011 E2E test passes. All previous E2E tests continue to pass.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Build validation, type safety, UX consistency, security, and full regression.

- [ ] T013 Build TypeScript and run frontend unit tests: `cd src/WorkoutTracker.Web && npm run build` — confirm zero type errors; `npm test` — confirm all Vitest tests pass (router.test.ts, utils.test.ts, theme.test.ts, prestart-modal.test.ts). Fix any type errors in `session-detail.ts` or `history.ts`.

- [ ] T014 [P] Run full backend integration test suite: `dotnet test src/WorkoutTracker.UnitTests` — confirm all 6 new `GetSessionDetail_*` tests pass and zero regressions in existing tests.

- [ ] T015 [P] Run full Playwright E2E test suite: `dotnet test src/WorkoutTracker.E2ETests` — confirm `HistoryPage_SessionEntry_NavigatesToDetailPage`, `SessionDetailPage_ShowsExerciseTable`, `SessionDetailPage_BackNavigation_ReturnsToHistory`, `SessionDetailPage_ShowsPreviousData_WhenPriorSessionExists`, `HistoryPage_NoExpandCollapseAffordance` all pass; confirm `HistoryPage_EmptyState_ShowsMessage`, `HistoryPage_WithSessions_ShowsSessions`, `HistoryPage_NoGroupHeaders_FlatList`, `HistoryPage_EntryShowsDateBelowName`, `HistoryPage_HasH1Heading`, `HistoryPage_LoadingState_ShownInitially` all still pass; confirm `HistoryPage_SessionExpandCollapse` and `HistoryPage_SessionDetails_ShowsExerciseData` have been removed.

- [ ] T016 UX consistency review in `src/WorkoutTracker.Web/wwwroot/css/styles.css`: verify all `.session-detail__*` rules exclusively use existing CSS custom properties (`--color-white`, `--color-bg`, `--color-border`, `--color-text`, `--color-text-light`, `--color-primary`, `--spacing-sm`, `--spacing-md`, `--spacing-xl`, `--spacing-lg`, `--radius`, `--font-size-sm`, `--font-size-base`, `--font-size-xl`, `--min-touch-target`); confirm no hardcoded colours or pixel values where tokens exist; confirm the table's muted header and light previous-column styling matches the visual hierarchy described in `contracts/ui-contract.md`.

- [ ] T017 Security review in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` and `src/WorkoutTracker.Api/Program.cs`: (a) confirm `escapeHtml()` is applied to `exerciseName`, `loggedWeight`, and `previousWeight` before they are injected into `innerHTML` — effort integers and `getEffortLabel()` return values are not user-controlled and do not require escaping; (b) confirm the `sessionId` GUID route constraint (`:guid`) on both the API and web proxy endpoints prevents non-GUID inputs from reaching the handler; (c) confirm the EF Core `Where(ws => ws.WorkoutSessionId == sessionId)` predicate is parameterised (no string concatenation); document the single-user cross-user exception in a comment consistent with features 008 and 013.

- [ ] T018 [P] Validate PR-003 performance with a large session in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`: (a) add `GetSessionDetail_PerformanceWithLargeSession` — seed a session with 25 unique exercises via `CreateWorkoutWithExerciseAsync` + `CreateSessionAsync`; record `Stopwatch` around a `GET /api/sessions/{sessionId}` HTTP call; assert elapsed milliseconds < 500; (b) navigate to the detail page for that session and visually confirm all 25 rows render without layout issues (manual verification note in PR description). This validates that T002's EF Core query does not degrade under realistic load as required by PR-003.

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Foundational): No dependencies — start immediately
  └─► Phase 2 (US1): Depends on Foundational completion (T001, T002, T003)
        ├─► Phase 3 (US2): Depends on US1 completion (T004–T008)
        └─► Phase 4 (US3): Depends on US1 completion (T007, T008)
Phase 5 (Polish): Depends on all story phases complete
```

### Within Each Phase

- **Phase 1**: T001 (write tests) → T002 (implement, tests pass) → T003 [P] (proxy, parallel with T002)
- **Phase 2**: T004 [P] (write E2E tests) → T005 → T006 → T007 → T008 [P]
- **Phase 3**: T009 [P] (write E2E test) → T010 (verify + fix)
- **Phase 4**: T011 [P] (write E2E test) → T012 (audit + fix)
- **Phase 5**: T013 → T014 [P] → T015 [P] → T016 [P] → T017 → T018 [P]

### Parallel Opportunities

```bash
# Phase 1 — after T001 (tests written):
Task T002: "Implement GET /api/sessions/{sessionId} in Program.cs"
Task T003: "Add web proxy route in WorkoutTracker.Web/Program.cs"   # ← parallel with T002

# Phase 2 — start T004 while T005–T008 are being worked on:
Task T004: "Write E2E tests in WorkoutHistoryTests.cs"              # ← parallel with T005–T008
Task T008: "Add CSS styles + remove old CSS in styles.css"          # ← parallel with T005–T007

# Phase 5 — run all checks together:
Task T014: "dotnet test src/WorkoutTracker.UnitTests"               # ← parallel
Task T015: "dotnet test src/WorkoutTracker.E2ETests"                # ← parallel
Task T016: "UX consistency review"                                  # ← parallel
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete **Phase 1** (Foundational) — T001 → T002 → T003
2. Complete **Phase 2** (US1) — T004 → T005 → T006 → T007 → T008
3. **STOP and VALIDATE**: Click a history entry and confirm navigation to a fully functional detail page
4. Run T004 E2E tests — confirm all three pass

### Incremental Delivery

- After Phase 2: History → Detail page navigation works; table shows current weight + effort; previous columns show "—" (MVP)
- After Phase 3: Previous weight and effort populate from prior sessions (full value)
- After Phase 4: Expand-collapse affordances are confirmed gone; DOM is clean

### Single-Developer Sequence (Recommended)

```
T001 → T002 (+ T003 in parallel) → T004 → T005 → T006 → T007 → T008
     → T009 → T010
     → T011 → T012
     → T013 → T014 → T015 → T016 → T017 → T018
```
