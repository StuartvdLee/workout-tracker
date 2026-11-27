# Feature Specification: Workout Statistics Dashboard

**Feature Branch**: `stuartvdlee-workout-statistics-dashboard`

**Created**: 2026-11-27

**Status**: Draft

**Input**: GitHub issue [#198: Add a workout statistics dashboard](https://github.com/StuartvdLee/workout-tracker/issues/198)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Review workout statistics for a period (Priority: P1)

As a person who tracks workouts, I want to see a summary of my activity for the current week, month, or year so I can understand my recent training at a glance.

**Why this priority**: A useful view of recent activity is the dashboard's core value.

**Independent Test**: With completed workouts recorded in the current period, open the dashboard and verify that each available period view shows the correct date range and workout summary.

**Acceptance Scenarios**:

1. **Given** completed workouts exist in the current week, **When** I open the dashboard, **Then** I see the current week selected, its date range, and the statistics for workouts in that week.
2. **Given** the dashboard is showing a period, **When** I switch between weekly, monthly, and yearly views, **Then** the selected view shows the matching current period and its statistics.
3. **Given** I am viewing a past period, **When** I return to the current period, **Then** the dashboard shows the current period's date range and statistics.

---

### User Story 2 - Explore past activity and trends (Priority: P2)

As a person who tracks workouts, I want to browse earlier periods and understand which exercises I did and how my recorded effort changed.

**Why this priority**: Historical comparisons help users recognize activity and effort patterns beyond their latest workouts.

**Independent Test**: With workouts and recorded effort values in multiple periods, navigate to each period and verify its exercise activity and effort summary against the displayed workout history.

**Acceptance Scenarios**:

1. **Given** I am viewing a period before the current one, **When** I navigate to the preceding period, **Then** the displayed range and statistics move to that adjacent period.
2. **Given** the selected period contains exercises and recorded effort values, **When** I review its statistics, **Then** exercise activity and the effort trend are labeled and correspond only to that period.
3. **Given** I am viewing the current period, **When** I try to navigate to a future period, **Then** the dashboard does not present future activity as historical statistics.
4. **Given** a workout in the selected period contributes to displayed statistics, **When** I edit or delete that workout, **Then** the dashboard reflects the updated workout history.

---

### User Story 3 - Understand empty or incomplete history (Priority: P2)

As a person who tracks workouts, I want the dashboard to explain when a period has no activity or when some history lacks usable values so I do not mistake missing information for a meaningful result.

**Why this priority**: Empty and legacy records must not produce misleading summaries.

**Independent Test**: View a period with no workouts, then a period with workouts containing missing or unusable effort values, and verify that each state is clearly distinguished.

**Acceptance Scenarios**:

1. **Given** no completed workouts fall within the selected period, **When** I view that period, **Then** I see a useful empty state and no fabricated activity or trend.
2. **Given** workouts exist but effort values are missing or unusable, **When** I view the effort summary, **Then** those values are excluded and the dashboard explains that there is insufficient recorded effort data.
3. **Given** some but not all workouts have usable effort values, **When** I view the effort summary, **Then** it is based only on usable values and makes clear how many values contribute.

---

### User Story 4 - Use the dashboard across devices and access needs (Priority: P3)

As a person using different devices or assistive technology, I want the dashboard's controls and statistics to remain understandable and usable.

**Why this priority**: Statistics are only useful when people can reliably access and interpret them.

**Independent Test**: Navigate the dashboard using a keyboard and a narrow viewport; verify that controls remain operable and every visual statistic has a readable non-color-dependent alternative.

**Acceptance Scenarios**:

1. **Given** I use only a keyboard, **When** I change the period or navigate history, **Then** I can reach and operate every control and identify the selected period.
2. **Given** I view a chart or other visual statistic, **When** color is unavailable or cannot be distinguished, **Then** labels or an equivalent text representation communicate the same values.
3. **Given** I use a narrow screen, **When** I open the dashboard, **Then** its controls and statistics remain readable without essential information being clipped.

### Edge Cases

- Workouts at the start or end of a period are included according to their local calendar date; daylight-saving changes do not shift a workout into a neighboring date.
- A week begins on Monday and ends on Sunday; a month and year follow their calendar boundaries in the user's local time.
- A period can contain workouts but no valid values for a particular statistic; that statistic is identified as unavailable rather than shown as a misleading zero.
- Editing or deleting a workout can remove or change the only value contributing to a statistic; the summary reflects the resulting history.
- Legacy entries with missing, malformed, or unsupported values are handled without preventing valid entries from appearing in other statistics.
- A future period is not displayed as though it contains completed workout history.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The dashboard MUST provide weekly, monthly, and yearly period views and identify the selected period with its date range.
- **FR-002**: The dashboard MUST open on the current period and allow users to navigate to previous periods and return to the current period.
- **FR-003**: The dashboard MUST use local calendar dates consistently: Monday-through-Sunday weeks, calendar months, and calendar years.
- **FR-004**: The dashboard MUST show the count of completed workouts whose local dates fall within the selected period.
- **FR-005**: The dashboard MUST summarize exercise activity using exercise appearances in completed workouts during the selected period, with labels that state what is being counted.
- **FR-006**: The dashboard MUST show an effort summary or trend based on usable effort values recorded for completed workouts in the selected period, using the existing effort scale and clearly identifying the number of contributing values.
- **FR-007**: The dashboard MUST exclude missing, malformed, or unsupported values from calculations that require those values and MUST distinguish unavailable statistics from a measured value of zero.
- **FR-008**: The dashboard MUST provide a useful empty state when the selected period contains no completed workouts and a clear unavailable-data state when workouts exist but a statistic cannot be calculated.
- **FR-009**: Statistics MUST reflect workout history after a workout is added, edited, or deleted.
- **FR-010**: Visual statistics MUST have labels and a text alternative that conveys their values without relying on color alone.
- **FR-011**: Period controls and statistics MUST be operable and understandable with keyboard navigation and readable at narrow viewport sizes.
- **FR-012**: The dashboard MUST not imply that future periods contain completed workout history.

### Quality Requirements

- **Security and privacy**: The dashboard MUST use the existing single-user workout-history data scope. It MUST NOT add sharing, cross-user aggregation, or a new identity model.
- **Usability**: A user MUST be able to identify the selected date range, the meaning of each statistic, and any unavailable data without relying on color or unexplained numbers.
- **Performance**: For a history of up to 1,000 completed workouts, at least 95% of period changes MUST show the updated summary within 2 seconds.

### Key Entities *(include if feature involves data)*

- **Reporting period**: A selected week, month, or year, including its local start and end dates.
- **Completed workout**: A dated workout included in period counts and exercise activity.
- **Exercise activity**: The number of completed workouts in the period in which each exercise appears.
- **Effort observation**: A usable effort value associated with a completed workout, interpreted on the existing effort scale and counted when summarizing a period.
- **Period statistic**: A labeled count, exercise summary, or effort summary derived from completed workouts and their usable recorded values within a reporting period.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can switch among weekly, monthly, and yearly views and identify the selected date range in under 10 seconds.
- **SC-002**: For every supported period, the displayed completed-workout count and exercise activity match the completed history for that period in all acceptance tests.
- **SC-003**: Missing or unusable values produce no fabricated effort results; summaries use only valid recorded values and disclose their contributing count.
- **SC-004**: Users can navigate to a previous period and return to the current period using the dashboard controls in all supported viewport sizes.
- **SC-005**: At least 95% of period changes show the updated summary within 2 seconds for histories of up to 1,000 completed workouts.
- **SC-006**: Every visual statistic has a non-color-dependent text alternative conveying the same values.

## Assumptions

- The dashboard uses completed workout history and values already tracked by the product; it does not invent repetitions or workout duration.
- The existing workout history is treated as the product's single-user dataset; this feature reuses its current access model and does not add or replace sign-in controls.
- Effort values use the existing product scale. Missing, malformed, or unsupported effort data is not imputed or treated as zero.
- The user's local calendar determines the date of a workout and the boundaries of reporting periods.
- The dashboard follows the existing workout-history data access rules and does not add sharing or export behavior.
- Future periods are not needed for this feature; navigation is limited to past periods and the current period.
