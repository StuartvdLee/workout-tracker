# API Contract: POST /api/workouts/{workoutId}/sessions (Extended)

**Feature**: `010-randomize-exercise-order`  
**Date**: 2026-05-08

---

## Overview

This document describes the **extension** to the existing `POST /api/workouts/{workoutId}/sessions` endpoint introduced by feature `010-randomize-exercise-order`. The endpoint itself is unchanged; only the request body schema gains one optional field.

The original endpoint was introduced in feature `004-add-workouts` and extended in feature `005-active-workout-effort` (which added the `effort` field).

---

## Endpoint

```
POST /api/workouts/{workoutId:guid}/sessions
```

---

## Purpose

Creates a new workout session, logging the exercises performed. With this feature, the request now includes an optional `sequence` field per exercise that records the 0-based position at which the exercise was displayed during the session (which may differ from the template order when shuffle was used).

---

## Request

### Path Parameter

| Parameter   | Type   | Required | Description                    |
|-------------|--------|----------|--------------------------------|
| `workoutId` | `guid` | YES      | ID of the planned workout      |

### Request Body

```json
{
  "loggedExercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "effort": 7,
      "sequence": 0
    },
    {
      "exerciseId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "loggedWeight": "60",
      "effort": 5,
      "sequence": 1
    }
  ]
}
```

### `loggedExercises` Item Fields

| Field          | Type     | Nullable | Description                                                                                    |
|----------------|----------|----------|------------------------------------------------------------------------------------------------|
| `exerciseId`   | `guid`   | NO       | ID of the exercise as defined in the planned workout                                           |
| `loggedWeight` | `string` | YES      | Weight used (free-form; e.g., `"80"`, `"80.5"`)                                                |
| `effort`       | `int`    | YES      | Perceived effort on a 1–10 scale. Added in feature 005.                                        |
| `sequence`     | `int`    | YES      | **NEW** — 0-based display position during the session. `null` is accepted for backward compatibility. |

### Extension: `sequence` field

- **Type**: nullable `int`
- **Values**: `0`, `1`, `2`, … up to `(exercise count - 1)`
- **Meaning**: The position at which this exercise was shown to the user during the session. `0` = displayed first. For non-shuffled sessions, this matches the `PlannedWorkoutExercise.Sequence` display order. For shuffled sessions, this reflects the shuffled order.
- **Omitting or sending `null`**: Valid. The server stores `null`. This is the correct value for sessions created by clients that predate this feature.

---

## Response

Identical to the pre-existing endpoint behaviour. No response body changes.

### 201 Created

Session created successfully. The response body contains the created session:

```json
{
  "workoutSessionId": "4b4f1e3c-...",
  "plannedWorkoutId": "3fa85f64-...",
  "workoutName": "Push Day",
  "loggedExercises": [
    {
      "loggedExerciseId": "a1b2c3d4-...",
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "notes": null,
      "effort": 7,
      "sequence": 0
    },
    {
      "loggedExerciseId": "e5f6a7b8-...",
      "exerciseId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "loggedWeight": "60",
      "notes": null,
      "effort": 5,
      "sequence": 1
    }
  ]
}
```

The `sequence` value on each `loggedExercise` item reflects what was submitted — `null` if the client omitted it.

### 404 Not Found

The specified `workoutId` does not correspond to a known planned workout.

### 400 Bad Request

Existing validation rules apply (duplicate `exerciseId` entries, unrecognised exercise IDs, etc.). No new validation rules are introduced for `sequence`.

---

## Proxy Route (WorkoutTracker.Web)

The proxy route for this endpoint is unchanged:

```
POST /api/workouts/{workoutId}/sessions
  → WorkoutTracker.Web  →  WorkoutTracker.Api  →  PostgreSQL
```

---

## Notes

- The `sequence` values are set by the frontend at save time. The backend stores them as-is without validation (values outside the 0–(n-1) range are accepted; enforcement is a frontend concern).
- The order of items in the `loggedExercises` array is NOT used to infer sequence. The explicit `sequence` field is the authoritative record.
- This extension does not break any existing callers. Any client omitting `sequence` will have it stored as `null`.
