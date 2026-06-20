# Tasks: Workout Overall Effort

**Input**: Design documents from `/specs/016-workout-overall-effort/`
**Branch**: `016-workout-overall-effort`
**Feature**: Capture overall workout effort via a pop-up modal on save; display on history and session detail pages

**Tests**: Automated tests are REQUIRED. Backend integration tests in `SessionApiTests.cs` (8 new tests); Playwright E2E tests in `WorkoutHistoryTests.cs` (7 new tests). See plan.md for full test list.

**Organization**: Tasks grouped by user story — each story is independently testable. Foundation phase must complete before any user story work begins.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in same phase)
- **[Story]**: Which user story ([US1], [US2], [US3])
- Exact file paths included in all descriptions

---

## Phase 1: Foundation (Blocking Prerequisites)

**Purpose**: Data model and migration changes that all three user stories depend on. Must be complete before any user story implementation begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Add `public int? OverallEffort { get; set; }` property to `WorkoutSession` in `src/WorkoutTracker.Infrastructure/Data/Models/WorkoutSession.cs`
- [x] T002 [P] Add check constraint `ck_workout_session_overall_effort_range` (`overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10)`) to the `WorkoutSession` entity block in `WorkoutTrackerDbContext.OnModelCreating` in `src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs`
- [x] T003 Generate EF Core migration `AddOverallEffortToWorkoutSession` with `AddColumn<int>(nullable: true)` and `AddCheckConstraint` for `overall_effort` on `workout_session` table in `src/WorkoutTracker.Infrastructure/Data/Migrations/`

**Checkpoint**: Migration generated and applies cleanly — all stories unblocked.

---

## Phase 2: User Story 1 — Rate Overall Workout Effort on Save (Priority: P1) 🎯 MVP

**Goal**: When the user presses "Save Workout", an effort modal appears with a 1–10 slider. Confirming (or skipping) completes the save with the effort value (or null) stored on the session.

**Independent Test**: Complete an active workout, press "Save Workout", verify the effort modal appears, rate it 7, confirm — then call `GET /api/sessions` and verify `overallEffort: 7` on the resulting session.

- [x] T004 [P] [US1] Add CSS classes `.effort-modal-backdrop`, `.effort-modal`, `.effort-modal__title`, `.effort-modal__desc`, `.effort-modal__slider-group`, `.effort-modal__label`, `.effort-modal__value`, `.effort-modal__band`, `.effort-modal__slider`, `.effort-modal__actions`, `.effort-modal__save`, `.effort-modal__skip` (mirror `.discard-backdrop` / `.discard-modal__*` visual pattern) in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T005 [US1] Add `public int? OverallEffort { get; set; }` to `SessionCreateRequest`, add range validation (`if (request.OverallEffort is not null && (request.OverallEffort < 1 || request.OverallEffort > 10))` → 400), assign `session.OverallEffort = request.OverallEffort`, and add `OverallEffort = session.OverallEffort` to the 201 Created response projection in `src/WorkoutTracker.Api/Program.cs`
- [x] T006 [US1] Add effort modal HTML scaffold (`#effort-backdrop` div containing `#effort-modal` `role="alertdialog"` with title, desc, slider-group, and actions buttons `#effort-modal-save` / `#effort-modal-skip` per ui-contract.md) to the page scaffold function in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T007 [US1] Add `let pendingOverallEffort: number | null = null;` module-level state; add `openEffortModal()` (reset slider to `data-touched="false"`, reset `#overall-effort-value` to "Not rated", set `display:block` on `#effort-backdrop`, move focus to `#effort-modal-save`) and `closeEffortModal()` (set `display:none`) in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T008 [US1] Add `handleEffortSliderInput()` (set `data-touched="true"`, update `pendingOverallEffort`, update `#overall-effort-value` and `#overall-effort-band` text, update `aria-valuetext`); wire slider `input` event listener in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T009 [US1] Add `handleEffortSave()` (close modal, call `handleSave(pendingOverallEffort ?? null)` — treat as null if `data-touched === "false"`) and `handleEffortSkip()` (close modal, call `handleSave(null)`); intercept "Save Workout" button click to call `openEffortModal()` instead of `handleSave` directly; add Escape-key listener and `#effort-backdrop` click listener (stop propagation on `#effort-modal` click) to call `handleEffortSkip()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T010 [US1] Update `handleSave` function signature to `async function handleSave(overallEffort: number | null): Promise<void>` and update the POST request body to `JSON.stringify({ loggedExercises, overallEffort })` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T011 [P] [US1] Add integration tests `CreateSession_StoresOverallEffort_WhenProvided`, `CreateSession_StoresNullOverallEffort_WhenNotProvided`, `CreateSession_StoresNullOverallEffort_WhenOverallEffortOmitted`, and `CreateSession_Returns400_WhenOverallEffortOutOfRange` (test both too-low and too-high values) in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [x] T012 [P] [US1] Add E2E tests `SaveWorkout_EffortModal_AppearsOnSave` (verify `#effort-backdrop` visible after save button click), `SaveWorkout_EffortModal_SkipSavesWithoutEffort` (click Skip, verify session saved with `overallEffort` null via API), and `SaveWorkout_EffortModal_ConfirmSavesWithEffort` (drag slider, click Save, verify session saved with correct `overallEffort` value) in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

**Checkpoint**: US1 complete — effort modal appears on save, confirming or skipping stores the correct value.

---

## Phase 3: User Story 2 — View Overall Effort on Workout History Page *(Removed post-delivery)*

> **Note**: US2 was implemented as specified, then removed at the user's request after delivery — the effort display on the history card was considered too cluttered. Tasks T013, T015, and T017 were implemented and then reverted. T014 and T016 remain in place (the API still returns `overallEffort` in `GET /api/sessions`; the integration test still passes).

- [x] ~~T013~~ [P] [US2] ~~Add CSS class `.history-session__overall-effort`~~ — **Reverted**: CSS removed.
- [x] T014 [US2] Add `OverallEffort = ws.OverallEffort` to the `.Select(ws => new { ... })` anonymous type projection in `GET /api/sessions` in `src/WorkoutTracker.Api/Program.cs` — **Still in place**: API returns the field even though the frontend no longer displays it.
- [x] ~~T015~~ [US2] ~~Add `overallEffort` to `WorkoutSession` interface and update `renderSession()` in `history.ts`~~ — **Reverted**: `history.ts` reverted to not include or display `overallEffort`.
- [x] T016 [P] [US2] Add integration test `GetSessions_ReturnsOverallEffort` — **Still in place**: test passes (API still returns the field).
- [x] ~~T017~~ [P] [US2] ~~Add E2E tests `HistoryPage_ShowsOverallEffort_WhenSessionHasEffort` and `HistoryPage_NoEffortShown_WhenSessionHasNoEffort`~~ — **Reverted**: E2E tests removed.

---

## Phase 4: User Story 3 — View Overall Effort on Session Detail Page (Priority: P3)

**Goal**: Opening a past session shows a summary row below the exercises table with the current session's overall effort and the previous session's overall effort (for the same planned workout). Missing values display "—". Ad-hoc sessions show no comparison.

**Independent Test**: Seed two sessions for the same workout (first with effort 6, second with effort 8), open the second session detail, and verify the `.session-detail__overall-effort-row` shows "8 · All Out" and "6 · Moderate". Open the first session and verify `previousOverallEffort` area shows "—".

- [x] T018 [P] [US3] Add CSS classes `.session-detail__overall-effort-row` (flex row below table; padding consistent with card interior), `.session-detail__overall-effort-label` (bold/dark), `.session-detail__overall-effort-value`, `.session-detail__overall-effort-prev-label` (colour `var(--color-text-light)`), `.session-detail__overall-effort-prev-value` (colour `var(--color-text-light)`) in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T019 [US3] Extend the prior session `.Select()` in `GET /api/sessions/{sessionId}` to project `new { ws.OverallEffort, LoggedExercises = ... }` (instead of projecting `LoggedExercises` only); add `OverallEffort = session.OverallEffort` and `PreviousOverallEffort = priorSession?.OverallEffort` to the 200 response anonymous type in `src/WorkoutTracker.Api/Program.cs`
- [x] T020 [US3] Add `import { getEffortLabel } from "../utils.js";`, add `readonly overallEffort: number | null;` and `readonly previousOverallEffort: number | null;` to `SessionDetailWithPrevious` interface, and update `renderDetailTable()` to **always** render `.session-detail__overall-effort-row` below the `</table>` close tag (row is unconditionally shown; display `<span class="session-detail__no-data">—</span>` for null or undefined values using `!= null` loose equality) in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`
- [x] T021 [P] [US3] Add integration tests `GetSessionDetail_ReturnsOverallEffort` (session with effort, verify field in response), `GetSessionDetail_ReturnsPreviousOverallEffort_WhenPriorSessionExists` (two sessions for same workout, verify second returns `previousOverallEffort`), and `GetSessionDetail_ReturnsNullPreviousOverallEffort_WhenNoPriorSession` (first-ever session for workout, verify `previousOverallEffort: null`) in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [x] T022 [P] [US3] Add E2E tests `SessionDetailPage_ShowsOverallEffortSummaryRow` (seed session with effort, open detail, verify `.session-detail__overall-effort-row` present with correct text), `SessionDetailPage_ShowsPreviousOverallEffort_WhenPriorSessionExists` (seed two sessions for same workout with effort, open second, verify previous value shows first session's effort label), and `SessionDetailPage_ShowsNoPreviousComparison_ForAdHocSession` (seed ad-hoc session with effort — no `plannedWorkoutId` — open detail, verify `.session-detail__overall-effort-row` shows current effort but `previousOverallEffort` area shows "—") in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

**Checkpoint**: US3 complete — summary row shows current and previous overall effort; all null/missing edge cases handled.

---

## Phase 5: Polish & Verification

**Purpose**: Confirm all code compiles, all tests pass, and no regressions are introduced.

- [x] T023 Run `dotnet build src/WorkoutTracker.slnx` and confirm zero errors and zero warnings in modified files
- [x] T024 [P] Run `cd src/WorkoutTracker.Web && npm run build` and confirm TypeScript compiles with no errors (`strict: true`, `noUnusedLocals`, `noUnusedParameters`)
- [x] T025 [P] Run `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj` and confirm all backend integration tests pass (including the 8 new overall effort tests)
- [x] T026 [P] Run `cd src/WorkoutTracker.Web && npm test` and confirm all Vitest frontend tests pass

---

## Dependency Graph

```
T001 ──┐
T002 ──┴─→ T003 ──→ T005 ──→ T006 ──→ T007 ──→ T008 ──→ T009 ──→ T010
                 │                                               │
                 │         T004 (CSS) ─────────────────────────→ T012 (E2E)
                 │         T011 (tests) ────────────────────────┘
                 │
                 ├──→ T014 ──→ T015
                 │    T013 (CSS) ─→ T017 (E2E)
                 │    T016 (tests)
                 │
                 └──→ T019 ──→ T020
                      T018 (CSS) ─→ T022 (E2E)
                      T021 (tests)

T023, T024, T025, T026 run after all phases complete
```

**Story Dependencies**:
- **US2 and US3** each depend only on Foundation (T001–T003) — they can proceed in parallel with US1 once the migration exists
- All three user stories are independently testable once the foundation phase is complete

---

## Parallel Execution Examples

### US1 (after T003 completes)
```
Stream A: T005 (API POST changes) → T006 → T007 → T008 → T009 → T010
Stream B: T004 (CSS)
Stream C: T011 (backend tests, after T005)
Stream D: T012 (E2E tests, after T010 + T004)
```

### US2 (after T003 completes, independently of US1)
```
Stream A: T014 (API GET /sessions) → T015 (history.ts)
Stream B: T013 (CSS)
Stream C: T016 (backend test, after T014)
Stream D: T017 (E2E tests, after T015 + T013)
```

### US3 (after T003 completes, independently of US1 and US2)
```
Stream A: T019 (API GET /sessions/{id}) → T020 (session-detail.ts)
Stream B: T018 (CSS)
Stream C: T021 (backend tests, after T019)
Stream D: T022 (E2E tests, after T020 + T018)
```

---

## Implementation Strategy

**MVP Scope**: Phase 1 + Phase 2 (US1) = 12 tasks — delivers the core capture flow end-to-end.

**Increment 2**: Phase 3 (US2) — 5 tasks — closes the feedback loop on the history page.

**Increment 3**: Phase 4 (US3) — 5 tasks — completes the detail view with comparison.

The three user stories can be implemented in any order after Foundation, as each one is independently testable. Starting with US1 is recommended since it is the data-capture path that the read-side stories depend on in production.

---

## Task Summary

| Phase | Tasks | Parallelizable | Purpose |
|---|---|---|---|
| Phase 1: Foundation | T001–T003 | T001 ‖ T002 | Data model + migration |
| Phase 2: US1 (P1) | T004–T012 | T004, T011, T012 | Effort modal + save flow |
| Phase 3: US2 (P2) | T013–T017 | T013, T016, T017 | History page display |
| Phase 4: US3 (P3) | T018–T022 | T018, T021, T022 | Session detail summary row |
| Phase 5: Polish | T023–T026 | T024, T025, T026 | Build + test verification |
| **Total** | **26 tasks** | **13 parallelizable** | |

| User Story | Task Count | New Test Count |
|---|---|---|
| US1 — Rate on Save | 9 | 4 integration + 3 E2E |
| US2 — History Page | 5 | 1 integration + 2 E2E |
| US3 — Detail Page | 5 | 3 integration + 2 E2E |
| **Total tests** | | **8 integration + 7 E2E** |
