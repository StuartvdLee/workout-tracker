# Research: Previous Exercise Performance in Active Workout

**Feature**: `008-workout-exercise-history`  
**Date**: 2026-05-06  
**Phase**: 0 — Resolve unknowns from Technical Context

---

## Decision 1: New endpoint vs. extending `GET /api/workouts/{workoutId}`

**Decision**: Add a new dedicated endpoint `GET /api/workouts/{workoutId}/previous-performance`.

**Rationale**: The existing `GET /api/workouts/{workoutId}` is consumed by both the active session page and the workouts management page (list/edit). Embedding previous performance data in that response would bloat every workout fetch with session history data, including cases where the data is never used (e.g., the workouts list). A separate endpoint keeps each caller's payload minimal and the responsibilities separated.

**Alternatives considered**:
- Extending `GET /api/workouts/{workoutId}`: rejected — adds unnecessary payload to all callers; tightly couples the workout template shape with session history.
- A separate `GET /api/workouts/{workoutId}/sessions/latest`: rejected — the word "sessions" implies returning the full session record; `previous-performance` is more precise about the purpose (summary of what to show in the UI).

---

## Decision 2: Query strategy for the most recent session

**Decision**: Use EF Core `OrderByDescending` on the `CompletedAt` shadow property, then `Take(1)`, with an `Include` of `LoggedExercises`.

```csharp
var lastSession = await db.WorkoutSessions
    .Where(ws => ws.PlannedWorkoutId == workoutId)
    .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
    .Include(ws => ws.LoggedExercises)
    .FirstOrDefaultAsync();
```

**Rationale**: `CompletedAt` is already used as a shadow property in the existing `GET /api/sessions` endpoint with `EF.Property<DateTime>(ws, "CompletedAt")`. Using the same pattern is consistent with the existing codebase. `FirstOrDefaultAsync()` returns `null` (no session yet) gracefully without throwing. The `Include` of `LoggedExercises` retrieves only the single most recent session's exercises, which is a bounded set.

**Alternatives considered**:
- Querying all sessions and selecting the max: rejected — more verbose and less efficient than `OrderByDescending(...).Take(1)`.
- Querying via a raw SQL or `FromSqlRaw`: rejected — unnecessary complexity when EF Core LINQ covers this cleanly.

---

## Decision 3: Response format

**Decision**: Return a flat response with `hasPreviousSession` and `exercises` array (keyed by `exerciseId`).

```json
{
  "hasPreviousSession": false,
  "exercises": []
}
```
```json
{
  "hasPreviousSession": true,
  "completedAt": "2026-05-01T10:00:00Z",
  "exercises": [
    { "exerciseId": "uuid", "loggedWeight": "80", "effort": 7 },
    { "exerciseId": "uuid", "loggedWeight": null, "effort": null }
  ]
}
```

**Rationale**: `hasPreviousSession: false` provides a clear, unambiguous signal to the frontend to show the "First session" state rather than inferring it from an empty array. `completedAt` is included as display context (could be used in future for "last performed X days ago"). The array mirrors the frontend need: iterate over the planned workout's exercises and look up each exerciseId to retrieve previous data. Null values for weight/effort are explicit and typed — the frontend handles them as "not recorded" rather than "not found".

**Alternatives considered**:
- Return a map/object keyed by `exerciseId`: rejected — JSON serialisation of dictionary keys in C# is straightforward but slightly less idiomatic; an array is simpler and consistent with how all other API responses in this project return exercise lists.
- Omit `hasPreviousSession` and rely on empty array: rejected — an empty array could also mean "no exercises logged" for a session that does exist; `hasPreviousSession` disambiguates.

---

## Decision 4: Frontend concurrent fetch strategy

**Decision**: Use `Promise.allSettled` to fetch workout data and previous-performance data concurrently. Render exercise inputs once both resolve. If previous-performance is rejected (network error or non-ok response), render the "Could not load previous data" error state per exercise and allow the session to proceed.

**Rationale**: `Promise.allSettled` (not `Promise.all`) ensures a previous-performance failure does not prevent the active session from loading — the workout data fetch succeeding is sufficient to render a usable page. This satisfies FR-009 from the spec. The two fetches are independent and can proceed concurrently without one blocking the other.

**Alternatives considered**:
- `Promise.all`: rejected — a failure in either fetch would reject the whole promise, blocking the session from rendering if previous data is unavailable.
- Sequential fetch (workout first, then previous-performance): rejected — unnecessary latency; both requests can be in-flight simultaneously.
- Show skeleton placeholders while previous data loads separately: rejected — unnecessary complexity for a single-user local setup where both requests complete within the same time window. `Promise.allSettled` with a single render pass is simpler.

---

## Decision 5: Previous data display placement and states

**Decision**: Within each `active-session__exercise-item`, insert a `<div class="active-session__exercise-previous">` section **above** the input fields. It shows one of four states:

| State | Content |
|-------|---------|
| Loading (initial skeleton — only if using sequential fetch) | n/a (not used; see Decision 4) |
| First session | `<span class="active-session__previous-empty">First session — no previous data</span>` |
| Previous data available | `<span>Last time: [weight] KG · Effort: [value] — [label]</span>` (or just weight, or just effort, depending on what was recorded) |
| Error (fetch failed) | `<span class="active-session__previous-error">Could not load previous data</span>` |

Partial data (weight recorded but no effort, or effort but no weight) is handled by conditionally including each segment — only non-null values are shown.

**Rationale**: Placing previous data above the inputs follows the natural reading order — reference first, then action. Using `--color-text-muted` (established in feature 001) for all previous-data text makes it visually secondary without requiring a new CSS variable. The BEM element name `active-session__exercise-previous` follows the established `active-session__*` namespace.

**Alternatives considered**:
- Display previous data below the inputs: rejected — the inputs are the primary action; reference data above reduces context-switching.
- Inline placeholder in each input: rejected — mixing read-only history values with editable input placeholders would confuse the affordance.

---

## Decision 6: No new `utils.ts` helper needed

**Decision**: The previous data formatting (weight + effort label) is done inline in `active-session.ts` — no new shared utility is extracted.

**Rationale**: The formatting is a single-use operation: build a display string per exercise for one page. The `getEffortLabel()` function in `utils.ts` already covers the effort label derivation. Extracting a `formatPreviousPerformance()` helper into `utils.ts` for a one-time rendering call would be over-engineering (constitution principle: avoid speculative abstractions).

**Alternatives considered**:
- Add `formatPreviousPerformance(weight, effort)` to `utils.ts`: rejected — single call site; the logic is two `if (value !== null)` checks; not worth abstracting.

---

## Decision 7: `WorkoutSession.PlannedWorkoutId` indexing

**Decision**: The query filters `WorkoutSessions` by `PlannedWorkoutId`. Verify that this column is indexed in the EF Core model. If not, this is a known performance note for the tasks file (not a blocker — single-user app with small data volumes).

**Rationale**: `PlannedWorkoutId` is a foreign key (`FK → planned_workout`). EF Core with Npgsql automatically creates an index on foreign key columns by convention. No manual index creation is needed.

**Alternatives considered**: N/A — FK index is automatic.

---

## Summary of All Decisions

| # | Decision | Outcome |
|---|----------|---------|
| 1 | New endpoint vs. extending existing | New `GET /api/workouts/{workoutId}/previous-performance` |
| 2 | Query strategy | `OrderByDescending(CompletedAt).FirstOrDefaultAsync()` with `Include(LoggedExercises)` |
| 3 | Response format | `{ hasPreviousSession, completedAt?, exercises[] }` |
| 4 | Frontend fetch strategy | `Promise.allSettled` — concurrent, failure-tolerant |
| 5 | Display placement and states | Above inputs; four states: first-session, data available, error |
| 6 | New utils.ts helper | None — inline in `active-session.ts` |
| 7 | FK index | Automatic via EF Core convention — no action needed |
