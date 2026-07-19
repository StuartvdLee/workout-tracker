# Implementation Plan: Edit Exercise Order in Current Workout

**Branch**: `030-edit-exercise-order` | **Date**: 2026-07-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/030-edit-exercise-order/spec.md`

## Summary

Add an order-editing mode to the active/current workout screen (`active-session.ts`). A top-right "Edit order" button switches the current workout from the normal logging view into a collapsed sortable list that shows exercise names only, hides weight/effort/previous-data details, and uses the same drag, touch, keyboard, handle, live announcement, and visual behavior already implemented for the "Edit Workout" selected-exercises list. The reordered in-memory `workout.exercises` array preserves existing per-exercise log entries by exercise ID; saving the workout continues to submit `sequence` values through the existing session endpoint. No database migration, backend endpoint, or API contract change is required.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend — no changes), TypeScript ~7.0.2 (frontend — primary change)  
**Primary Dependencies**: ASP.NET Core, Aspire, EF Core/Npgsql, vanilla TypeScript, Playwright, Vitest.  
**Storage**: PostgreSQL via EF Core — no schema changes; current session save already persists `LoggedExercise.Sequence`  
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests, Playwright E2E tests, Vitest frontend unit tests for shared utilities  
**Target Platform**: Web browser (mobile-first responsive UI; mouse, touch, and keyboard reorder support)  
**Project Type**: Web application (SPA-style frontend served by ASP.NET Core / .NET Aspire orchestration)  
**Performance Goals**: Enter/exit order-editing mode within 1 second; drag/drop feedback under 100 ms for up to 50 exercises; no extra network round-trip for entering order-editing mode  
**Constraints**: No external JavaScript/CSS libraries; preserve strict TypeScript checks; reuse existing reorder behavior from `workouts.ts`; do not modify workout-template order or add backend endpoints  
**Scale/Scope**: Single-user app; changes limited to active-session UI, shared sortable-list helper extraction, CSS, and E2E coverage

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode is enforced by `src/WorkoutTracker.Web/tsconfig.json`. The existing `reorder<T>()` utility in `utils.ts` is already tested and MUST be reused. To avoid duplicating the drag/touch/keyboard implementation from `workouts.ts`, extract the reusable sortable-list behavior into a shared frontend module, then have both `workouts.ts` and `active-session.ts` use it. New CSS follows existing BEM patterns (`active-session__*` for current workout and existing `workout-selected__*` sortable-row classes for shared rows). No `any`, no broad silent fallbacks, and no backend changes.
- **Testing**: Automated coverage is mandatory. Add Playwright E2E tests for active-session order editing: button visibility, collapsed name-only mode, drag reorder updates visible order, exiting restores weight/effort controls, and existing entered values remain associated with the same exercise after reorder. Existing `reorder()` Vitest tests remain the unit-level proof for array movement; add no new utility tests unless extraction introduces testable pure helpers. Existing E2E reorder tests for create/edit workout must continue to pass after helper extraction.
- **Security**: No new external input, secrets, endpoints, or trust boundaries. The feature only reorders exercises already loaded for the current workout. Server-side authorization is unchanged in this single-user app. User-supplied exercise names continue to be inserted via `textContent`, not unsafe HTML.
- **User Experience Consistency**: The order editor MUST match the established "Edit Workout" reorder pattern: six-dot drag handle, live DOM movement during drag, touch clone behavior, keyboard pick-up/move/drop, `.sr-only` announcements, and drag visual states. The top-right "Edit order" action belongs in `.active-session__header` in normal mode; while editing, that header action is hidden and the footer `#session-save` action is relabeled to the clear exit action "Done". Collapsed rows show names only; weight, effort, previous data, and targets are hidden while editing.
- **Performance**: Enter/exit is a synchronous DOM re-render with no fetch. Reorder uses in-memory array movement and re-render, bounded by the current workout exercise count. For up to 50 exercises this stays below the 100 ms interaction budget. Existing session save path remains one POST with `sequence` values.

## Project Structure

### Documentation (this feature)

```text
specs/030-edit-exercise-order/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # Documents unchanged session API/sequence behavior
│   └── ui-contract.md   # Current workout order-editing UI contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                         # MODIFIED: active-session header/action/order-list styles; reuse sortable-row styles
    └── ts/
        ├── sortable-list.ts                   # NEW: shared sortable-list DnD/touch/keyboard behavior extracted from workouts.ts
        ├── utils.ts                           # UNCHANGED: existing reorder<T>() remains the array movement primitive
        └── pages/
            ├── active-session.ts              # MODIFIED: edit-order mode, collapsed order list, in-memory exercise reorder
            └── workouts.ts                    # MODIFIED: consume shared sortable-list helper instead of private duplicate logic

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutReorderTests.cs                 # MODIFIED: add active-session reorder E2E coverage; keep existing workout editor tests

src/WorkoutTracker.Api/                        # UNCHANGED
src/WorkoutTracker.Infrastructure/             # UNCHANGED
src/WorkoutTracker.UnitTests/                  # UNCHANGED unless existing compile-time DTOs require incidental updates
```

**Structure Decision**: Preserve the existing .NET Aspire solution and frontend folder layout. This is a frontend-focused feature with one shared TypeScript helper extraction so the current workout uses the same reorder implementation as the "Edit Workout" screen. Existing backend `LoggedExercise.Sequence` behavior is sufficient, so no migration, endpoint, or API DTO change is planned.

## Complexity Tracking

> No constitution violations — no entries required.

## Phase 0: Research & Clarification

**Output**: [research.md](./research.md)

Key decisions from prior plans reused here:

1. **Feature 006 (`006-reorder-exercises`)** established the canonical reorder UX: `reorder<T>()`, six-dot drag handle, HTML5 drag/drop, touch drag clone, keyboard fallback, `sr-only` live announcements, and no backend changes for template reordering.
2. **Feature 010 (`010-randomize-exercise-order`)** established that active-session display order is an in-memory exercise array concern and session save writes explicit `sequence` values.
3. **Feature 013 (`013-show-exercise-order`)** confirmed `LoggedExercise.Sequence` is already available and used as a session-order concept, with no schema changes needed.
4. **Feature 005 (`005-active-workout-effort`)** established active-session weight/effort controls and the requirement that entered data remain tied to exercise IDs.

No `NEEDS CLARIFICATION` markers remain.

## Phase 1: Design & Contracts

**Output**:

- [data-model.md](./data-model.md)
- [contracts/ui-contract.md](./contracts/ui-contract.md)
- [contracts/api-contract.md](./contracts/api-contract.md)
- [quickstart.md](./quickstart.md)
- `.github/copilot-instructions.md` updated to reference this plan

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts are complete.*

- **Code Quality** ✅ — Design extracts a shared sortable-list helper instead of duplicating `workouts.ts` drag/drop logic. Existing `reorder<T>()` remains the pure movement primitive. New active-session UI state is explicit (`isOrderEditing`) and keeps log data keyed by exercise ID.
- **Testing** ✅ — E2E coverage is specified for the new current-workout user journey and regression coverage is retained for existing workout editor reorder. Unit coverage for `reorder()` already exists in `utils.test.ts`.
- **Security** ✅ — No new endpoint or trust boundary; existing exercise names are rendered with `textContent`; no unsafe HTML for user-supplied values.
- **User Experience Consistency** ✅ — UI contract reuses the "Edit Workout" reorder affordances and states, including touch and keyboard behavior. Collapsed rows show only names while editing.
- **Performance** ✅ — No additional API calls. DOM work is bounded by exercise count and inherits the existing reorder approach validated for small workout lists.

No violations. Plan is ready for `/speckit.tasks`.
