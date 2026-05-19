# Research: Dedicated Targeted Muscles Page

**Feature**: `017-targeted-muscles-page`  
**Phase**: 0 — Outline & Research

---

## No Unknowns

No NEEDS CLARIFICATION items exist. All decisions are resolved by examining the existing codebase and building on patterns established in features 003–016.

---

## Decision Log

### D-001: New API endpoints pattern

**Decision**: `PATCH /api/muscles/{muscleId:guid}` for rename; `DELETE /api/muscles/{muscleId:guid}` for deletion. Uses `PATCH` (not `PUT`) because only the `name` field can be updated — a partial update.

**Rationale**: Matches the existing `PUT /api/exercises/{exerciseId:guid}` and `DELETE /api/exercises/{exerciseId:guid}` patterns directly. Route constraint `{muscleId:guid}` ensures non-GUID paths return 400 automatically, consistent with all other resource endpoints.

**Alternatives considered**: `PUT` for rename — rejected because `PUT` implies a full resource replacement; only `name` is being updated, so `PATCH` is semantically correct. No alternatives for `DELETE`.

---

### D-002: Duplicate check for PATCH (rename)

**Decision**: Use `EF.Functions.ILike` with `ExerciseQueryHelper.EscapeLike` excluding the current muscle from the check (`m.MuscleId != muscleId`). Wrap in the advisory-lock + `CreateExecutionStrategy().ExecuteAsync()` pattern.

**Rationale**: Matches the `POST /api/muscles` implementation exactly. The advisory lock (`pg_advisory_xact_lock`) prevents two concurrent renames from both passing the duplicate check. Using `CreateExecutionStrategy().ExecuteAsync()` is required because `NpgsqlRetryingExecutionStrategy` (registered by Aspire) throws `InvalidOperationException` if `BeginTransactionAsync` is called outside its scope — a bug discovered and fixed in feature 015.

**Alternatives considered**: Relying solely on a unique DB index — this works as a safety net but does not give a user-friendly error message; application-level validation is still needed.

---

### D-003: Cascade delete behaviour

**Decision**: `DELETE /api/muscles/{id}` simply deletes the `Muscle` row; EF Core cascade (`OnDelete(DeleteBehavior.Cascade)`) on the `Muscle → ExerciseMuscle` FK automatically removes all join rows.

**Rationale**: Already configured in `WorkoutTrackerDbContext.cs` (line ~84). No additional application logic is needed. This matches the `DELETE /api/exercises/{id}` behaviour for the Exercise → ExerciseMuscle cascade.

**Alternatives considered**: Application-level delete of `ExerciseMuscle` rows before deleting the muscle — unnecessary; the DB handles it.

---

### D-004: Frontend page structure

**Decision**: New `muscles.ts` page module following the exact same structure as `workouts.ts` and `exercises.ts`: exported `render(container)` async function, module-level mutable state, inline HTML scaffold, event binding after render.

**Rationale**: All existing pages use this pattern; `muscles.ts` has the simplest state model of any page (no nested entities, no multi-select, just a flat list of muscles).

**Alternatives considered**: Shared component/service abstractions — rejected as speculative abstraction not yet needed in this codebase (constitution principle I: avoid speculative abstractions).

---

### D-005: Muscle card visual design on the dedicated page

**Decision**: Muscle chips on the Targeted Muscles page use the existing `.muscle-toggle` chip style as the visual base, extended with `.muscle-card` modifier to add edit/delete icon buttons overlaid or appended. The chips are NOT interactive toggles on this page — they are display cards.

**Rationale**: The user explicitly asked to "copy the style and layout from the exercises page". The muscle grid on the exercises page uses `.muscle-toggle` chips. Reusing the same chip shape/colour on the new page maintains visual consistency. The edit/delete icons follow the pattern used on exercise list items.

**Alternatives considered**: A list layout (like the exercise list) — valid but the user specifically referenced the grid/chip layout currently on the exercises page.

---

### D-006: Removal of add-muscle form from Exercises page

**Decision**: Remove the `.muscle-add` block from both the create-exercise form and the edit-exercise modal in `exercises.ts`. Remove the associated state (`isAddingMuscle`, `isEditAddingMuscle`), event handlers, and the `handleAddMuscle` / `insertMuscleAlphabetically` functions that are no longer needed there.

**Rationale**: These functions move to `muscles.ts`. Keeping them in `exercises.ts` creates dead code, violates constitution principle I, and confuses the user (two places to manage muscles).

**Alternatives considered**: Keeping the inline form as a convenience shortcut — rejected; the spec explicitly states it must be removed.

---

### D-007: `insertMuscleAlphabetically` — shared or duplicated?

**Decision**: Copy (do not import from exercises.ts) the `insertMuscleAlphabetically` helper into `muscles.ts`. The function is a 4-line utility; no shared module is warranted.

**Rationale**: Creating a shared utility module solely for this 4-line function is a speculative abstraction. Both pages maintain their own muscle list state (`muscles: Muscle[]`). The exercises page still needs to insert a new muscle into its local state when the page is refreshed — but since it no longer creates muscles, it just re-fetches.

**Alternatives considered**: Extract to `utils.ts` — reasonable if the function is used in 3+ places, but currently it would only be in `muscles.ts` post-refactor.

---

### D-008: Sidebar navigation entry

**Decision**: Add a "Targeted Muscles" link with `href="/muscles"` and `data-page="muscles"` to `index.html`. Position it after "Exercises" in the sidebar, as it is a child/related concept.

**Rationale**: `sidebar.ts` uses `data-page` attribute to compute the active link class. The route `/muscles` → `data-page="muscles"` follows the exact pattern of the other links (`/workouts` → `data-page="workouts"`, `/exercises` → `data-page="exercises"`).

**Alternatives considered**: Nesting muscles under exercises in the nav — rejected; the dedicated page is a first-class page deserving its own top-level nav entry.
