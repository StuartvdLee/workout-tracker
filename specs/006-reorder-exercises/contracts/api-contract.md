# API Contract: Reorder Exercises in a Workout

**Feature**: `006-reorder-exercises`  
**Date**: 2026-05-02

## Overview

**No API changes are required for this feature.**

The existing `POST /api/workouts` and `PUT /api/workouts/{id}` endpoints already derive exercise sequence from the order of the `exercises[]` array in the request body. Sending exercises in the user's desired order is sufficient; the backend assigns `Sequence = i + 1` and stores it. All GET endpoints already return exercises ordered by `Sequence`.

This contract documents the existing endpoint behaviour for completeness, clarifying how order is already communicated to the backend.

---

## Existing Endpoint: POST `/api/workouts` (unchanged)

Creates a new planned workout. Exercise order in the request body determines stored sequence.

### Request

```
POST /api/workouts
Content-Type: application/json
```

```json
{
  "name": "Push Day",
  "exercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "targetReps": null,
      "targetWeight": null
    },
    {
      "exerciseId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "targetReps": null,
      "targetWeight": null
    }
  ]
}
```

### Fields

| Field | Type | Required | Constraints |
|---|---|---|---|
| `name` | string | Yes | 1–150 characters, unique (case-insensitive) |
| `exercises` | array | Yes | Minimum 1 item; no duplicates |
| `exercises[].exerciseId` | uuid | Yes | Must reference an existing exercise |
| `exercises[].targetReps` | string | No | Free-form (e.g., "8-12"); null if not set |
| `exercises[].targetWeight` | string | No | Free-form (e.g., "60kg"); null if not set |

> **Order semantics**: The first element of `exercises[]` receives `Sequence = 1`, the second receives `Sequence = 2`, and so on. The order in which the frontend sends the array is the sequence that is persisted and returned.

### Response: 201 Created

```json
{
  "plannedWorkoutId": "uuid",
  "name": "Push Day",
  "exerciseCount": 2,
  "exercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Bench Press",
      "targetReps": null,
      "targetWeight": null
    },
    {
      "exerciseId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "name": "Overhead Press",
      "targetReps": null,
      "targetWeight": null
    }
  ]
}
```

Exercises in the response are returned in `Sequence` order (same as the submitted order).

---

## Existing Endpoint: PUT `/api/workouts/{workoutId}` (unchanged)

Updates an existing planned workout. Existing `PlannedWorkoutExercise` rows are deleted and recreated from the submitted array. Exercise order in the request body determines the new stored sequence.

### Request

```
PUT /api/workouts/{workoutId}
Content-Type: application/json
```

Same body shape as `POST /api/workouts`. The submitted `exercises[]` array **replaces** the previous exercise list entirely — position is re-derived from index.

> **Reorder semantics**: To reorder exercises on an existing workout, submit the same exercise IDs in the new desired order. The backend deletes the old `PlannedWorkoutExercise` rows and recreates them with `Sequence` values derived from the new array order.

### Response: 200 OK

Same shape as the `POST` 201 response. Exercises returned in the new `Sequence` order.

---

## Existing Endpoint: GET `/api/workouts/{workoutId}` (unchanged)

Returns a single planned workout with exercises in `Sequence` order.

### Response: 200 OK

```json
{
  "plannedWorkoutId": "uuid",
  "name": "Push Day",
  "exerciseCount": 2,
  "exercises": [
    {
      "exerciseId": "uuid",
      "name": "Bench Press",
      "targetReps": null,
      "targetWeight": null
    },
    {
      "exerciseId": "uuid",
      "name": "Overhead Press",
      "targetReps": null,
      "targetWeight": null
    }
  ]
}
```

---

## No New Endpoints

No new endpoints are introduced. No existing endpoint signatures or response shapes change.
