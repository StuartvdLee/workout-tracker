# Research: Workout History Detail Page

**Feature**: `014-workout-history-detail-page`
**Branch**: `014-workout-history-detail-page`

---

## R-001: Router Pattern for Session Detail Page

**Question**: How should the session ID be passed to the detail page given the current exact-path router?

**Decision**: Use the `/history/session` route with `?id=<sessionId>` query param.

**Rationale**: The router in `router.ts` performs exact-path matching (`routes.find(r => r.path === path)`). It already strips query strings before matching. The session ID is therefore passed as `?id=<sessionId>`, read in the page module via `new URLSearchParams(window.location.search).get("id")`. This is identical to how `active-session.ts` reads `?id=<workoutId>` — zero changes to the router are required.

**Alternatives considered**:
- Path segments (`/history/<sessionId>`): Would require a regex/prefix router change — adds scope and risk.
- Fragment hash (`#session-<id>`): Inconsistent with existing routing model.

---

## R-002: API Design — Single Endpoint vs. Two Calls

**Question**: Should the detail page make two API calls (one for the session, one for previous data), or should there be a single endpoint that returns everything?

**Decision**: A single new endpoint `GET /api/sessions/{sessionId}` that returns session exercises merged with previous-session data.

**Rationale**: The detail page needs exactly one piece of data: the five-column table. Fetching it in one call eliminates a waterfall request and avoids a second loading state. The backend already has the query pattern from `GET /api/workouts/{workoutId}/previous-performance`. Merging both queries in one handler is straightforward and keeps the frontend simple.

**Response shape**:
```json
{
  "workoutSessionId": "guid",
  "plannedWorkoutId": "guid | null",
  "workoutName": "string",
  "completedAt": "ISO 8601",
  "exercises": [
    {
      "loggedExerciseId": "guid",
      "exerciseName": "string",
      "loggedWeight": "string | null",
      "effort": "number | null",
      "previousWeight": "string | null",
      "previousEffort": "number | null"
    }
  ]
}
```

**Alternatives considered**:
- Reuse `GET /api/sessions` (returns all sessions): Overfetch — returns all session data when only one is needed.
- Two calls (`GET /api/sessions/{id}` + `GET /api/sessions/{id}/previous`): Doubles requests; waterfall if sequential.

---

## R-003: Previous-Session Lookup Scope

**Question**: The `previous-performance` endpoint always returns the *most recent* session for a workout. For the detail page, the viewed session may not be the most recent one. How should "previous" be defined?

**Decision**: "Previous" means the most recently completed session for the same `plannedWorkoutId` that has a `CompletedAt` earlier than the viewed session (using `ThenByDescending(WorkoutSessionId)` as a tie-breaker, consistent with existing sort conventions).

**Rationale**: If a user views an older session, they should see the session that preceded it — not the current most-recent session. This gives historically accurate comparison data.

**Query logic** (C#):
```csharp
var priorSession = await db.WorkoutSessions
    .Where(ws =>
        ws.PlannedWorkoutId == session.PlannedWorkoutId &&
        ws.WorkoutSessionId != sessionId &&
        EF.Property<DateTime>(ws, "CompletedAt") <= EF.Property<DateTime>(session, "CompletedAt"))
    .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
    .ThenByDescending(ws => ws.WorkoutSessionId)
    .Select(ws => ws.LoggedExercises.Select(le => new { le.ExerciseId, le.LoggedWeight, le.Effort }).ToList())
    .FirstOrDefaultAsync();
```

**Edge cases**:
- `plannedWorkoutId == null` (ad-hoc session): No prior session lookup — all previous fields are `null`.
- No prior session found: All previous fields are `null`.
- Tie on `CompletedAt`: `ThenByDescending(WorkoutSessionId)` provides deterministic ordering.

---

## R-004: CSS Table Design

**Question**: Should the detail table use `<table>` HTML or CSS grid/flexbox div rows?

**Decision**: Use a `<table>` with `<thead>` and `<tbody>`, styled to match the existing card/list aesthetic using existing CSS custom properties.

**Rationale**: Five structured columns with header labels are semantically a table. The existing app uses `display: flex` for list items but a real `<table>` is more accessible for screen readers and provides inherent column alignment. The table is styled with `border-collapse: collapse` and the same border/background tokens used elsewhere.

**CSS variables to use**:
- Background: `var(--color-white)`
- Border: `var(--color-border)`
- Text: `var(--color-text)` (current values), `var(--color-text-light)` (previous/header values)
- Padding: `var(--spacing-sm)` / `var(--spacing-md)`
- Border radius: `var(--radius)`

---

## R-005: History Entry Interaction After Removal of Expand

**Question**: The history entry currently uses `<button class="history-session__header">`. Should it remain a `<button>` or become an `<a>` tag after the interaction changes to navigation?

**Decision**: Change the entry to use `navigate()` from the router on click, keeping it as a `<button>` with a `role="link"` — or alternatively render it as a `<button>` that calls `navigate('/history/session?id=...')`.

**Rationale**: The router's `navigate()` function handles SPA navigation without full page reloads. Using a `<button>` with an `onclick` calling `navigate()` is consistent with the existing pattern (e.g., `workouts.ts` start button calls `navigate()`). The toggle icon (`▸`) and `aria-expanded` attribute are removed; `aria-controls` is also removed.

**Simplified entry rendering** — the entry card becomes a `<button>` that navigates, showing only name + date + exercise count (no toggle).

---

## R-007: Duplicate Exercise Matching Strategy

**Question**: When the same exercise appears more than once in a session (e.g., Bench Press logged twice with different weights), how should the "previous" columns be populated?

**Decision**: Use **first-match by `ExerciseId`** — each row in the current session independently looks up the first `LoggedExercise` with a matching `ExerciseId` in the prior session's list. All duplicate rows in the current session will map to the same single prior-session row and show identical previous values.

**Rationale**: The edge case is uncommon — the planned workout builder does not expose a UI for adding the same exercise twice. Implementing sequence-index matching (row 1 of current → row 1 of prior, row 2 → row 2) would require ordered pairing logic that adds complexity without meaningful benefit for this workflow. The simpler first-match approach is consistent with the `008-workout-exercise-history` pattern and avoids introducing new abstractions.

**Accepted limitation**: If the same exercise genuinely appears multiple times in both sessions, every current-session row for that exercise will show the same previous values. This is documented here and in T002 so the implementer does not introduce more complex logic.

**Alternatives considered**:
- Sequence-index pairing: Match the nth occurrence in the current session to the nth occurrence in the prior session. More accurate for deliberate duplicates, but adds ordering and out-of-bounds logic.
- No previous values for duplicate exercises: Show "—" if any duplicate is detected. Conservative but loses useful data for the common case where the exercise is not duplicated.


**Confirmed tests to be replaced** (they assert the old expand/collapse behaviour):

| Test | Action |
|------|--------|
| `HistoryPage_SessionExpandCollapse` | REPLACE with `HistoryPage_SessionEntry_NavigatesToDetailPage` |
| `HistoryPage_SessionDetails_ShowsExerciseData` | REPLACE with `SessionDetailPage_ShowsExerciseTable` |

**New tests to add**:

| Test | What it verifies |
|------|-----------------|
| `HistoryPage_SessionEntry_NavigatesToDetailPage` | Clicking an entry navigates to `/history/session?id=...` |
| `SessionDetailPage_ShowsExerciseTable` | Detail page shows a table with exercise name, weight, effort |
| `SessionDetailPage_BackNavigation_ReturnsToHistory` | Back button/link returns to History page |
| `SessionDetailPage_ShowsPreviousData_WhenPriorSessionExists` | Previous weight/effort columns populated after 2nd session |

**Tests to retain unchanged**:
- `HistoryPage_EmptyState_ShowsMessage`
- `HistoryPage_LoadingState_ShownInitially`
- `HistoryPage_WithSessions_ShowsSessions`
- `HistoryPage_NoGroupHeaders_FlatList`
- `HistoryPage_EntryShowsDateBelowName`
- `HistoryPage_HasH1Heading`
