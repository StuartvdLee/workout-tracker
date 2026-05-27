---

description: "Task list for replacing Workouts and Exercises sidebar icons"
---

# Tasks: Sidebar Icons for Workouts and Exercises

**Input**: Design documents from `/specs/027-sidebar-icons/`  
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

- [x] T001 Confirm the SVG paths and required wrapper attributes in `specs/027-sidebar-icons/contracts/ui-contract.md`.
- [x] T002 Locate the two target SVG blocks in `src/WorkoutTracker.Web/wwwroot/index.html` — `data-page="workouts"` anchor and `data-page="exercises"` anchor.

---

## Phase 2: User Story 1 - Updated Sidebar Icons (Priority: P1) 🎯 MVP

**Goal**: Replace the Workouts and Exercises sidebar icons with the Lucide `dumbbell` and `sport-shoe` icons respectively, while all other sidebar icons remain unchanged.

**Independent Test**: Open the application; the Workouts item shows the dumbbell icon and the Exercises item shows the sport-shoe icon; Let's go!, Muscles, and History icons are unchanged; both new icons render correctly in light and dark themes.

- [x] T003 [US1] Replace the Workouts SVG block (inside `data-page="workouts"`) with the Lucide `dumbbell` paths in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T004 [US1] Replace the Exercises SVG block (inside `data-page="exercises"`) with the Lucide `sport-shoe` paths in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T005 [US1] Verify all SVG wrapper attributes (`class="sidebar__icon"`, `width="20"`, `height="20"`, `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`, `stroke-width="2"`, `stroke-linecap="round"`, `stroke-linejoin="round"`, `aria-hidden="true"`) are preserved on both updated elements in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T006 [US1] Confirm the three unchanged sidebar icons (Let's go!, Muscles, History) are unmodified in `src/WorkoutTracker.Web/wwwroot/index.html`.
- [x] T007 [US1] Run `cd src/WorkoutTracker.Web && npm run build && npm test` to confirm no regressions.

**Checkpoint**: User Story 1 is complete — both icons replaced, all attributes preserved, build passes.

---

## Phase 3: Polish & Verification

**Purpose**: Manual smoke check and final sign-off.

- [x] T008 [P] Open the application in a browser and perform the manual smoke check from `specs/027-sidebar-icons/quickstart.md` (both themes, narrow viewport).
- [x] T009 [P] Run the full E2E suite to confirm no sidebar-related regressions: `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no dependencies — start immediately.
- **Phase 2 (US1)**: depends on Phase 1 — single story, no parallel story work.
- **Phase 3 (Polish)**: depends on Phase 2 completion.

### Within Phase 2

- T003 and T004 are independent (different SVG blocks in the same file — edit sequentially to avoid conflicts).
- T005 and T006 are verification steps after T003 and T004.
- T007 runs after T005 and T006.

---

## Parallel Execution Examples

### Phase 3 (Polish)

```bash
# These can run in parallel:
Task T008: Manual smoke check in browser
Task T009: dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```

---

## Implementation Strategy

### MVP (the entire feature is a single story)

1. Complete Phase 1 (Setup — 2 tasks).
2. Complete Phase 2 (US1 — 5 tasks).
3. Complete Phase 3 (Polish — 2 tasks).
4. Done.
