# Research: Reorder Exercises in a Workout

**Feature**: `006-reorder-exercises`  
**Date**: 2026-05-02

## Summary

All design decisions are resolved. No backend changes are required — the `Sequence` field on `PlannedWorkoutExercise` already exists and is already read and written by the API. This feature is a **pure frontend change**: replacing the `Set<string>` exercise selection model with an ordered `string[]` and adding drag-and-drop reordering to the exercise lists in the create form and edit modal.

---

## Decision 1: Backend Changes Required

**Decision**: None required.

**Rationale**: `PlannedWorkoutExercise.Sequence` (integer, `NOT NULL`) is already present in the data model (introduced in feature 004). The API already:
- Assigns `Sequence = i + 1` (1-indexed) based on the order of the `exercises[]` array in `POST /api/workouts` and `PUT /api/workouts/{id}` request bodies.
- Returns exercises ordered by `Sequence` in `GET /api/workouts`, `GET /api/workouts/{id}`, and the created/updated responses.

Sending exercises in the desired order in the request body is all that is needed. No new endpoints, DTOs, migrations, or API changes are required.

**Alternatives considered**: Adding a dedicated `PATCH /api/workouts/{id}/exercises/reorder` endpoint — rejected as unnecessary; the existing PUT endpoint already replaces the full exercise list and re-derives sequence from submission order.

---

## Decision 2: Selected Exercise State — `Set` → `Array`

**Decision**: Replace `Set<string>` with `string[]` for `selectedExercises` and `editSelectedExercises` in `workouts.ts`.

**Rationale**: `Set<string>` preserves insertion order but provides no API for arbitrary reordering. Swapping two elements requires reconstructing the entire set. `Array<string>` with guard functions (`includes` check on add, `filter`/`splice` for remove, `splice`+`splice` for reorder) provides all the same invariants plus O(1) in-place reordering.

**Uniqueness enforcement**: A guard in the add operation (`if (!selectedExercises.includes(id)) selectedExercises.push(id)`) replaces the automatic deduplication that `Set` provided. This is called before every add.

**Alternatives considered**: A wrapper class/Map hybrid — rejected as over-engineered for a list of tens of items.

---

## Decision 3: Drag-and-Drop Implementation Strategy

**Decision**: Implement drag-and-drop using the **HTML5 DnD API** (mouse/pointer) with a **parallel touch event handler** (touchstart/touchmove/touchend) for mobile. Both paths share a single `reorder(fromIndex, toIndex)` function that mutates the array and triggers a full re-render.

**Rationale**:
- HTML5 DnD works well on desktop browsers (Chrome, Firefox, Safari, Edge) and is zero-dependency.
- HTML5 DnD does **not** work on iOS Safari and is unreliable on Android Chrome; a touch event handler is required.
- Sharing a single `reorder()` function between the two paths avoids duplication and keeps both paths in sync.
- Full re-render on every reorder is simple and correct; performance is acceptable for lists of at most 20 exercises.

**Key implementation details**:
- `dragstart`: store `draggingIndex` in module scope; set `dataTransfer.effectAllowed = 'move'` and `dataTransfer.setData('text/plain', index)` (mandatory for Firefox).
- `dragover`/`dragenter` on the `<ul>`: `event.preventDefault()` to allow drop; add visual "drag-over" CSS class to the target `<li>`.
- `drop` on the `<ul>`: call `reorder(fromIndex, toIndex)`.
- `dragend` on `<ul>`: clean up all visual state.
- Event listeners attached to the **`<ul>` container** (delegation), not each `<li>`, to survive re-renders.
- Touch handler registered as **non-passive** (`{ passive: false }`) so `touchmove` can call `event.preventDefault()` to suppress scroll.
- During touch drag, the dragged `<li>` is set to `opacity: 0.4` and a fixed-position clone follows the finger. `pointer-events: none` is set on the clone so `document.elementFromPoint()` returns the element beneath.

**Alternatives considered**:
- Pointer Events API — rejected; still requires the same manual implementation as touch events and adds no benefit over combining HTML5 DnD + touch.
- Third-party library (e.g., Sortable.js) — rejected; the project constraint is "no external JS/CSS frameworks or libraries".

---

## Decision 4: Keyboard Accessibility for Reordering

**Decision**: Each exercise `<li>` gets a drag handle `<button>` with `aria-label="Drag to reorder [exercise name]"`. The handle also supports keyboard reordering via **Up/Down arrow keys** and **Space** to select/deselect for moving. An `aria-live="polite"` region announces the result of each reorder.

**Rationale**: HTML5 DnD is not keyboard accessible. The specification (FR-006) requires the feature to work across input types. A keyboard-operable secondary mechanism on the drag handle button satisfies accessibility requirements without adding a separate modal dialog.

**Keyboard interaction model**:
- Tab to the drag handle button.
- Press `Space` to "pick up" the item (toggle selection; `aria-pressed="true"` or custom state).
- Press `↑` / `↓` to move the item one position.
- Press `Space` or `Enter` again to "drop" (deselect).
- Press `Escape` to cancel and return to original position.
- On every move, the `aria-live` region announces "Exercise [name] moved to position [N] of [total]".

---

## Decision 5: DOM Rendering Strategy

**Decision**: Full re-render of the selected-exercises `<ul>` on every mutation (add, remove, reorder).

**Rationale**: Simple, predictable, and no risk of DOM/data divergence. Each render call clears `list.innerHTML` and rebuilds from the array. Event listeners are attached via delegation on the `<ul>` container (for DnD events) and directly on dynamically created buttons (for remove and keyboard reorder) — the delegation listeners survive re-renders, the button listeners are attached during render.

**Alternatives considered**: Surgical DOM manipulation (insertBefore) — rejected for the same reasons as in feature 005 research: tight coupling between DOM and model, risk of divergence.

---

## Decision 6: Drag Handle Visual Affordance

**Decision**: Use a **six-dot SVG grip icon** as the drag handle, positioned on the left side of each exercise `<li>`. CSS: `cursor: grab` at rest; `cursor: grabbing` while actively dragging (applied to `body` via a class on `dragstart`/`touchstart`).

**Rationale**: The six-dot grip is the most widely recognised drag affordance on the web (Google, Notion, Trello). Positioning it on the left is the standard for list reordering. `cursor: grab`/`grabbing` is the correct CSS cursor pair for drag handles (not `cursor: move`).

**ARIA**: The handle `<button>` has `aria-label="Drag to reorder [exercise name]"`. The `<li>` has `aria-roledescription="sortable item"`. An `aria-live="polite"` region at the top of the selected-exercises section announces reorder results.

---

## Decision 7: Scope — Create Form and Edit Modal Only

**Decision**: Drag-to-reorder is added to:
1. The create-workout selected exercises list (`#workout-selected-list` / `selectedExercises: string[]`)
2. The edit-workout modal selected exercises list (`#edit-selected-list` / `editSelectedExercises: string[]`)

The **active session view** (`active-session.ts`) and the **workout list** display (`renderWorkoutList`) are **out of scope** — exercises in an active session are already locked to the planned workout order, and the workout list is read-only.

---

## Resolved Unknowns

| Unknown | Resolution |
|---|---|
| Does the backend need changes? | No — `Sequence` is already stored and returned |
| Which DnD approach works cross-device? | HTML5 DnD (mouse) + touch event handler (mobile) sharing `reorder()` |
| Does adding/removing after reorder break sequence? | No — sequence is re-derived from array order on every API submission |
| Is `Set→Array` a breaking change for the API? | No — the API already accepts an ordered array of exercise objects |
| Do any existing tests need updating? | Backend tests: no change needed (API contract unchanged). Frontend Vitest tests: `workouts.ts` is not currently covered by tests; new unit tests for the `reorder()` helper function should be added |
