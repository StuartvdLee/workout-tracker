# Tasks: History Page Entry Redesign

**Input**: Design documents from `/specs/012-history-entry-design/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ui-contract.md ✅

**Tests**: Automated E2E tests are required. The failing test from the old design
(`HistoryPage_DateGrouping_ShowsToday`) must be replaced with two new tests that
fail before implementation and pass after.

**Organization**: This feature touches 3 files (no new files). Tasks are ordered
so tests are written first (and fail), then implementation satisfies them.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different file — no conflict risk)
- **[Story]**: Which user story this task belongs to ([US1], [US2])

---

## Phase 1: Setup

No new projects, dependencies, or files are required. All changes are in existing
files. No setup tasks needed — proceed directly to foundational work.

---

## Phase 2: Foundational — Replace Broken E2E Test

**Purpose**: The existing `HistoryPage_DateGrouping_ShowsToday` test asserts the
old `.history-group__date-label` selector. It must be removed and replaced with
two new tests **before** implementation so they fail red and can be made green.

**⚠️ CRITICAL**: Complete this before any implementation work.

- [X] T001 Replace `HistoryPage_DateGrouping_ShowsToday` with `HistoryPage_NoGroupHeaders_FlatList` and `HistoryPage_EntryShowsDateBelowName` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` — use the exact test bodies from plan.md Phase 2 Workstream C; confirm both new tests **fail** before proceeding

**Checkpoint**: Two new E2E tests exist and are red. All other existing tests still pass.

---

## Phase 3: User Story 1 — View History Without Date Group Headers (Priority: P1) 🎯 MVP

**Goal**: Replace grouped sessions with a flat list; each entry shows the workout
name in bold and the full date + time as a muted secondary line.

**Independent Test**: Navigate to History with sessions logged on different days.
Confirm no `.history-group__date-label` elements exist; confirm each
`.history-session` contains a visible `.history-session__date` element with
non-empty text.

### Implementation for User Story 1

- [X] T002 [US1] Rewrite `renderSessions()` and `renderSession()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/history.ts`

- [X] T003 [P] [US1] Update `src/WorkoutTracker.Web/wwwroot/css/styles.css`

**Checkpoint**: `HistoryPage_NoGroupHeaders_FlatList` and `HistoryPage_EntryShowsDateBelowName` are now green. History page renders a flat list with name + date.

---

## Phase 4: User Story 2 — Expand/Collapse Behaviour Preserved (Priority: P2)

**Goal**: Confirm the expand/collapse interaction and exercise detail view work
correctly after the header restructuring.

**Independent Test**: Click any history entry — details expand; click again — they
collapse. `HistoryPage_SessionExpandCollapse` and `HistoryPage_SessionDetails_ShowsExerciseData`
pass without modification.

### Implementation for User Story 2

- [X] T004 [US2] Verify expand/collapse is intact after the `renderSession()` changes — confirm `HistoryPage_SessionExpandCollapse` and `HistoryPage_SessionDetails_ShowsExerciseData` pass; if any selector breaks (e.g. `aria-expanded` or `.history-session__header`), fix the regression in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`

**Checkpoint**: All expand/collapse and exercise-detail E2E tests pass. Both user stories are independently functional.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T005 [P] TypeScript build — ✅ zero errors

- [X] T006 Full E2E suite — run all tests in `src/WorkoutTracker.E2ETests/`

- [X] T007 [P] Dark mode regression check — `.history-session__date` inherits `var(--color-text-light)` correctly in both themes

- [X] T008 [P] Quickstart validation — walkthrough confirmed matches implemented behaviour

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — start immediately
- **US1 (Phase 3)**: Depends on T001 (tests written and red first)
- **US2 (Phase 4)**: Depends on Phase 3 complete (expand/collapse lives in `renderSession()`)
- **Polish (Phase 5)**: Depends on both user stories complete

### Within User Story 1

- T002 and T003 are **parallel** (different files — `history.ts` vs `styles.css`)
- Both must complete before the Phase 3 checkpoint

### Parallel Opportunities

```bash
# Phase 3 — run simultaneously (different files):
Task T002: "Rewrite renderSessions/renderSession in history.ts"
Task T003: "Update CSS in styles.css"

# Phase 5 — run simultaneously (independent checks):
Task T005: "npm run build (TypeScript)"
Task T007: "Dark mode regression check"
Task T008: "Quickstart walkthrough"
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete T001 (write failing tests)
2. Complete T002 + T003 in parallel (implementation)
3. **STOP and VALIDATE**: `HistoryPage_NoGroupHeaders_FlatList` and `HistoryPage_EntryShowsDateBelowName` are green; the History page looks correct
4. Deploy / demo MVP

### Full Delivery

1. T001 → T002 ∥ T003 → T004 → T005 ∥ T006 ∥ T007 ∥ T008

---

## Notes

- `escapeHtml()` must remain in place for all user-supplied content rendered to the DOM (per SR-001)
- The `formatDate()` helper does not need a Vitest unit test — consistent with the page-rendering convention from features 001–009
- T003 is safe to work on simultaneously with T002 because they touch different files
- The `.history-group` CSS block and `getDateLabel()` function must be **deleted**, not commented out (constitution: no dead code)
