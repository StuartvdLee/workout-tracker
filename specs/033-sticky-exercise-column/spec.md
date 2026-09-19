# Feature Specification: Sticky Exercise Column

**Feature Branch**: `033-sticky-exercise-column`
**Created**: 2026-09-19
**Status**: Delivered
**Input**: User description: "Keep the Exercise column visible on the left while horizontally scrolling a past workout table, with statistics passing behind it."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Keep Exercise Context While Scrolling (Priority: P1)

A user viewing a past workout can scroll horizontally through all recorded statistics while the exercise-name column remains fixed at the left edge of the table. This lets the user identify which exercise every visible statistic belongs to.

**Why this priority**: Maintaining the relationship between an exercise and its statistics is the core purpose of the change and directly addresses the usability problem.

**Independent Test**: Open a past workout whose table is wider than its visible area, scroll fully from left to right, and verify that every exercise name remains visible and aligned with its row throughout the interaction.

**Acceptance Scenarios**:

1. **Given** a past workout table is wider than its visible area, **When** the user scrolls horizontally, **Then** the Exercise column remains fixed against the left side of the table's visible area.
2. **Given** the user is scrolling horizontally, **When** statistic columns move toward the fixed Exercise column, **Then** their labels and values disappear behind the Exercise column rather than appearing over or through it.
3. **Given** the table contains multiple exercise rows, **When** the user scrolls to any horizontal position, **Then** each fixed exercise name remains vertically aligned with its corresponding statistics row.
4. **Given** the user scrolls back to the table's starting position, **When** the table is fully left-aligned again, **Then** the Exercise column has the same placement and visual appearance as it had when the page first loaded.

---

### User Story 2 - Preserve Table Readability Across Themes and Sizes (Priority: P2)

A user can read the fixed Exercise column clearly on supported screen sizes and in every available visual theme, without content overlap or an inconsistent table appearance.

**Why this priority**: The fixed column is only useful if it remains legible and visually integrated in all supported viewing conditions.

**Independent Test**: View and horizontally scroll the past-workout table at narrow and wide viewport sizes in each available theme, confirming that the fixed column remains readable and visually consistent.

**Acceptance Scenarios**:

1. **Given** the application is using any supported visual theme, **When** the user scrolls the past-workout table, **Then** the fixed Exercise header and cells remain opaque and consistent with the surrounding table.
2. **Given** the viewport is narrow enough to require horizontal scrolling, **When** the user scrolls to the final statistic column, **Then** the Exercise column remains visible without preventing the final statistic from being viewed.
3. **Given** the table is at its zero horizontal scroll offset, **When** the page loads or the user returns to the left boundary, **Then** the table retains its existing layout and the Exercise column has no offset artifact.

### Edge Cases

- Exercise names that fit within the viewport-aware column limit MUST remain on one line with trailing spacing. Longer valid names, including the 150-character maximum, MUST be visually truncated without covering all statistic space, while their full value remains available through accessible text and a hover title.
- The fixed column MUST remain correctly layered while scrolling to either horizontal boundary.
- Rows with missing or placeholder statistic values MUST retain alignment with their exercise names.
- A past workout containing one exercise MUST behave consistently with a workout containing many exercises.
- The empty, loading, and error states for a past workout MUST remain unchanged because they do not present a horizontally scrollable exercise table.
- Keyboard, trackpad, touch, and scrollbar-based horizontal scrolling MUST produce the same fixed-column behavior where those input methods are supported.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Exercise header and every exercise-name cell in a past-workout table MUST remain fixed at the left edge of the table's visible area during horizontal scrolling.
- **FR-002**: All statistic columns MUST continue to scroll horizontally while the Exercise column remains fixed.
- **FR-003**: Scrolling statistic headers and values MUST be visually obscured when they pass behind the fixed Exercise column.
- **FR-004**: The Exercise header MUST remain aligned with the fixed exercise-name cells below it.
- **FR-005**: Each fixed exercise-name cell MUST remain aligned with its corresponding statistics row at every horizontal scroll position.
- **FR-006**: Users MUST be able to scroll far enough to view the complete content of the final statistic column.
- **FR-007**: The fixed Exercise column MUST preserve the table's typography, spacing, outer border, and body-row dividers; its header and body cells MUST use the same light-grey, theme-aware surface, with no vertical divider at the column's trailing edge or horizontal divider between the table and the Overall Effort row.
- **FR-008**: The behavior MUST apply whenever the past-workout table overflows horizontally and MUST preserve the existing table layout without an offset artifact at the zero horizontal scroll position.
- **FR-009**: The Exercise column MUST derive its width from the longest displayed exercise name up to a viewport-aware maximum, include trailing spacing, and MUST NOT use a fixed column width. Names beyond the maximum MUST truncate visually while preserving the full accessible name and hover title.
- **FR-010**: The change MUST NOT alter workout data, statistic values, column order, row order, or existing session actions.

### Security & Privacy Requirements

- **SR-001**: The feature MUST NOT introduce new data collection, storage, transmission, or access behavior.
- **SR-002**: Exercise names and workout statistics MUST continue to follow the application's existing display and data-access protections.

### User Experience Consistency Requirements

- **UX-001**: The fixed Exercise column MUST use the existing past-workout table's visual language in all supported themes.
- **UX-002**: The fixed column MUST not flicker, become transparent, or reveal scrolling content beneath it during ordinary interaction.
- **UX-003**: Existing loading, empty, success, and error-state wording and behavior MUST remain unchanged.
- **UX-004**: Existing horizontal scrolling methods MUST remain available; the feature MUST not require a new gesture or control.

### Performance Requirements

- **PR-001**: With a past-workout table containing 50 exercise rows, at least 19 of 20 sampled horizontal scroll interactions MUST paint the requested position within 100 milliseconds.
- **PR-002**: The fixed-column behavior MUST add zero network requests and zero JavaScript scroll event handlers to the past-workout detail flow.
- **PR-003**: Before release, the behavior MUST be verified at zero, 25%, 50%, 75%, and maximum horizontal scroll offsets, with 50 rows, and in every supported visual theme.

## Dependencies

- The existing past-workout detail page and its horizontally scrollable exercise statistics table remain available.
- The application's existing visual themes and supported viewport range define the presentation contexts to verify.

## Assumptions

- The Exercise column is the first column in the past-workout table and remains the leftmost column.
- The Exercise column is content-sized from the longest displayed exercise name up to a viewport-aware maximum that preserves visible statistic space; user-configurable column resizing remains outside this feature's scope.
- Only the past-workout detail table is affected; tables elsewhere in the application retain their current behavior.
- Vertical page scrolling and the current table header behavior are unchanged.
- Existing supported browsers and input methods define the compatibility scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At zero, 25%, 50%, 75%, and maximum horizontal scroll offsets, every visible exercise name remains fixed at the left edge and aligned with its statistics row within 2 CSS pixels.
- **SC-002**: In 100% of tested rows, statistic content is fully hidden while passing behind the Exercise column, with no text or numbers visible through it.
- **SC-003**: Users can reach and read the complete final statistic column on all supported viewport sizes that require horizontal scrolling.
- **SC-004**: At the zero horizontal scroll offset, the table retains its current layout and the Exercise column has no offset artifact.
- **SC-005**: With 50 exercise rows, at least 19 of 20 sampled horizontal scroll interactions paint the requested position within 100 milliseconds and show no layering error.
- **SC-006**: All supported visual themes pass the fixed-column readability and opacity checks without contrast or overlap regressions.
