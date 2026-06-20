# Quickstart: Workout Overall Effort

**Feature**: `016-workout-overall-effort`
**Branch**: `016-workout-overall-effort`

---

## What This Feature Does

When you finish logging a workout and press **Save Workout**, a pop-up modal appears asking you to rate your overall effort on a 1–10 scale (the same scale used for individual exercises). You can rate your session and click **Save**, or press **Skip** to save without a rating.

The overall effort appears on the **Session Detail page** — a summary row below the exercises table shows the current session's overall effort alongside the previous session's overall effort.

---

## User Flows

### Flow 1: Rate Overall Effort at Save

1. Complete exercises in an active session.
2. Click **Save Workout**.
3. The "Overall Workout Effort" modal appears.
4. Drag the slider to your effort level (1–10). The label updates in real time (e.g., "Hard").
5. Click **Save** to record the rating and save the session.
   - _Or_ click **Skip** (or press Escape / click backdrop) to save without a rating.
6. The session is saved and you are redirected to the workout history page.

### Flow 2: View Effort Comparison on Session Detail

1. From the **Workout History** page, click on a session card to open the detail view.
2. The session detail page shows the exercises table as before.
3. Below the table, a summary row always shows:
   - **Overall Effort**: `8 · All Out` (or `—` if no effort was recorded)
   - **Previous**: `6 · Moderate` (or `—` if no prior session, or prior session has no effort)
4. If the current session has no overall effort: **Overall Effort** shows `—`.
5. If there is no previous session (e.g., first time doing this workout, or an ad-hoc session): **Previous** shows `—`.

---

## Implementation Overview

### Backend

1. **`WorkoutSession.cs`**: Add `public int? OverallEffort { get; set; }`.
2. **`WorkoutTrackerDbContext.cs`**: Add check constraint for `overall_effort` range (1–10) in `OnModelCreating`.
3. **EF Migration**: Generate migration adding `overall_effort` nullable integer column with the check constraint.
4. **`Program.cs`**:
   - `SessionCreateRequest`: Add `public int? OverallEffort { get; set; }`.
   - POST `/api/workouts/{id}/sessions`: Validate range, assign to session, include in 201 response.
   - GET `/api/sessions`: Include `OverallEffort` in projection.
   - GET `/api/sessions/{id}`: Include `OverallEffort` in response; include `PreviousOverallEffort` from prior session projection.

### Frontend

5. **`active-session.ts`**: Add effort modal HTML, intercept save button to open modal, handle confirm/skip to call `handleSave(overallEffort)`, reset modal state on open.
6. **`history.ts`**: No net change — effort display was implemented then removed (too cluttered).
7. **`session-detail.ts`**: Add `overallEffort` + `previousOverallEffort` to `SessionDetailWithPrevious` interface; always render summary row below exercises table in `renderDetailTable()`, using "—" for null values.
8. **`styles.css`**: Add CSS for `.effort-modal*` and `.session-detail__overall-effort-row` and children. (`.history-session__overall-effort` and `.history-session__meta` were added then removed.)

### Tests

9. **`SessionApiTests.cs`**: 8 new integration tests covering save with/without effort, validation, and detail retrieval with/without previous.
10. **`WorkoutHistoryTests.cs`**: 5 new E2E tests covering modal appearance, skip, confirm, and session detail display. (History-page display tests were added then removed.)

---

## Key Commands

```bash
# Apply migration (run once after adding the migration file)
cd src && dotnet ef database update --project WorkoutTracker.Infrastructure --startup-project WorkoutTracker.Api

# Build
dotnet build src/WorkoutTracker.slnx

# Frontend build
cd src/WorkoutTracker.Web && npm run build

# Backend tests (requires PostgreSQL at TEST_DB_CONNECTION)
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj

# Frontend tests
cd src/WorkoutTracker.Web && npm test

# E2E tests (requires running Aspire app)
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```
