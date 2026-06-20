# Data Model: Add Targeted Muscles In-App

**Feature**: `015-manage-targeted-muscles`

---

## Existing Entity: Muscle

No schema changes. The `Muscle` table already exists.

| Field      | Type   | Constraints                  |
|------------|--------|------------------------------|
| MuscleId   | Guid   | Primary key                  |
| Name       | string | Required (`text`, non-null)  |

**Relationships**: One `Muscle` → many `ExerciseMuscle` join rows.

**Seeded rows** (unchanged): Adductors, Back, Biceps, Calves, Chest, Core, Forearms, Glutes, Hamstrings, Quads, Shoulders, Triceps (12 total, fixed GUIDs).

**New rows**: Created dynamically via `POST /api/muscles`; assigned `Guid.NewGuid()` at creation time.

---

## Validation Rules

| Rule                        | Enforcement          | Error                                            |
|-----------------------------|----------------------|--------------------------------------------------|
| Name must not be empty      | Backend + frontend   | "Muscle name is required."                       |
| Name must not be whitespace | Backend (Trim check) | "Muscle name is required."                       |
| Name ≤ 100 characters       | Backend + frontend   | "Muscle name must be 100 characters or fewer."   |
| Name must be unique (ci)    | Backend (`ILike` + transaction lock) | "A muscle with this name already exists."        |

All validation is enforced on the backend. Frontend performs pre-flight checks for empty and length to avoid unnecessary round-trips.

---

## New API Request Record

```csharp
internal sealed class MuscleCreateRequest
{
    public string? Name { get; set; }
}
```

Placed at the bottom of `Program.cs` alongside `ExerciseCreateRequest`.

---

## No Migrations Required

The `Muscle` table was introduced in migration `20260328171550_AddMusclesAndExerciseConstraints`. Creating new rows via `POST /api/muscles` requires no schema changes.
