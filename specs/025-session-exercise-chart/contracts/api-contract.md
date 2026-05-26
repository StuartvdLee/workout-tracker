# API Contract: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Date**: 2026-05-26

## New Endpoint: GET /api/workouts/{workoutId}/session-trends

Returns up to 50 most-recent completed sessions for a given planned workout, ordered chronologically (oldest first), with per-session overall effort and per-exercise weight and effort data. Intended for client-side chart rendering.

### Request

```
GET /api/workouts/{workoutId}/session-trends
```

| Parameter   | Location | Type | Required | Notes                     |
|-------------|----------|------|----------|---------------------------|
| workoutId   | Path     | GUID | Yes      | The planned workout's ID  |

No request body. No query parameters.

### Responses

#### 200 OK — sessions found (or workout exists but has no sessions)

```json
[
  {
    "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "completedAt": "2026-04-01T09:30:00Z",
    "overallEffort": 7,
    "exercises": [
      {
        "exerciseId": "a1b2c3d4-...",
        "exerciseName": "Bench Press",
        "loggedWeight": "80",
        "effort": 6
      },
      {
        "exerciseId": "e5f6a7b8-...",
        "exerciseName": "Squat",
        "loggedWeight": "100",
        "effort": 8
      }
    ]
  },
  {
    "workoutSessionId": "4fb96a75-...",
    "completedAt": "2026-04-08T09:45:00Z",
    "overallEffort": null,
    "exercises": [
      {
        "exerciseId": "a1b2c3d4-...",
        "exerciseName": "Bench Press",
        "loggedWeight": "82.5",
        "effort": null
      }
    ]
  }
]
```

Returns an empty array `[]` when the workout exists but has no recorded sessions.

#### 404 Not Found — workout does not exist

```json
{ "error": "Workout not found." }
```

### Implementation Notes

- Results capped at 50 sessions: `OrderByDescending(CompletedAt).Take(50)`, then reversed in-memory to chronological order for the response array.
- `CompletedAt` MUST be accessed via `EF.Property<DateTime>(ws, "CompletedAt")` — it is a shadow property with no CLR counterpart.
- Exercises within each session are ordered by `Sequence` to maintain consistent ordering.
- `exerciseName` is taken from the `Exercise` navigation property (`le.Exercise.Name`), consistent with other session endpoints.
- No authentication is required (single-user app, consistent with features 008, 013, 014, 016).

### Web Proxy

The WorkoutTracker.Web project proxies this endpoint:

```
GET /api/workouts/{workoutId}/session-trends
→ proxied to WorkoutTracker.Api GET /api/workouts/{workoutId}/session-trends
```

Route registration in `WorkoutTracker.Web/Program.cs` follows the existing `MapGet` proxy pattern used for other `/api/workouts/{workoutId}/*` routes.

---

## Existing Endpoints (unchanged)

The following existing endpoints are read by `session-detail.ts` and remain unchanged:

| Endpoint                            | Usage                                              |
|-------------------------------------|----------------------------------------------------|
| `GET /api/sessions/{sessionId}`     | Loads session details; provides `plannedWorkoutId` |
