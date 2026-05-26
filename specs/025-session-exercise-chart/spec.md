# Feature Specification: Session Exercise Chart

**Feature Branch**: `025-session-exercise-chart`  
**Created**: 2026-05-26  
**Status**: Draft  
**Input**: User description: "I want to display a chart on a previous session's page. The chart should display info about weight and effort of previous individual exercises as well as previous overall session efforts. Not everything should be displayed at once, that would be messy. I want a dropdown for the chart to indicate what data I want to display. I want the graph to display a line chart"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Exercise Weight History (Priority: P1)

As a user reviewing a completed workout session, I want to see how the weight lifted for a specific exercise has changed over previous sessions, so I can track my progression.

**Why this priority**: Weight progression is the most fundamental metric for strength training. Seeing trends over time is the primary reason a user visits a past session page.

**Independent Test**: Can be fully tested by selecting an exercise from the dropdown on the session detail page, choosing "Weight" as the data type, and verifying a line chart appears with data points from historical sessions.

**Acceptance Scenarios**:

1. **Given** I am on a past session's detail page, **When** I open the chart dropdown and select an exercise with "Weight" data, **Then** a line chart appears showing weight values across previous sessions in chronological order.
2. **Given** I have selected an exercise, **When** the chart renders, **Then** each data point corresponds to a session where that exercise was performed, with the date on the x-axis and weight on the y-axis.
3. **Given** I have selected an exercise, **When** there is only one historical data point, **Then** the chart still renders that single point without errors.

---

### User Story 2 - View Exercise Effort History (Priority: P2)

As a user reviewing a completed workout session, I want to see how my effort for a specific exercise has changed over previous sessions, so I can understand if I am pushing myself consistently.

**Why this priority**: Effort is a subjective but important metric that complements objective weight data; it helps users understand if effort and weight are correlated.

**Independent Test**: Can be fully tested by selecting an exercise and choosing "Effort" as the data type, verifying a line chart shows effort scores over time.

**Acceptance Scenarios**:

1. **Given** I am on a past session's detail page, **When** I select an exercise and choose "Effort" from the chart dropdown, **Then** a line chart appears showing effort values for that exercise across historical sessions.
2. **Given** I have selected "Effort" for an exercise, **When** the chart renders, **Then** effort values are plotted on the y-axis with dates on the x-axis.

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
2. **Given** the dropdown is open, **When** I view the options, **Then** I can see all exercises from the current session listed individually plus the "Overall Session Effort" option.

---

### Edge Cases

- What happens when an exercise has never been performed in a previous session? → The chart shows an empty/no-data state with a friendly message.
- What happens when only the current session exists (no history)? → The chart indicates there is no historical data to display.
- What happens if weight data is missing for some sessions but present for others? → Only sessions with recorded data are plotted; gaps are shown on the chart.
- How does the chart behave on a slow network or delayed data load? → A loading indicator is shown while data is being fetched; the chart renders once data is available.
- What happens on a narrow/mobile viewport? → The chart is responsive and remains legible on smaller screens.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The session detail page MUST include a line chart component for displaying historical workout data.
- **FR-002**: The chart MUST include a dropdown selector that lists all exercises performed in the current session plus an "Overall Session Effort" option.
- **FR-003**: Only one dataset MUST be displayed on the chart at a time (no multi-series by default).
- **FR-004**: When an exercise is selected, the chart MUST allow the user to choose between "Weight" and "Effort" as the data dimension, OR the dropdown MUST list each exercise-metric combination as a distinct selectable option (e.g., "Bench Press – Weight", "Bench Press – Effort").
- **FR-005**: The chart MUST render as a line chart with date/session on the x-axis and the selected metric on the y-axis.
- **FR-006**: The chart MUST source data from all recorded sessions of the same workout, not only the current session.
- **FR-007**: When no historical data is available for the selected metric, the chart MUST display an empty state message.
- **FR-008**: The chart MUST update dynamically when the user changes the dropdown selection without requiring a page reload.
- **FR-009**: Data points MUST be ordered chronologically on the x-axis.

### Security & Privacy Requirements

- **SR-001**: The chart MUST only display data belonging to the currently authenticated user; no cross-user data leakage is permitted.
- **SR-002**: All data requests for chart data MUST be subject to the same authorization checks as other session data.

### User Experience Consistency Requirements

- **UX-001**: The chart dropdown MUST reuse existing dropdown/select styling from the application's design system.
- **UX-002**: The chart MUST define loading, empty, and error states consistent with other data-heavy components in the app.
- **UX-003**: Terminology in the dropdown (e.g., "Overall Session Effort", exercise names, "Weight", "Effort") MUST match the language used elsewhere on the session detail page.

### Performance Requirements

- **PR-001**: The chart MUST load and render within a timeframe that does not disrupt the overall session page load experience.
- **PR-002**: Switching between dropdown options MUST feel immediate to the user; data already loaded for the page MUST be used where possible to avoid repeated network requests.
- **PR-003**: The chart MUST remain responsive and usable when a workout has been performed many times (high volume of historical data points).

### Key Entities

- **WorkoutSession**: A completed instance of a workout on a specific date, with an optional overall effort score.
- **SessionExercise**: An individual exercise performed within a session, with recorded sets, weights, and an optional effort score.
- **Workout**: The parent template that groups sessions together, used to identify which sessions share the same workout for historical comparison.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can switch between chart data options in under 2 seconds without a page reload.
- **SC-002**: The chart correctly displays all historical data points for the selected metric with no missing or duplicated entries.
- **SC-003**: 100% of chart interactions (dropdown change, page load) handle loading, empty, and error states without unhandled errors.
- **SC-004**: The chart is fully usable on viewport widths from 320px and above.
- **SC-005**: No data from other users is ever returned or displayed in the chart.

## Assumptions

- The session detail page already exists (feature 014-workout-history-detail-page) and this chart is added to it.
- Weight and effort data are already being recorded and stored per session exercise (features 005, 016).
- The number of distinct exercises per session is small enough that listing them individually in the dropdown remains practical (no pagination needed for the dropdown).
- A suitable charting library is already available in the project or can be added without significant dependency overhead; if not, the implementation team will select the lightest available option.
- "Weight" refers to the weight lifted per set; if multiple sets exist, the average or maximum weight per session will be used — the implementation team should decide and apply consistently.
