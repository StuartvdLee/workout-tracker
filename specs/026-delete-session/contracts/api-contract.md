# API Contract: Delete Session

## Endpoint

`DELETE /api/sessions/{sessionId}`

---

## Path Parameters

| Parameter | Type | Constraint | Description |
|-----------|------|------------|-------------|
| sessionId | string | UUID v4 | The unique identifier of the session to delete |

Non-UUID values are rejected by the route constraint — ASP.NET Core returns 404 automatically.

---

## Responses

### 204 No Content — Success

Session and all associated logged exercises have been permanently deleted.

**Body**: none

---

### 404 Not Found — Session does not exist

**Body** (application/json):
```json
{ "error": "Session not found." }
```

---

### 502 Bad Gateway — API unavailable (Web proxy only)

Returned by the Web proxy when the API service is unreachable.

**Body** (application/json):
```json
{ "error": "API unavailable." }
```

---

## Side Effects

- All `LoggedExercise` rows with `WorkoutSessionId = {sessionId}` are deleted via cascade.
- The session no longer appears in `GET /api/sessions`.
- `GET /api/sessions/{sessionId}` returns 404 after deletion.

---

## Idempotency

Calling `DELETE /api/sessions/{sessionId}` on an already-deleted session returns `404`. The frontend treats both `204` and `404` as "success" (session is gone) and navigates to the History page in both cases.

---

## Security

- GUID route constraint prevents non-GUID inputs from reaching the handler.
- EF Core parameterises the GUID value — no SQL injection risk.
- Error responses contain only a static message string — no session data is leaked.
