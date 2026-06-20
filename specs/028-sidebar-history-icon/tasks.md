---

description: "Task list for replacing the History sidebar icon"
---

# Tasks: Sidebar History Icon Change

**Input**: Design documents from `/specs/028-sidebar-history-icon/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: No new automated tests required. Existing frontend build and Vitest suite provide regression coverage. Manual smoke check verifies visual correctness.

**Organization**: Single user story; all implementation work fits in one phase.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label (`[US1]`)
- Every task includes an explicit file path

---

## Phase 1: Setup (Shared Context)

**Purpose**: Confirm scope and canonical SVG paths before making changes.

- [x] T001 Confirm the SVG paths and required wrapper attributes in `specs/028-sidebar-history-icon/contracts/ui-contract.md`.
- [x] T002 Locate the target SVG block in `src/WorkoutTracker.Web/wwwroot/index.html` — the `<svg>` inside the `<a data-page="history">` anchor.

---

## Phase 2: User Story 1 - Updated History Icon (Priority: P1) 🎯 MVP

**Goal**: Replace the History sidebar icon (plain clock face) with the Lucide `history` icon (circular back-arrow with clock hands), while all other sidebar icons remain unchanged.

**Independent Test**: Open the application; the History sidebar item shows the Lucide history icon (arc + arrowhead corner + clock hands); Let's go!, Workouts, Exercises, and Muscles icons are unchanged; the new icon renders correctly in both light and dark themes.

- [x] T003 [US1] Replace the History SVG block (inside `<a data-page="history">`) with the Lucide `history` paths from `specs/028-sidebar-history-icon/contracts/ui-contract.md` in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T004 [US1] Verify all SVG wrapper attributes (`class="sidebar__icon"`, `width="20"`, `height="20"`, `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`, `stroke-width="2"`, `stroke-linecap="round"`, `stroke-linejoin="round"`, `aria-hidden="true"`) are preserved on the updated element in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T005 [US1] Confirm the four unchanged sidebar icons (Let's go!, Workouts, Exercises, Muscles) are unmodified in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T006 [US1] Run `cd src/WorkoutTracker.Web && npm run build && npm test` to confirm no regressions.

**Checkpoint**: User Story 1 is complete — History icon replaced, all attributes preserved, build passes.

---

## Phase 3: Polish & Verification

**Purpose**: Manual smoke check and final sign-off.

- [x] T007 [P] Open the application in a browser and perform the manual smoke check from `specs/028-sidebar-history-icon/quickstart.md` (both themes, narrow viewport).
- [x] T008 [P] Run the full E2E suite to confirm no sidebar-related regressions: `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (US1)**: Depends on Phase 1 — single story, no parallel story work.
- **Phase 3 (Polish)**: Depends on Phase 2 completion.

### Within Phase 2

- T003 is the core implementation step.
- T004 and T005 are verification steps after T003 — review the file to confirm attribute and content correctness.
- T006 runs after T004 and T005.

### Within Phase 3

- T007 and T008 are independent and can run in parallel.

---

## Parallel Execution Examples

### Phase 3 (Polish)

```bash
# These can run in parallel:
Task T007: Manual smoke check in browser (both themes, narrow viewport)
Task T008: dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```

---

## Implementation Strategy

### MVP (the entire feature is a single story)

1. Complete Phase 1 (Setup — 2 tasks).
2. Complete Phase 2 (US1 — 4 tasks).
3. Complete Phase 3 (Polish — 2 tasks).
4. Done.
