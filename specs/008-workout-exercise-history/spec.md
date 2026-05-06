# Feature Specification: Previous Exercise Performance in Active Workout

**Feature Branch**: `008-workout-exercise-history`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "When I start a workout, I want to be able to see the previous weight and effort for exercises for that workout. Don't include previous weight for just the exercises themselves because exercises might be added to multiple workouts. So to reiterate: previous weight and effort for that exercise for that workout."

## Context

When a user starts an active workout session from a planned workout, the exercise entry form currently shows only empty fields — the user has no reference for what weight or effort they used the last time they did this same planned workout. This feature surfaces that historical data per exercise, scoped specifically to the planned workout being run, so the user can make informed decisions without needing to rely on memory.

This builds on the planned workout / workout session model established in `004-add-workouts` and the weight-and-effort data model introduced in `005-active-workout-effort`.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — See Previous Weight and Effort per Exercise on Workout Start (Priority: P1)

When a user opens an active workout session from a planned workout that has been completed at least once before, each exercise row in the session view shows the weight (in KG) and effort rating from the most recent completed session for that same planned workout. This gives the user an immediate reference point so they know where to start without relying on memory.

**Why this priority**: This is the core capability of the feature. Without it, the entire feature delivers no value. All other stories depend on this data being available and displayed.

**Independent Test**: Can be fully tested by completing a planned workout session with at least one exercise that has a weight and effort recorded, then starting a new session for the same planned workout and confirming each exercise row displays the previous weight and effort values from the prior session.

**Acceptance Scenarios**:

1. **Given** a planned workout has been completed at least once, **When** the user starts a new session for that same planned workout, **Then** each exercise row displays the weight and effort recorded for that exercise in the most recent completed session for this planned workout.
2. **Given** a planned workout has been completed and an exercise had no weight recorded in the last session, **When** the user starts a new session, **Then** no previous weight is shown for that exercise (the field is empty or clearly labelled "No previous data").
3. **Given** a planned workout has been completed and an exercise had no effort recorded in the last session, **When** the user starts a new session, **Then** no previous effort is shown for that exercise.
4. **Given** the user starts a session for a planned workout that has never been completed before, **When** the session view loads, **Then** no previous weight or effort values are shown for any exercise — a friendly message (e.g., "First time — no previous data") is displayed per exercise or for the session as a whole.
5. **Given** the same exercise (e.g., "Bench Press") appears in multiple different planned workouts, **When** the user starts a session for Planned Workout A, **Then** only the previous performance data from Planned Workout A's last session is shown — not data from other workouts that include the same exercise.

---

### User Story 2 — Previous Data Distinguishes Between Multiple Prior Sessions (Priority: P2)

The previous data shown always reflects the **most recent** completed session for the planned workout, not an average or an older session. This keeps the reference data consistent and predictable — the user always knows they are being shown "what I did last time."

**Why this priority**: Without specifying which prior session to surface, the display would be ambiguous and potentially misleading. This story makes the behaviour explicit and testable, but it depends on Story 1 being complete.

**Independent Test**: Can be fully tested by completing a planned workout twice with different weights and efforts, then starting a third session and verifying that the values shown match only the second (most recent) completed session's data.

**Acceptance Scenarios**:

1. **Given** a planned workout has been completed multiple times, **When** the user starts a new session, **Then** the previous weight and effort values displayed are from the single most recently completed session for that planned workout.
2. **Given** the most recent completed session recorded a higher weight than an earlier session, **When** the user starts a new session, **Then** the higher (most recent) weight is displayed — not the earlier, lower value.
3. **Given** the most recent session left an exercise's effort blank, but an earlier session had an effort recorded, **When** the user starts a new session, **Then** no effort is shown for that exercise (the most recent session's absence of data takes precedence).

---

### Edge Cases

- What happens when the user starts a session for a planned workout that has been completed, but the exercise being shown was added to the planned workout after the last session? The previously recorded data does not include that exercise, so no previous data should be shown for it, and the first-time message should apply to that exercise individually.
- What happens when the user is viewing the active session and the previous data fails to load (e.g., network error)? The session view must still be fully usable — the weight and effort fields remain editable. The previous data area should show a non-blocking error state (e.g., "Could not load previous data") without preventing the user from continuing.
- What happens when the session view loads on a slow connection? The previous data may arrive after the initial render. The layout must not shift in a disruptive way when previous data appears — a stable placeholder (skeleton or "Loading…" text) should occupy the space while data is fetching.
- What happens when the planned workout has a large number of exercises? All exercises must display previous data correctly without performance degradation or layout issues.
- What happens when a previously completed session's data is later deleted? The system must gracefully handle the absence of prior session data and revert to the "no previous data" state for affected exercises.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When an active workout session is started from a planned workout, the system MUST retrieve the most recently completed session for that same planned workout.
- **FR-002**: For each exercise in the active session, the system MUST display the weight (in KG) recorded for that exercise in the most recently completed session of the same planned workout, if available.
- **FR-003**: For each exercise in the active session, the system MUST display the effort rating (1–10 and label) recorded for that exercise in the most recently completed session of the same planned workout, if available.
- **FR-004**: Previous weight and effort MUST be scoped to the planned workout — data from other planned workouts that contain the same exercise MUST NOT be surfaced.
- **FR-005**: If no previous session exists for the planned workout, the system MUST display a first-time indicator (e.g., "First time — no previous data") and MUST NOT show empty or misleading fields.
- **FR-006**: If a specific exercise has no previous weight or effort (because it was missing from the last session, or not recorded), that exercise MUST show no previous data for the missing field — it MUST NOT fall back to data from other workouts.
- **FR-007**: Previous data MUST be displayed as read-only reference information alongside (not replacing) the active weight and effort input fields for the current session.
- **FR-008**: The active session's editable fields (current weight, current effort) MUST remain fully functional regardless of the state of the previous data display.
- **FR-009**: If previous data cannot be loaded due to an error, the session MUST remain fully usable and the previous data area MUST show a non-blocking error state.

### Security & Privacy Requirements

- **SR-001**: The system MUST enforce that users can only retrieve previous session data for planned workouts and sessions they own — no cross-user data leakage.
- **SR-002**: All data retrieved for previous performance MUST be validated and sanitized before display to prevent injection of malicious content.

### User Experience Consistency Requirements

- **UX-001**: Previous weight and effort values MUST be visually distinct from the editable current-session fields (e.g., labelled "Last time" or "Previous") using existing design patterns in the app.
- **UX-002**: Loading, empty (first time), success, and error states MUST all be defined and handled for the previous data display area.
- **UX-003**: Terminology MUST be consistent with existing labels: weight in "KG", effort labels as "Easy / Moderate / Hard / All Out" matching `005-active-workout-effort`.

### Performance Requirements

- **PR-001**: Previous performance data MUST be available within a time frame that does not block the user from starting to interact with the session view.
- **PR-002**: The data retrieval for previous session data is a read-only operation scoped to a single planned workout — it should not introduce unbounded queries across all historical sessions.
- **PR-003**: Performance MUST be verified by confirming the active session view remains fully interactive while previous data loads.

### Key Entities

- **PlannedWorkout**: A named workout template containing an ordered list of exercises. Identifies the scope for "previous" data — data is always retrieved per planned workout, not per exercise globally.
- **WorkoutSession**: A completed record of an actual performed workout, linked to a PlannedWorkout. Contains per-exercise entries with weight and effort values.
- **LoggedExercise**: The per-exercise record within a WorkoutSession (DB table: `logged_exercise`), holding the weight (KG) and effort (1–10) recorded for that exercise during that session.

## Assumptions

- The most recently completed session is determined by session completion date/time, with the single most recent session selected.
- "Completed" means the session has been fully saved/submitted, not an in-progress session.
- Weight and effort remain optional fields — absence of either is a valid state that this feature handles gracefully.
- The previous data is displayed inline within the active session view, not in a separate modal or page.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users starting a repeated planned workout can see the previous weight and effort for every exercise without leaving the active session view.
- **SC-002**: Previous data shown is always sourced exclusively from the most recent completed session for the exact planned workout being run — zero instances of cross-workout data contamination.
- **SC-003**: The active session view remains fully interactive while previous data loads — no blocking state observed during standard network conditions.
- **SC-004**: All four states (loading, first-time/empty, success with data, and error) are visually distinguishable and consistently presented across all exercises in the session view.
- **SC-005**: Users can enter and save current session weight and effort values regardless of whether previous data is available or has failed to load.
