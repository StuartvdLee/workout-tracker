# API Contract: Workout Overall Effort

**Feature**: `016-workout-overall-effort`

---

## Changed Endpoints

### POST /api/workouts/{workoutId}/sessions

**Change**: Adds optional `overallEffort` field to the request body. Adds `overallEffort` to the 201 response.

#### Request

```http
POST /api/workouts/{workoutId}/sessions
Content-Type: application/json
```

```json
{
  "loggedExercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80 KG",
      "notes": "felt strong",
      "effort": 8
    }
  ],
  "overallEffort": 7
}
```

| Field | Type | Required | Constraints |
|---|---|---|---|
| `loggedExercises` | array | Yes | Unchanged |
| `loggedExercises[].exerciseId` | UUID string | Yes | Must exist |
| `loggedExercises[].loggedWeight` | string \| null | No | Unchanged |
| `loggedExercises[].notes` | string \| null | No | Unchanged |
| `loggedExercises[].effort` | integer \| null | No | 1–10 inclusive |
| `overallEffort` | integer \| null | No | 1–10 inclusive if present; null = not rated |

**New validation**: If `overallEffort` is provided and is not in [1, 10], respond `400 Bad Request`:
```json
{ "error": "Overall effort must be between 1 and 10." }
```

#### Response — 201 Created (unchanged structure, adds field)

```json
{
  "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "plannedWorkoutId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "workoutName": "Push Day",
  "overallEffort": 7,
  "loggedExercises": [
    {
      "loggedExerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80 KG",
      "notes": "felt strong",
      "effort": 8
    }
  ]
}
```

`overallEffort` is `null` when the session was saved without rating.

#### Response — 400 Bad Request (new case)

```json
{ "error": "Overall effort must be between 1 and 10." }
```

#### Unchanged responses

- `404 Not Found` — workout not found (unchanged)
- `400 Bad Request` — existing per-exercise effort validation (unchanged)

---

### GET /api/sessions

**Change**: Adds `overallEffort` field to each session object in the response array.

#### Response — 200 OK

```json
[
  {
    "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "plannedWorkoutId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "workoutName": "Push Day",
    "completedAt": "2026-05-19T08:30:00Z",
    "overallEffort": 7,
    "loggedExercises": [
      {
        "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      }
    ]
  }
]
```

`overallEffort` is `null` when the session was saved without rating.

#### Unchanged

Request, query parameters, ordering, and all other fields are unchanged.

---

### GET /api/sessions/{sessionId}

**Change**: Adds `overallEffort` and `previousOverallEffort` fields to the response.

#### Response — 200 OK

```json
{
  "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "plannedWorkoutId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "workoutName": "Push Day",
  "completedAt": "2026-05-19T08:30:00Z",
  "overallEffort": 8,
  "previousOverallEffort": 6,
  "exercises": [
    {
      "loggedExerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "exerciseName": "Bench Press",
      "loggedWeight": "80 KG",
      "notes": null,
      "effort": 8,
      "previousLoggedWeight": "75 KG",
      "previousNotes": null,
      "previousEffort": 7
    }
  ]
}
```

| Field | Type | Notes |
|---|---|---|
| `overallEffort` | integer \| null | Null when session was saved without rating |
| `previousOverallEffort` | integer \| null | Null when no prior session, ad-hoc session, or prior session has no overall effort |

All previously existing fields are unchanged.

#### Unchanged

Request, path parameters, 404 response, and all other fields are unchanged.
