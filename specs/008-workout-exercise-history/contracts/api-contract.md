# API Contract: Previous Exercise Performance in Active Workout

**Feature**: `008-workout-exercise-history`  
**Date**: 2026-05-06

## Overview

This contract documents the single new endpoint introduced by feature 008. All existing endpoints defined in features 004 and 005 are unchanged.

---

## New Endpoint: GET `/api/workouts/{workoutId}/previous-performance`

Returns the weight and effort recorded for each exercise in the most recently completed session for the given planned workout. This is a read-only endpoint used by the active session view to surface reference data before the user starts logging.

### Request

```
GET /api/workouts/{workoutId}/previous-performance
```

| Parameter  | Type | Location | Required | Notes |
|------------|------|----------|----------|-------|
| `workoutId` | uuid | path | Yes | The `plannedWorkoutId` of the workout being started |

No request body.

### Response: 200 OK — No Previous Session

Returned when the planned workout exists but has never been completed.

```json
{
  "hasPreviousSession": false,
  "completedAt": null,
  "exercises": []
}
```

### Response: 200 OK — Previous Session Found

Returned when at least one completed session exists for this planned workout. Always reflects the **single most recently completed** session.

```json
{
  "hasPreviousSession": true,
  "completedAt": "2026-05-01T10:30:00Z",
  "exercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "effort": 7
    },
    {
      "exerciseId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "loggedWeight": null,
      "effort": null
    },
    {
      "exerciseId": "1c2d3e4f-5a6b-7c8d-9e0f-1a2b3c4d5e6f",
      "loggedWeight": "60",
      "effort": null
    }
  ]
}
```

### Response Fields

| Field | Type | Notes |
|-------|------|-------|
| `hasPreviousSession` | boolean | `true` if at least one completed session exists for this planned workout |
| `completedAt` | ISO 8601 datetime or null | Completion timestamp of the most recent session; null when `hasPreviousSession` is false |
| `exercises` | array | Per-exercise data from the most recent session; empty array when `hasPreviousSession` is false |
| `exercises[].exerciseId` | uuid | Maps to the exercise in the planned workout |
| `exercises[].loggedWeight` | string or null | Weight string recorded in the previous session, or null if not recorded |
| `exercises[].effort` | integer (1–10) or null | Effort rating recorded in the previous session, or null if not recorded |

### Scoping Rule

> The `exercises` array contains only exercises logged in the most recent session for **this specific planned workout**. Exercises from sessions belonging to other planned workouts are never included, even if they share the same `exerciseId`.

### Response: 404 Not Found

Returned when the `workoutId` does not correspond to an existing planned workout.

```json
{
  "error": "Workout not found."
}
```

### Error Responses

| Status | Condition | Body |
|--------|-----------|------|
| 404 | `workoutId` does not exist | `{ "error": "Workout not found." }` |
| 400 | `workoutId` is not a valid GUID | ASP.NET Core default model binding error |

---

## Unchanged Endpoints

All endpoints from features 004 and 005 are unaffected:
- `GET /api/workouts`
- `GET /api/workouts/{workoutId}`
- `POST /api/workouts`
- `PUT /api/workouts/{workoutId}`
- `DELETE /api/workouts/{workoutId}`
- `POST /api/workouts/{workoutId}/sessions`
- `GET /api/sessions`
