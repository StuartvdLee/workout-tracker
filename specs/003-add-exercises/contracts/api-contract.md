# API Contract: Add Exercises

**Feature**: 003-add-exercises
**Date**: 2026-07-15

## Base URL

All endpoints are served from the API project at the Aspire-assigned URL, proxied through the Web project's `/api/*` route.

## Endpoints

### GET /api/muscles

Returns the predefined list of muscles available for exercise targeting.

**Response** `200 OK`:
```json
[
  { "muscleId": "uuid", "name": "string" }
]
```

**Response shape**:

| Field    | Type   | Description                       |
| -------- | ------ | --------------------------------- |
| muscleId | string | UUID of the muscle                |
| name     | string | Display name (e.g., "Chest")      |

**Ordering**: Alphabetical by name.

**Notes**: The list is static (seeded data). No create/update/delete endpoints for muscles.

---

### GET /api/exercises

Returns all exercises with their associated muscles.

**Response** `200 OK`:
```json
[
  {
    "exerciseId": "uuid",
    "name": "string",
    "muscles": [
      { "muscleId": "uuid", "name": "string" }
    ]
  }
]
```

**Response shape**:

| Field              | Type     | Description                             |
| ------------------ | -------- | --------------------------------------- |
| exerciseId         | string   | UUID of the exercise                    |
| name               | string   | Display name of the exercise            |
| muscles            | array    | Associated muscles (may be empty)       |
| muscles[].muscleId | string   | UUID of the muscle                      |
| muscles[].name     | string   | Display name of the muscle              |

**Ordering**: Alphabetical by exercise name.

**Notes**: Returns an empty array `[]` when no exercises exist.

---

### POST /api/exercises

Creates a new exercise with optional muscle associations.

**Request body**:
```json
{
  "name": "string",
  "muscleIds": ["uuid"]
}
```

| Field     | Type     | Required | Constraints                                    |
| --------- | -------- | -------- | ---------------------------------------------- |
| name      | string   | Yes      | Non-empty after trim, max 150 chars, unique CI |
| muscleIds | string[] | No       | Array of valid muscle UUIDs (may be empty or omitted) |

**Response** `201 Created`:
```json
{
  "exerciseId": "uuid",
  "name": "string",
  "muscles": [
    { "muscleId": "uuid", "name": "string" }
  ]
}
```

**Response** `400 Bad Request` (validation failure):
```json
{
  "error": "string"
}
```

**Validation error messages**:

| Condition                     | Error message                                      |
| ----------------------------- | -------------------------------------------------- |
| Name is empty/whitespace      | `"Exercise name is required."`                     |
| Name exceeds 150 characters   | `"Exercise name must be 150 characters or fewer."` |
| Name is a case-insensitive duplicate | `"An exercise with this name already exists."`|
| Invalid muscle ID in array    | `"One or more selected muscles are invalid."`      |

**Notes**: The `name` is trimmed (leading/trailing whitespace removed) before validation and storage.

---

### PUT /api/exercises/{exerciseId}

Updates an existing exercise's name and/or muscle associations.

**Path parameter**:

| Parameter  | Type   | Description          |
| ---------- | ------ | -------------------- |
| exerciseId | string | UUID of the exercise |

**Request body**:
```json
{
  "name": "string",
  "muscleIds": ["uuid"]
}
```

| Field     | Type     | Required | Constraints                                              |
| --------- | -------- | -------- | -------------------------------------------------------- |
| name      | string   | Yes      | Non-empty after trim, max 150 chars, unique CI (excluding self) |
| muscleIds | string[] | No       | Array of valid muscle UUIDs (may be empty or omitted)     |

**Response** `200 OK`:
```json
{
  "exerciseId": "uuid",
  "name": "string",
  "muscles": [
    { "muscleId": "uuid", "name": "string" }
  ]
}
```

**Response** `400 Bad Request` (validation failure):
```json
{
  "error": "string"
}
```

**Response** `404 Not Found`:
```json
{
  "error": "Exercise not found."
}
```

**Validation error messages**: Same as POST, with the uniqueness check excluding the exercise being updated.

**Notes**: On update, existing muscle associations are replaced entirely — the provided `muscleIds` becomes the complete set. Omitting `muscleIds` or sending an empty array removes all muscle associations.

## Existing Endpoints (unchanged)

| Method | Path                | Description                  | Used By   |
| ------ | ------------------- | ---------------------------- | --------- |
| GET    | `/api/workout-types` | Returns sorted workout types | Home page |
