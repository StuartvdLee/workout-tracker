# API Contract: Workout Sets Selection

**Status**: Implemented in PR #159

## POST `/api/workouts/{workoutId}/sessions`

Extends the existing session creation request.

```json
{
  "sets": 5,
  "overallEffort": 7,
  "loggedExercises": []
}
```

| Field | Type | Required | Constraints |
|---|---|---:|---|
| `sets` | integer | Yes | Exactly 3 or 5 |

The `201 Created` response adds top-level `sets`.

**Validation error**: `400 Bad Request` with `{ "error": "Sets must be 3 or 5." }` when missing, null, or outside the allowed values.

## GET `/api/workouts/{workoutId}/previous-performance`

Each exercise comparison adds the source historical session's sets:

```json
{
  "hasPreviousSession": true,
  "completedAt": "2026-09-18T18:00:00Z",
  "exercises": [
    {
      "exerciseId": "11111111-1111-1111-1111-111111111111",
      "loggedWeight": "80",
      "effort": 7,
      "sequence": 0,
      "sets": 5,
      "completedAt": "2026-09-18T18:00:00Z"
    }
  ]
}
```

`sets` is nullable for legacy sessions. It comes from the same historical session as the selected weight-or-effort exercise comparison. The existing usability predicate remains unchanged: Sets alone does not make a historical exercise row selectable or suppress fallback to older usable weight or effort.

## GET `/api/sessions`

Each session summary adds top-level nullable `sets`.

## GET `/api/sessions/{sessionId}`

Adds top-level nullable `sets` and per-exercise nullable `previousSets`:

```json
{
  "workoutSessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "sets": 5,
  "exercises": [
    {
      "loggedExerciseId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "exerciseId": "11111111-1111-1111-1111-111111111111",
      "loggedWeight": "80",
      "previousWeight": "77.5",
      "effort": 7,
      "previousEffort": 6,
      "previousSets": 3
    }
  ]
}
```

Current sets is top-level because it is session-wide. The frontend repeats it in each table row. `previousSets` is attached per exercise because latest usable comparison data may come from different prior sessions.

## PUT `/api/sessions/{sessionId}`

Extends the existing historical session update request with one top-level field:

```json
{
  "sets": 3,
  "overallEffort": 7,
  "loggedExercises": []
}
```

| Field | Type | Required | Constraints |
|---|---|---:|---|
| `sets` | integer or null | No | Omitted preserves the stored value; 3 or 5 replaces it; explicit null clears it |

A successful `200 OK` response uses the extended session-detail shape. Invalid non-null values return `400 Bad Request` with `{ "error": "Sets must be 3 or 5." }`. Request binding MUST retain property-presence information so omitted and explicit null are not treated as the same operation.

## GET `/api/workouts/{workoutId}/session-trends`

Each chart data point adds the session-level nullable `sets` value:

```json
{
  "dataPoints": [
    {
      "completedAt": "2026-09-18T18:00:00Z",
      "overallEffort": 7,
      "sets": 5,
      "exercises": [
        {
          "exerciseId": "11111111-1111-1111-1111-111111111111",
          "exerciseName": "Bench Press",
          "loggedWeight": "80",
          "effort": 7
        }
      ]
    }
  ]
}
```

`sets` is `null` for sessions recorded before this feature. It is emitted once per data point, never per exercise, because it is session-wide. The existing newest-first 50-session cap, chronological output ordering, and single-query projection are unchanged, so no extra request or round trip is introduced.

## Web Proxy

No new routes are required. Existing POST, GET, and PUT proxy routes forward the extended request/response JSON unchanged. Proxy failure behavior remains `502 Bad Gateway` with the existing error body. The existing `session-trends` proxy route forwards the additional `sets` field without modification.

## Compatibility

- Existing stored sessions return `sets: null` and `previousSets: null` where applicable.
- Existing consumers must tolerate these additive nullable response properties.
- Existing PUT clients that omit `sets` preserve the stored value; explicit `sets: null` clears it.
- New session creation rejects missing sets.
- `session-trends` data points carry `sets: null` for legacy sessions; consumers must treat it as a gap rather than a value.
- No extra API request is introduced by any affected page. The delivered implementation also tolerates omitted legacy Sets fields in frontend responses, rendering them as absent/no-data rather than `undefined`.
