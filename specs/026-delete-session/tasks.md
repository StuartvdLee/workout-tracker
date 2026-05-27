---
description: "Task list for 026-delete-session"
---

# Tasks: Delete Session

**Input**: Design documents from `/specs/026-delete-session/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Automated tests are REQUIRED per Constitution II — backend integration and E2E coverage are included in the relevant user story phases.

**Organization**: Tasks grouped by user story. No DB migration needed — `LoggedExercise` already has `ON DELETE CASCADE` on `WorkoutSessionId`.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2)
- Exact file paths included in all descriptions

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: The DELETE API endpoint and its web proxy must exist before any frontend or test work can be completed end-to-end.

**⚠️ CRITICAL**: No user story implementation can be fully tested until this phase is complete.

- [x] T001 Add `DELETE /api/sessions/{sessionId:guid}` handler to `src/WorkoutTracker.Api/Program.cs` — find session by GUID, return 404 with `{ "error": "Session not found." }` if missing, `db.WorkoutSessions.Remove(session)` + `SaveChangesAsync()` (cascade removes LoggedExercise rows), return `Results.NoContent()`. Follow the pattern of the existing `DELETE /api/workouts/{workoutId:guid}` handler.
- [x] T002 [P] Add proxy `DELETE /api/sessions/{sessionId:guid}` to `src/WorkoutTracker.Web/Program.cs` — follow the established `try/catch` proxy pattern used for `DELETE /api/workouts/{workoutId:guid}`: forward to API, return `NoContent()` on 204, forward error JSON on other codes, return 502 with `{ "error": "API unavailable." }` on exception.

**Checkpoint**: `DELETE /api/sessions/{id}` returns 204 via both the API directly and the Web proxy.

---

## Phase 2: User Story 1 — Delete Session from Detail Page (Priority: P1) 🎯 MVP

**Goal**: User can delete any session from its detail page via a confirmation modal, is redirected to the History page with a "Session deleted." banner, and sees the deletion confirmed immediately.

**Independent Test**: Navigate to any session detail page → click "Delete session" → confirm → verify redirect to History page, "Session deleted." banner visible, and the session no longer appears in the list.

### Tests for User Story 1

- [x] T003 [P] [US1] Add 4 backend integration tests for `DELETE /api/sessions/{sessionId}` to `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`:
  1. Returns 204 and `GET /api/sessions/{sessionId}` then returns 404
  2. Returns 204 and session absent from `GET /api/sessions` list
  3. Returns 204 and all `LoggedExercise` rows for the session are removed (verify via direct DB query or absence from GET response)
  4. Returns 404 with `{ "error": "Session not found." }` when sessionId does not exist

### Implementation for User Story 1

- [x] T004 [P] [US1] Add BEM CSS styles to `src/WorkoutTracker.Web/wwwroot/css/styles.css`:
  - `.session-detail__delete-section` — wrapper div, positioned after chart content
  - `.session-detail__delete` — destructive-action button styling (follows existing delete/danger button convention)
  - `.history-page__banner` — success banner styling (subtle, distinct from error states)
- [x] T005 [US1] Append delete section HTML and confirmation modal HTML to `#session-detail-content` after chart (or after overall-effort row for ad-hoc sessions) in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`:
  - Delete section: `<div class="session-detail__delete-section">`, button `#session-detail-delete`, error div `#session-detail-delete-error` (`role="alert"` `aria-live="polite"`)
  - Modal: `.discard-modal-backdrop` `#session-delete-confirm-backdrop`, `.discard-modal` with `role="alertdialog"` `aria-modal="true"` `aria-labelledby="session-delete-confirm-title"` `aria-describedby="session-delete-confirm-desc"`, Delete button `#session-delete-confirm-ok` (`.discard-modal__discard`), Keep session button `#session-delete-confirm-cancel` (`.discard-modal__continue`)
- [x] T006 [US1] Wire event handlers for the confirmation modal in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`:
  - `#session-detail-delete` click → show `#session-delete-confirm-backdrop`, focus `#session-delete-confirm-ok`
  - `#session-delete-confirm-cancel` click → hide backdrop, return focus to `#session-detail-delete`
  - Escape key on modal → same as cancel
  - Tab key in modal → trap focus between `#session-delete-confirm-ok` and `#session-delete-confirm-cancel` (reuse `trapModalTabKey` from `prestart-modal.js` if available, else inline)
- [x] T007 [US1] Implement confirmed-delete logic in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`:
  - `isDeleting` module-level boolean guard (prevents double-submit)
  - `#session-delete-confirm-ok` click: set `isDeleting = true`, disable `#session-detail-delete`, hide modal, `DELETE /api/sessions/{sessionId}`
  - On 204 or 404 response: `navigate("/history?deleted=1")`
  - On other error or network failure: show message in `#session-detail-delete-error`, re-enable button, reset `isDeleting`
- [x] T008 [P] [US1] Update `render()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts`:
  - Read `new URLSearchParams(window.location.search).get("deleted") === "1"`
  - If true: call `history.replaceState(null, "", "/history")` immediately to clean URL
  - Include `<p class="history-page__banner" role="status">Session deleted.</p>` in rendered HTML (immediately after `<h1>`, before loading/empty/list elements) when flag is set
- [x] T009 [P] [US1] Add 5 E2E tests to `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`:
  1. `DeleteSession_DeleteButton_VisibleOnSessionDetailPage` — navigate to session detail, assert `.session-detail__delete` button is visible
  2. `DeleteSession_ClickDelete_ShowsConfirmationModal` — click delete button, assert `.discard-modal-backdrop` is visible
  3. `DeleteSession_CancelModal_KeepsSessionAndStaysOnPage` — open modal, click "Keep session", assert modal hidden, session detail page still shown, session still accessible via GET
  4. `DeleteSession_ConfirmDelete_RedirectsToHistoryWithBanner` — confirm deletion, assert redirected to `/history`, assert `.history-page__banner` visible with "Session deleted." text
  5. `DeleteSession_ConfirmedSession_AbsentFromHistoryList` — after deletion, assert the deleted session's workout name no longer appears in `.history-session` list

**Checkpoint**: User Story 1 is fully functional and independently testable. All 5 E2E tests and 4 integration tests pass.

---

## Phase 3: User Story 2 — Graceful Handling of Already-Deleted Sessions (Priority: P2)

**Goal**: Navigating directly to the URL of a deleted session shows "Session not found." rather than an error or blank screen.

**Independent Test**: After deleting a session, navigate directly to `/history/session?id={deletedId}` — assert "Session not found." is displayed.

**Note**: The `session-detail.ts` 404 handler already displays "Session not found." when `GET /api/sessions/{id}` returns 404. US2 is satisfied by the existing frontend 404 handling combined with the new DELETE endpoint; the only task here is E2E test coverage to prove the behaviour.

### Implementation for User Story 2

- [x] T010 [US2] Add 1 E2E test to `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`:
  - `DeleteSession_StaleUrl_ShowsSessionNotFound` — delete a session via API, navigate directly to `/history/session?id={deletedId}`, assert `.session-detail__error` or error region contains "Session not found."

**Checkpoint**: Stale session URLs degrade gracefully.

---

## Phase 4: Polish & Cross-Cutting Concerns

- [x] T011 Run full build and test suite to confirm no regressions:
  ```
  dotnet build src/WorkoutTracker.slnx
  cd src/WorkoutTracker.Web && npm run build && npm test
  dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj
  dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
  ```

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundational)**: No dependencies — start immediately
- **Phase 2 (US1)**: T003 and T004 can start as soon as Phase 1 is in progress (different files); T005–T009 require Phase 1 complete for end-to-end validation
- **Phase 3 (US2)**: Requires Phase 1 + Phase 2 (T005–T007) complete; T010 requires the DELETE endpoint and frontend 404 path both working
- **Phase 4 (Polish)**: Requires all prior phases complete

### Within Phase 2

```
T001 ──────────────────────────────────────────────────────┐
T002 [P] (write in parallel, different file) ──────────────┤
                                                            ▼
T003 [P] [US1] (write tests before verifying)       ── all complete
T004 [P] [US1] (CSS, different file)                ──     │
T005 [US1] ──── T006 [US1] ──── T007 [US1]         ──     │
T008 [P] [US1] (history.ts, different file)         ──     │
T009 [P] [US1] (E2E tests, different file)          ──     ▼
                                                     Checkpoint: US1 done
```

### Parallel Opportunities

Within Phase 1: T001 and T002 are in different files and can be written in parallel.  
Within Phase 2: T003, T004, T008, and T009 are all in different files from T005–T007 and can be developed in parallel.

---

## Parallel Example: Phase 2

```
# These can run in parallel (different files):
Task T003: backend tests in SessionApiTests.cs
Task T004: CSS in styles.css
Task T008: history.ts banner
Task T009: E2E tests in WorkoutHistoryTests.cs

# These must run sequentially (same file, session-detail.ts):
Task T005 → T006 → T007
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1 (T001, T002)
2. Complete Phase 2 (T003–T009)
3. **STOP and VALIDATE**: Run integration tests + E2E tests for US1
4. Deploy/demo if ready

### Incremental Delivery

1. Phase 1 → endpoint working
2. Phase 2 → full delete flow with modal + banner + tests (MVP!)
3. Phase 3 → stale URL E2E coverage
4. Phase 4 → verification pass

---

## Notes

- T001/T002 can be written together since they follow an identical pattern to the existing workout DELETE handlers
- The modal HTML in T005 MUST use `.discard-modal-backdrop` / `.discard-modal` CSS classes exactly — no new modal CSS required, only the delete-section and banner styles (T004)
- T007's `isDeleting` guard must reset in the `finally` block, not in success/error branches, to prevent stuck state on unexpected exceptions
- T008's `history.replaceState` must be called before generating the HTML string, so the banner conditional is evaluated with the clean URL state
- T010 (US2) can share the `CreateWorkoutAndSessionViaApiAsync` helper already present in `WorkoutHistoryTests.cs`, and call the API directly to delete before navigating
