# Data Model: Dedicated Targeted Muscles Page

**Feature**: `017-targeted-muscles-page`

---

## No Schema Changes

No database migrations are required for this feature. The `Muscle` and `ExerciseMuscle` tables already exist and are correctly configured.

---

## Existing Entities (unchanged)

### Muscle

| Field      | Type    | Constraints                            |
|------------|---------|----------------------------------------|
| MuscleId   | Guid    | PK, not null                           |
| Name       | string  | Required, no explicit max length in DB |

**Notes**:
- Names are validated at the application layer to max 100 characters (established in feature 015).
- Duplicate names are prevented case-insensitively at the application layer via `ILike` + advisory lock.
- No unique index on `Name` in the DB (application enforces uniqueness; the seeded rows have well-known GUIDs).

### ExerciseMuscle (join table)

| Field           | Type | Constraints                                                |
|-----------------|------|------------------------------------------------------------|
| ExerciseMuscleId | Guid | PK, not null                                              |
| ExerciseId      | Guid | FK → Exercise, `OnDelete(DeleteBehavior.Cascade)`         |
| MuscleId        | Guid | FK → Muscle, `OnDelete(DeleteBehavior.Cascade)`           |
| Unique index on (ExerciseId, MuscleId) |  |                                          |

**Cascade behaviour**: Deleting a `Muscle` row automatically deletes all `ExerciseMuscle` rows that reference it. No application-level join-table cleanup is needed in `DELETE /api/muscles/{id}`.

---

## API Request / Response Shapes (new)

### MuscleUpdateRequest (new C# record)

```csharp
record MuscleUpdateRequest(string? Name);
```

Used by `PATCH /api/muscles/{muscleId}`.

### PATCH response body (200 OK)

```json
{
  "muscleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Hip Flexors"
}
```

### DELETE response (204 No Content)

No body.

---

## TypeScript Types (new in muscles.ts)

```typescript
interface Muscle {
  readonly muscleId: string;
  readonly name: string;
}
```

Identical to the `Muscle` interface already defined in `exercises.ts`. Each page module defines its own — no shared type module is introduced.
