# API Contract: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Date**: 2026-05-27

## New Endpoint: `GET /api/workouts/{workoutId}/session-trends`

Returns up to 50 most-recent sessions for a planned workout, ordered chronologically (oldest first), for chart rendering.

### Request

```
GET /api/workouts/{workoutId}/session-trends
```

| Parameter | Location | Type | Required | Notes |
|---|---|---|---|---|
| `workoutId` | Path | GUID | Yes | Planned workout ID |

No body. No query parameters.

### Responses

#### 200 OK

```json
{
  "dataPoints": [
    {
      "completedAt": "2026-04-01T09:30:00Z",
      "overallEffort": 7,
      "exercises": [
        {
          "exerciseId": "a1b2c3d4-...",
          "exerciseName": "Bench Press",
          "loggedWeight": "80",
          "effort": 6
        }
      ]
    }
  ]
}
```

If the workout exists but has no sessions:

```json
{ "dataPoints": [] }
```

#### 404 Not Found

```json
{ "error": "Workout not found." }
```

### Implementation Notes

- Query uses `OrderByDescending(EF.Property<DateTime>(ws, "CompletedAt"))`, `ThenByDescending(ws.WorkoutSessionId)`, `Take(50)`, then in-memory reverse to chronological.
- `CompletedAt` is a shadow property and must be accessed via `EF.Property<DateTime>(ws, "CompletedAt")`.
- Each data point includes session `overallEffort` and per-exercise `exerciseId`, `exerciseName`, `loggedWeight`, `effort`.

### Web Proxy

`WorkoutTracker.Web` proxies the same route:

```
GET /api/workouts/{workoutId}/session-trends
→ GET /api/workouts/{workoutId}/session-trends (API)
```
