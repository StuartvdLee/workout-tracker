# API Contract: Active Workout UI — Effort Tracking

**Feature**: `005-active-workout-effort`  
**Date**: 2026-04-27

## Overview

This contract documents the changes to the existing session endpoints introduced by feature 005. All other endpoints defined in `004-add-workouts` are unchanged.

---

## Modified Endpoint: POST `/api/workouts/{workoutId}/sessions`

Creates a new completed workout session linked to a planned workout.

### Change

The `loggedExercises` array now accepts an optional `effort` field per exercise.

### Request

```
POST /api/workouts/{workoutId}/sessions
Content-Type: application/json
```

```json
{
  "loggedExercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "effort": 7,
      "notes": null
    },
    {
      "exerciseId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "loggedWeight": null,
      "effort": null,
      "notes": null
    }
  ]
}
```

### Fields

| Field                         | Type     | Required | Constraints                                   |
|-------------------------------|----------|----------|-----------------------------------------------|
| `loggedExercises`             | array    | Yes      | Can be empty `[]`                             |
| `loggedExercises[].exerciseId`| uuid     | Yes      | Must be an exercise in the planned workout    |
| `loggedExercises[].loggedWeight` | string | No    | Arbitrary string (e.g., "80", "bodyweight")   |
| `loggedExercises[].effort`    | integer  | No       | 1–10 inclusive, or null/omitted               |
| `loggedExercises[].notes`     | string   | No       | Free text                                     |

> **Note**: `loggedReps` is not part of the current request contract or `SessionLoggedExerciseItem` DTO shape. Consumers and tests should omit this field.

### Validation

- `effort` present and outside [1, 10] → `400 Bad Request`
- `exerciseId` not in the planned workout → `400 Bad Request`
- `workoutId` not found → `404 Not Found`

### Response: 201 Created

```json
{
  "workoutSessionId": "uuid",
  "plannedWorkoutId": "uuid",
  "workoutName": "Push Day",
  "loggedExercises": [
    {
      "loggedExerciseId": "uuid",
      "exerciseId": "uuid",
      "loggedWeight": "80",
      "effort": 7,
      "notes": null
    }
  ]
}
```

### Error Responses

| Status | Body                                                       |
|--------|------------------------------------------------------------|
| 400    | `{ "error": "Effort must be between 1 and 10." }`         |
| 400    | `{ "error": "One or more logged exercises are not part of this workout." }` |
| 404    | `{ "error": "Workout not found." }`                       |

---

## Modified Endpoint: GET `/api/sessions`

Returns all completed sessions in reverse chronological order.

### Change

The `loggedExercises` items in the response now include `effort`.

### Response: 200 OK

```json
[
  {
    "workoutSessionId": "uuid",
    "plannedWorkoutId": "uuid",
    "workoutName": "Push Day",
    "completedAt": "2026-04-27T09:30:00Z",
    "loggedExercises": [
      {
        "loggedExerciseId": "uuid",
        "exerciseId": "uuid",
        "exerciseName": "Bench Press",
        "loggedWeight": "80",
        "effort": 7,
        "notes": null
      },
      {
        "loggedExerciseId": "uuid",
        "exerciseId": "uuid",
        "exerciseName": "Overhead Press",
        "loggedWeight": null,
        "effort": null,
        "notes": null
      }
    ]
  }
]
```

### Fields (LoggedExercise item)

| Field              | Type          | Notes                                   |
|--------------------|---------------|-----------------------------------------|
| `loggedExerciseId` | uuid          | Unchanged                               |
| `exerciseId`       | uuid          | Unchanged                               |
| `exerciseName`     | string        | Unchanged                               |
| `loggedWeight`     | string / null | Unchanged                               |
| `effort`           | integer / null| NEW: 1–10, or null if not recorded      |
| `notes`            | string / null | Unchanged                               |

> **Note**: `loggedReps` is no longer included in the API response. The column is preserved in the DB but removed from the response DTO.

---

## C# DTO Changes (Program.cs)

### `SessionLoggedExerciseItem` (request DTO)

```csharp
internal sealed class SessionLoggedExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public int? Effort { get; set; }   // NEW
    public string? Notes { get; set; }
}
```

### Inline anonymous response type (projection in `MapPost` and `MapGet`)

Both endpoints must project `le.Effort` in their anonymous result objects.

---

## Unchanged Endpoints

All other endpoints from feature 004 are unaffected:
- `POST /api/workouts`, `GET /api/workouts`, `GET /api/workouts/{id}`, `PUT /api/workouts/{id}`, `DELETE /api/workouts/{id}`
- `GET /api/exercises`, `GET /api/muscles`
