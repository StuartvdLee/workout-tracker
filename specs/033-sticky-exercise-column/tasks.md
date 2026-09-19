# Tasks: Sticky Exercise Column

**Input**: Design documents from `/specs/033-sticky-exercise-column/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/ui-contract.md`, `quickstart.md`

**Tests**: Automated Playwright coverage is required because browser geometry, scrolling, and paint order cannot be verified reliably in jsdom. Existing frontend build and Vitest suites remain regression gates.

**Organization**: Tasks are grouped by user story so the core fixed-column behavior can be delivered as the MVP before theme and responsive hardening.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because the task operates on different files or independent validation surfaces
- **[Story]**: Maps work to the corresponding user story in `spec.md`
- Every task names the exact file or validation target

## Phase 1: Setup (Baseline Verification)

**Purpose**: Establish the pre-change regression baseline and preserve existing session-detail behavior.

- [x] T001 Run the existing `SessionDetailPage_*` Playwright tests in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` and record any pre-existing failures before modifying feature code

---

## Phase 2: Foundational (Existing Shared Table Contract)

**Purpose**: Confirm the existing shared structure that both user stories rely on.

No new foundational implementation is required. `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts` already renders view and edit tables with the shared `.session-detail__table-wrapper`, `.session-detail__table`, `.session-detail__th`, and `.session-detail__cell--exercise` classes, and `src/WorkoutTracker.Web/wwwroot/css/styles.css` already defines horizontal overflow and seven-column sizing; the delivered implementation changes only the first-column presentation.

**Checkpoint**: Existing shared view/edit table markup is confirmed; user-story work can proceed without TypeScript, API, or data-model changes.

---

## Phase 3: User Story 1 - Keep Exercise Context While Scrolling (Priority: P1) 🎯 MVP

**Goal**: Keep the Exercise header and exercise-name cells fixed at the left edge while statistic columns scroll behind them in both view and edit modes.

**Independent Test**: Open a past workout at a narrow viewport, scroll the table from its left boundary to an intermediate and maximum offset, and verify the Exercise header/cells remain aligned to the wrapper while statistic cells move behind them; repeat in edit mode.

### Tests for User Story 1 ⚠️

> Write these browser tests first. Verify their red state in a temporary worktree created at the branch-point commit so the existing uncommitted stylesheet change remains untouched; run the green state in the current worktree after implementation.

- [x] T002 [US1] Add a Playwright test beside `SessionDetailPage_ShowsExerciseTable` in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` that proves the table overflows and, at zero, 25%, 50%, 75%, and maximum `scrollLeft`, statistic cells move as expected while the Exercise header/body cells stay within 2 CSS pixels of the wrapper's left edge and return to zero without an offset artifact; prove the test fails in a temporary branch-point worktree before running it against the current worktree
- [x] T003 [US1] Add an edit-mode Playwright regression test in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` that enters `#session-detail-edit`, repeats the fixed-position checks while weight, sets, and effort controls move horizontally, and proves the test fails in the same temporary branch-point worktree without modifying the existing uncommitted CSS

### Implementation for User Story 1

- [x] T004 [US1] Implement the fixed Exercise header/body cells in `src/WorkoutTracker.Web/wwwroot/css/styles.css` using the existing session-detail table selectors, the wrapper as the scroll boundary, the shared light-grey header-row surface for body and header cells, explicit stacking order, an existing border-colour trailing divider, and no header/body divider
- [x] T005 [US1] Run the new fixed-column tests plus existing session-detail table and edit-mode tests in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`, confirming view/edit alignment and no regression to seven-column content or edit controls

**Checkpoint**: User Story 1 is complete when exercise context remains visible and aligned throughout horizontal scrolling in both table modes.

---

## Phase 4: User Story 2 - Preserve Table Readability Across Themes and Sizes (Priority: P2)

**Goal**: Keep the fixed column opaque, readable, and visually consistent in light/dark themes and at supported viewport sizes without blocking access to the final statistic column or changing the zero-scroll layout.

**Independent Test**: Scroll the table at supported viewport sizes in light and dark themes, verify opaque theme-matched fixed surfaces and correct layering, confirm long exercise names remain contained, confirm the final column is fully reachable, and confirm returning to zero preserves the existing layout.

### Tests for User Story 2 ⚠️

> Add the theme and responsive assertions before making any follow-up styling adjustments.

- [x] T006 [US2] Add light- and dark-theme Playwright assertions in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` that fixed header/body cells have non-transparent computed backgrounds matching their surrounding surfaces and stack above ordinary statistic cells while scrolled
- [x] T007 [US2] Add responsive Playwright coverage in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` for an overflowing supported viewport, maximum-scroll final-column visibility, restoration to the zero-scroll layout without an offset artifact, and a long exercise name that remains contained without overlapping statistic cells

### Implementation for User Story 2

- [x] T008 [US2] Refine the fixed-column rules in `src/WorkoutTracker.Web/wwwroot/css/styles.css` to use only existing theme tokens, preserve the current `52rem` table minimum width and statistics-column allocation, use `table-layout: auto` with first-column `width: 1%`, keep exercise names unwrapped, size the Exercise column from its longest name with trailing padding and no fixed width, remove the header bottom border while retaining body-row dividers, and preserve the zero-scroll layout
- [x] T009 [US2] Run the theme, responsive, boundary, and long-name tests in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`, confirming the last column remains fully readable and fixed surfaces never reveal scrolling values beneath them

**Checkpoint**: User Story 2 is complete when all supported themes and tested viewport sizes preserve readability, opacity, alignment, and full table access.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Complete repository-wide regression, security-scope, UX, and performance verification.

- [x] T010 [P] Run `npm run build && npm test` from `src/WorkoutTracker.Web/package.json` to verify strict TypeScript compilation and the complete Vitest regression suite
- [x] T011 [P] Run the 20-sample, 50-row scroll-to-paint protocol from `specs/033-sticky-exercise-column/quickstart.md` in light and dark themes, verify at least 19 samples complete within 100 ms, calculate the p95 result, and add the dated samples, p95, pass/fail result, and available scrollbar/trackpad/touch observations to `specs/033-sticky-exercise-column/quickstart.md`
- [x] T012 Run the full `src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj` suite and `git diff --check`, then review changes to `src/WorkoutTracker.Web/wwwroot/css/styles.css` and `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` to confirm zero new JavaScript scroll handlers, network requests, data paths, dependencies, or accessibility-semantic changes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Starts immediately and establishes the baseline.
- **Phase 2 (Foundational)**: Depends on T001; requires confirmation only because the shared table contract already exists.
- **Phase 3 (US1)**: Starts after Phase 2; T002 and T003 must be authored before T004, and T005 validates the completed story.
- **Phase 4 (US2)**: Depends on US1's fixed-column behavior; T006 and T007 precede T008, and T009 validates the completed story.
- **Phase 5 (Polish)**: Depends on all selected user stories; T010 and T011 can run in parallel, then T012 performs final regression and scope review.

### User Story Dependency Graph

```text
Setup baseline (T001)
        |
Existing shared table contract (Phase 2)
        |
US1: Fixed Exercise context (T002-T005) [MVP]
        |
US2: Theme and responsive readability (T006-T009)
        |
Polish and full validation (T010-T012)
```

### User Story Dependencies

- **User Story 1 (P1)**: Has no dependency on another user story and delivers the requested MVP.
- **User Story 2 (P2)**: Builds on US1's fixed positioning but has independent theme, viewport, long-name, and final-column acceptance tests.

### Within Each User Story

1. Add the Playwright regression tests and prove they detect missing behavior in a temporary worktree at the branch-point commit, leaving the current uncommitted CSS untouched.
2. Implement or refine the CSS behavior in the current worktree.
3. Run the story's focused test set.
4. Stop at the checkpoint and verify the story independently before continuing.

### Parallel Opportunities

- T010 and T011 operate on independent automated/manual validation surfaces and can run in parallel after US2.
- Test design for US2 can be reviewed while US1 validation runs, but edits to `WorkoutHistoryTests.cs` must be serialized to avoid file conflicts.
- CSS implementation tasks must be serialized because T004 and T008 modify the same rule block in `styles.css`.

---

## Parallel Example: User Story 1

User Story 1 deliberately has limited code parallelism because both regression tests belong in one established E2E file and must precede the CSS implementation:

```text
1. T002 → add view-mode geometry regression
2. T003 → add edit-mode geometry regression
3. T004 → implement shared fixed-column CSS
4. T005 → run focused US1 tests
```

---

## Parallel Example: User Story 2

Theme and responsive scenarios share the same E2E file, so serialize source edits but parallelize review/preparation:

```text
Developer A: Prepare T006 theme/opacity/layering assertions
Developer B: Prepare T007 viewport/long-name/final-column cases
Merge serially into WorkoutHistoryTests.cs, then execute T008 and T009
```

---

## Parallel Example: Final Validation

```text
Task T010: Run frontend build and Vitest regression suite
Task T011: Run the 20-sample 50-row performance/theme protocol and record timing plus input-method evidence
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001 and confirm the Phase 2 shared-table foundation.
2. Complete T002-T003 as failing browser regressions.
3. Complete T004 to implement the fixed Exercise column.
4. Complete T005 and independently demonstrate horizontal scrolling in view/edit modes.
5. Stop and review the MVP before theme/responsive hardening.

### Incremental Delivery

1. **MVP**: US1 preserves exercise context during horizontal scrolling.
2. **Readability hardening**: US2 validates themes, viewport boundaries, long names, and final-column reachability.
3. **Release confidence**: Phase 5 runs frontend/full E2E regressions, the 20-sample 50-row performance protocol, security-scope review, and whitespace validation.

### Single-Developer Execution

Execute tasks in ID order. This is the safest path because the runtime and E2E changes are concentrated in two shared files and test-first ordering is required.

---

## Notes

- No API, storage, migration, routing, or TypeScript rendering task is required.
- Preserve the user's existing uncommitted stylesheet work; use a temporary branch-point worktree for red-state regression evidence and the current worktree for implementation/green-state validation.
- `[P]` is intentionally limited because most implementation and automated test work modifies shared files.
- Every task is scoped to the exact files identified by the implementation plan.
