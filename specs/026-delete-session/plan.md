# Implementation Plan: Delete Session

**Branch**: `026-delete-session` | **Date**: 2026-05-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/026-delete-session/spec.md`

## Summary

Add a "Delete session" button to the session detail page (positioned below the exercise chart), guarded by a confirmation modal that reuses the established `.discard-modal-backdrop` / `.discard-modal` pattern. Confirming deletion calls a new `DELETE /api/sessions/{sessionId}` endpoint, which removes the `WorkoutSession` and its child `LoggedExercise` records (via the existing cascade-delete FK constraint — no migration needed), then navigates to the History page with a `?deleted=1` query param. The History page reads this param, shows a brief "Session deleted" success banner, and cleans up the URL via `history.replaceState`. Cancelling the modal keeps the user on the session detail page. Network failures surface an error message without navigating away.

## Technical Context

**Language/Version**: C# on .NET 10 (backend), TypeScript ~6.0.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
**Storage**: PostgreSQL via EF Core — **no schema changes**. `LoggedExercise` already has `OnDelete(DeleteBehavior.Cascade)` on its `WorkoutSessionId` FK; deleting a `WorkoutSession` automatically removes all child rows.
**Testing**: xUnit + WebApplicationFactory integration tests (real PostgreSQL); Playwright E2E tests
**Target Platform**: Web browser (mobile-first, responsive)
**Project Type**: Web application (SPA with .NET Aspire orchestration)
**Performance Goals**: Delete operation is a single keyed lookup + cascading removal; response time is well within the budget of existing data-mutating actions.
**Constraints**: Strict TypeScript (noUnusedLocals, noUnusedParameters, noImplicitReturns); BEM CSS; no external JS frameworks; existing tests must continue to pass; `CompletedAt` accessed via `EF.Property<DateTime>` where needed.
**Scale/Scope**: Touches `Program.cs` (API), `Program.cs` (Web proxy), `session-detail.ts`, `history.ts`, `styles.css`, `SessionApiTests.cs`, `WorkoutHistoryTests.cs`; no migrations.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: New API endpoint follows the inline anonymous-type projection pattern already used for muscles/exercises/workouts DELETE handlers. Web proxy follows the established `try/catch` proxy pattern. TypeScript changes use only `const`, typed interfaces, and no `any`. BEM naming: new block `.session-detail__delete-section`, modifier `.session-detail__delete`; history success banner `.history-page__banner`. Confirmation modal reuses `.discard-modal-backdrop` / `.discard-modal` CSS classes exactly as in features 021–023. No speculative abstractions. ✅

- **Testing**:
  - **Backend integration tests** (`SessionApiTests.cs`): New tests for `DELETE /api/sessions/{sessionId}`:
    1. Returns 204 and session is no longer retrievable via `GET /api/sessions/{sessionId}` (404)
    2. Returns 204 and session no longer appears in `GET /api/sessions` list
    3. Returns 204 and all associated `LoggedExercise` records are removed (cascade)
    4. Returns 404 with error JSON when session does not exist
  - **E2E (Playwright / `WorkoutHistoryTests.cs`)**: New tests:
    1. Delete button is visible on the session detail page below the chart area
    2. Clicking delete shows the confirmation modal
    3. Cancelling the modal keeps the user on the session detail page; session is still retrievable
    4. Confirming deletion redirects to the History page and shows "Session deleted" banner
    5. Deleted session no longer appears in the History list after deletion
    6. Navigating directly to the URL of a deleted session shows "Session not found"
  - Tests are mandatory per Constitution II. ✅

- **Security**: The new `DELETE /api/sessions/{sessionId:guid}` endpoint uses a GUID route constraint — ASP.NET Core's route binding rejects non-GUID values with 404 automatically. EF Core parameterises the GUID, preventing SQL injection. Single-user app — no cross-user session leakage is structurally possible (consistent with SR-002 exception documented across features 008, 013, 014, 016, 025). No new secrets, integrations, or trust boundaries introduced. Error responses return only a static string — no session data leaked. ✅

- **User Experience Consistency**: Confirmation modal reuses `.discard-modal-backdrop` / `.discard-modal` classes, button labels ("Delete" for destructive action, "Keep session" for safe action), `role="alertdialog"`, `aria-modal="true"`, `aria-labelledby`, `aria-describedby`, focus management (focus sent to Delete button on open; Escape dismisses; Tab trapped within modal). This is identical to the pattern established in features 021–023 and `active-session.ts`. Delete button follows the destructive-action visual convention. All four states defined: loading (button disabled + indicator), success (redirect + banner), error (inline error message), cancel (no change). Success banner on History page follows the same error/status message convention. ✅

- **Performance**: Delete is a single keyed lookup followed by a cascading removal — EF Core issues one `DELETE` statement per cascade level, all within a single `SaveChangesAsync()` call. No N+1, no aggregations. The redirect to the History page is an existing route render. ✅

## Project Structure

### Documentation (this feature)

```text
specs/026-delete-session/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # DELETE /api/sessions/{sessionId} endpoint contract
│   └── ui-contract.md   # Delete button, confirmation modal, success banner contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                                   MODIFIED: add DELETE /api/sessions/{sessionId:guid}

src/WorkoutTracker.Web/
├── Program.cs                                   MODIFIED: add proxy DELETE /api/sessions/{sessionId:guid}
└── wwwroot/
    ├── css/
    │   └── styles.css                           MODIFIED: add .session-detail__delete-section,
    │                                                       .session-detail__delete (btn),
    │                                                       .history-page__banner BEM styles
    └── ts/
        └── pages/
            ├── session-detail.ts                MODIFIED: add delete button below chart,
            │                                              confirmation modal HTML + logic,
            │                                              DELETE fetch, navigate on success,
            │                                              error display on failure,
            │                                              double-submit guard (isDeleting flag)
            └── history.ts                       MODIFIED: read ?deleted=1 on render,
                                                           show/hide success banner,
                                                           replaceState to clean URL

src/WorkoutTracker.UnitTests/
└── Api/
    └── SessionApiTests.cs                       MODIFIED: add DELETE endpoint tests (4 new tests)

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs                   MODIFIED: add delete flow E2E tests (6 new tests)
```

**Structure Decision**: Web application pattern (ASP.NET Core + Aspire + vanilla TypeScript SPA). No new projects or files. All changes are surgical additions to existing files. No DB migration — cascade delete already configured.

## Key Implementation Details

### DELETE /api/sessions/{sessionId:guid} — API (Program.cs)

Follows the established DELETE handler pattern verbatim:

```csharp
app.MapDelete("/api/sessions/{sessionId:guid}", async (Guid sessionId, WorkoutTrackerDbContext db) =>
{
    var session = await db.WorkoutSessions
        .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId);

    if (session is null)
        return Results.Json(new { error = "Session not found." }, statusCode: 404);

    db.WorkoutSessions.Remove(session);
    await db.SaveChangesAsync();

    return Results.NoContent();
});
```

`LoggedExercise` rows are removed automatically by the existing `OnDelete(DeleteBehavior.Cascade)` constraint — no explicit Include or manual removal needed.

### DELETE /api/sessions/{sessionId:guid} — Web Proxy (Program.cs)

Follows the established proxy pattern from `DELETE /api/workouts/{workoutId:guid}`:

```csharp
app.MapDelete("/api/sessions/{sessionId:guid}", async (Guid sessionId, ILogger<Program> logger, IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var client = httpClientFactory.CreateClient("api");
        var response = await client.DeleteAsync($"/api/sessions/{sessionId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return Results.NoContent();
        var responseContent = await response.Content.ReadAsStringAsync();
        return Results.Content(responseContent, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        WebProxyLog.ProxyError(logger, $"DELETE /api/sessions/{sessionId}", ex);
        return Results.Json(new { error = "API unavailable." }, statusCode: 502);
    }
});
```

### Delete Button + Modal Placement (session-detail.ts)

The delete section is appended to `#session-detail-content` after the existing table and overall-effort row, matching the user's requirement ("below the chart"). It is rendered only once, when session detail loads successfully, alongside the chart section.

```html
<div class="session-detail__delete-section">
  <button class="session-detail__delete" id="session-detail-delete" type="button">
    Delete session
  </button>
  <div class="session-detail__error" id="session-detail-delete-error"
       role="alert" aria-live="polite" style="display:none;"></div>
</div>
```

### Confirmation Modal (session-detail.ts)

Reuses `.discard-modal-backdrop` / `.discard-modal` CSS classes (no new CSS for the modal structure). IDs follow the established per-page naming convention:

```html
<div class="discard-modal-backdrop" id="session-delete-confirm-backdrop" style="display:none;">
  <div class="discard-modal" role="alertdialog" aria-modal="true"
       aria-labelledby="session-delete-confirm-title"
       aria-describedby="session-delete-confirm-desc">
    <h2 class="discard-modal__title" id="session-delete-confirm-title">Delete session?</h2>
    <p class="discard-modal__desc" id="session-delete-confirm-desc">
      This will permanently remove this session and cannot be undone.
    </p>
    <div class="discard-modal__actions">
      <button class="discard-modal__discard" type="button" id="session-delete-confirm-ok">Delete</button>
      <button class="discard-modal__continue" type="button" id="session-delete-confirm-cancel">Keep session</button>
    </div>
  </div>
</div>
```

### isDeleting guard (session-detail.ts)

```typescript
let isDeleting = false;

async function handleDeleteConfirmed(sessionId: string): Promise<void> {
  if (isDeleting) return;
  isDeleting = true;

  const deleteBtn = document.getElementById("session-detail-delete") as HTMLButtonElement | null;
  const errorEl = document.getElementById("session-detail-delete-error") as HTMLElement | null;
  if (deleteBtn) deleteBtn.disabled = true;

  try {
    const response = await fetch(`/api/sessions/${encodeURIComponent(sessionId)}`, { method: "DELETE" });
    if (response.ok || response.status === 404) {
      // 404 means already gone — treat as success
      navigate("/history?deleted=1");
      return;
    }
    throw new Error(`Delete failed: ${response.status}`);
  } catch {
    if (errorEl) {
      errorEl.textContent = "Failed to delete session. Please try again.";
      errorEl.style.display = "";
    }
    if (deleteBtn) deleteBtn.disabled = false;
  } finally {
    isDeleting = false;
  }
}
```

### Success Banner (history.ts)

```typescript
export async function render(container: HTMLElement): Promise<void> {
  const params = new URLSearchParams(window.location.search);
  const showDeletedBanner = params.get("deleted") === "1";

  if (showDeletedBanner) {
    history.replaceState(null, "", "/history");
  }

  container.innerHTML = `
    <div class="history-page">
      <h1 class="history-page__title">Workout History</h1>
      ${showDeletedBanner ? `<p class="history-page__banner" role="status">Session deleted.</p>` : ""}
      ...
    </div>
  `;
  ...
}
```

`role="status"` (assertive-lite) announces the message to screen readers without interrupting. The banner is part of the page structure and disappears on the next navigation — no timer needed.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts are complete.*

- **Code Quality** ✅ — No new projects. Surgical additions to existing files. All patterns copied verbatim from established conventions. Strict TypeScript enforced throughout. `isDeleting` guard prevents double-submit. No `any`, no dead code, BEM naming consistent.
- **Testing** ✅ — 4 backend integration tests cover the DELETE endpoint (204, 404, cascade, removal from list). 6 E2E tests cover the full user journey. All mandatory.
- **Security** ✅ — GUID route constraint + EF parameterisation prevent injection. Static error messages. Single-user SR-002 exception documented consistently with prior features.
- **User Experience Consistency** ✅ — Modal reuses `.discard-modal-backdrop` / `.discard-modal` classes and focus management pattern from features 021–023. Delete button follows destructive-action convention. All four states (loading, success, error, cancel) defined. Success banner uses `role="status"`.
- **Performance** ✅ — Single keyed DELETE with cascading removal. No N+1. No heavy queries.

No violations.

