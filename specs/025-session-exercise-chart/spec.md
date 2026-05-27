# Feature Specification: Session Exercise Chart

**Feature Branch**: `025-session-exercise-chart`  
**Created**: 2026-05-26  
**Status**: Implemented  
**Input**: User description: "I want to display a chart on a previous session's page. The chart should display info about weight and effort of previous individual exercises as well as previous overall session efforts. Not everything should be displayed at once, that would be messy. I want a dropdown for the chart to indicate what data I want to display. I want the graph to display a line chart"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Exercise Weight History (Priority: P1)

As a user reviewing a completed workout session, I want to see how the weight lifted for a specific exercise has changed over previous sessions, so I can track my progression.

**Why this priority**: Weight progression is the most fundamental metric for strength training. Seeing trends over time is the primary reason a user visits a past session page.

**Independent Test**: Can be fully tested by selecting an exercise from the dropdown on the session detail page and verifying a line chart appears with both weight (blue) and effort (red) lines across historical sessions.

**Acceptance Scenarios**:

1. **Given** I am on a past session's detail page, **When** I open the chart dropdown and select an exercise, **Then** a line chart appears showing weight values across previous sessions in chronological order.
2. **Given** I have selected an exercise, **When** the chart renders, **Then** each data point corresponds to a session where that exercise was performed, with the date on the x-axis and weight on the y-axis.
3. **Given** I have selected an exercise, **When** there is only one historical data point, **Then** the chart still renders that single point without errors.

---

### User Story 2 - View Exercise Effort History (Priority: P2)

As a user reviewing a completed workout session, I want to see how my effort for a specific exercise has changed over previous sessions, so I can understand if I am pushing myself consistently.

**Why this priority**: Effort is a subjective but important metric that complements objective weight data; it helps users understand if effort and weight are correlated.

**Independent Test**: Can be fully tested by selecting an exercise and verifying the effort line (red) is shown for that exercise over time.

**Acceptance Scenarios**:

1. **Given** I am on a past session's detail page, **When** I select an exercise from the chart dropdown, **Then** the chart includes an effort line (red) for that exercise across historical sessions.
2. **Given** I have selected an exercise, **When** the chart renders, **Then** effort values are plotted on the right y-axis (0–10) with dates on the x-axis.

---

### User Story 3 - View Overall Session Effort History (Priority: P3)

As a user reviewing a completed workout session, I want to see how my overall session effort has trended over previous sessions of the same workout, so I can gauge my general fatigue and commitment.

**Why this priority**: Overall session effort provides a high-level summary view without requiring the user to drill into individual exercises.

**Independent Test**: Can be fully tested by selecting "Overall Session Effort" from the dropdown and verifying a line chart showing session-level effort across historical sessions of the same workout.

**Acceptance Scenarios**:

1. **Given** I am on a past session's detail page, **When** I select "Overall Session Effort" from the chart dropdown, **Then** a line chart appears showing the overall effort score per session for that workout over time.
2. **Given** I select "Overall Session Effort", **When** there are no previous sessions for this workout, **Then** the chart shows an empty state message rather than an empty chart.

---

### User Story 4 - Switch Between Chart Data (Priority: P2)

As a user, I want to switch what data is displayed in the chart using the dropdown, so I can explore different metrics without navigating away.

**Why this priority**: The dropdown is the core navigation mechanism for the chart; switching between metrics is an essential interaction.

**Independent Test**: Can be tested by selecting one metric, confirming the chart updates, then selecting a different metric and confirming the chart updates again.

**Acceptance Scenarios**:

1. **Given** a chart is displaying data for one exercise/metric, **When** I select a different option from the dropdown, **Then** the chart updates to show the newly selected data without a full page reload.
2. **Given** the dropdown is open, **When** I view the options, **Then** I can see all exercises from the current session listed once each, plus the "Overall Session Effort" option.

---

### Edge Cases

- What happens when an exercise has no historical weight and no historical effort? → The chart shows an empty/no-data state with a friendly message.
- What happens when only the current session exists (no history)? → The chart indicates there is no historical data to display.
- What happens if weight or effort data is missing for some sessions but present for others? → Only sessions with recorded data are plotted; gaps are shown on the chart.
- How does the chart behave on a slow network or delayed data load? → A loading indicator is shown while data is being fetched; the chart renders once data is available.
- What happens on a narrow/mobile viewport? → The chart is responsive and remains legible on smaller screens.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The session detail page MUST include a line chart component for displaying historical workout data.
- **FR-002**: The chart MUST include a dropdown selector that lists all exercises performed in the current session (one option per exercise) plus an "Overall Session Effort" option.
- **FR-003**: The chart MUST display one selection mode at a time: either (a) overall effort only, or (b) a selected exercise with both weight and effort lines.
- **FR-004**: When an exercise is selected, the chart MUST render both weight and effort for that exercise together in the same chart (weight in blue, effort in red).
- **FR-005**: The chart MUST render as a line chart with date/session on the x-axis and the selected metric on the y-axis.
- **FR-006**: The chart MUST source data from all recorded sessions of the same workout, not only the current session.
- **FR-007**: When no historical data is available for the selected series/selection, the chart MUST display an empty state message.
- **FR-008**: The chart MUST update dynamically when the user changes the dropdown selection without requiring a page reload.
- **FR-009**: Data points MUST be ordered chronologically on the x-axis.
- **FR-010**: If trends data cannot be loaded, the chart MUST fall back to rendering from current session data so the dropdown remains enabled and interactive.

### Security & Privacy Requirements

- **SR-001**: The chart MUST only display data belonging to the currently authenticated user; no cross-user data leakage is permitted.
- **SR-002**: All data requests for chart data MUST be subject to the same authorization checks as other session data.

### User Experience Consistency Requirements

- **UX-001**: The chart dropdown MUST reuse existing dropdown/select styling from the application's design system.
- **UX-002**: The chart MUST define loading, empty, and error states consistent with other data-heavy components in the app.
- **UX-003**: Terminology in the dropdown (e.g., "Overall Session Effort", exercise names, "Weight", "Effort") MUST match the language used elsewhere on the session detail page.

### Performance Requirements

- **PR-001**: The chart section MUST be visible and interactive within 2 seconds of the session detail page finishing its initial load on a standard broadband connection. This MUST be validated during implementation.
- **PR-002**: Switching between dropdown options MUST feel immediate to the user; data already loaded for the page MUST be used where possible to avoid repeated network requests.
- **PR-003**: The chart MUST remain responsive and usable when a workout has been performed many times (high volume of historical data points).

### Key Entities

- **WorkoutSession**: A completed instance of a workout on a specific date, with an optional overall effort score.
- **SessionExercise**: An individual exercise performed within a session, with recorded sets, weights, and an optional effort score.
- **Workout**: The parent template that groups sessions together, used to identify which sessions share the same workout for historical comparison.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can switch between chart data options in under 2 seconds without a page reload. The initial chart render (default "Overall Session Effort" series) MUST also complete within 2 seconds of the session detail page loading.
- **SC-001a**: The chart section is measurably validated against the 2-second budget during implementation (see T020).
- **SC-002**: The chart correctly displays historical data points for the selected option with no missing or duplicated entries.
- **SC-003**: 100% of chart interactions (dropdown change, page load) handle loading, empty, and error states without unhandled errors.
- **SC-004**: The chart is fully usable on viewport widths from 320px and above.
- **SC-005**: No data from other users is ever returned or displayed in the chart.

## Assumptions

- The session detail page already exists (feature 014-workout-history-detail-page) and this chart is added to it.
- Weight and effort data are already being recorded and stored per session exercise (features 005, 016).
- The number of distinct exercises per session is small enough that listing them individually in the dropdown remains practical (no pagination needed for the dropdown).
- **No external charting library is used.** The chart is rendered as inline SVG with vanilla TypeScript. This decision was made during planning to avoid new dependencies (see plan.md).
- **`loggedWeight` is always a single numeric string (or null).** Weight is entered via `<input type="number" step="0.5">` in the active session UI — compound strings such as "80/85/90" are not possible. `Number(loggedWeight)` is the correct parse; any `NaN` result indicates invalid data, not a multi-set format. An integration test asserts this invariant.
- **The currently-viewed session IS included as the rightmost data point** in the chart. This is consistent with the `GET /api/workouts/{workoutId}/previous-performance` endpoint which returns all sessions without filtering the current one. Showing the current session's position in the trend gives the user context for where they stand "right now."
