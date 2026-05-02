# Implementation Plan: Reorder Exercises in a Workout

**Branch**: `006-reorder-exercises` | **Date**: 2026-05-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-reorder-exercises/spec.md`

## Summary

Add drag-and-drop exercise reordering to the planned workout create form and edit modal. The backend already stores and returns exercises in `Sequence` order — this feature is a **pure frontend change** to `workouts.ts`. The `Set<string>` exercise selection model is replaced with an ordered `string[]`, and HTML5 DnD (mouse) + touch event handlers (mobile) are added to both the create and edit exercise lists, sharing a single `reorder(fromIndex, toIndex)` function. A keyboard-operable fallback (Space to pick up, ↑/↓ to move) is provided on the drag handle button. No migrations, no API endpoint changes, and no backend test changes are required.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend — no changes), TypeScript 5.9.3 (frontend — primary change)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks or libraries)  
**Storage**: PostgreSQL via EF Core — no schema changes; `planned_workout_exercise.sequence` already exists  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (backend — no changes needed); Vitest frontend unit tests (add tests for `reorder()` helper)  
**Target Platform**: Web browser (mobile-first, responsive); touch drag support required for iOS Safari and Android Chrome  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Drag reorder response within 100 ms of drop; no perceptible delay on lists up to 20 exercises  
**Constraints**: No external JS/CSS frameworks or libraries (vanilla TypeScript only); existing tests must continue to pass; touch event listeners must be non-passive to allow `preventDefault()` on `touchmove`  
**Scale/Scope**: Single-user; changes touch 2 files (`workouts.ts`, `styles.css`) + 1 new Vitest test file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. CSS follows BEM naming convention. The `Set<string>` → `string[]` migration must update every reference: state declarations, add guards (`includes` before push), `size` → `length`, `.delete()` → `filter`, `new Set()` reset → `[]`. All four call sites in `workouts.ts` must be updated consistently. A new `reorder()` helper function is extracted and placed in `utils.ts` (consistent with the `getEffortLabel` pattern from feature 005). ✅ No deviations.

- **Testing**:
  - **Backend xUnit integration tests**: No changes needed — the API contract is unchanged. All 45 existing tests must continue to pass.
  - **Frontend Vitest unit tests**: The `reorder(arr, fromIndex, toIndex)` helper function in `utils.ts` MUST have unit tests covering: move first to last, move last to first, move middle to first, no-op (same index), single-element array. This is consistent with the `normalisePath` pattern from feature 003/004.
  - **Regression**: Existing 7 Vitest tests in `router.test.ts` must continue to pass.
  - ✅ Tests treated as mandatory, not optional.

- **Security**:
  - No new API endpoints or user input types introduced.
  - The exercise order is submitted as an array of existing exercise IDs — the backend already validates each ID against the database.
  - `dataTransfer.setData` stores only an exercise array index (integer), not any user-supplied content; no injection risk.
  - `document.elementFromPoint()` returns a DOM element; the only action taken is index calculation — no dynamic HTML is generated from its return value.
  - ✅ No new trust boundaries; existing security posture unchanged.

- **User Experience Consistency**:
  - Drag handle uses the same BEM modifier pattern as other action buttons (`.workout-form__remove-btn`, `.workout-list__edit-btn`).
  - Visual states (dragging opacity, drop-target border) use existing CSS custom properties (`--color-primary`, `--color-text-muted`, `--color-surface-hover`).
  - Error states after a failed save retain the reordered list — consistent with how other forms preserve state on error.
  - Drag affordance is hidden when `exercises.length < 2` — no visual noise for non-actionable interactions.
  - `.sr-only` class: if not already in `styles.css`, it must be added (standard visually-hidden utility pattern). ✅

- **Performance**:
  - Reorder response < 100 ms: the `reorder()` function is synchronous array mutation + a full `list.innerHTML` re-render. For ≤ 20 items this is < 5 ms including layout; no async work involved. ✅
  - Touch clone follows the finger in `touchmove` — no layout recalculations on the original list during drag; only the clone is repositioned via `transform: translate`. ✅
  - No new API calls are introduced by the reorder action itself — only the existing create/save API calls remain. ✅

## Project Structure

### Documentation (this feature)

```text
specs/006-reorder-exercises/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # Documents unchanged API order semantics
│   └── ui-contract.md   # Drag handle HTML/CSS/ARIA contracts
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                     # MODIFIED: add drag handle, dragging, drop-over, sr-only styles
    └── ts/
        ├── utils.ts                       # MODIFIED: add reorder() helper function
        ├── pages/
        │   └── workouts.ts                # MODIFIED: Set→Array, drag-and-drop, keyboard reorder, ARIA
        └── __tests__/
            └── workouts.test.ts           # NEW: Vitest unit tests for reorder() helper

src/WorkoutTracker.Infrastructure/        # UNCHANGED
src/WorkoutTracker.Api/                   # UNCHANGED
src/WorkoutTracker.Tests/                 # UNCHANGED (backend tests)
```

**Structure Decision**: The existing .NET Aspire solution structure is preserved. No new projects. The `reorder()` function is added to the existing `utils.ts` shared module (consistent with `getEffortLabel` from feature 005 — shared logic goes in utils, not in page modules). The Vitest test file follows the existing `__tests__/router.test.ts` pattern.

## Complexity Tracking

> No constitution violations — no entries required.

## Implementation Phases

### Phase 0: Research & Clarification

**Output**: `research.md` ✅ Complete

**Key findings**:
1. **No backend changes needed** — `sequence` field already exists, is already written and read by the API.
2. **HTML5 DnD + touch event parallel implementation** — both paths call a shared `reorder(fromIndex, toIndex)` function.
3. **`Set<string>` → `string[]`** — uniqueness enforced by an `includes` guard on add.
4. **Six-dot SVG grip icon** — left side of each exercise item; `cursor: grab`/`grabbing`.
5. **Full re-render** on every mutation — simple, correct, performant for this list size.

### Phase 1: Design & Contracts

**Output**: All complete ✅

- `research.md` — all unknowns resolved
- `data-model.md` — no schema changes; documents `Set→Array` state model change
- `contracts/api-contract.md` — documents unchanged API order semantics for completeness
- `contracts/ui-contract.md` — drag handle HTML/CSS/ARIA specification
- `quickstart.md` — user-facing walkthrough for mouse, touch, and keyboard reorder

**Constitution Check (post-design)**: All Phase 1 outputs confirm no backend changes, no migrations, no new API surfaces. UX patterns follow existing conventions. No constitution violations.

### Phase 2: Implementation (Dependency Graph)

**Prerequisites**: Phase 1 complete ✅

**Workstream A: Shared utility**
1. Add `reorder<T>(arr: T[], fromIndex: number, toIndex: number): void` to `utils.ts`
   - Validates `fromIndex` and `toIndex` are within bounds before mutating
   - Mutates in place: `arr.splice(toIndex, 0, arr.splice(fromIndex, 1)[0])`
2. Add Vitest unit tests in `__tests__/workouts.test.ts` covering move-first-to-last, move-last-to-first, move-middle-to-first, no-op, single-element

**Workstream B: `workouts.ts` — state model change (`Set→Array`)**
1. Change declarations: `let selectedExercises: string[] = []` and `let editSelectedExercises: string[] = []`
2. Update all add operations: `if (!selectedExercises.includes(id)) selectedExercises.push(id)`
3. Update all remove operations: `selectedExercises = selectedExercises.filter(id => id !== removeId)`
4. Update all empty checks: `.size === 0` → `.length === 0`
5. Update all resets: `= new Set()` → `= []`
6. Update all `Array.from(selectedExercises)` → direct array use (already an array)
7. Update `renderSelectedExercisesList` and `renderEditSelectedExercisesList` to iterate array instead of Set
8. Update `renderExerciseDropdown` and `renderEditExerciseDropdown` to use `!selectedExercises.includes()` check

**Workstream C: `workouts.ts` — drag-and-drop**

*All DnD event listeners are attached to the `<ul>` container via delegation to survive re-renders.*

1. **`buildSelectedExerciseItem`** — add six-dot drag handle `<button>` (left of name), `draggable="true"` on `<li>`, `data-index`, `aria-roledescription="sortable item"` (only when list has ≥ 2 items)
2. **HTML reorder announce region** — add `<div class="sr-only" aria-live="polite" id="workout-reorder-announce">` to create form and edit modal markup in `render()`
3. **`initDragAndDrop(listId, getArray, onReorder)`** — shared function that attaches HTML5 DnD + touch listeners to a `<ul>`:
   - `dragstart`: store `draggingIndex`; `dataTransfer.setData('text/plain', index)` (Firefox compatibility); `effectAllowed = 'move'`; add `body.is-dragging` class
   - `dragover`/`dragenter` on `<ul>`: `preventDefault()`; add `.workout-selected__item--drag-over` to target `<li>`
   - `dragleave` on `<ul>`: remove `.workout-selected__item--drag-over`
   - `drop` on `<ul>`: get target index from `closest('li').dataset.index`; call `reorder(arr, from, to)`; trigger `onReorder()`; announce result
   - `dragend` on `<ul>`: remove `body.is-dragging`; remove all drag visual classes
   - `touchstart` on drag handle buttons (non-passive): record start index; clone element; position fixed; set original to `.workout-selected__item--dragging`; `document.body.classList.add('is-dragging')`
   - `touchmove` (non-passive): reposition clone via `transform: translate`; use `elementFromPoint()` to find target `<li>`; add `.workout-selected__item--drag-over`
   - `touchend`: commit reorder; remove clone; remove all visual state; call `onReorder()`; announce result
4. **Keyboard reorder** on drag handle `<button>` `keydown`:
   - `Space`: toggle picked-up state; `aria-pressed` toggle
   - `ArrowUp`/`ArrowDown`: if picked up, call `reorder()`; re-render; move focus to the handle at the new index; announce
   - `Enter`: confirm (same as second Space)
   - `Escape`: restore original position; clear picked-up state
5. Call `initDragAndDrop('workout-selected-list', () => selectedExercises, renderExerciseDropdown)` in `initForm()`
6. Call `initDragAndDrop('edit-selected-list', () => editSelectedExercises, renderEditExerciseDropdown)` in `renderEditExerciseDropdown()` (after re-render)

**Workstream D: `styles.css`**
1. Add `.workout-selected__drag-handle` styles: flex, 44×44px touch target, `background: none`, `border: none`, `cursor: grab`, muted colour, hover/focus-visible states
2. Add `body.is-dragging .workout-selected__drag-handle { cursor: grabbing }`
3. Add `.workout-selected__item--dragging { opacity: 0.4 }`
4. Add `.workout-selected__item--drag-over { border-top: 2px solid var(--color-primary) }`
5. Add `.sr-only` visually-hidden utility class (if not already present)
6. Update `.workout-selected__item` to `display: flex; align-items: center` (drag handle sits left of name)

**Dependencies**:
- Workstream B must complete before C (C depends on `string[]` type)
- Workstream A can proceed in parallel with B and D
- All workstreams must complete before final integration test run
