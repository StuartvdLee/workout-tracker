# API Contract: Current Workout Exercise Order Editing

## Overview

No new API endpoint, request field, response field, database migration, or backend validation rule is introduced by this feature.

The feature reuses the existing active-session save contract established by prior exercise-order features.

## Unchanged Endpoint: `POST /api/workouts/{workoutId}/sessions`

The current workout save continues to use:

```http
POST /api/workouts/{workoutId}/sessions
Content-Type: application/json
```

Request body remains:

```json
{
  "loggedExercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "effort": 7,
      "sequence": 0
    }
  ],
  "overallEffort": 6
}
```

## Sequence Semantics

- `sequence` remains the 0-based display position of the exercise in the active/current workout at save time.
- Manual current-workout reordering changes the order of the client-side `loggedExercises` generation by changing `workout.exercises`.
- The backend stores `sequence` exactly as already supported.
- The planned workout template order is not modified.

## Error Responses

Unchanged from the existing session-save endpoint:

| Status | Condition |
|---|---|
| `201 Created` | Session saved successfully |
| `400 Bad Request` | Existing validation failure, such as invalid exercise data |
| `404 Not Found` | Workout ID does not exist |

## Unchanged Read Endpoints

The following endpoints are consumed as before:

- `GET /api/workouts/{workoutId}` loads the current workout exercises.
- `GET /api/workouts/{workoutId}/previous-performance` loads previous performance data.
- No endpoint is called when entering or exiting order-editing mode.
