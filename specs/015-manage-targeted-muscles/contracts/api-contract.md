# API Contract: POST /api/muscles

**Feature**: `015-manage-targeted-muscles`

---

## Endpoint

`POST /api/muscles`

---

## Request

**Content-Type**: `application/json`

```json
{
  "name": "Hip Flexors"
}
```

| Field | Type   | Required | Notes                            |
|-------|--------|----------|----------------------------------|
| name  | string | Yes      | Trimmed before validation; max 100 chars |

---

## Responses

### 201 Created

Muscle was created successfully.

```json
{
  "muscleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Hip Flexors"
}
```

### 400 Bad Request — Empty or whitespace name

```json
{ "error": "Muscle name is required." }
```

### 400 Bad Request — Name too long

```json
{ "error": "Muscle name must be 100 characters or fewer." }
```

### 400 Bad Request — Duplicate name (case-insensitive)

```json
{ "error": "A muscle with this name already exists." }
```

---

## Web Proxy

The Web project (`WorkoutTracker.Web/Program.cs`) exposes an identical proxy route:

```
POST /api/muscles  →  forwards to API POST /api/muscles
```

Pattern mirrors existing `POST /api/exercises` proxy (lines 55–72 of `WorkoutTracker.Web/Program.cs`).

---

## Notes

- `muscleId` is a server-generated UUID; clients MUST NOT supply it.
- The `name` field is stored as trimmed. "  Hip Flexors  " is stored as "Hip Flexors".
- Duplicate check is case-insensitive: "biceps" is rejected when "Biceps" exists.
- The new muscle will appear in subsequent `GET /api/muscles` responses sorted alphabetically.
- The endpoint wraps the advisory-lock + duplicate-check + insert + commit inside `db.Database.CreateExecutionStrategy().ExecuteAsync()` to be compatible with `NpgsqlRetryingExecutionStrategy` (registered by the Aspire Npgsql integration). Calling `BeginTransactionAsync()` directly outside the strategy's scope throws `InvalidOperationException`.
