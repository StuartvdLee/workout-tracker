# Research: Sticky Exercise Column

## Context

This feature reuses the session-detail page and table patterns established by:

- `specs/014-workout-history-detail-page/plan.md` and its UI contract
- `specs/031-edit-past-workouts/plan.md` and its edit-mode UI contract
- `specs/032-workout-sets-selection/plan.md`, which expanded the table to seven columns and established the current horizontal overflow behavior
- `specs/024-change-effort-slider-colours/plan.md` and `specs/028-sidebar-history-icon/plan.md` for the repository's approach to small, frontend-only visual changes

## Decision 1: Keep the first column within the existing table

**Decision**: Keep the Exercise header and exercise-name cells in the existing semantic table and make them remain at the inline start of the existing horizontal scroll container.

**Rationale**: The table already preserves row and column semantics, the wrapper already owns horizontal scrolling, and both view and edit modes use the same table classes. Keeping one table avoids duplicated markup and guarantees row alignment.

**Alternatives considered**:

- **Render a separate exercise-name table beside the statistics table**: rejected because independent rows can lose vertical alignment when names wrap or edit controls change row height.
- **Copy exercise names into a floating overlay with script-driven positioning**: rejected because it adds state synchronization, resize handling, and accessibility duplication for a browser-native layout requirement.
- **Replace the table with a custom grid**: rejected because it would be a broad structural change and risk regressions to established semantics and editing behavior.

## Decision 2: Use the existing table wrapper as the scrolling boundary

**Decision**: Retain `.session-detail__table-wrapper` as the sole horizontal scroll container and anchor the Exercise column to its left edge.

**Rationale**: The wrapper already uses horizontal overflow and visually contains the table. This makes the fixed column local to the table card rather than fixed to the page or viewport.

**Alternatives considered**:

- **Fix the column to the browser viewport**: rejected because it would detach the column from the table when the page layout moves.
- **Add a second nested scroller**: rejected because nested horizontal scrolling is confusing and unnecessary.

## Decision 3: Give fixed cells opaque, theme-aware surfaces and explicit layering

**Decision**: The entire fixed Exercise column, including its body cells, uses the existing light-grey header-row surface colour, while the header layers above body cells. A subtle existing border-colour trailing edge distinguishes the fixed column from moving statistics; the header row has no bottom divider.

**Rationale**: Opaque backgrounds are required for values to disappear behind the names. Existing colour tokens automatically preserve light/dark theme behavior, while explicit layering prevents scrolling content from painting above the fixed cells.

**Alternatives considered**:

- **Transparent fixed cells**: rejected because statistic text remains visible beneath the exercise names.
- **Hard-coded light and dark colours**: rejected because it duplicates theme definitions and can drift from the surrounding table.
- **Apply clipping or masks to every statistic column**: rejected because the fixed column itself can provide the required occlusion with less complexity.

## Decision 4: Apply the behavior to view and edit tables through shared classes

**Decision**: Use the existing shared session-detail table/header/cell classes so both read-only and editing modes receive identical fixed-column behavior without TypeScript markup changes.

**Rationale**: `renderDetailTable` and `renderEditTable` already emit the same table wrapper, table, header, and exercise-cell classes. A shared presentation rule prevents mode-specific drift.

**Alternatives considered**:

- **Add different sticky classes in each render function**: rejected because it requires unnecessary TypeScript changes and duplicates a presentation concern.
- **Support view mode only**: rejected because entering edit mode would reintroduce the same loss of exercise context.

## Decision 5: Verify geometry and occlusion with Playwright

**Decision**: Add focused Playwright coverage to the existing `WorkoutHistoryTests.cs` session-detail tests. Verify that scrolling changes the statistic cell position while the Exercise header/cell remain aligned to the wrapper's left edge, and verify the fixed cells have opaque backgrounds and correct stacking in view and edit modes.

**Rationale**: Sticky positioning, scroll offsets, paint order, and rendered geometry require a real browser. Vitest/jsdom does not provide reliable layout measurements, so a frontend unit test would not prove the behavior.

**Alternatives considered**:

- **CSS text-presence assertion only**: rejected because it does not prove behavior in a browser.
- **Manual-only validation**: rejected because the constitution requires automated regression coverage for behavior changes.
- **Screenshot-only comparison**: rejected as the primary check because pixel snapshots are more environment-sensitive than targeted geometry assertions; screenshots may supplement manual review.

## Decision 6: Keep the change frontend-only and bounded

**Decision**: Modify only session-detail CSS and focused E2E coverage. Do not change data models, APIs, persistence, routing, or table data.

**Rationale**: The requested behavior is purely presentational, all required semantic hooks already exist, and adding new data or runtime logic would not improve the result.

**Alternatives considered**:

- **Persist a user preference for fixed columns**: rejected because the feature is required behavior, not a configurable preference.
- **Add runtime scroll listeners**: rejected because browser-native positioning provides the behavior without per-frame script work.

## Decision 7: Size the Exercise column from its content

**Decision**: Use automatic table layout with `width: 1%` on the first header/body cells, `white-space: nowrap` on exercise names, and trailing `var(--spacing-md)` padding. Do not set a fixed pixel or percentage width for the Exercise column.

**Rationale**: The first column should be only as wide as the longest displayed exercise name plus a small margin, while the existing table minimum width and statistic-column sizing preserve horizontal scrolling. The shrink-to-content hint prevents automatic layout from absorbing excess table width.

No `NEEDS CLARIFICATION` items remain.
