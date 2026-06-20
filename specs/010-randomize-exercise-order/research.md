# Research: Randomize Workout Exercise Order

**Feature**: `010-randomize-exercise-order`  
**Date**: 2026-05-08  
**Phase**: 0 — Resolve unknowns from Technical Context

---

## Decision 1: Where to surface the randomise option (pre-start screen design)

**Decision**: Add a new pre-start modal in `workouts.ts` that opens when the user clicks the "Start" button. The modal shows the exercise list, a "Randomise order" toggle, and a "Start Workout" button. The existing direct navigation (`navigate('/active-session?id=...')`) becomes the confirmation action inside the modal.

**Rationale**: There is no existing pre-start screen — the "Start" button currently navigates immediately to the active session. The modal approach:
- Follows the established pattern: `workouts.ts` already contains a delete confirmation modal and an edit modal (both rendered inline in the page HTML, controlled by `initDeleteModal()` / `initEditModal()` functions). A pre-start modal is a third modal in the same file using the same `[name]-modal-backdrop` BEM pattern.
- Avoids adding a new route/page, which would require changes to `router.ts` and `main.ts` and introduce a page that only exists as a transitional state.
- Keeps the exercises data in scope: `workouts[]` is already loaded in `workouts.ts` and each `Workout` object already includes its `exercises[]` array (returned by `GET /api/workouts`). No extra API call is needed to show the exercise list in the modal.
- Matches the spec requirement that the option appears "when I start the workout (maybe before?)".

**Alternatives considered**:
- A new `/pre-start?id=...` route: rejected — requires new route entry, new page file, and a page that only exists as a transition state. Adds complexity for no user-facing benefit.
- Inline toggle on the workout list card (no modal): rejected — the exercise list preview and re-shuffle action require more vertical space than a compact list card allows; the toggle also needs a clear "Confirm" action to prevent accidental starts.
- Modifying the active-session page to show a shuffle option before loading: considered but rejected — `active-session.ts` is the logging page; mixing pre-start UX into it couples two distinct interactions and would complicate the already-complex `render()` function.

---

## Decision 2: How to pass the shuffled order to active-session

**Decision**: Encode the shuffled exercise IDs as a comma-separated URL query parameter `order`, e.g.:
`/active-session?id=<workoutId>&order=<guid1>,<guid2>,<guid3>`.

`active-session.ts` reads this parameter after loading the workout, and reorders `workout.exercises` in memory before rendering.

**Rationale**:
- Stateless and simple: no additional client-side storage (no `sessionStorage`/`localStorage`) required. The order is self-contained in the URL.
- Consistent with how `workoutId` is already passed (`?id=...`). Adding `?order=...` follows the same `URLSearchParams` pattern already used in `active-session.ts` for `params.get("id")`.
- The `workout.exercises` array from the API is already validated server-side. The client re-orders an already-loaded, already-validated array — no new trust boundary is introduced.
- If the `order` parameter is absent or malformed, `active-session.ts` falls back to the API's natural sort order (by `Sequence`), preserving the default behaviour for non-shuffled starts.

**Alternatives considered**:
- `sessionStorage` with a keyed entry: rejected — adds statefulness that is harder to reason about on navigation back/forward and page refresh; URL encapsulates all the information needed.
- A POST to a new `/api/workouts/{id}/shuffled-session` endpoint that returns a reordered exercise list: rejected — the shuffle is a client-side UX preference, not a server concern; adding a server round-trip for a pure in-memory reorder adds latency for no benefit.
- Storing the order in a module-level variable shared between `workouts.ts` and `active-session.ts`: rejected — there is no shared module state between pages in this application; pages are independent modules loaded by the router.

---

## Decision 3: Shuffle algorithm

**Decision**: Add a `shuffle<T>(arr: readonly T[]): T[]` pure function to `utils.ts` using the Fisher-Yates (Knuth) algorithm. The function returns a new shuffled array and does NOT mutate the input. The result is seeded by `Math.random()`.

```typescript
export function shuffle<T>(arr: readonly T[]): T[] {
  const result = [...arr];
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}
```

**Rationale**:
- Fisher-Yates produces a uniformly random permutation in O(n) — the correct algorithm for an unbiased shuffle. Alternatives like `arr.sort(() => Math.random() - 0.5)` produce biased results.
- A pure function (returns new array, no mutation) is consistent with the `shuffle` use case: the pre-start modal needs to preserve the original order in the module state while displaying a shuffled preview. The existing `reorder()` function in `utils.ts` mutates in place, which is appropriate for its drag-and-drop use case. `shuffle` is different and should not mutate.
- `Math.random()` is sufficient for this use case. Cryptographic randomness (`crypto.getRandomValues`) is not required — the shuffle is a training variety tool, not a security-sensitive operation.
- Adding to `utils.ts` follows the established pattern: `getEffortLabel` (feature 005) and `reorder` (feature 006) are both in `utils.ts`. Shared, reusable, independently testable helpers go in `utils.ts`.

**Alternatives considered**:
- Inline shuffle inside `workouts.ts`: rejected — not independently testable; duplicates if ever needed elsewhere; inconsistent with the project pattern of putting helpers in `utils.ts`.
- Mutating version: rejected — the pre-start modal needs both the original order (for when toggle is turned off) and the shuffled order (for preview); a pure function is the correct abstraction.

---

## Decision 4: Recording exercise order in session data

**Decision**: Add a nullable `int Sequence` column to the `logged_exercise` table and a corresponding `Sequence` property to `LoggedExercise`. The frontend populates `Sequence` (0-based index) in `SessionLoggedExerciseItem` when saving a session, reflecting the actual display order (whether shuffled or not). A new EF Core migration `AddSequenceToLoggedExercise` applies the schema change.

**Rationale**:
- The spec (FR-006) requires the session to record the actual exercise order performed. Currently `LoggedExercise` has no ordering field — exercises in a session have no guaranteed retrieval order. Adding `Sequence` makes the recorded order explicit and durable.
- Nullable (`int?`) preserves backward compatibility: existing sessions have `null` for `Sequence`, which is a legitimate historical state. No backfill is needed.
- The `Sequence` value is set by the frontend at save time (0, 1, 2, …) in the order exercises were displayed on the active-session page. No additional API design is needed — the existing `SessionLoggedExerciseItem` DTO is extended with one nullable field.
- The migration follows the exact pattern of `AddEffortToLoggedExercise` (feature 005): one nullable column added to `logged_exercise`.

**Alternatives considered**:
- Array position in the POST body as implicit order: rejected — the array order in a JSON body is not guaranteed to be preserved in all serialization paths; explicit field is unambiguous.
- A separate `workout_session_exercise_order` table: rejected — overkill for a single integer per logged exercise; a column on `logged_exercise` is simple and direct.
- No schema change; order inferred from `PlannedWorkoutExercise.Sequence` on read: rejected — this would permanently lose the information about what order was actually performed in a shuffled session.

---

## Decision 5: Template immutability guarantee

**Decision**: The shuffle operates entirely on the frontend in-memory state. No API call that modifies `PlannedWorkoutExercise.Sequence` is made at any point in the shuffle flow. The only write operation in the entire feature is the existing `POST /api/workouts/{workoutId}/sessions` at session save time, extended with the `Sequence` field.

**Rationale**:
- `PlannedWorkout` and `PlannedWorkoutExercise` are never touched by this feature. The `Sequence` values on `PlannedWorkoutExercise` remain exactly as set by feature 006 (reorder exercises). Template immutability (FR-007, SC-004) is therefore structurally guaranteed — there is no code path that could mutate it.
- This also means no backend validation is needed to prevent template mutation from this feature.

---

## Decision 6: Behaviour with 1 exercise (edge case)

**Decision**: When a workout has exactly 1 exercise, the "Randomise order" toggle is hidden from the pre-start modal. The modal still appears (to let users confirm before starting), but without the shuffle controls. No shuffle is applied.

**Rationale**: A shuffle of a 1-element array is always the same order — the control would be misleading. FR-008 requires the control to be "unavailable (disabled or hidden)" for fewer than 2 exercises; hidden is chosen as cleaner than a disabled control with no explanation.

For exactly 2 exercises, the toggle is shown and enabled. There is only one alternative permutation, so the shuffle will always produce the reversed order (or, with very low probability, the same order on a re-shuffle from the same state — acceptable per the spec).

---

## Decision 7: Re-shuffle state retention on navigation away

**Decision**: If the user navigates away from the pre-start modal (e.g., closes the modal or navigates to a different page) after enabling shuffle, the shuffle state is discarded. The next time they open the pre-start modal for any workout, the toggle defaults to off.

**Rationale**: The shuffle is a session-scoped, per-activation preference (spec Assumption 5: "the randomise toggle defaults to off at the start of every session"). There is no requirement to persist or remember the toggle state between visits to the pre-start modal. Discarding it is the simplest and safest default.
