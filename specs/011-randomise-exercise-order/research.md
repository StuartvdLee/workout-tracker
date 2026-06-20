# Research: Randomise Exercise Order UX Simplification

**Feature**: `011-randomise-exercise-order`  
**Date**: 2026-05-09  
**Phase**: 0 — Resolve unknowns from Technical Context

---

## Decision 1: Homepage toggle placement and show/hide logic

**Decision**: The "Randomise exercise order" iOS-style toggle is placed **inline inside the `workout-form`**, between the error message div and the "Start Workout" submit button. It uses a new `workout-form__randomise` row containing a label and an iOS-style `role="switch"` button (`workout-form__randomise-btn`). The toggle is hidden by default and revealed only when the selected workout has 2 or more exercises.

To know the exercise count without an extra API call, the `PlannedWorkout` interface in `home.ts` is updated to include `exerciseCount: number` — this field is already returned by `GET /api/workouts` (confirmed: the API selects `ExerciseCount = pw.Exercises.Count`). A `Map<string, PlannedWorkout>` replaces the `Set<string>` used for ID validation, so exercise count is immediately available on select change.

**Rationale**:
- Inline placement (not in a modal) is precisely what the spec requires: the toggle must be visible before clicking "Start Workout", not inside a confirmation step.
- Using the already-returned `exerciseCount` field avoids any additional API call for toggle show/hide logic.
- Updating `PlannedWorkout` to include `exerciseCount` is a minimal interface extension; the field is already present in the API response. No backend change is needed.
- The toggle defaults to off on every page load and on every workout select change, consistent with FR-002 and the principle of least surprise.

**Alternatives considered**:
- Always show the toggle regardless of exercise count: rejected — FR-005 requires the toggle to be hidden for single-exercise workouts (randomising one exercise is a no-op and the control would confuse users).
- Fetch workout detail on select change to get exercise count: rejected — this adds a latency-sensitive API call on every dropdown change; `exerciseCount` is already in the list response and should be used.
- Load workouts with full exercises array in `home.ts` (store the whole array): considered but rejected — the toggle show/hide only needs `exerciseCount`, not the full exercises list. The full list is only needed when the toggle is on and "Start Workout" is clicked.

---

## Decision 2: Homepage navigation flow (toggle off vs. on)

**Decision**: 
- **Toggle off**: Navigate directly to `/active-session?id=<workoutId>` with no intermediate API call.
- **Toggle on**: Fetch `/api/workouts/<workoutId>` to get the exercise IDs, shuffle them, then navigate to `/active-session?id=<workoutId>&order=<shuffled-ids>`.

This is more efficient than feature 010's approach (which always fetched workout detail before navigating).

**Rationale**:
- When the toggle is off, the active session page (`active-session.ts`) already fetches the workout detail itself. The homepage has no need to pre-fetch it.
- When the toggle is on, the homepage needs the exercise IDs to construct the `?order=` parameter. The existing `GET /api/workouts/{id}` endpoint is the correct source.
- If the API call fails when the toggle is on, the fallback (consistent with feature 010) is direct navigation without `?order=`, so the session still starts in the default order.
- This change eliminates one API call on every "Start Workout" action when the toggle is off — a small but real performance improvement.

**Alternatives considered**:
- Always fetch workout detail before navigating (keep feature 010 behaviour): rejected — unnecessary latency when the user just wants to start without randomising.
- Fetch workout detail eagerly on select change and cache it: rejected — adds API calls on every selection, most of which will never be used (user may browse the dropdown without starting).

---

## Decision 3: Workouts page simplified modal design

**Decision**: The existing complex pre-start modal is replaced by a minimal modal with:
- Title: `"Randomise exercise order?"` (serves as the question, no separate `<p>` needed)
- Two action buttons: `"Yes"` (primary style) and `"No"` (secondary/outline style)
- No exercise preview list, no toggle, no re-shuffle button

The modal BEM block remains `prestart-modal` / `prestart-modal-backdrop` for CSS continuity. The `trapModalTabKey` focus trap from `prestart-modal.ts` is retained. Escape closes without starting.

"Yes" → shuffle `prestartWorkout.exercises` (already in memory) → navigate with `?order=`  
"No" → navigate directly with `?id=` only

**Rationale**:
- The `Workout` objects loaded by the Workouts page already include the full `exercises` array (returned by `GET /api/workouts`). No additional API call is needed when clicking "Yes" — the shuffle is an in-memory operation.
- Retaining the `prestart-modal` BEM block avoids renaming CSS classes and reduces the diff.
- The simplified modal has fewer state variables: `prestartIsShuffled`, the shuffle toggle, and the exercise preview list are all removed. The only remaining state is `prestartWorkout` and `prestartTriggerBtn`.
- Focus return to the trigger button is preserved (accessibility requirement per existing pattern).

**Alternatives considered**:
- Replace the modal with two separate "Start (Shuffled)" and "Start" buttons on each workout card: rejected — clutters the list UI and is not what the spec describes.
- Skip the modal for the Workouts page entirely (use an inline toggle like homepage): rejected — the spec explicitly requires a Yes/No modal for the Workouts page start flow.
- Use a native `confirm()` dialog: rejected — native dialogs cannot be styled and are inconsistent with the app's established modal pattern.

---

## Decision 4: Fate of `prestart-modal.ts`

**Decision**: `prestart-modal.ts` is **kept** but trimmed: the `PrestartExercisePreview` interface and `renderPrestartExercisePreview` function are removed. The `getVisibleModalButtons` helper and `trapModalTabKey` export are retained; `trapModalTabKey` is still used by the simplified workouts page modal.

`home.ts` removes its import of `renderPrestartExercisePreview` and `trapModalTabKey` (the homepage no longer has any modal; it needs neither).

**Rationale**:
- `trapModalTabKey` is a genuinely shared utility and should remain in its own module to stay reusable for future modals.
- `renderPrestartExercisePreview` has no callers after this feature and must be removed to comply with `noUnusedLocals` in the TypeScript compiler config.
- Deleting `prestart-modal.ts` entirely would require moving `trapModalTabKey` into `workouts.ts` as a private function — less reusable and inconsistent with the project's pattern of shared utilities in dedicated modules.

**Alternatives considered**:
- Move `trapModalTabKey` into `utils.ts`: considered but rejected — `utils.ts` contains pure data-manipulation functions; DOM-specific keyboard helpers don't belong there.
- Keep `renderPrestartExercisePreview` as dead code: rejected — TypeScript strict mode (`noUnusedLocals`) would flag it as an error.

---

## Decision 5: CSS changes

**Decision**:
- **Remove**: `.prestart-modal__reshuffle-btn*` styles (re-shuffle button gone entirely).
- **Remove**: `.prestart-modal__exercise-list`, `.prestart-modal__exercise-item`, `.prestart-modal__exercise-empty` styles (exercise preview list gone).
- **Remove**: `.prestart-modal__shuffle` row styles from the modal block (the shuffle toggle row no longer exists inside the modal).
- **Keep**: `.prestart-modal__shuffle-btn` geometry/transition CSS — reused as the visual template for the new homepage inline toggle button.
- **Add**: `workout-form__randomise` row styles (flex row with label and toggle) for the homepage inline control.
- **Add**: `workout-form__randomise-btn` and `workout-form__randomise-btn[aria-checked="true"]` styles — visually identical to `prestart-modal__shuffle-btn` but in the `workout-form__*` BEM block.
- **Add**: `prestart-modal__yes-btn` (primary, same as `prestart-modal__start-btn`) and `prestart-modal__no-btn` (secondary, same as `prestart-modal__cancel-btn`) as named aliases in the stylesheet.

**Rationale**: Using proper semantic class names (`yes-btn`, `no-btn`) for the simplified modal buttons makes the HTML self-documenting and avoids the naming mismatch of using `start-btn` / `cancel-btn` for buttons that now read "Yes" / "No". Duplicating the visual style via CSS is a small cost; the button styles are simple and well-understood.

---

## Summary of files changed

| File | Change type | Description |
|------|-------------|-------------|
| `home.ts` | Modify | Remove modal; add inline toggle; simplify start flow |
| `workouts.ts` | Modify | Replace complex modal with Yes/No modal; remove reshuffle/toggle/preview |
| `prestart-modal.ts` | Modify | Remove `PrestartExercisePreview` + `renderPrestartExercisePreview` |
| `styles.css` | Modify | Remove reshuffle/exercise-list CSS; add `workout-form__randomise*`; add `yes/no-btn` |

No backend files, no migration, no new files.
