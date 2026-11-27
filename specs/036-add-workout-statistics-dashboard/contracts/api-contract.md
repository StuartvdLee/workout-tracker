# API Contract: Workout Statistics

## Request

`GET /api/statistics/workouts?startUtc={instant}&endUtc={instant}` via the Web proxy and existing API. Both parameters are required ISO 8601 UTC instants with `Z`; the interval is inclusive at start and exclusive at end. The browser independently converts the selected local period's start and next start to UTC.

Validate parseability, UTC designation, `startUtc < endUtc`, and a maximum elapsed interval of 370 days (permits a leap year and DST/offset transitions). Reject invalid or excessive input with `400` and a generic validation message. Use parameterized EF filtering on the completion timestamp; do not accept client-supplied user identifiers as authorization. The endpoint is read-only and does not promise to validate whether arbitrary callers supplied a calendar-aligned interval.

## Success — 200 JSON

```json
{
  "workoutCount": 2,
  "exerciseActivity": [
    { "exerciseId": "00000000-0000-0000-0000-000000000001", "name": "Squat", "workoutCount": 2 }
  ],
  "history": { "workoutCount": 4, "firstCompletedAt": "2026-09-02T10:00:00Z" },
  "effort": {
    "contributingCount": 1,
    "observations": [
      { "sessionId": "00000000-0000-0000-0000-000000000002", "completedAt": "2026-11-26T10:00:00Z", "value": 7 }
    ]
  }
}
```

Exercise counts represent distinct sessions, not rows or sets. Observations contain only supported overall effort values (integers 1–10); contributor count equals the length of the observations array. Sort activity by display name then ID and observations by completion instant then session ID. A missing exercise name uses a clear fallback. `workoutCount: 0` with empty arrays means no activity; nonzero workouts with no observations means effort unavailable. Null/unsupported effort does not remove a workout from other statistics.

`history` is an aggregate of completed workouts before `endUtc`, including workouts before `startUtc`. It contains their count and the earliest completion instant, or is null when none exist. The dashboard uses it to calculate the average over local calendar periods from the first recorded workout through the selected period, including empty periods.

## Failure, privacy and freshness

Use the existing single-user workout-history data scope and Web/API access path. Do not add sharing, cross-user aggregation, or a new identity model. Period details are filtered to the requested interval; the history aggregate includes prior completed workouts, without returning their details. Existing deployment access controls are unchanged and are not asserted to provide SSO or user-level isolation. API/proxy failures are non-success responses with generic messages, not an empty success.

Read current data per request and do not cache a derived summary across edits/deletes. Project only the fields needed for this response; avoid logging workout values. Limit period detail queries to the requested range and compute the earlier history aggregate in the database.
