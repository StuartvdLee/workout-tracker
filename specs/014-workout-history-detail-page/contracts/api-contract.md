# API Contract: GET /api/sessions/{sessionId}

**Feature**: `014-workout-history-detail-page`
**Endpoint**: `GET /api/sessions/{sessionId}`
**Method**: GET
**Auth**: None (single-user application — consistent with all existing endpoints)

---

## Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `sessionId` | GUID | Yes | The unique identifier of the workout session to retrieve |

---

## Success Response — 200 OK

```json
{
  "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "plannedWorkoutId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "workoutName": "Push Day",
  "completedAt": "2026-05-15T14:30:00Z",
  "exercises": [
    {
      "loggedExerciseId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "exerciseName": "Bench Press",
      "loggedWeight": "80 KG",
      "effort": 7,
      "previousWeight": "77.5 KG",
      "previousEffort": 6
    },
    {
      "loggedExerciseId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "exerciseName": "Overhead Press",
      "loggedWeight": null,
      "effort": null,
      "previousWeight": null,
      "previousEffort": null
    }
  ]
}
```

### Field Notes

| Field | Nullable | Notes |
|-------|----------|-------|
| `workoutSessionId` | No | GUID string |
| `plannedWorkoutId` | Yes | `null` for ad-hoc sessions |
| `workoutName` | No | `WorkoutSession.WorkoutName` ?? `PlannedWorkout.Name` |
| `completedAt` | No | ISO 8601 UTC datetime string |
| `exercises` | No | Empty array `[]` if no exercises were logged |
| `exercises[].loggedExerciseId` | No | GUID string |
| `exercises[].exerciseName` | No | Resolved from `Exercise.Name` |
| `exercises[].loggedWeight` | Yes | `null` if not recorded |
| `exercises[].effort` | Yes | Integer 1–10; `null` if not recorded |
| `exercises[].previousWeight` | Yes | `null` if no prior session, no match, or ad-hoc |
| `exercises[].previousEffort` | Yes | `null` if no prior session, no match, or ad-hoc |

---

## Error Responses

### 404 Not Found

Returned when the `sessionId` does not match any session in the database.

```json
{ "error": "Session not found." }
```

---

## Previous Session Lookup Rules

1. **Ad-hoc session** (`plannedWorkoutId == null`): all `previousWeight` and `previousEffort` fields are `null`.
2. **No prior session**: same as above.
3. **Prior session found**: the most recently completed `WorkoutSession` for the same `plannedWorkoutId` where `CompletedAt ≤ session.CompletedAt` and `WorkoutSessionId ≠ sessionId`, ordered by `CompletedAt DESC` then `WorkoutSessionId DESC`.
4. **Exercise not in prior session**: `previousWeight` and `previousEffort` for that row are `null`.

---

## Web Proxy Route

`WorkoutTracker.Web/Program.cs` adds a proxy at the same path:

```
GET /api/sessions/{sessionId:guid}
```

Forwards to `WorkoutTracker.Api` at `GET /api/sessions/{sessionId}`.
Follows the same try/catch + `WebProxyLog.ProxyError` pattern as all other proxied routes.
