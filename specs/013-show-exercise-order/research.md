# Research: Previous Exercise Order Indicator in Active Workout

**Feature**: `013-show-exercise-order`  
**Date**: 2026-05-18  
**Phase**: 0 — Resolve unknowns from Technical Context

---

## Decision 1: Data source — new field or new storage?

**Decision**: Use the existing `LoggedExercise.Sequence` column (int?, already in the DB schema and populated on every session save) as the source for the `#x` position indicator. No schema change, no migration.

**Rationale**: `LoggedExercise.Sequence` is already stored as a 0-based index when the active session is saved (`active-session.ts` sets `sequence: index` for each exercise in the `map()` call). It is already included in the `LoggedExercise` EF model and in the DB snapshot. The `previous-performance` endpoint already fetches `LoggedExercises` via `Include` — adding `Sequence` to the projection is a one-line change.

**Alternatives considered**:
- Derive order from the `LoggedExerciseId` insertion order: rejected — insertion order is not reliably reflected by a surrogate GUID key; using `Sequence` is explicit and already designed for this purpose.
- Add a new `PreviousOrder` field to the `previous-performance` response independent of `Sequence`: rejected — `Sequence` is already the canonical order field on `LoggedExercise`; duplicating it under a new name would add confusion.

---

## Decision 2: Existing `previous-performance` endpoint vs. new endpoint

**Decision**: Extend `GET /api/workouts/{workoutId}/previous-performance` by adding `sequence` to the `LoggedExercise` projection. No new endpoint.

**Rationale**: The position indicator is contextually part of the "previous session" data, alongside weight and effort. It is fetched from the same session, in the same query, for the same purpose (reference data shown before the user logs their current session). Splitting it into a separate endpoint would require a second round-trip with no benefit.

**Alternatives considered**:
- New `GET /api/workouts/{workoutId}/previous-order` endpoint: rejected — unnecessary round-trip; the data comes from the same row already fetched by `previous-performance`.

---

## Decision 3: 0-based vs. 1-based display

**Decision**: `Sequence` is stored as a 0-based index (first exercise = 0). Display as 1-based (`#1`, `#2`, `#3`) using `sequence + 1` at render time.

**Rationale**: The spec says `#x` where `x` is "the number in the order" — natural language counting is 1-based. Displaying `#0` would be confusing to users. The conversion is trivial: `#${entry.sequence + 1}` in `active-session.ts`. The stored 0-based value is correct and unchanged.

**Alternatives considered**:
- Change `Sequence` storage to 1-based: rejected — the existing `active-session.ts` uses `index` from `Array.prototype.map()` which is 0-based; changing this would require updating all session save logic and existing data (migration). The conversion at render time is simpler.

---

## Decision 4: Rendering placement — prepend to "Last time" or separate element?

**Decision**: Prepend `#x` as the first segment in the `parts` array used to build the "Last time" value string. Result: `Last time: #2 · 80 KG · 7 — Hard` (all three), `Last time: #2` (position only, weight/effort null).

**Rationale**: The position indicator is one of three reference datums for the exercise ("where it was, what was lifted, how hard"). Treating it symmetrically with weight and effort — as the first segment in the same joined string — keeps the "Last time" line coherent and avoids introducing a new DOM element or CSS class. No new CSS is needed; the indicator inherits `active-session__previous-value` styling (`--color-text-muted`, `0.85rem`).

**Alternatives considered**:
- Separate `<span class="active-session__previous-order">#2</span>` before the "Last time" label: rejected — requires a new CSS class and complicates the HTML structure for a one-field addition; the existing concatenation approach is sufficient.
- Show position only when weight or effort is also available: rejected — spec intent is to always show the previous position when it exists, regardless of whether weight/effort were recorded; this is useful when exercises were done but nothing was logged.

---

## Decision 5: Null `Sequence` handling (old or no-sequence sessions)

**Decision**: If `sequence` is `null` in the `PreviousExerciseData` response, the `#x` indicator is simply omitted from the display. Weight and effort are shown as before. If all three are null, the "First session — no previous data" fallback applies unchanged.

**Rationale**: `Sequence` was added to `LoggedExercise` as a nullable field (`int?`). Sessions created before `Sequence` began being populated (or tests that post without a `sequence` field) will have `null`. Omitting the indicator gracefully degrades the display to the feature-008 state without any error or placeholder. This is the correct contract — "no indicator" means "position not recorded", not "an error occurred".

**Alternatives considered**:
- Show a `—` placeholder when `sequence` is null: rejected — showing a dash implies data was expected; null means "not available for this session" and the indicator should simply be absent.
- Error if `sequence` is null: rejected — null is a valid and expected state for historical sessions.

---

## Decision 6: Test strategy for `Sequence`

**Decision**: Add one new test `GetPreviousPerformance_ReturnsSequence_FromLastSession` that posts a session with explicit `Sequence` values and asserts the response includes them. Update `PreviousExerciseDataDto` in `SessionApiTests.cs` to include `int? Sequence`. Update `GetPreviousPerformance_ReturnsWeightAndEffort_FromLastSession` to also assert `Sequence`.

**Rationale**: The new test provides explicit proof that `Sequence` flows from POST (session save) → DB → GET (previous-performance) correctly. Existing tests that post without `Sequence` continue to verify the null path naturally — no changes needed to those test bodies. This keeps the delta minimal while achieving complete coverage of the new field.

**Alternatives considered**:
- Update all existing previous-performance tests to assert Sequence: rejected — most tests post sessions without `sequence`, so all assertions would be `Assert.Null(result.Exercises[n].Sequence)` — this adds noise without insight. One focused new test is cleaner.

---

## Summary of All Decisions

| # | Decision | Outcome |
|---|----------|---------|
| 1 | Data source | Existing `LoggedExercise.Sequence` (int?) — no migration |
| 2 | Endpoint | Extend existing `previous-performance` — no new endpoint |
| 3 | Display index | 1-based (`sequence + 1`) — natural counting for users |
| 4 | Render placement | Prepend `#x` to "Last time" value string — no new CSS |
| 5 | Null Sequence | Omit indicator gracefully — degrades to feature-008 state |
| 6 | Testing | One new test + DTO update + assertion in one existing test |
