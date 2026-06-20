# API Contract: PATCH + DELETE /api/muscles/{muscleId}

**Feature**: `017-targeted-muscles-page`

---

## Endpoint 1: PATCH /api/muscles/{muscleId}

**Purpose**: Rename an existing muscle.

### Route

`PATCH /api/muscles/{muscleId:guid}`

### Request

**Content-Type**: `application/json`

```json
{
  "name": "Hip Flexors"
}
```

| Field | Type   | Required | Notes                                              |
|-------|--------|----------|----------------------------------------------------|
| name  | string | Yes      | Trimmed before validation; max 100 chars; must be unique (case-insensitive) among all OTHER muscles |

### Responses

#### 200 OK — Rename successful

```json
{
  "muscleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Hip Flexors"
}
```

#### 400 Bad Request — Empty or whitespace name

```json
{ "error": "Muscle name is required." }
```

#### 400 Bad Request — Name too long

```json
{ "error": "Muscle name must be 100 characters or fewer." }
```

#### 400 Bad Request — Duplicate name (case-insensitive, different muscle)

```json
{ "error": "A muscle with this name already exists." }
```

#### 404 Not Found — Muscle does not exist

```json
{ "error": "Muscle not found." }
```

### Implementation Notes

- Retrieve the muscle by `muscleId`; return 404 if not found.
- Trim name; validate not empty, not over 100 chars.
- Use advisory lock (`pg_advisory_xact_lock(MuscleNameAdvisoryLockId)`) within `CreateExecutionStrategy().ExecuteAsync()` — same pattern as `POST /api/muscles` (feature 015).
- Duplicate check: `ILike` + `EscapeLike`, excluding the current muscle (`m.MuscleId != muscleId`).
- On success return the updated `{ muscleId, name }`.

### Web Proxy

`PATCH /api/muscles/{muscleId}  →  forwards to API PATCH /api/muscles/{muscleId}`

Pattern mirrors `PUT /api/exercises/{exerciseId}` proxy.

---

## Endpoint 2: DELETE /api/muscles/{muscleId}

**Purpose**: Delete a muscle and its associated exercise-muscle links (via DB cascade).

### Route

`DELETE /api/muscles/{muscleId:guid}`

### Request

No request body.

### Responses

#### 204 No Content — Deletion successful

No response body.

#### 404 Not Found — Muscle does not exist

```json
{ "error": "Muscle not found." }
```

### Implementation Notes

- Retrieve the muscle by `muscleId`; return 404 if not found.
- Call `db.Muscles.Remove(muscle)` and `db.SaveChangesAsync()`.
- The `ExerciseMuscle` join rows are automatically deleted by the configured DB cascade (`OnDelete(DeleteBehavior.Cascade)` on the `MuscleId` FK).
- No advisory lock needed — delete is inherently idempotent by route GUID.
- Return `Results.NoContent()` on success.

### Web Proxy

`DELETE /api/muscles/{muscleId}  →  forwards to API DELETE /api/muscles/{muscleId}`

Pattern mirrors `DELETE /api/exercises/{exerciseId}` proxy.
