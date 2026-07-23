# API Contract: Edit Past Workouts

## New Endpoint: `PUT /api/sessions/{sessionId}`

Updates editable values on an existing completed workout session.

```http
PUT /api/sessions/{sessionId}
Content-Type: application/json
```

## Path Parameters

| Parameter | Type | Required | Description |
|---|---|---:|---|
| `sessionId` | GUID | Yes | Completed workout session to update |

## Request Body

```json
{
  "overallEffort": 7,
  "loggedExercises": [
    {
      "loggedExerciseId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "loggedWeight": "82.5",
      "effort": 8
    },
    {
      "loggedExerciseId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "loggedWeight": null,
      "effort": null
    }
  ]
}
```

| Field | Type | Required | Constraints |
|---|---|---:|---|
| `overallEffort` | integer \| null | No | 1-10 inclusive if provided; null clears/not rated |
| `loggedExercises` | array | Yes | May be empty when session has no exercises or only overall effort is edited |
| `loggedExercises[].loggedExerciseId` | GUID string | Yes | Must belong to `{sessionId}` |
| `loggedExercises[].loggedWeight` | string \| null | No | Null means intentionally empty; non-null value max 100 chars |
| `loggedExercises[].effort` | integer \| null | No | 1-10 inclusive if provided; null clears/not rated |

## Success Response - 200 OK

Returns the same session-detail shape as `GET /api/sessions/{sessionId}` after the update is saved.

```json
{
  "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "plannedWorkoutId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "workoutName": "Push Day",
  "completedAt": "2026-05-15T14:30:00Z",
  "overallEffort": 7,
  "previousOverallEffort": 6,
  "exercises": [
    {
      "loggedExerciseId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "exerciseId": "11111111-1111-1111-1111-111111111111",
      "exerciseName": "Bench Press",
      "loggedWeight": "82.5",
      "effort": 8,
      "previousWeight": "80",
      "previousEffort": 7
    }
  ]
}
```

## Error Responses

| Status | Condition | Body |
|---|---|---|
| `400 Bad Request` | request body is missing or not valid JSON | `{ "error": "A JSON request body is required." }` |
| `400 Bad Request` | `overallEffort` outside 1-10 | `{ "error": "Overall effort must be between 1 and 10." }` |
| `400 Bad Request` | exercise `effort` outside 1-10 | `{ "error": "Effort must be between 1 and 10." }` |
| `400 Bad Request` | `loggedWeight` exceeds 100 characters | `{ "error": "Logged weight must not exceed 100 characters." }` |
| `400 Bad Request` | duplicate `loggedExerciseId` in request | `{ "error": "Each logged exercise may only be included once." }` |
| `400 Bad Request` | a provided logged exercise does not belong to the target session | `{ "error": "One or more logged exercises are not part of this session." }` |
| `404 Not Found` | session does not exist | `{ "error": "Session not found." }` |
| `502 Bad Gateway` | web proxy cannot reach API | `{ "error": "API unavailable." }` |

## Update Semantics

- Only `WorkoutSession.OverallEffort`, `LoggedExercise.LoggedWeight`, and `LoggedExercise.Effort` are mutable.
- `WorkoutSessionId`, `PlannedWorkoutId`, `WorkoutName`, `CompletedAt`, `LoggedExerciseId`, `ExerciseId`, and `Sequence` are preserved.
- All requested row updates are saved in a single operation.
- Clearing a value is represented by `null`. The frontend normalizes cleared weight inputs to `null` before saving so read views can consistently display the existing no-data indicator.
- The response should be generated from the saved session state so current and previous comparison values reflect persisted data.

## Web Proxy Route

`WorkoutTracker.Web/Program.cs` adds a matching proxy route:

```http
PUT /api/sessions/{sessionId:guid}
```

Behavior:

- Reads the JSON body from the web request.
- Forwards it to `WorkoutTracker.Api` as `PUT /api/sessions/{sessionId}`.
- Returns the API response body, content type, and status code unchanged.
- Logs failures with `WebProxyLog.ProxyError(logger, $"PUT /api/sessions/{sessionId}", ex)`.
- Returns `502` with `{ "error": "API unavailable." }` if forwarding fails.

## Existing Endpoints Affected by Source Data

These endpoints do not need contract shape changes, but their returned values must reflect saved edits because they read the updated source rows:

- `GET /api/sessions`
- `GET /api/sessions/{sessionId}`
- `GET /api/workouts/{workoutId}/previous-performance`
- `GET /api/workouts/{workoutId}/session-trends`
