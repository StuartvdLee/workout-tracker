# API Contract: GET /api/sessions/latest

**Feature**: `009-last-workout-hint`  
**Date**: 2026-05-06

---

## Endpoint

```
GET /api/sessions/latest
```

---

## Purpose

Returns the name and completion date of the most recently completed workout session. Used exclusively by the home page to render the "last workout" hint below the "Start Workout" button.

---

## Request

No path parameters, query parameters, or request body.

---

## Response

### 200 OK — No sessions exist

```json
{
  "hasSession": false
}
```

### 200 OK — Session exists

```json
{
  "hasSession": true,
  "workoutName": "Push",
  "completedAt": "2026-05-03T09:15:00Z"
}
```

| Field         | Type    | Nullable | Description                                                                 |
|---------------|---------|----------|-----------------------------------------------------------------------------|
| `hasSession`  | boolean | NO       | `true` if at least one completed session exists; `false` otherwise          |
| `workoutName` | string  | YES      | Snapshot of the planned workout name recorded at session creation time. `null` only if the session was created before the `workout_name` snapshot was introduced (legacy edge case). Present when `hasSession` is `true`. |
| `completedAt` | string  | YES      | ISO 8601 UTC timestamp of session completion. Present when `hasSession` is `true`. |

---

## Error Responses

This endpoint has no parameters and performs only a read from the database. No validation errors are possible. On unexpected server error, the standard ASP.NET Core 500 response is returned — the frontend silently swallows any non-2xx response.

---

## Proxy Route (WorkoutTracker.Web)

The Web frontend project proxies this endpoint to the API:

```
GET /api/sessions/latest  →  WorkoutTracker.Web  →  WorkoutTracker.Api  →  PostgreSQL
```

The Web proxy route follows the same pattern as all other proxy routes in `WorkoutTracker.Web/Program.cs`.

---

## Notes

- The `workoutName` field is a **snapshot** stored on the session at creation time — it does not change if the planned workout is subsequently renamed or deleted.
- This endpoint is intentionally minimal. It returns only what the home page hint requires — no `LoggedExercises`, no session ID, no planned workout ID.
- The frontend is responsible for formatting `completedAt` into a human-readable string (e.g., "3 May 2026").
