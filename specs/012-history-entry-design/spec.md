# Feature Specification: History Page Entry Redesign

**Feature Branch**: `012-history-entry-design`  
**Created**: 2026-05-10  
**Status**: Draft  
**Input**: User description: "I want to change the History page. I want to get rid of the headers saying 'Today', 'Yesterday', 'x Days Ago'. Instead, I want the design to be more in line with the rest of the app. I want an entry to just say the workout name in bold and then the date underneath it, but less pronounced."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Workout History Without Date Group Headers (Priority: P1)

A user opens the History page to review their past workouts. Instead of seeing sessions grouped under headers like "Today", "Yesterday", or "3 Days Ago", they see a flat list of sessions. Each session shows the workout name prominently and the full date directly underneath it in a quieter, secondary style — consistent with the visual language used elsewhere in the app.

**Why this priority**: This is the core of the change. Removing the grouped headers and reformatting each entry is the entire feature.

**Independent Test**: Navigate to the History page with at least two workouts logged on different days. Verify no group headers are present and each entry shows name + date in the new format.

**Acceptance Scenarios**:

1. **Given** the user has completed workouts on multiple different days, **When** they open the History page, **Then** no "Today", "Yesterday", or "X days ago" headers are visible anywhere on the page.
2. **Given** the user opens the History page, **When** they view any history entry, **Then** the workout name is displayed in bold as the primary text.
3. **Given** the user opens the History page, **When** they view any history entry, **Then** the full date (e.g., "Saturday, 10 May 2026") is displayed directly below the workout name in a visually less prominent style.
4. **Given** the user has multiple workouts logged on the same day, **When** they view the History page, **Then** each workout appears as its own entry with its own date — no grouping occurs.

---

### User Story 2 - Expand a History Entry to See Exercise Details (Priority: P2)

A user taps on a history entry to see the exercises logged in that session. The existing expand/collapse behaviour is preserved; only the entry header's visual presentation changes.

**Why this priority**: The expand/collapse interaction is a core existing behaviour that must continue to work correctly after the header redesign.

**Independent Test**: Click any history entry and verify the exercise detail section still expands and collapses correctly.

**Acceptance Scenarios**:

1. **Given** a history entry is in its collapsed state, **When** the user clicks it, **Then** the exercise details expand below the header.
2. **Given** a history entry is expanded, **When** the user clicks it again, **Then** the exercise details collapse.

---

### Edge Cases

- What happens when the History page loads with no sessions? The empty-state message is displayed unchanged; the new entry layout is not relevant.
- What happens when a session has no exercises logged? The entry still shows the workout name and date; when expanded, a "No exercises logged" message is displayed.
- What happens on slow networks or delayed data? The existing loading state is shown while data loads; entries render with the new layout once data arrives.
- What is the date format for a session completed today? The full date is shown (e.g., "Saturday, 10 May 2026") — no special-casing for "today" or "yesterday".

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The History page MUST NOT display any relative date group headers ("Today", "Yesterday", "X days ago").
- **FR-002**: Each history entry MUST display the workout name as the primary, bold text.
- **FR-003**: Each history entry MUST display the full, human-readable date (day of week, day number, month, and year) beneath the workout name as secondary text.
- **FR-004**: Sessions MUST be displayed in a flat list, ordered from most recent to oldest, with no grouping by date.
- **FR-005**: The expand/collapse interaction on each entry MUST continue to function as before.
- **FR-006**: The exercise count indicator on each entry MUST remain visible.

### Security & Privacy Requirements

- **SR-001**: Workout names and dates rendered in the list MUST be escaped to prevent injection of malicious content (existing behaviour must be preserved).

### User Experience Consistency Requirements

- **UX-001**: The secondary date text style MUST visually match the muted/secondary text treatment used elsewhere in the app (e.g., same colour token and font size as other supporting labels).
- **UX-002**: Loading, empty, success, and error states MUST behave identically to the existing History page — only the entry layout changes.
- **UX-003**: The entry layout MUST follow the same card/list-item visual pattern used in other pages of the app.

### Performance Requirements

- **PR-001**: The History page MUST load and render the full session list within the same time budget as the current implementation — this is a presentational change with no new data fetching.
- **PR-002**: No additional network requests are introduced by this change.

### Assumptions

- The full date format is hard-coded to the `en-GB` locale (e.g., "10 May 2026"), consistent with how dates are formatted elsewhere in the app (see also: feature 009). The weekday is not shown.
- The existing exercise count badge and expand/collapse toggle remain in each entry header.
- The time-of-day display is retained as a secondary detail combined with the date (e.g., "10 May 2026 · 2:30 PM").

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero relative date group headers ("Today", "Yesterday", "X days ago") appear anywhere on the History page after the change.
- **SC-002**: Every history entry on the page shows the workout name as the visually dominant text element and the full date as a visually subordinate element.
- **SC-003**: The History page continues to render and allow interaction (expand/collapse) without errors or regressions.
- **SC-004**: The visual appearance of the date text on each entry is consistent with the muted secondary text style used elsewhere in the application.
- **SC-005**: All E2E tests for the History page (including the two replacement tests for `HistoryPage_DateGrouping_ShowsToday`) pass after the change.
