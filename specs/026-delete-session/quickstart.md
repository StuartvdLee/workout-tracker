# Quickstart: Delete Session

## What this feature adds

A "Delete session" button on the session detail page (below the chart) that permanently removes the session and redirects to the History page with a success banner.

---

## Testing manually

### Prerequisites
- App running locally via `dotnet run` or Aspire

### Steps

1. **Log a session**: Complete a workout via the app (or seed one via the API).
2. **Open the session**: Navigate to History → click the session entry → confirm the session detail page loads.
3. **Scroll to bottom**: The "Delete session" button appears below the chart.
4. **Click "Delete session"**: A confirmation modal appears.
5. **Cancel**: Click "Keep session" or press Escape → modal closes, session is unchanged.
6. **Delete**: Click "Delete session" again → confirm → you are redirected to the History page with a "Session deleted." banner.
7. **Verify removal**: The deleted session no longer appears in the History list.
8. **Verify stale URL**: Paste the old session URL (`/history/session?id=...`) into the browser → the page shows "Session not found."

---

## Build & test commands

```bash
# TypeScript build (strict, catches type errors)
cd src/WorkoutTracker.Web && npm run build

# Frontend unit tests
cd src/WorkoutTracker.Web && npm test

# Backend integration tests (requires PostgreSQL at TEST_DB_CONNECTION)
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj

# E2E tests (requires running app)
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```

---

## Key files changed

| File | Change |
|------|--------|
| `src/WorkoutTracker.Api/Program.cs` | Add `DELETE /api/sessions/{sessionId:guid}` |
| `src/WorkoutTracker.Web/Program.cs` | Add proxy `DELETE /api/sessions/{sessionId:guid}` |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` | Delete button, modal, fetch logic, `pendingDeleteSessionId` guard |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts` | Read `?deleted=1`, render banner |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | Full-width solid red delete button + banner styles |
| `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` | 4 new DELETE endpoint tests |
| `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` | 6 new E2E tests |
| `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs` | Add mock `DELETE /api/sessions/{sessionId}` endpoint |
