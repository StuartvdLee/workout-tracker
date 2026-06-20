# Research: Add Targeted Muscles In-App

**Feature**: `015-manage-targeted-muscles`
**Branch**: `015-manage-targeted-muscles`

---

## R-001: Duplicate Check Approach

**Question**: How should the `POST /api/muscles` endpoint enforce case-insensitive uniqueness?

**Decision**: Use `EF.Functions.ILike` with `ExerciseQueryHelper.EscapeLike`, identical to how `POST /api/exercises` handles duplicate exercise names.

**Rationale**: `EF.Functions.ILike` is already used in three places in `Program.cs` (POST exercises, PUT exercises, POST/PUT workouts). `ExerciseQueryHelper.EscapeLike` is a shared static helper that sanitises LIKE pattern characters (`%`, `_`, `\`). Reusing the same pattern avoids a new abstraction, stays consistent with the codebase, and handles all edge cases (accidental wildcards in user input).

**Code pattern**:
```csharp
var normalizedName = ExerciseQueryHelper.EscapeLike(name);
var duplicate = await db.Muscles
    .AnyAsync(m => EF.Functions.ILike(m.Name, normalizedName, "\\"));
if (duplicate)
    return Results.Json(new { error = "A muscle with this name already exists." }, statusCode: 400);
```

**Alternatives considered**:
- `m.Name.ToLower() == name.ToLower()`: Would work but cannot be translated to a parameterised SQL query by EF Core on PostgreSQL in all contexts; ILike is the established project pattern.
- `EF.Functions.Like` (case-sensitive): Does not meet the case-insensitive requirement.

---

## R-002: HTTP Status for Duplicate Name

**Question**: Should a duplicate-name response return `400 Bad Request` or `409 Conflict`?

**Decision**: Return `400 Bad Request` with `{ error: "A muscle with this name already exists." }`.

**Rationale**: The existing `POST /api/exercises` returns 400 for duplicates (line 84 of `Program.cs`), not 409. Consistency with the established pattern takes priority over strict REST semantics. The frontend already handles 400 responses by reading `data.error` from the JSON body, so no new error-handling path is needed on the client.

**Alternatives considered**:
- `409 Conflict`: Semantically more precise for a uniqueness violation, but would require new client-side handling and is inconsistent with `POST /api/exercises`.

---

## R-003: Placement of "Add Muscle" UI

**Question**: Where in the exercises page UI should the "Add muscle" capability live?

**Decision**: An inline mini-form (text input + "Add" button + error element) placed immediately below the muscle toggle group in both the create form (`#exercise-muscles`) and the edit modal (`#edit-exercise-muscles`).

**Rationale**: The user is already in the context of selecting muscles when they notice a muscle is missing. An inline affordance keeps them in flow without a modal-within-a-modal or navigation away. The HTML is injected by `exercises.ts` alongside the rest of the page scaffold — no new page module required.

**HTML IDs**:
- Create form: `#add-muscle-name`, `#add-muscle-btn`, `#add-muscle-error`
- Edit modal: `#edit-add-muscle-name`, `#edit-add-muscle-btn`, `#edit-add-muscle-error`

**Alternatives considered**:
- Separate "Muscles" management page: Heavier scope; muscles are always accessed in the context of exercises, so a dedicated page adds navigation overhead.
- Modal-within-a-modal (prompt dialog): Breaks focus management; adds complexity; inconsistent with existing patterns.

---

## R-004: Auto-Select After Adding

**Question**: Should a newly added muscle be automatically selected (toggled on) in the current form after it is created?

**Decision**: No — the new muscle appears in the list in correct alphabetical position but is left unselected. The user explicitly toggles it if they want to associate it with the current exercise.

**Rationale**: Auto-selection was initially considered but rejected by the user: adding a muscle is a separate action from choosing to use it on the current exercise. Leaving it unselected keeps the interaction consistent with how the existing seed muscles behave and avoids unexpected form state changes.

**Alternatives considered**:
- Auto-select: Saves a click for the common case but creates surprise if the user added the muscle for future use rather than the current exercise.

---

## R-005: Re-render Strategy for Both Toggle Containers

**Question**: When a muscle is added from the create form, should the edit modal's toggle list also be refreshed?

**Decision**: Both `renderMuscleToggles()` (create form) and `renderEditMuscleToggles()` (edit modal) are called after every successful add, because both iterate over the shared `muscles[]` module-level array. Calling both ensures that if the edit modal is subsequently opened it already reflects the new muscle.

**Rationale**: `muscles[]` is module-level state. Both render functions already perform a full innerHTML rebuild. Calling both is cheap (small list, DOM-only) and safe. The edit modal is hidden when the create form is active, so the double re-render has no visible effect.

**Alternatives considered**:
- Lazy re-render (only re-render the active context): Slightly more efficient but risks the edit modal showing a stale list if opened immediately after a create-form addition.

---

## R-006: Test Isolation for Existing Count Test

**Question**: Does adding a `POST /api/muscles` route risk breaking `MuscleToggles_AllTwelveDisplayed` (which asserts exactly 12 toggles)?

**Decision**: No change needed to the existing test. The test database is reset via `ResetDataAsync()` in `InitializeAsync()` before every test. Any muscles created in previous tests are rolled back; the seeded 12 muscles are restored. The new E2E tests that add a muscle operate in isolation.

**Rationale**: Confirmed by reviewing `ApiFixture.cs` — `ResetDataAsync` truncates and re-seeds tables between tests. The E2E WebAppFixture also resets between test classes.

**Alternatives considered**:
- Updating the assert to `>=12`: Unnecessary given per-test reset.

---

## R-007: Client-Side Update Strategy After Successful Add

**Question**: After `POST /api/muscles` returns 201, should the client insert the new muscle directly into the `muscles[]` array client-side, or re-fetch the full list from `GET /api/muscles`?

**Decision**: Re-fetch via `reloadMuscles()` — a thin async helper that calls `GET /api/muscles` and replaces `muscles[]` with the response — then call `renderMuscleToggles()` and `renderEditMuscleToggles()`.

**Rationale**: Re-fetching is the authoritative approach: the backend is the single source of truth for the persisted, sorted list. The plan originally called for a client-side `insertMuscleAlphabetically()` helper, but re-fetching is simpler (no sort logic to replicate), eliminates edge cases (e.g. backend trims or normalises the name), and has negligible performance cost on a small table. A `ReloadMusclesResult` discriminated union type propagates fetch errors without exceptions.

**Alternatives considered**:
- `insertMuscleAlphabetically(muscle)`: Fewer round-trips but duplicates sort logic and can drift from the server's ordering.

---

## R-008: NpgsqlRetryingExecutionStrategy and User-Initiated Transactions

**Question**: Can `POST /api/muscles` call `db.Database.BeginTransactionAsync()` directly when Npgsql is configured with a retrying execution strategy?

**Decision**: No. Wrap the entire transactional block in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`.

**Rationale**: The Aspire Npgsql integration registers `NpgsqlRetryingExecutionStrategy`, which does not allow user-initiated transactions opened outside its own execution scope. Calling `BeginTransactionAsync()` directly throws `InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions.` The fix is to wrap the advisory lock + duplicate check + insert + commit inside `CreateExecutionStrategy().ExecuteAsync()`. The duplicate/result flags are captured via outer variables since the lambda cannot return early from the endpoint.

**Alternatives considered**:
- Disabling the retrying strategy: Would remove transient-failure resilience; rejected.
- Removing the advisory lock: Would allow duplicate muscles under concurrent requests; rejected.
