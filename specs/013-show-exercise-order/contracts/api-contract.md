# API Contract: Previous Exercise Order Indicator in Active Workout

**Feature**: `013-show-exercise-order`  
**Date**: 2026-05-18

## Overview

This contract documents the single change to the existing `GET /api/workouts/{workoutId}/previous-performance` endpoint introduced by feature 013. One new field (`sequence`) is added to each exercise entry in the response. All other fields and endpoint behaviour are unchanged from feature 008.

---

## Modified Endpoint: GET `/api/workouts/{workoutId}/previous-performance`

### Change Summary

The `exercises` array items now include a `sequence` field: the 0-based position the exercise occupied in the most recently completed session. The frontend converts this to 1-based for display (`#1`, `#2`, etc.).

### Request

Unchanged from feature 008.

```
GET /api/workouts/{workoutId}/previous-performance
```

| Parameter   | Type | Location | Required | Notes |
|-------------|------|----------|----------|-------|
| `workoutId` | uuid | path     | Yes      | The `plannedWorkoutId` of the workout being started |

No request body.

### Response: 200 OK — No Previous Session

Unchanged from feature 008.

```json
{
  "hasPreviousSession": false,
  "completedAt": null,
  "exercises": []
}
```

### Response: 200 OK — Previous Session Found

```json
{
  "hasPreviousSession": true,
  "completedAt": "2026-05-01T10:30:00Z",
  "exercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "effort": 7,
      "sequence": 0
    },
    {
      "exerciseId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "loggedWeight": null,
      "effort": null,
      "sequence": 1
    },
    {
      "exerciseId": "1c2d3e4f-5a6b-7c8d-9e0f-1a2b3c4d5e6f",
      "loggedWeight": "60",
      "effort": null,
      "sequence": 2
    }
  ]
}
```

### Response Fields

| Field | Type | Notes |
|-------|------|-------|
| `hasPreviousSession` | boolean | `true` if at least one completed session exists for this planned workout |
| `completedAt` | ISO 8601 datetime or null | Completion timestamp of the most recent session |
| `exercises` | array | Per-exercise data from the most recent session; empty array when `hasPreviousSession` is false |
| `exercises[].exerciseId` | uuid | Maps to the exercise in the planned workout |
| `exercises[].loggedWeight` | string or null | Weight recorded in the previous session, or null if not recorded |
| `exercises[].effort` | integer (1–10) or null | Effort rating recorded in the previous session, or null if not recorded |
| `exercises[].sequence` | **integer (0-based) or null** | **NEW** Position of the exercise in the previous session (0 = first). `null` for sessions saved before sequence tracking was introduced |

### Sequence Field Rules

- `sequence` is a 0-based integer. The frontend displays it as 1-based (`#1`, `#2`, etc.) by adding 1.
- `sequence: null` means the position was not recorded for this session (e.g., session pre-dates this feature). The frontend omits the `#x` indicator for null sequences — it does not show a placeholder.
- `sequence` values in a well-formed session form a contiguous range starting at 0 (0, 1, 2, …). The frontend does not validate this; it uses the value as-is.

### Error Responses

Unchanged from feature 008.

| Status | Condition | Body |
|--------|-----------|------|
| 404 | `workoutId` does not exist | `{ "error": "Workout not found." }` |
| 400 | `workoutId` is not a valid GUID | ASP.NET Core default model binding error |

---

## Unchanged Endpoints

All other endpoints are unaffected by this feature.
