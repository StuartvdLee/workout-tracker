# Research: Workout Overall Effort

**Feature**: `016-workout-overall-effort`
**Branch**: `016-workout-overall-effort`

---

## R-001: WorkoutSession Schema Change — Additive Nullable Column

**Question**: How should `overall_effort` be added to the `workout_session` table without breaking existing sessions?

**Decision**: Add `OverallEffort` as a nullable `int?` property to `WorkoutSession.cs` and generate a standard EF Core `AddColumn` migration. Add a check constraint `ck_workout_session_overall_effort_range` enforcing `overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10)` in `WorkoutTrackerDbContext.OnModelCreating`, identical to the existing `ck_logged_exercise_effort_range` constraint on `logged_exercise`.

**Rationale**: Nullable column means existing rows receive `NULL` and continue to work without data backfill. The check constraint pattern is already established in feature 005. EF Core generates a simple `AddColumn<int?>(nullable: true)` with no risk to existing data.

**Code pattern (DbContext)**:
```csharp
modelBuilder.Entity<WorkoutSession>(entity =>
{
    // existing config ...
    entity.HasCheckConstraint(
        "ck_workout_session_overall_effort_range",
        "overall_effort IS NULL OR (overall_effort >= 1 AND overall_effort <= 10)");
});
```

**Alternatives considered**:
- Default value of 0: Rejected — 0 is not a valid effort value; same reasoning as feature 005 Decision 1.
- Separate `SessionEffort` join table: Rejected — unnecessary indirection for a single scalar field.

---

## R-002: API — Where to Pass Overall Effort on Session Create

**Question**: Should `overallEffort` be a top-level field in `SessionCreateRequest` or embedded within `LoggedExercises`?

**Decision**: Top-level field in `SessionCreateRequest`. The request body becomes:
```json
{
  "loggedExercises": [ ... ],
  "overallEffort": 7
}
```

**Rationale**: Overall effort is a session-level property, not a per-exercise property. Embedding it inside an exercise item would be semantically wrong and require repeating it per exercise. Adding it as a top-level field in `SessionCreateRequest` (a `int? OverallEffort` C# property) is the minimal, correct change.

**Validation**: Mirror the per-exercise validation already in the endpoint:
```csharp
if (request.OverallEffort is not null && (request.OverallEffort < 1 || request.OverallEffort > 10))
    return Results.Json(new { error = "Overall effort must be between 1 and 10." }, statusCode: 400);
```

**Error response**: `400 Bad Request` with `{ "error": "..." }` — consistent with all other validation errors in the endpoint.

**Alternatives considered**:
- Separate `PATCH /api/sessions/{id}/effort` endpoint: Heavier scope; requires the session to already exist; complicates the save flow (two requests).

---

## R-003: GET /api/sessions — Adding Overall Effort to History List

**Question**: How should `overallEffort` be included in the history list response without changing existing fields?

**Decision**: Add `OverallEffort = ws.OverallEffort` to the existing `.Select(ws => new { ... })` projection in `GET /api/sessions`. No other changes to the endpoint.

**Rationale**: `OverallEffort` is a directly mapped property on `WorkoutSession` (not a shadow property), so it is straightforwardly accessible in LINQ projections. Adding it to the anonymous type is additive — existing callers that ignore unknown JSON fields are unaffected. The `WorkoutSession` TypeScript interface in `history.ts` is updated to add `readonly overallEffort: number | null`.

---

## R-004: GET /api/sessions/{sessionId} — Adding Overall Effort and Previous

**Question**: How should the session detail endpoint return both the current session's `overallEffort` and the previous session's?

**Decision**: Extend the existing `GET /api/sessions/{sessionId}` handler in two places:

1. Add `OverallEffort = session.OverallEffort` to the outer response projection.
2. Change the prior session `.Select()` from projecting only `LoggedExercises` to projecting an anonymous type with both `OverallEffort` and `LoggedExercises`:
```csharp
.Select(ws => new {
    ws.OverallEffort,
    LoggedExercises = ws.LoggedExercises
        .Select(le => new { le.ExerciseId, le.LoggedWeight, le.Effort })
        .ToList()
})
.FirstOrDefaultAsync();
```
Then add `PreviousOverallEffort = priorSession?.OverallEffort` to the response.

**Rationale**: The prior-session query already exists and is scoped to the same `PlannedWorkoutId` lookup (R-003 from feature 014). Extending its projection to include `OverallEffort` costs nothing extra — it is a direct column on `workout_session` already joined by EF Core.

**Edge cases** (consistent with feature 014 research R-003):
- `PlannedWorkoutId == null` (ad-hoc session): `priorSession` is `null`; `PreviousOverallEffort` is `null`.
- No prior session found: `PreviousOverallEffort` is `null`.
- Prior session exists but has no overall effort: `PreviousOverallEffort` is `null` (the column is nullable).

---

## R-005: Effort Modal — Reuse Discard Modal Pattern

**Question**: How should the "rate your effort" pop-up be implemented in `active-session.ts`?

**Decision**: Scaffold the effort modal HTML inline in the page alongside the existing discard modal, hidden initially (`display:none`). Use the identical pattern: `alertdialog` role, `aria-modal="true"`, backdrop click-to-dismiss, Escape-key-dismiss, focus trap. Intercept the "Save Workout" button click to open the modal instead of calling `handleSave` directly; call `handleSave(effort)` only after the user confirms or skips.

**Modal HTML scaffold** (added to page scaffold alongside `#discard-backdrop`):
```html
<div class="effort-modal-backdrop" id="effort-backdrop" style="display:none;">
  <div class="effort-modal" role="alertdialog" aria-modal="true"
       aria-labelledby="effort-modal-title" aria-describedby="effort-modal-desc">
    <h2 class="effort-modal__title" id="effort-modal-title">How hard was that?</h2>
    <p class="effort-modal__desc" id="effort-modal-desc">
      Rate your overall workout effort (optional).
    </p>
    <div class="effort-modal__slider-group">
      <label class="effort-modal__label" for="overall-effort-slider">
        Effort
        <span class="effort-modal__value" id="overall-effort-value">Not rated</span>
      </label>
      <span class="effort-modal__band" id="overall-effort-band"></span>
      <input class="effort-modal__slider" type="range" id="overall-effort-slider"
             min="1" max="10" step="1" data-touched="false"
             aria-label="Overall workout effort" aria-valuemin="1" aria-valuemax="10"
             aria-valuetext="Not rated" />
    </div>
    <div class="effort-modal__actions">
      <button class="effort-modal__save" type="button" id="effort-modal-save">Save</button>
      <button class="effort-modal__skip" type="button" id="effort-modal-skip">Skip</button>
    </div>
  </div>
</div>
```

**State**: Add `let pendingOverallEffort: number | null = null` at module level. The slider `input` event sets `pendingOverallEffort` (and marks `data-touched="true"`). When the user clicks "Save", `pendingOverallEffort` is passed to `handleSave`. When the user clicks "Skip", `null` is passed.

**handleSave signature change**: `async function handleSave(overallEffort: number | null): Promise<void>`. The POST body becomes:
```ts
JSON.stringify({ loggedExercises, overallEffort })
```

**Slider reset**: On modal open, reset the slider to untouched state (`data-touched="false"`, `pendingOverallEffort = null`, value display "Not rated") so the user always starts fresh each time they save.

**Alternatives considered**:
- Browser `confirm()` / `prompt()`: Not styleable, not accessible, inconsistent with existing UX.
- Separate page route for effort entry: Over-engineered; breaks the flow.
- Inline effort slider always visible before the save button: Would clutter the active session UI; the feature spec explicitly requires a pop-up at save time.

---

## R-006: History Page — Rendering Overall Effort on Session Card

**Question**: Where and how should overall effort appear on a history session card?

**Decision**: Add a second span below the existing `.history-session__exercise-count` span, with class `.history-session__overall-effort`, containing the effort value and label (e.g., `"7 · Hard"`). Right-aligned via CSS (both spans are in the same flex column on the right side of the card). When `overallEffort` is `null`, the span is not rendered (no empty space).

**Rendering logic** (TypeScript):
```ts
const effortText = session.overallEffort !== null
  ? `${session.overallEffort} · ${getEffortLabel(session.overallEffort)}`
  : "";
// In template literal, conditionally include:
${effortText ? `<span class="history-session__overall-effort">${escapeHtml(effortText)}</span>` : ""}
```

**Note**: `getEffortLabel` is already imported in `active-session.ts` and defined in `utils.ts`. It needs to be imported in `history.ts` as well (it is not currently imported there).

**Alternatives considered**:
- Append effort to the exercise-count span (single line): Loses the layout separation; the confirmed spec decision places effort on a separate line below the exercise count.

---

## R-007: Session Detail — Rendering the Overall Effort Summary Row

**Question**: How should the overall effort summary row be rendered below the exercises table?

**Decision**: After the closing `</table>` tag in `renderDetailTable`, append a summary `<div>` showing current and previous overall effort. Use `getEffortLabel` for the labels. Display "—" for null values (consistent with `session-detail__no-data` pattern).

**Rendering condition**: The summary row is always rendered when `overallEffort` is not null. When `overallEffort` is null and `previousOverallEffort` is also null (e.g., an old session before this feature), the summary row is omitted entirely.

**HTML structure**:
```html
<div class="session-detail__overall-effort-row">
  <span class="session-detail__overall-effort-label">Overall effort</span>
  <span class="session-detail__overall-effort-value">8 · All Out</span>
  <span class="session-detail__overall-effort-prev-label">Previous</span>
  <span class="session-detail__overall-effort-prev-value">7 · Hard</span>
</div>
```
When previous is null: the prev-value span shows `<span class="session-detail__no-data">—</span>`.

**`getEffortLabel` import**: Already imported in `session-detail.ts`? No — currently not imported. Must add `import { getEffortLabel } from "../utils.js";`.

**Alternatives considered**:
- Adding a summary row inside the table `<tfoot>`: Merging session-level and exercise-level data in the same table violates semantic separation and risks confusing the "Overall effort" row with exercise rows.

---

## R-008: Test Isolation — Existing Tests

**Question**: Does adding `overall_effort` to the `POST` body risk breaking any existing session tests?

**Decision**: No changes needed to existing tests. `OverallEffort` is added to `SessionCreateRequest` as a nullable property with no `[Required]` attribute. Existing test payloads that omit `overallEffort` will deserialize with `OverallEffort = null`, which is valid. Existing assertions on session fields do not check `OverallEffort` (it did not exist), so they continue to pass.

**E2E tests**: The existing E2E tests post sessions via `page.APIRequest.PostAsync` with `DataObject` that does not include `overallEffort`. Since the field is optional, all existing flows continue to pass unchanged.
