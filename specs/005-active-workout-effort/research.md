# Research: Active Workout UI — Effort Tracking

**Feature**: `005-active-workout-effort`  
**Date**: 2026-04-27

## Decision 1: EF Core Additive Migration (Nullable Column)

**Decision**: Add `Effort` as a nullable `int?` column to the existing `logged_exercise` table via a standard EF Core `AddColumn` migration. No default value is set; existing rows receive `NULL`.

**Rationale**: The column is optional (effort is never required), so `NULL` is the correct absence value. All existing `LoggedExercise` rows continue to work without any data backfill. EF Core generates a simple `migrationBuilder.AddColumn<int?>(nullable: true)` with no risk of table locking on insert-only traffic patterns in PostgreSQL.

**Alternatives considered**:
- *Default value of 0*: Rejected — 0 is not a valid effort value and would make it impossible to distinguish "not recorded" from a deliberately chosen low effort.
- *Separate `ExerciseEffort` join table*: Rejected — unnecessary indirection for a single scalar field with a 1:1 relationship to `LoggedExercise`.

---

## Decision 2: Effort Slider — HTML `<input type="range">` 

**Decision**: Use a native HTML `<input type="range" min="1" max="10" step="1">` for the effort slider. The input starts with no value (removed `value` attribute so it does not snap to a default). The `input` event drives a live label update. The slider is left unset (tracked as `null` in the `LogEntry` map) when the user never touches it.

**Rationale**: Native range input provides keyboard navigation (arrow keys ±1, Home/End), touch support, and accessible semantics without any third-party library. It aligns with the project's no-external-JS constraint. The browser enforces min/max, so range attacks are blocked client-side; server-side validation provides the final gate.

**Unset state handling**: The slider's visual position (which defaults to the midpoint) is not meaningful when the user has not interacted with it. A `data-touched` attribute (set on first `input` event) distinguishes "untouched" from "touched at a specific value". When untouched, the `LogEntry.effort` is stored as `null` and submitted as `null` (omitted from the JSON payload). The label area shows a neutral prompt ("Rate effort") until first touch.

**Alternatives considered**:
- *Custom rendered slider (divs + JS)*: Rejected — significant complexity for no UX gain in this single-user app.
- *Numeric input 1–10*: Rejected — a slider maps naturally to the spec's stated intensity bands and is faster to interact with on mobile.

---

## Decision 3: Backward Compatibility — Existing Sessions

**Decision**: When `effort` is `NULL` on a `LoggedExercise` row (i.e., sessions saved before this feature), the history view silently omits the effort display for that exercise. No placeholder, no "N/A", no migration of existing data.

**Rationale**: The spec explicitly states that effort absence is valid and should display "nothing, not an error or placeholder" (FR-013). Old sessions are immutable (spec FR-028 intent) and should not be retroactively altered.

**Alternatives considered**:
- *Backfill existing rows with a default value*: Rejected — would falsely imply a user provided effort data they did not.
- *Display "—" for unrated exercises*: Rejected — spec says display nothing for unrated; a dash adds noise.

---

## Decision 4: Reps — Remove from UI, Preserve in DB

**Decision**: The `LoggedReps` column on `logged_exercise` is NOT dropped from the database. It is simply no longer exposed in the active session UI or the history display. No migration removes the column.

**Rationale**: Dropping a column requires a migration, risks accidental data loss on rollback, and is irreversible without a backup. Since reps data may exist in previously saved sessions, preserving the column maintains full data fidelity. The spec states "reps data MUST be hidden from the UI" (FR-014) — not deleted.

**Alternatives considered**:
- *Drop `LoggedReps` via migration*: Rejected — unnecessary data loss risk; column can be removed in a future housekeeping migration once confirmed no rollback is needed.

---

## Decision 5: Weight KG — Label Only (No Unit Conversion)

**Decision**: The weight value is stored and displayed as a plain string (unchanged from feature 004). The only change is the UI label: the input label in `active-session.ts` changes from "Weight" to "Weight (KG)" (or an equivalent suffix approach). History display appends "KG" to the weight string when rendering.

**Rationale**: The spec assumption states "Weight is assumed to always be in KG. There is no requirement for unit switching in this release." Changing the storage type would require a migration and break old sessions. A label change achieves the spec goal (FR-002, FR-011) with minimal risk.

**Alternatives considered**:
- *Store weight as a numeric type*: Rejected — the existing schema uses `string?` for flexibility (e.g., "bodyweight", "20.5"); changing type is out of scope.

---

## Decision 6: Effort Band Labels

**Decision**: The intensity band is determined client-side by a pure function mapping a 1–10 value to a string:

```
1–3  → "Easy"
4–6  → "Moderate"
7–8  → "Hard"
9–10 → "All Out"
```

The label is stored as the numeric value only; the band name is always derived at display time from the stored integer. This keeps the data model clean and the labels updateable without migration.

**Alternatives considered**:
- *Store the band label string*: Rejected — derived data should not be persisted; a label change would not be back-applied to old records.

---

## Decision 7: Shared Effort Label Utility (`utils.ts`)

**Decision**: Extract `getEffortLabel(value: number): string` into a new shared module `src/WorkoutTracker.Web/wwwroot/ts/utils.ts`, exported and imported by both `active-session.ts` and `history.ts` via `import { getEffortLabel } from "../utils.js"`.

**Rationale**: The function is identical in both pages — a pure mapping from integer to band label string. Duplicating it violates Constitution I ("avoid duplication") and creates a maintenance risk: if band thresholds ever change, a single edit would need to be applied in two files. The TypeScript project uses `"module": "ES2022"` with `"include": ["wwwroot/ts/**/*.ts"]`, so a top-level `utils.ts` is automatically compiled without any `tsconfig.json` changes. The existing codebase already uses relative ES module imports (e.g., `import { navigate } from "../router.js"`), so this follows established project pattern.

**Alternatives considered**:
- *Duplicate in each page module*: Rejected — Constitution I prohibits duplication with no upside.
- *Inline as a private module-level function in each page*: Rejected — same duplication concern; identical logic in two places must stay in sync.
