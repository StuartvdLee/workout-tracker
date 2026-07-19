# API Contract: Latest Exercise Data

**Feature**: `029-latest-exercise-data`  
**Date**: 2026-07-19

## Updated Endpoint: `GET /api/workouts/{workoutId}/previous-performance`

Returns the latest usable historical comparison data for each exercise in the requested planned workout. This endpoint already exists; this feature updates its selection semantics.

### Request

```http
GET /api/workouts/{workoutId}/previous-performance
```

| Parameter | Location | Type | Required | Notes |
|---|---|---|---|---|
| `workoutId` | Path | GUID | Yes | Planned workout ID |

No request body. No query parameters.

### Response: 200 OK — No Usable Previous Data

Returned when the planned workout exists but no current workout exercise has usable historical comparison data.

```json
{
  "hasPreviousSession": false,
  "completedAt": null,
  "exercises": []
}
```

### Response: 200 OK — Latest Available Data Found

Returned when one or more current workout exercises have usable historical comparison data. Each exercise entry reflects that exercise's newest usable historical row. Entries may come from different sessions.

```json
{
  "hasPreviousSession": true,
  "completedAt": "2026-07-18T10:30:00Z",
  "exercises": [
    {
      "exerciseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "loggedWeight": "80",
      "effort": 7,
      "sequence": 0,
      "completedAt": "2026-07-18T10:30:00Z"
    },
    {
      "exerciseId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "loggedWeight": "60",
      "effort": null,
      "sequence": 2,
      "completedAt": "2026-07-10T09:15:00Z"
    }
  ]
}
```

### Response Fields

| Field | Type | Notes |
|---|---|---|
| `hasPreviousSession` | boolean | `true` when at least one current exercise has usable historical data |
| `completedAt` | ISO 8601 datetime or null | Newest `completedAt` among returned exercise entries; retained for compatibility |
| `exercises` | array | Latest usable per-exercise data; omits exercises with no usable data |
| `exercises[].exerciseId` | uuid | Current workout exercise identity |
| `exercises[].loggedWeight` | string or null | Latest non-blank recorded weight when available |
| `exercises[].effort` | integer 1-10 or null | Latest recorded effort when available |
| `exercises[].sequence` | integer or null | Sequence from the selected historical row; context only |
| `exercises[].completedAt` | ISO 8601 datetime | Completion time of the selected historical row for this exercise |

### Usable Data Rule

A historical logged exercise is usable when:

- `loggedWeight` is non-null and not blank; or
- `effort` is non-null.

Rows with only `sequence`, or with all comparison fields missing, MUST be skipped.

### Scoping Rule

Only sessions where `plannedWorkoutId` equals the requested `workoutId` contribute data. The same exercise in another planned workout MUST NOT be returned.

### Error Responses

| Status | Condition | Body |
|---|---|---|
| 404 | `workoutId` does not exist | `{ "error": "Workout not found." }` |
| 400 | `workoutId` is not a valid GUID | ASP.NET Core default model binding error |

### Web Proxy

`WorkoutTracker.Web` continues to proxy the same route unchanged:

```text
GET /api/workouts/{workoutId}/previous-performance
→ GET /api/workouts/{workoutId}/previous-performance (API)
```

## Updated Review Endpoint: `GET /api/sessions/{sessionId}`

The existing session-detail endpoint MUST use the same latest-usable per-exercise selection rule when populating review comparison fields.

### Review Response Fields

Each `exercises[]` item keeps its existing fields and updates the source of:

| Field | Type | Notes |
|---|---|---|
| `previousWeight` | string or null | Latest non-blank historical `loggedWeight` for the same exercise before this session |
| `previousEffort` | integer 1-10 or null | Latest historical `effort` for the same exercise before this session |

### Review Selection Rule

- Only sessions for the same `plannedWorkoutId` before the reviewed session contribute data.
- Rows with blank `loggedWeight` and null `effort` MUST be skipped.
- Each exercise selects its own latest usable prior row, so review rows may compare against different historical sessions.
- Exercises with no usable prior row return `previousWeight: null` and `previousEffort: null`.
