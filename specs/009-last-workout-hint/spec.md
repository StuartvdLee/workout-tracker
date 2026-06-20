# Feature Specification: Last Workout Hint on Home Page

**Feature Branch**: `009-last-workout-hint`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "I want to implement a little more information on the home/landing page. Underneath the 'Start Workout' button, I want a small piece of text saying what my last workout was. That way, I don't have to look it up on the history page and I don't accidentally start the same workout twice in a row"

## Context

The home/landing page currently shows an application title, a workout type dropdown, and a "Start Workout" button. A user who has already completed one or more workouts has no quick way to see what they last did without navigating away to a history view. This feature adds a small informational hint below the "Start Workout" button showing the name and date of the most recently completed workout session, giving the user the context they need to choose their next workout without leaving the home screen.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — See Last Workout on Home Page (Priority: P1)

When a user opens the home page and has previously completed at least one workout session, a small line of text is visible below the "Start Workout" button. It shows the name of the most recently completed planned workout and the date it was completed. The user can glance at this hint to decide whether to repeat that workout type or choose a different one, without navigating to a history page.

**Why this priority**: This is the entire feature. It directly solves the stated problem of accidentally repeating the same workout. All value is delivered by this single story.

**Independent Test**: Can be fully tested by completing one workout session, returning to the home page, and confirming that the last workout name and date appear beneath the "Start Workout" button.

**Acceptance Scenarios**:

1. **Given** at least one workout session has been completed, **When** the user opens the home page, **Then** a line of text below the "Start Workout" button displays the name of the most recently completed planned workout and the date it was completed.
2. **Given** the last workout hint is visible, **When** the user selects the same workout from the dropdown, **Then** the hint remains visible so the user can compare their selection to what they last did.
3. **Given** multiple workout sessions have been completed, **When** the user opens the home page, **Then** only the most recently completed session is referenced in the hint — not an earlier one.

---

### User Story 2 — No Hint Shown for First-Time Users (Priority: P1)

When a user opens the home page and has never completed a workout session, the area below the "Start Workout" button is empty. No placeholder text, no spinner, and no error message is shown — the layout is identical to how the page looked before this feature was introduced.

**Why this priority**: Equally critical as Story 1 because this is the state every new user will encounter. Showing an error or a broken label for first-time users would degrade the experience from the start.

**Independent Test**: Can be fully tested by opening the app with no completed sessions and confirming that nothing appears below the "Start Workout" button.

**Acceptance Scenarios**:

1. **Given** no workout sessions have been completed, **When** the user opens the home page, **Then** no text or element is displayed below the "Start Workout" button.
2. **Given** no workout sessions have been completed, **When** the home page loads and the data check completes, **Then** the page layout is visually identical to the existing home page — no empty space or placeholder is visible.

---

### Edge Cases

- **No completed sessions**: No hint is shown; the page is identical to the pre-feature state.
- **Data retrieval is slow**: While the last workout data is being fetched, no hint is shown (the hint area remains empty). The hint appears once data is available. The "Start Workout" button and workout dropdown are not blocked and remain usable during this wait.
- **Data retrieval fails**: No hint is shown and no error is surfaced to the user. The home page continues to function normally without the hint.
- **Workout has a very long name**: The hint text wraps or truncates gracefully without breaking the page layout.
- **Date formatting**: The date displayed is human-readable and unambiguous (e.g., "3 May 2026"), not a raw timestamp or locale-ambiguous format such as "05/03/26".

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The home page MUST display a short informational hint below the "Start Workout" button when at least one workout session has been completed.
- **FR-002**: The hint MUST show the name of the most recently completed planned workout.
- **FR-003**: The hint MUST show the date the most recently completed workout session was finished.
- **FR-004**: When no completed workout sessions exist, the hint MUST NOT be rendered — no placeholder text, empty container, or error state is shown.
- **FR-005**: The hint MUST reflect only the single most recently completed session; older sessions MUST NOT influence the displayed text.
- **FR-006**: The hint MUST remain visible and unchanged while the user interacts with the workout dropdown and the "Start Workout" button on the same page load.
- **FR-007**: A failure to retrieve last workout data MUST NOT prevent the home page from loading or the "Start Workout" button from functioning.

### Security & Privacy Requirements

- **SR-001**: The last workout data returned to the home page MUST be scoped to the current user's sessions only; no other user's data may be included.
- **SR-002**: The hint is read-only and display-only; it MUST NOT expose any data modification capability.

### User Experience Consistency Requirements

- **UX-001**: The hint text MUST use the same typographic style, spacing, and colour palette already established on the home page; no new design tokens are introduced.
- **UX-002**: The hint area MUST handle all four states — loading (empty/invisible), empty (no sessions), success (hint shown), and error (silent fail, hint hidden) — without visual jank or layout shift.
- **UX-003**: The wording of the hint MUST match the workout name terminology used elsewhere in the application (e.g., the name shown in the workout dropdown and on the history page).

### Performance Requirements

- **PR-001**: The last workout data fetch MUST NOT block or delay the initial render of the home page; the page MUST be interactive (dropdown and button usable) before the hint data is available.
- **PR-002**: The hint MUST appear within a time consistent with a normal page data load — no secondary round trips or polling are required.

### Key Entities

- **Workout Session**: A completed instance of a planned workout, identified by its planned workout name and a completion date/time.
- **Planned Workout**: The named workout template a session is based on (e.g., "Push", "Pull", "Legs"), as defined in the add-workouts feature (`004-add-workouts`).

## Assumptions

- "Last workout" means the most recently **completed** session, not an in-progress or abandoned one.
- Workout sessions already record a completion date/time as established in `005-active-workout-effort`.
- The planned workout name is already stored as part of the session record and does not need to be derived from a separate lookup.
- The application is single-user for now; no multi-tenancy scoping beyond current user is required.
- The hint text format is: **"Last workout: [Planned Workout Name] — [Date]"** (e.g., "Last workout: Push — 3 May 2026"). The exact copy may be refined during implementation but the two data points (name + date) are fixed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A returning user can identify their last completed workout name and date from the home page without navigating away — verified by manual test on a device with at least one completed session.
- **SC-002**: A new user or a user with no completed sessions sees no additional element below the "Start Workout" button — the home page is visually unchanged from the pre-feature state.
- **SC-003**: The home page remains fully interactive (dropdown and button enabled) regardless of whether the last workout data has loaded — the primary workflow is never blocked by the hint.
- **SC-004**: A data fetch failure produces no visible error state; the home page loads normally and the hint area is simply absent.
- **SC-005**: The hint text wraps or truncates correctly for workout names of any length, with no layout breakage on standard mobile viewport widths.
