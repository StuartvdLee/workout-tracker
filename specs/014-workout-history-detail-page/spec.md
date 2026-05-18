# Feature Specification: Workout History Detail Page

**Feature Branch**: `014-workout-history-detail-page`  
**Created**: 2026-05-18  
**Status**: Draft  
**Input**: User description: "I want to rework the way that the history of workouts work. Instead of a previous workout folding open, I just want it to be clickable and open an entirely new page when clicked. The new page should display the workout in a table form containing the exercises, the weight, the weight of the same exercise of the previous same type of workout, the effort and the effort of the same exercise of the previous same type of workout. The table should fit the design of the rest of the application."

## Context

The History page currently shows completed workout sessions as a flat list of entries. Clicking an entry expands it inline to reveal the exercises logged in that session (accordion/fold-open behaviour introduced in feature 012). This feature replaces the inline expand-collapse interaction with navigation to a dedicated detail page, and enriches the detail view by showing — for each exercise — both the values logged in the selected session and the values logged for the same exercise in the most recent prior session of the same planned workout.

This builds on the workout session model from `004-add-workouts`, the weight/effort data model from `005-active-workout-effort`, and the previous-session lookup pattern established in `008-workout-exercise-history`.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navigate to Workout Session Detail Page (Priority: P1)

A user taps a completed workout entry on the History page. Instead of the entry expanding inline, they are taken to a dedicated detail page for that session. The page shows the workout name, date, and a table of exercises with the weight and effort logged during that session. They can navigate back to the History page from the detail page.

**Why this priority**: This is the foundation of the feature. Removing the inline expand and replacing it with page navigation is the primary interaction change. All other stories depend on this page existing.

**Independent Test**: Can be fully tested by tapping any entry on the History page and verifying: (a) no inline expansion occurs, (b) a detail page is shown, (c) the workout name and date match the selected entry, (d) the back navigation returns to the History page.

**Acceptance Scenarios**:

1. **Given** the user is on the History page with at least one completed session, **When** they tap a session entry, **Then** they are navigated to a dedicated detail page for that session — the entry does NOT expand inline.
2. **Given** the user is on the session detail page, **When** they use the back navigation control, **Then** they are returned to the History page.
3. **Given** the user is on the session detail page, **When** the page loads, **Then** the workout name and the date of the session are displayed prominently at the top of the page.
4. **Given** the user is on the session detail page, **When** the page loads, **Then** all exercises logged in that session are displayed in a table.
5. **Given** the session detail page loads, **When** the table renders, **Then** the table uses the same visual design language (colours, typography, spacing) as other tabular or list content in the application.

---

### User Story 2 - View Exercise Performance with Previous Session Comparison (Priority: P2)

On the session detail page, each exercise row in the table shows four data points: the weight and effort logged in the selected session, and — for reference — the weight and effort logged for the same exercise in the most recent prior session of the same planned workout. This lets the user immediately see how their performance changed between sessions.

**Why this priority**: This is the main information value of the detail page. Without the comparison columns, the page would be functionally equivalent to the old inline expand but less convenient. The comparison is what makes a dedicated page worthwhile.

**Independent Test**: Can be fully tested by completing two sessions of the same planned workout with different weights and efforts, navigating to the detail page for the second session, and verifying that each exercise row shows both the second session's values and the first session's values in the correct columns.

**Acceptance Scenarios**:

1. **Given** the selected session is not the first session for this planned workout, **When** the detail table renders, **Then** each exercise row shows the weight logged in the selected session and the weight logged for the same exercise in the most recent prior session of the same planned workout.
2. **Given** the selected session is not the first session for this planned workout, **When** the detail table renders, **Then** each exercise row shows the effort logged in the selected session and the effort logged for the same exercise in the most recent prior session of the same planned workout.
3. **Given** the selected session is the first (or only) completed session for this planned workout, **When** the detail table renders, **Then** the "previous weight" and "previous effort" columns show a clear empty or "—" indicator — no values are fabricated or pulled from other workouts.
4. **Given** an exercise in the selected session was not present in the most recent prior session (e.g., it was added to the planned workout after that prior session), **When** the detail table renders, **Then** the previous weight and effort cells for that exercise show the empty or "—" indicator.
5. **Given** a prior session exists but a specific exercise had no weight recorded in it, **When** the detail table renders, **Then** the previous weight cell for that exercise shows the empty or "—" indicator — not zero or a fallback from another session.
6. **Given** a prior session exists but a specific exercise had no effort recorded in it, **When** the detail table renders, **Then** the previous effort cell shows the empty or "—" indicator.

---

### User Story 3 - Remove Inline Expand-Collapse from History Entries (Priority: P3)

The History page entries no longer have any expand-collapse behaviour. Each entry is a clean, tappable card that leads to the detail page. There are no toggle icons or expanded states.

**Why this priority**: This is the cleanup/simplification aspect. The interaction change (Story 1) already means the expand won't trigger, but the UI should be updated to remove visual affordances for the old behaviour (e.g., chevron/toggle icons). It's lower priority because Story 1 already delivers the new interaction; this is about polish and removing legacy UI.

**Independent Test**: Can be fully tested by opening the History page and verifying that no entry has a visible expand/collapse toggle or shows an expanded state, and that the exercise count or other summary information previously shown on the entry card is still visible.

**Acceptance Scenarios**:

1. **Given** the user is on the History page, **When** the page loads, **Then** no history entry displays a chevron, toggle icon, or any affordance suggesting it can be expanded inline.
2. **Given** the user is on the History page, **When** they look at any entry, **Then** the entry shows the workout name, date, and any summary information (e.g., exercise count) as defined in the current entry design — but no expandable section.

---

### Edge Cases

- What happens when the session has no exercises logged? The detail page still loads and displays the workout name and date. The table area shows an empty state message (e.g., "No exercises were logged for this session.").
- What happens when the detail page is loaded but the session data cannot be retrieved (e.g., network error)? The page shows a non-blocking error message. The back navigation remains accessible so the user can return to the History page.
- What happens when the detail page is loading slowly? A loading state (skeleton or spinner) is shown in place of the table while data is being fetched. The workout name and date may be passed from the History page to avoid a blank header.
- What happens when the selected session has no prior session for the same planned workout? The previous weight and previous effort columns show "—" for all rows — no error state is shown, as this is an expected condition for the first session.
- What happens when the user navigates directly to the detail page URL (e.g., bookmarked or refreshed)? The page loads normally using the session identifier from the URL. If the session does not exist or does not belong to the user, a not-found or access-denied state is shown.
- What happens if the same exercise appears multiple times in a planned workout? Each occurrence is treated as a separate row in the table, preserving the order from the session.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The History page MUST NOT expand a session entry inline when tapped; tapping a session entry MUST navigate the user to a dedicated session detail page.
- **FR-002**: The History page entries MUST NOT display any expand-collapse toggle, chevron, or affordance suggesting inline expansion.
- **FR-003**: The session detail page MUST display the workout name and the date/time of the session at the top of the page.
- **FR-004**: The session detail page MUST display a table listing all exercises logged in that session, in the order they were recorded.
- **FR-005**: Each row in the exercise table MUST include the following five columns: Exercise Name, Weight (selected session), Previous Weight, Effort (selected session), Previous Effort.
- **FR-006**: "Previous Weight" and "Previous Effort" MUST be sourced from the same exercise in the most recently completed prior session of the same planned workout — data from other planned workouts MUST NOT be used.
- **FR-007**: When no prior session exists for the planned workout, or when a prior session exists but did not include a value for the given exercise and field, the corresponding cell MUST display a clear empty indicator (e.g., "—") and MUST NOT display zero, a blank space, or data from another workout.
- **FR-008**: The session detail page MUST provide a clearly labelled back navigation control that returns the user to the History page.
- **FR-009**: The exercise table design MUST be visually consistent with the existing design language of the application (colours, typography, spacing, border style).
- **FR-010**: The session detail page MUST handle a loading state while data is being fetched, displaying a stable placeholder that does not shift the layout when data arrives.
- **FR-011**: The session detail page MUST handle an error state if session data cannot be retrieved, without removing the back navigation control.
- **FR-012**: The session detail page MUST handle an empty state if the session contains no exercises, with a descriptive message.

### Security & Privacy Requirements

- **SR-001**: The session detail page MUST maintain the existing single-user application model: no authentication or per-user scoping is introduced, and session detail data remains scoped to the single app user context.
- **SR-002**: The session identifier used in the detail page URL MUST NOT expose predictable or sequential identifiers that would allow enumeration of other users' sessions.
- **SR-003**: All exercise names, weights, and effort values rendered in the table MUST be properly escaped to prevent injection of malicious content.

### User Experience Consistency Requirements

- **UX-001**: The exercise table on the detail page MUST use the same visual treatment (card background, text colour, font sizes) as other data displays in the application — no new design patterns are introduced.
- **UX-002**: Loading, empty, error, and success states MUST each be visually distinct and consistent with how those states are handled elsewhere in the application.
- **UX-003**: Column headers in the exercise table MUST use clear, concise labels. "Previous" values MUST be labelled in a way that communicates they are from the prior session of the same workout (e.g., "Prev. Weight", "Prev. Effort").
- **UX-004**: The back navigation on the detail page MUST use the same navigation pattern and label style as other back controls in the application.

### Performance Requirements

- **PR-001**: The session detail page MUST load and render its initial state (at minimum the workout name, date, and loading placeholder for the table) within 1.5 seconds of navigation on a typical mobile connection (measured from tap to loading-placeholder visible in the table area). This MUST be validated manually or via a performance test before the feature is considered complete.
- **PR-002**: Fetching the session exercises and prior session data is the primary data retrieval hot path — both SHOULD be fetched together where possible to minimise round-trips.
- **PR-003**: The `GET /api/sessions/{sessionId}` endpoint MUST respond in under 500 ms (server-side, p95) for sessions with up to 25 exercises. The detail page table MUST render all rows without visible layout reflow. This MUST be validated in T018 before release.

### Key Entities

- **Workout Session**: A completed instance of a planned workout, identified by a unique session identifier, associated with a planned workout, and timestamped with a completion date/time.
- **Session Exercise Entry**: A record of a specific exercise within a completed session, containing the exercise name, the weight logged (in KG, optional), and the effort rating (1–10, optional).
- **Previous Session**: The most recently completed session for the same planned workout that predates the currently viewed session — used as the comparison baseline for all "previous" column values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of history entries navigate to a detail page on tap — zero entries expand inline after this change is deployed.
- **SC-002**: The session detail page displays all five table columns (Exercise, Weight, Previous Weight, Effort, Previous Effort) for every session that has at least one logged exercise.
- **SC-003**: For sessions with a prior completed session of the same planned workout, 100% of exercise rows show correct previous values sourced exclusively from that prior session.
- **SC-004**: For sessions with no prior session, or exercises not present in the prior session, 100% of affected cells show the empty indicator — no incorrect or misleading values are displayed.
- **SC-005**: The detail page loads and displays its initial state within the budget defined in PR-001 (1.5 s on a typical mobile connection) — validated manually or via T018 before release.
- **SC-006**: 100% of loading, empty, error, and success states on the detail page use the same visual treatment as equivalent states in the rest of the application.
