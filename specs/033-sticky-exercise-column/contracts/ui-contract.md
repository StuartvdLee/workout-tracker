# UI Contract: Sticky Exercise Column

## Purpose

Define the user-visible behavior of the Exercise column in the horizontally scrollable past-workout table.

## Affected Surface

- **Route**: `/history/session?id=<sessionId>`
- **Modes**: read-only session detail and session edit mode
- **Existing scroll container**: `.session-detail__table-wrapper`
- **Existing table**: `.session-detail__table`
- **Existing header cells**: `.session-detail__th`
- **Existing exercise cells**: `.session-detail__cell--exercise`

No route, API, data, or copy contract changes are introduced.

## Scroll Contract

1. The wrapper remains the only horizontal scrolling region for the exercise statistics table.
2. When the wrapper's horizontal scroll offset increases, all statistic columns move normally.
3. The Exercise header and exercise-name cells remain visually anchored to the wrapper's left content edge.
4. When the scroll offset returns to zero, the Exercise column matches its original position and dimensions.
5. The final statistic column remains fully reachable.

## Occlusion and Layering Contract

1. Fixed exercise-name cells have an opaque light-grey surface matching the table header row.
2. The fixed Exercise header uses the same table header-row surface as the fixed body cells.
3. No horizontal divider is shown between the table header row and the first body row.
4. Moving statistic headers and cells pass behind fixed Exercise cells and are not readable through them.
5. The fixed Exercise header layers above fixed body cells where paint regions could meet.
6. No vertical divider or shadow marks the fixed column's trailing edge.
7. The table retains its normal bottom border, and the Overall Effort row has no separate top border.
8. No fixed surface may obscure the exercise text itself.

## Content and Alignment Contract

1. The `Exercise` header remains aligned with exercise names.
2. Every exercise name remains vertically aligned with the statistics in its own row.
3. The Exercise column width is determined by the longest displayed exercise name up to `min(20rem, 55vw)`, and exercise names remain on one line.
4. The Exercise column includes trailing `var(--spacing-md)` padding and uses no fixed pixel or percentage width. Names exceeding the maximum truncate with an ellipsis while retaining full cell text, `aria-label`, and `title`; statistics-column sizing, typography, and spacing remain unchanged.
5. Statistic content, row order, and column order remain unchanged.

## Mode Contract

| Mode | Required behavior |
|---|---|
| View | Exercise header and names remain fixed during horizontal scrolling |
| Edit | Exercise header and names remain fixed while inputs/selects in statistic columns scroll |
| Empty table row | Existing empty-state rendering remains unchanged |
| Loading/error | Existing non-table states remain unchanged |
| After save/cancel re-render | Fixed-column behavior remains active without user reinitialization |

## Theme Contract

- All fixed Exercise-column cells consume the existing semantic header-row surface token.
- Resolved light and resolved dark states cover the visual behavior of light, dark, and system theme preferences.
- No fixed light-colour literals are introduced for dark mode and no separate theme-specific behavior is required.

## Accessibility Contract

- Native table structure, `<th scope="col">`, row ordering, and table accessible names remain unchanged.
- The feature introduces no focusable element and changes no keyboard navigation order.
- Keyboard or assistive-technology users retain the same table semantics and horizontal scrolling mechanisms provided by the existing page/browser.
- Edit controls retain their existing accessible labels.

## Automated Verification Contract

Focused Playwright coverage must:

1. Load a session-detail table at a supported viewport where it overflows horizontally.
2. Record wrapper, Exercise header/cell, and a statistic cell geometry at zero scroll.
3. Move the wrapper to zero, 25%, 50%, 75%, and maximum horizontal offsets and wait for the browser to paint after each change.
4. Assert statistic cells move at non-zero offsets while the Exercise header/cells stay at the wrapper's left edge within 2 CSS pixels.
5. Return to zero and assert the original layout has no offset artifact.
6. Assert header/body fixed cells have non-transparent computed backgrounds.
7. Assert the fixed cells' stacking order is above ordinary statistic cells.
8. Repeat the essential fixed-position check in edit mode.
9. Confirm the wrapper can reach its maximum horizontal scroll offset and the final column is visible.
10. With 50 rows, sample 20 scroll-to-paint interactions and require at least 19 samples within 100 milliseconds; record the p95 result.
