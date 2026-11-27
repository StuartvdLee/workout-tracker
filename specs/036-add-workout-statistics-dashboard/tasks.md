---

description: "Task list for the workout statistics dashboard"
---

# Tasks: Workout Statistics Dashboard

**Input**: Design documents from `/specs/036-add-workout-statistics-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Focused Vitest, API integration, and Playwright E2E tests are required by the feature specification and project constitution.

**Organization**: Tasks are grouped by user story so each increment can be implemented and validated independently.

## Format: Task ID, optional parallel marker, optional story label, and action with exact file path

- **[P]**: Can run in parallel because it changes a different file and has no incomplete-task prerequisite
- **Story label**: Add US1–US4 to user-story phase tasks only
- Every task names its exact implementation, test, or documentation path

## Path Conventions

- **Web application**: Existing projects under `src/`; feature design and validation documents under `specs/036-add-workout-statistics-dashboard/`
- No new runtime dependency, persistence table, or migration is planned

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature's existing API, Web, and test-project conventions without introducing new infrastructure.

- [x] T001 Confirm the existing .NET, Vitest, PostgreSQL integration-test, and Playwright setup is sufficient and keep any required prerequisite corrections in `specs/036-add-workout-statistics-dashboard/quickstart.md`; do not add dependencies or migrations

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the feature preserves the product's current single-user workout-history scope.

- [x] T002 Confirm and document that this feature reuses the existing single-user history scope and adds no sharing or identity model in `specs/036-add-workout-statistics-dashboard/spec.md` and `specs/036-add-workout-statistics-dashboard/plan.md`; this scope decision does not claim deployment SSO or user-level isolation.

**Checkpoint**: The existing single-user history scope is preserved; user-story implementation can proceed.

---

## Phase 3: User Story 1 - Review workout statistics for a period (Priority: P1) 🎯 MVP

**Goal**: Show the current local week, month, or year with its date range and correct completed-workout and exercise activity summary.

**Independent Test**: With completed workouts inside and outside the current period, open the dashboard and verify the current week, period switching, date range, workout count, and distinct-workout exercise counts against database history.

### Tests for User Story 1

- [x] T003 [P] [US1] Add Vitest cases for Monday week, month/year boundaries, DST-safe local boundaries, and conversion to UTC in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/statistics-period.test.ts`
- [x] T004 [P] [US1] Add PostgreSQL-backed API integration tests for inclusive start/exclusive end filtering, completed-workout totals, and distinct exercise appearances in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`

- [x] T005 [P] [US1] Add a Playwright journey for the current-period dashboard, period switching, date range, workout count, and exercise activity in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 1

- [x] T006 [P] [US1] Implement local period selection and independent UTC boundary conversion in `src/WorkoutTracker.Web/wwwroot/ts/pages/statistics-period.ts`, following `data-model.md` — `Reporting period.kind`, `Reporting period.localStart`, `Reporting period.localEndExclusive`, `Reporting period.startUtc`, and `Reporting period.endUtc`
- [x] T007 [P] [US1] Implement and validate the bounded read-only `GET /api/statistics/workouts` query and response projection in `src/WorkoutTracker.Api/Program.cs`; require parseable UTC instants, `startUtc < endUtc`, and a maximum 370-day range, use parameterized `[startUtc, endUtc)` filtering, deduplicate each exercise per workout, and return only required summary fields following `data-model.md` — `WorkoutSession.CompletedAt`, `WorkoutSession.OverallEffort`, `Effort observation.value`, and the `Exercise activity` distinct `(session ID, exercise ID)` rule
- [x] T008 [US1] Proxy `GET /api/statistics/workouts` through the Web app, preserving downstream status codes and returning a generic unavailable response on proxy failure, in `src/WorkoutTracker.Web/Program.cs`
- [x] T009 [US1] Register the `/statistics` route and add the dashboard navigation entry using the existing SPA patterns in `src/WorkoutTracker.Web/wwwroot/ts/main.ts` and `src/WorkoutTracker.Web/wwwroot/index.html`
- [x] T010 [US1] Create the dashboard page with current-week default, Week/Month/Year selection, displayed inclusive local date range, completed-workout count, and exercise activity in `src/WorkoutTracker.Web/wwwroot/ts/pages/statistics.ts`
- [x] T011 [US1] Add dashboard layout and period-control styling consistent with existing responsive tokens in `src/WorkoutTracker.Web/wwwroot/css/styles.css`

**Checkpoint**: The P1 current-period summary works against the real API and can be verified without historical navigation or advanced states.

---

## Phase 4: User Story 2 - Explore past activity and trends (Priority: P2)

**Goal**: Browse past periods, inspect chronologically ordered usable overall-effort observations, and see source-history edits reflected in fresh summaries.

**Independent Test**: With workouts and valid effort values in multiple periods, navigate to an earlier period and verify its effort list and contributor count; edit or delete a contributing workout, re-enter or refresh that period, and verify current source data is shown with no future navigation.

### Tests for User Story 2

- [x] T012 [US2] Add Playwright journeys for Previous/Current/disabled Next, effort chronology and contributor count, and updated statistics after workout deletion in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 2

- [x] T013 [US2] Implement Previous, Next, and Current navigation, switching period kinds, fresh reads on page re-entry, and latest-request-wins handling in `src/WorkoutTracker.Web/wwwroot/ts/pages/statistics.ts`; never navigate beyond the current period or leave stale results presented as the selected period
- [x] T014 [US2] Render ordered overall-workout-effort dates and values with the exact contributing-workout count, using only API observations and no chart-only representation, in `src/WorkoutTracker.Web/wwwroot/ts/pages/statistics.ts`

**Checkpoint**: Users can navigate the supported history and review fresh, ordered effort observations independently of empty/incomplete-state handling.

---

## Phase 5: User Story 3 - Understand empty or incomplete history (Priority: P2)

**Goal**: Distinguish no workouts, unavailable statistics, partial usable effort data, invalid requests, and API failures without inventing results.

**Independent Test**: View an empty period, a period with workouts but no supported effort, and a period with a mixture of supported and unsupported effort values; verify empty/unavailable/partial states and generic validation or load errors are distinct.

### Tests for User Story 3

- [x] T015 [P] [US3] Add API integration tests for missing, malformed, non-UTC, reversed, and over-370-day ranges, plus omitted unsupported effort values without changing workout counts, in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [x] T016 [P] [US3] Add Playwright coverage for no-workout, no-usable-effort, partial-effort, and failed-request/retry states in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 3

- [x] T017 [US3] Render distinct empty-period, unavailable-exercise, unavailable-effort, partial-contributor, loading, error, and retry states without fabricated zeros or prior-period values in `src/WorkoutTracker.Web/wwwroot/ts/pages/statistics.ts`

**Checkpoint**: Empty and incomplete records cannot be mistaken for measured zeroes or successful empty responses.

---

## Phase 6: User Story 4 - Use the dashboard across devices and access needs (Priority: P3)

**Goal**: Make all controls and values understandable and operable with keyboard and assistive technology, including at narrow viewport widths.

**Independent Test**: Complete all dashboard actions using only a keyboard, confirm focus and selected/disabled states and equivalent text values, and inspect a narrow viewport for readable, unclipped essential content.

### Tests for User Story 4

- [x] T018 [US4] Add Playwright keyboard, focus, accessible-name, non-color text-equivalence, and narrow-viewport layout checks in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

### Implementation for User Story 4

- [x] T019 [US4] Add programmatic selected/disabled semantics, visible focus, loading/result announcements, and narrow-screen dashboard layout refinements in `src/WorkoutTracker.Web/wwwroot/ts/pages/statistics.ts` and `src/WorkoutTracker.Web/wwwroot/css/styles.css`

**Checkpoint**: The dashboard remains operable and interpretable without color perception, pointer input, or a wide viewport.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the agreed performance target, automated behavior, and bounded response scope; preserve evidence in the feature validation guide.

- [ ] T020 Measure at least 100 period changes against 1,000 completed workouts through the real Web/API/PostgreSQL path, verify result correctness and the under-2-second p95 target, and record environment and results in `specs/036-add-workout-statistics-dashboard/quickstart.md` (**not run; the current Playwright fixture uses an in-memory API mock**)
- [x] T021 Run the automated Vitest, .NET unit/API, and Playwright commands in `specs/036-add-workout-statistics-dashboard/quickstart.md`; record outcomes and confirm the response stays within the documented single-user history scope without claiming deployment SSO or user-level isolation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependency; confirm the existing project/test foundations and avoid adding unplanned infrastructure.
- **Foundational (Phase 2)**: Depends on Setup; preserve the existing single-user history scope without adding identity or sharing.
- **User Stories (Phase 3+)**: Depend on Setup and Foundational scope confirmation. Complete in priority order for incremental delivery; once shared interfaces exist, independent tests and separate story work may proceed in parallel when file ownership permits.
- **Polish (Phase 7)**: Depends on the desired stories; automated behavior and performance evidence must pass before release.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after foundational scope confirmation; establishes the period summary and dashboard foundation.
- **User Story 2 (P2)**: Extends the US1 dashboard/API response with history navigation and effort presentation; its independent journey is testable once US1 is complete.
- **User Story 3 (P2)**: Extends the US1/US2 response handling with empty, unavailable, partial, validation, and failure states; testable after the shared page and API are present.
- **User Story 4 (P3)**: Applies keyboard, assistive-technology, and responsive requirements to the completed dashboard interactions; testable after the UI stories.

### Within Each User Story

- Write focused tests before implementation and verify the expected behavior is initially missing.
- Period calculations and database projection precede API proxy and page integration.
- Complete each story checkpoint before treating that story as independently deliverable.
- Keep summary fields and date-range filtering within the existing single-user completed-history scope.

### Parallel Opportunities

- T003 and T004 are independent tests in separate files.
- T014 and T015 are independent validation tests in separate files.
- The Vitest and API tests can be developed independently because they use separate files; Playwright work across stories shares `WorkoutHistoryTests.cs` and should be sequenced.
- Story work can be assigned in parallel after the P1 interface is stable, but tasks that touch `statistics.ts`, `styles.css`, or the same API/test file must be sequenced or coordinated rather than run concurrently.

---

## Parallel Example: User Story 1

```bash
# Start the calendar and API tests together; they use separate files:
Task: "Add Vitest period-boundary tests in src/WorkoutTracker.Web/wwwroot/ts/__tests__/statistics-period.test.ts"
Task: "Add PostgreSQL API summary tests in src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational; confirm the feature adds no identity, sharing, or cross-user behavior.
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE** the current-period view and summary against database history, including period boundaries and response scope.
5. Do not release until the automated and performance validation in Phase 7 is complete.

### Incremental Delivery

1. Complete Setup and Foundational; preserve the existing single-user history access scope.
2. Add User Story 1 and validate the period summary independently (MVP scope).
3. Add User Story 2 and validate historical navigation, effort ordering, and refresh after edits/deletes.
4. Add User Story 3 and validate empty, incomplete, and failure states.
5. Add User Story 4 and validate keyboard, text alternatives, and narrow-screen behavior.
6. Complete performance measurement and automated validation before release; each story adds capability without introducing persistence or unrelated metrics.

### Parallel Team Strategy

1. Complete Setup and Foundational scope confirmation before endpoint implementation.
2. For User Story 1, run the calendar Vitest and PostgreSQL API test work in parallel; coordinate implementation against the API contract.
3. After the P1 interface is stable, assign separate story workstreams only where files do not overlap; coordinate edits to the shared page, stylesheet, API program, and fixture files.
4. Run all browser-level acceptance and performance checks against the integrated application.

---

## Notes

- Every task uses the required checkbox, sequential ID, optional justified `[P]`, story label only in story phases, and an exact file path.
- `[P]` is reserved for tasks in separate files with no incomplete prerequisite; shared-file work is explicitly sequenced.
- `data-model.md` citations identify the canonical constrained fields; task descriptions do not duplicate or redefine those constraints.
- Existing deployment access controls are unchanged; the feature does not claim to verify SSO or user-level isolation.
- No chart, migration, runtime dependency, new metric, or unrelated feature is included.
