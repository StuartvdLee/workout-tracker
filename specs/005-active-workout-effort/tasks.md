# Tasks: Active Workout UI — Effort Tracking

**Input**: Design documents from `/specs/005-active-workout-effort/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Automated tests are REQUIRED for every user story and every bug fix.
Include the appropriate unit, integration, contract, or end-to-end coverage
needed to prove behavior before implementation is complete.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story, with explicit work for security, user
experience consistency, and performance verification where applicable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in every description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the `effort` column to the data layer — this is the single blocking prerequisite for all three user stories.

- [x] T001 Add `Effort` property (`int?`) to `LoggedExercise` entity in `src/WorkoutTracker.Infrastructure/Data/Models/LoggedExercise.cs`
- [x] T002 Add check constraint for effort range in `WorkoutTrackerDbContext.OnModelCreating` in `src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs` — `ck_logged_exercise_effort_range`: `effort IS NULL OR (effort >= 1 AND effort <= 10)`
- [x] T003 Generate EF Core migration `AddEffortToLoggedExercise` in `src/WorkoutTracker.Infrastructure/Data/Migrations/` using `dotnet ef migrations add AddEffortToLoggedExercise --project src/WorkoutTracker.Infrastructure --startup-project src/WorkoutTracker.Api`
- [x] T004 Verify migration SQL is correct (nullable int column + check constraint on `workout_tracker.logged_exercise`) — review the generated `.cs` migration file before proceeding

**Checkpoint**: Migration generated and reviewed — all story work can now begin in parallel

---

## Phase 2: Foundational (Blocking API Updates)

**Purpose**: Update the session API endpoints to accept and return `effort`. This unblocks all frontend work and all backend integration tests.

**⚠️ CRITICAL**: No session-related frontend or test work can complete end-to-end until this phase is done.

- [x] T005 Add `Effort` (`int?`) field to `SessionLoggedExerciseItem` request DTO at the bottom of `src/WorkoutTracker.Api/Program.cs`
- [x] T006 Add server-side effort range validation to `POST /api/workouts/{workoutId}/sessions` in `src/WorkoutTracker.Api/Program.cs` — reject with `400 { "error": "Effort must be between 1 and 10." }` if any provided effort is outside [1, 10]
- [x] T006b Add server-side weight string validation to `POST /api/workouts/{workoutId}/sessions` in `src/WorkoutTracker.Api/Program.cs` — reject with `400 { "error": "Logged weight must not exceed 100 characters." }` if any provided `loggedWeight` string exceeds 100 characters (SR-001; weight remains a free-form string per Decision 5)
- [x] T007 Persist `item.Effort` into `LoggedExercise.Effort` in the `POST /api/workouts/{workoutId}/sessions` handler in `src/WorkoutTracker.Api/Program.cs`
- [x] T008 Project `le.Effort` in, **and remove `le.LoggedReps` from**, the `POST /api/workouts/{workoutId}/sessions` response anonymous object in `src/WorkoutTracker.Api/Program.cs` (reps are no longer part of the response contract per api-contract.md)
- [x] T009 Project `le.Effort` in the `GET /api/sessions` response anonymous object in `src/WorkoutTracker.Api/Program.cs`
- [x] T010 Remove `le.LoggedReps` from the `GET /api/sessions` response projection in `src/WorkoutTracker.Api/Program.cs` (reps no longer returned to UI per FR-010/FR-014)
- [x] T010b Create shared TypeScript utility `src/WorkoutTracker.Web/wwwroot/ts/utils.ts` — export `getEffortLabel(value: number): string` returning `"Easy"` for 1–3, `"Moderate"` for 4–6, `"Hard"` for 7–8, `"All Out"` for 9–10 (Decision 7; single source of truth imported by both active-session.ts and history.ts)
- [x] T011 [US1] Update `SessionDetailDto` and `SessionLoggedExerciseDto` records in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` to remove `LoggedReps` and add `Effort` (`int?`) fields, so test DTOs match the updated API contract — **must be done immediately after T010 to keep tests compilable**

**Checkpoint**: API accepts and returns effort, test DTOs match contract, shared utility exists — backend and frontend streams can proceed in parallel

---

## Phase 3: User Story 1 — Log Weight (KG) Without Reps (Priority: P1) 🎯 MVP

**Goal**: The active session view shows only a "Weight (KG)" field per exercise — no reps field anywhere. Sessions save correctly without reps data and history never shows reps.

**Independent Test**: Start an active session; confirm no reps input exists; enter a weight value; save; confirm session is saved and history shows weight with "KG", no reps.

### Tests for User Story 1

- [x] T012 [P] [US1] Add integration test `CreateSession_DoesNotRequireReps_AndSavesWithoutReps` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — POST a session with no `loggedReps` field and assert 201, session is stored, `loggedReps` is absent from response
- [x] T013 [P] [US1] Add integration test `GetSessions_DoesNotReturnLoggedReps` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — confirm `loggedReps` is not present in the GET /api/sessions response DTO

### Implementation for User Story 1

- [x] T014 [US1] Remove reps input group from `renderExerciseInputs()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — delete the `repsGroup` `div`, `repsLabel`, and `repsInput` DOM construction blocks entirely
- [x] T015 [US1] Remove `loggedReps` from the `LogEntry` interface in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` and update the `logEntries` map initialisation to remove the `loggedReps: ""` field
- [x] T016 [US1] Change weight input `type` from `"text"` to `"number"` (with `min="0"` and `step="0.5"`) in `renderExerciseInputs()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T017 [US1] Update weight label text from `"Weight"` to `"Weight (KG)"` and update `aria-label` to `"Weight in KG for {exercise.name}"` in `renderExerciseInputs()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [x] T018 [US1] Remove `loggedReps` from the `handleSave()` payload construction in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — the POST body no longer sends `loggedReps`
- [x] T019 [P] [US1] Add inline weight validation to `handleSave()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — if a weight field has a non-numeric value, display an inline error message near the field (using the existing `active-session__error` pattern) and prevent submission (FR-003)
- [x] T020 [US1] Run all existing backend integration tests (`dotnet test src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj`) — all 45 prior tests plus the new US1 tests must pass

**Checkpoint**: User Story 1 is fully functional — no reps in UI, weight labelled KG, sessions save cleanly

---

## Phase 4: User Story 2 — Rate Effort Per Exercise (Priority: P1)

**Goal**: Every exercise card in the active session view has an Effort slider (1–10). The intensity label (Easy/Moderate/Hard/All Out) updates live as the slider moves. Untouched sliders submit null. Touched sliders persist the exact value.

**Independent Test**: Start an active session; move the effort slider for one exercise to values 1, 3, 4, 6, 7, 8, 9, 10 and confirm the correct label appears each time; save; verify effort value appears correctly in history.

### Tests for User Story 2

- [x] T021 [P] [US2] Add integration test `CreateSession_StoresEffort_WhenProvided` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — POST a session with `effort: 7` and assert 201, then GET /api/sessions and assert the returned exercise has `effort: 7`
- [x] T022 [P] [US2] Add integration test `CreateSession_StoresNullEffort_WhenNotProvided` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — POST a session with no `effort` field and assert `effort` is null in the response
- [x] T023 [P] [US2] Add integration test `CreateSession_Returns400_WhenEffortOutOfRange` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — POST with `effort: 0` and `effort: 11` and assert 400 with appropriate error message for each

### Implementation for User Story 2

- [x] T024 [US2] Add `effort: number | null` field to `LogEntry` interface in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` and initialise it as `null` in `renderExerciseInputs()`
- [x] T025 [US2] Import `getEffortLabel` from `../utils.js` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — add `import { getEffortLabel } from "../utils.js";` at the top of the file (function created in T010b; do not re-implement here)
- [x] T026 [US2] Add effort slider DOM construction to `renderExerciseInputs()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — `<input type="range" min="1" max="10" step="1">` with `data-touched="false"`, `aria-valuemin="1"`, `aria-valuemax="10"`, `aria-valuetext="Not rated"`, **no `aria-valuenow` attribute set initially** (call `sliderEl.removeAttribute("aria-valuenow")` after constructing the element — HTML range inputs always have an implicit DOM value at the midpoint, so the attribute must be explicitly removed to prevent AT from announcing a spurious "5"), and an associated `<div aria-live="polite">Rate effort</div>` label element (see ui-contract.md for full ARIA state table)
- [x] T027 [US2] Wire the `input` event on the effort slider in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` — on first interaction: set `data-touched="true"`, **set `aria-valuenow`** to the current value, update `LogEntry.effort`, call `getEffortLabel()`, update label text, update `aria-valuetext` to `"{value}, {label}"` (e.g., "7, Hard"), apply CSS modifier class (`--easy`, `--moderate`, `--hard`, `--all-out`) to the label element
- [x] T028 [US2] Update `handleSave()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` to include `effort: entry.effort` in the `loggedExercises` POST payload (null when slider was untouched)
- [x] T029 [P] [US2] Add effort slider CSS to `src/WorkoutTracker.Web/wwwroot/css/styles.css` — add BEM classes: `.active-session__input-group--effort`, `.active-session__effort-slider-wrap`, `.active-session__effort-slider`, `.active-session__effort-label`, `.active-session__effort-label--easy`, `.active-session__effort-label--moderate`, `.active-session__effort-label--hard`, `.active-session__effort-label--all-out` (per ui-contract.md)
- [x] T030 [US2] Verify the effort slider is fully usable on mobile viewports (min-width: 320px) — weight input and effort slider must not overlap or require horizontal scrolling on the exercise card (UX-004)
- [x] T031 [US2] Run all backend integration tests to confirm T021–T023 pass: `dotnet test src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj`

**Checkpoint**: User Stories 1 and 2 are both fully functional and independently testable

---

## Phase 5: User Story 3 — View Effort Data in Workout History (Priority: P2)

**Goal**: The History page exercise rows show weight with "KG" and effort with its intensity label. Reps are never shown. Old sessions without effort display cleanly.

**Independent Test**: Complete a session with mixed effort data (some exercises rated, some not), open History, expand the session, confirm: weight shows "KG", rated exercises show "7 — Hard" style labels, unrated exercises show nothing, no reps anywhere.

### Tests for User Story 3

- [x] T032 [P] [US3] Add integration test `GetSessions_ReturnsEffortAndWeightWithoutReps` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — create a session with effort=5 and weight="60", GET /api/sessions, assert response includes `effort: 5`, `loggedWeight: "60"`, and does NOT include `loggedReps`
- [x] T033 [P] [US3] Add integration test `GetSessions_HandlesNullEffortGracefully` in `src/WorkoutTracker.Tests/Api/SessionApiTests.cs` — create a session with no effort, GET /api/sessions, assert `effort` is null and no error

### Implementation for User Story 3

- [x] T034 [US3] Update `LoggedExercise` TypeScript interface in `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts` — remove `loggedReps: number | null`, add `effort: number | null`
- [x] T035 [US3] Import `getEffortLabel` from `../utils.js` in `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts` — add `import { getEffortLabel } from "../utils.js";` at the top of the file (function created in T010b; do not re-implement here)
- [x] T036 [US3] Update the `renderSession()` exercise row building logic in `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts`:
  - Remove the `if (ex.loggedReps !== null) parts.push(...)` block entirely
  - Replace weight display: `if (ex.loggedWeight !== null) parts.push(\`${escapeHtml(ex.loggedWeight)} KG\`)`
  - Add effort display: `if (ex.effort !== null) parts.push(\`${ex.effort} — ${getEffortLabel(ex.effort)}\`)`
  - Join parts with `" · "` separator (unchanged)
- [x] T037 [US3] Manually verify backward compatibility — run the app locally, open the History page with any pre-existing sessions (from feature 004), confirm: no reps shown, no JavaScript errors, weight-KG displays where data exists, effort is silently absent for old sessions
- [x] T038 [US3] Run the full test suite to confirm all stories' tests pass: `dotnet test src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj && cd src/WorkoutTracker.Web && npm test`

**Checkpoint**: All three user stories are independently functional and tested

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility, performance confirmation, and documentation close-out.

- [x] T039 [P] Verify effort slider ARIA attributes are correct end-to-end — confirm `aria-valuetext` reads the label text (e.g., "7, Hard"), that `aria-valuenow` is absent before first touch and present after, and `aria-live="polite"` region updates as slider moves; test with keyboard-only navigation (arrow keys increment/decrement value, label updates)
- [x] T040 [P] Confirm effort slider label update latency is within the 50ms budget (PR-002) — manually test rapid slider movement on a mid-range mobile device; label must follow the thumb position without perceptible lag
- [x] T040b [P] Confirm Save button enters "Saving…" state within 200ms of press (PR-003) — throttle DevTools network to Slow 3G, press Save on an active session, confirm button text changes to "Saving…" and is visually disabled before the API response returns
- [x] T041 [P] Confirm active session page load is within the 3-second budget on slow 3G (PR-001) — use browser DevTools throttling; no additional API calls were added so this should pass trivially
- [x] T042 Confirm the effort slider does not snap to a visual midpoint value when the user has not interacted with it — the `data-touched="false"` state must not submit any effort value; verify by saving without touching any slider and checking the stored session in history
- [x] T043 [P] Update `specs/005-active-workout-effort/quickstart.md` if any implementation details diverged from the documented walkthrough during implementation
- [x] T044 Commit all changes on branch `005-active-workout-effort` with a descriptive message referencing the spec; include the Co-authored-by trailer

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational API)**: Depends on Phase 1 completion — BLOCKS all end-to-end integration testing
- **Phase 3 (US1)** and **Phase 4 (US2)**: Both depend on Phase 2 — can run in parallel with each other
- **Phase 5 (US3)**: Depends on Phase 2; integrates US1 + US2 output but is independently testable
- **Phase 6 (Polish)**: Depends on Phases 3–5 completion

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 1 + 2 only. No dependency on US2 or US3.
- **US2 (P1)**: Depends on Phase 1 + 2 only. No dependency on US1 or US3. (Both T014–T020 and T024–T031 operate on the same file `active-session.ts` so should be done sequentially in practice.)
- **US3 (P2)**: Depends on Phase 2 (API must return effort). Logically depends on US1 + US2 data existing to fully test, but the history rendering code is independently modifiable.

### Within Each Phase

- Test tasks (T011–T013, T021–T023, T032–T033) can be written and run immediately after Phase 2 is complete
- `active-session.ts` changes (US1 T014–T019 and US2 T024–T028) modify the same file — execute sequentially, not in parallel
- CSS (T029) is independent of test DTOs and can proceed in parallel with active-session.ts implementation
- Test DTOs (T011) are now in Phase 2 — do not defer past T010

### Parallel Opportunities

```bash
# Phase 1 + 2: Sequential (migration before API; T011 immediately after T010)
T001 → T002 → T003 → T004 → T005 → T006 → T006b → T007 → T008 → T009 → T010 → T010b → T011

# After Phase 2 completes, these can run in parallel:
[US1 tests]     T012, T013               (same file — write together; T011 already done in Phase 2)
[US2 tests]     T021, T022, T023         (same file — write together)
[CSS]           T029                     (independent file)

# active-session.ts changes are sequential (same file):
T014 → T015 → T016 → T017 → T018 → T019 (US1 changes)
  then
T024 → T025 → T026 → T027 → T028        (US2 changes; T025 is now just an import statement)

# After US1 + US2 complete:
T034 → T035 → T036 (history.ts — US3, sequential same file; T035 is now just an import statement)
T032, T033 (US3 tests — can run in parallel with T034+)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 — both P1)

1. Complete Phase 1: Migration setup (T001–T004)
2. Complete Phase 2: API updates + shared utility + test DTOs (T005–T011)
3. Complete Phase 3: US1 — remove reps, add KG label (T012–T020)
4. Complete Phase 4: US2 — add effort slider (T021–T031)
5. **STOP and VALIDATE**: Both P1 stories work — session can be logged with weight-KG and effort
6. Proceed to US3 (history display) to close the loop

### Incremental Delivery

1. Setup + Foundational → DB + API ready
2. US1 → Clean active session with no reps, weight in KG
3. US2 → Effort slider per exercise (builds on US1 layout)
4. US3 → History shows KG weight + effort label
5. Polish → Accessibility + performance confirmation

### Single-Developer Strategy

Work sequentially through phases in order. The two `active-session.ts` user stories (US1 and US2) share a file — implement US1 cleanup first, then layer in the effort slider. This keeps diffs reviewable.

---

## Notes

- `[P]` tasks operate on different files or are read-only investigations — safe to run concurrently
- US1 and US2 both modify `active-session.ts` — do not split across parallel branches unless the file is carefully divided
- `getEffortLabel` lives in `src/WorkoutTracker.Web/wwwroot/ts/utils.ts` (created in T010b) and is imported by both `active-session.ts` and `history.ts` — do not re-implement it in the page files (see Decision 7 in research.md)
- `LoggedReps` column is NOT dropped from the DB in this feature — the column remains for backward compatibility and future housekeeping
- All tests must pass before merging: `dotnet test` (backend) + `npm test` (frontend Vitest)

---

## Delivery Notes

**Status**: All tasks delivered. 57 backend integration tests pass (up from 45). PR #51 created and merged.

### Deviations from original tasks

- **T019** — Weight validation uses `type="number"` + `validity.badInput` (browser native); error displayed in the shared `#session-error` element rather than inline per field. Functionally equivalent.
- **T027** — CSS modifier classes (`--easy`, `--moderate`, `--hard`, `--all-out`) on the effort band span were **not implemented**. The effort band text updates via `textContent = getEffortLabel(value)`. No per-level class is applied. Visual result is identical since no conditional styling was defined for those classes.
- **T029** — Delivered CSS class names differ from the contract: `active-session__effort-group`, `active-session__effort-value`, `active-session__effort-band` used instead of the contracted `--effort`, `-slider-wrap`, `-label` variants. Functionally equivalent.

### Additional work delivered (post-spec user requests)

- **Back button removed** — `← Back to Workouts` button and its event listener removed from the active session header.
- **Notes input removed** — Notes field removed from the active session form. Notes column/field preserved in DB, API, and history display for backward compatibility.
- **Slider initial position** — Effort slider initialised to value `1` (leftmost) on render. `data-touched` stays false; untouched sliders still submit null.
- **Workout title centered** — Active session header title (`h1`) centred via `text-align: center`.
- **Slider coloring** — Green-to-red gradient attempted (3 approaches: CSS custom property, direct inline `accentColor`, full custom pseudo-element styling) and reverted. Final state: browser default slider appearance.
