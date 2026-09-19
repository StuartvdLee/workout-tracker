# Implementation Plan: Sticky Exercise Column

**Branch**: `033-sticky-exercise-column` | **Date**: 2026-09-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/033-sticky-exercise-column/spec.md`

## Summary

Keep the Exercise header and exercise-name cells visible at the left edge of the existing horizontally scrollable past-workout table while statistic columns move behind them. Reuse the current `.session-detail__table-wrapper` overflow boundary and shared view/edit table classes, adding theme-aware fixed-column presentation rules and focused Playwright regression coverage. The delivered CSS uses native sticky positioning, automatic table layout, a shrink-to-content first column, and no header/body divider. No TypeScript rendering, backend, API, storage, routing, or data-contract changes are required.

## Technical Context

**Language/Version**: CSS (frontend presentation); TypeScript ~7.0.2 and C# / .NET 10 remain behaviorally unaffected
**Primary Dependencies**: Existing vanilla TypeScript session-detail markup, browser table/sticky layout support, existing CSS custom-property theme system, Playwright
**Storage**: N/A (no schema, persistence, or payload changes)
**Testing**: Frontend TypeScript build (`npm run build`) + Vitest regression suite (`npm test`) + focused and full Playwright E2E via `WorkoutTracker.E2ETests`
**Target Platform**: Modern web browsers supported by Workout Tracker, across desktop and narrow responsive viewports
**Project Type**: Web application (ASP.NET Core/.NET Aspire host with vanilla TypeScript frontend)
**Performance Goals**: With 50 rows, at least 19 of 20 sampled horizontal scroll interactions paint within 100 ms; add zero JavaScript scroll handlers and zero network requests
**Constraints**: No external CSS/JS framework; preserve seven-column ordering, size the Exercise column from its longest displayed name without a fixed width while applying a viewport-aware maximum, expose truncated full names accessibly, preserve native table semantics, view/edit rendering, theme tokens, and complete access to the final column
**Scale/Scope**: One existing CSS file plus focused session-detail Playwright tests; no new runtime module, endpoint, migration, or dependency

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Keep the change within the existing `session-detail__*` BEM block and reuse current semantic classes/tokens. Do not add scroll listeners, duplicate table markup, new abstractions, or mode-specific styling when shared selectors suffice. Run the strict TypeScript build and existing frontend tests as regression gates even though runtime TypeScript is unchanged. ✅
- **Testing**: Add Playwright coverage because sticky geometry and paint order require a real browser. Tests cover view mode, edit mode, left/right boundaries, fixed positioning, opaque surfaces/layering, and final-column reachability. Existing session-detail E2E tests and frontend tests remain mandatory regressions. ✅
- **Security**: No new input, output, storage, authorization path, secret, dependency, or external integration is introduced. Existing exercise-name escaping and data access remain unchanged. ✅
- **User Experience Consistency**: Reuse the current table wrapper, typography, border, shared light-grey header-row surface, and light/dark theme tokens while allowing the Exercise column to size from its longest name with trailing spacing. The header/body divider is removed while body-row dividers remain. Loading, empty, error, chart, edit, save, cancel, and delete behavior remain unchanged. ✅
- **Performance**: Use browser-native layout behavior with no JavaScript event handler or per-frame computation. Add zero requests and zero scroll handlers. For a 50-row table, require at least 19 of 20 sampled scroll interactions to paint within 100 ms, alongside automated geometry assertions. ✅

## Project Structure

### Documentation (this feature)

```text
specs/033-sticky-exercise-column/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-contract.md
└── tasks.md             # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    └── css/
        └── styles.css                      # MODIFIED: fixed Exercise header/cells,
                                            # opaque shared surface, layering, trailing divider, no header divider

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs              # MODIFIED: browser geometry and
                                            # view/edit horizontal-scroll coverage
```

**Structure Decision**: Preserve the existing session-detail page structure introduced by feature 014 and extended by features 031 and 032. Both view and edit tables already share `.session-detail__table-wrapper`, `.session-detail__table`, `.session-detail__th`, and `.session-detail__cell--exercise`, so this is a CSS-only runtime change with browser-level tests in the existing history E2E suite.

## Phase 0: Research Outcomes

Research is captured in [research.md](./research.md).

Key decisions reused from previous specs and current code:

1. **Feature 014** established the semantic table, horizontal detail-page surface, BEM naming, and Playwright ownership in `WorkoutHistoryTests.cs`.
2. **Feature 031** established that view and edit modes render separate tables with shared session-detail classes; the fixed-column rule must therefore target the shared presentation contract.
3. **Feature 032** established the seven-column order and `52rem` minimum width that intentionally produces horizontal overflow; this feature retains those constraints while allowing the first column to size from its content.
4. **Features 024 and 028** establish the repository pattern for small visual changes: retain existing markup and tokens, keep the runtime change surgical, and combine automated regression tests with a manual visual check.
5. Browser-native fixed positioning inside the current overflow wrapper avoids duplicated rows, scroll-event handlers, and accessibility regressions.
6. Opaque theme-token backgrounds plus explicit stacking are required for statistic values to disappear behind the Exercise column.
7. The delivered sizing strategy is `table-layout: auto`, `width: 1%` on the first header/body cells, `white-space: nowrap`, trailing `var(--spacing-md)` padding, and `max-width: min(20rem, 55vw)` on exercise cells. This replaces the former first-column percentage width, truncates extreme names with an ellipsis, and preserves statistic space. Full names remain available through cell text, `aria-label`, and `title`.

No `NEEDS CLARIFICATION` markers remain.

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Documents that persistence is unchanged and defines the fixed-column display entity, invariants, and UI transitions
- [contracts/ui-contract.md](./contracts/ui-contract.md): Defines scroll, occlusion, alignment, theme, accessibility, mode, and automated-verification behavior
- [quickstart.md](./quickstart.md): Implementation order and targeted/full verification commands
- `.github/copilot-instructions.md` updated to reference this plan

## Test Design

### Automated browser coverage

Add focused tests beside `SessionDetailPage_ShowsExerciseTable` in `WorkoutHistoryTests.cs`:

1. **View-mode fixed-column test**
   - Create/load a session with enough table width to overflow at a narrow viewport.
   - Assert `scrollWidth > clientWidth` on `.session-detail__table-wrapper`.
   - Capture the wrapper's left content edge and the bounding rectangles of the first header cell, first exercise cell, and a non-fixed statistic cell.
   - Set `scrollLeft` to zero, 25%, 50%, 75%, and maximum scroll offsets, waiting for a paint frame after each update, and assert:
     - the statistic cell moves horizontally at non-zero offsets;
     - the first header and exercise cell remain aligned to the wrapper's left edge within a 2 pixel tolerance;
     - the first header and exercise cell remain aligned with each other;
     - returning to zero leaves no offset artifact.
   - At the maximum offset, assert the final header/cell can be fully viewed.

2. **Occlusion/theme test**
   - At a non-zero scroll position, inspect computed backgrounds and stacking values for the fixed header/body cell and an ordinary statistic cell.
   - Assert the fixed backgrounds are not transparent and fixed stacking is above ordinary cells.
   - Run the essential assertions in light and dark resolved themes, or parameterize the test if the existing fixture supports it without duplication.

3. **Edit-mode regression test**
   - Enter edit mode through `#session-detail-edit`.
   - Scroll `.session-detail__table-wrapper` and repeat the fixed exercise-cell position assertion while editable controls move with statistic columns.
   - Save/cancel behavior itself remains covered by existing tests.

### Why no new Vitest unit test

The runtime change is CSS geometry. jsdom does not implement browser layout, scroll positioning, or paint-order behavior sufficiently to prove the feature. Existing Vitest tests still run as regressions, while Playwright provides the required behavioral evidence.

### Red-before-green baseline isolation

The current worktree contains an existing uncommitted stylesheet change that must be preserved. After adding the new Playwright tests, create a detached temporary worktree at `git merge-base HEAD main`, apply a reviewed patch containing only the new `WorkoutHistoryTests.cs` test changes, and run the focused tests there to capture the expected failure. Remove the temporary worktree afterward. Run the passing tests in the current worktree; do not stash, reset, overwrite, or discard the existing stylesheet change.

## Performance Budgets & Verification

| Budget ID | Metric | Target | Verification |
|---|---|---|---|
| PB-01 | Added network requests | 0 | Request observation during the E2E flow |
| PB-02 | Added JavaScript scroll handlers | 0 | Code review and source diff |
| PB-03 | Fixed-column horizontal drift | ≤ 2 CSS pixels at zero, 25%, 50%, 75%, and maximum offsets | Playwright geometry assertions |
| PB-04 | Scroll-to-paint latency with 50 rows | At least 19 of 20 samples complete within 100 ms | Playwright browser timing samples |
| PB-05 | Final-column reachability | 100% of final column visible at maximum scroll | Playwright geometry assertion |

The implementation uses native browser layout with constant presentation work per visible table cell. It introduces no network, storage, or per-scroll JavaScript path. PB-04 samples the time from assigning the target scroll offset through two animation frames, ensuring the updated browser paint has occurred before the measurement completes.

## Post-Design Constitution Check

- **Code Quality** ✅ — The design changes one existing BEM style block and one established E2E file. Shared selectors cover both table modes; no duplicate DOM or runtime scroll state is introduced.
- **Testing** ✅ — Playwright tests prove geometry, boundary reachability, surface opacity/layering, and edit-mode behavior. Existing build, Vitest, session-detail, and full E2E suites remain regression gates.
- **Security** ✅ — Display-only CSS and test changes leave data handling, escaping, authentication assumptions, and trust boundaries untouched.
- **User Experience Consistency** ✅ — Existing table structure, theme tokens, statistics-column sizing, semantics, and all surrounding page states/actions remain intact while the Exercise column becomes content-sized.
- **Performance** ✅ — No requests or script handlers are added; native browser layout is bounded by the existing table size. Explicit drift, reachability, and 50-row 19-of-20-under-100-ms budgets are defined.

No constitution violations. Plan is ready for `/speckit.tasks`.

## Complexity Tracking

> No constitution violations or exceptions identified.
