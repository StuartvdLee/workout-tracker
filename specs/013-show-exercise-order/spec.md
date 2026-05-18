# Feature Specification: Previous Exercise Order Indicator in Active Workout

**Feature Branch**: `013-show-exercise-order`  
**Created**: 2026-05-18  
**Status**: Draft  
**Input**: User description: "When in a workout, much like I want to see my previous effort and weight, I also want to see in what order the exercises were in previously. This can be indicated by a simple #x to indicate the number in the order of the previous workout"

## Context

The active workout session view already surfaces the previous weight (KG) and effort rating for each exercise from the most recently completed session for the same planned workout (introduced in `008-workout-exercise-history`). The user now wants one more piece of historical reference: the position the exercise occupied in that previous session — shown as `#1`, `#2`, `#3`, etc. This is especially useful when exercise order is randomised (`011-randomise-exercise-order`), as the user can see at a glance whether they are hitting an exercise earlier or later than they did last time, helping them calibrate expectations accordingly.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — See Previous Exercise Position in Active Session (Priority: P1)

When a user is in an active workout session for a planned workout that has been completed at least once before, each exercise row displays a small indicator showing the position that exercise occupied in the most recently completed session for that same planned workout. The indicator uses the format `#x` (e.g., `#1`, `#3`). It sits alongside the existing previous weight and effort reference data and is read-only.

**Why this priority**: This is the entire feature. It directly mirrors the pattern already established for previous weight and effort — one additional reference datum per exercise, sourced from the same prior session. All value is delivered by this single story.

**Independent Test**: Can be fully tested by completing a planned workout (recording at least the exercise order), then starting a new session for the same planned workout and confirming each exercise displays the correct `#x` position indicator matching its position in the previous session.

**Acceptance Scenarios**:

1. **Given** a planned workout has been completed at least once, **When** the user starts a new session for that same planned workout, **Then** each exercise row displays the position that exercise occupied in the most recently completed session, shown as `#x` (e.g., `#1`, `#2`).
2. **Given** the previous session had three exercises in order A, B, C, **When** the user starts a new session with the same order, **Then** A shows `#1`, B shows `#2`, and C shows `#3`.
3. **Given** the previous session had exercises in a randomised order (e.g., C was first, A was second, B was third), **When** the user starts a new session with a different order, **Then** each exercise still shows the `#x` position from the previous session regardless of its current position.
4. **Given** a planned workout has never been completed before, **When** the user starts a session, **Then** no `#x` indicator is shown for any exercise — consistent with the existing "no previous data" behaviour for weight and effort.
5. **Given** an exercise was added to the planned workout after the last session, **When** the user starts a new session, **Then** no `#x` indicator is shown for that exercise (it was not present in the previous session).

---

### Edge Cases

- What happens when the previous session had a different number of exercises than the current session? Exercises not present in the previous session show no `#x` indicator; exercises present in both show their previous position.
- What happens when the previous session data fails to load? The `#x` indicator must not appear (no placeholder or zero shown). The session remains fully usable, consistent with the error handling defined in `008-workout-exercise-history`.
- What happens on a slow network when previous data loads after the initial render? The `#x` indicator must appear without causing a disruptive layout shift — a stable placeholder occupies the space while data is loading.
- What happens when the active session has only one exercise? The single exercise shows `#1` if it was present in the previous session and was the only exercise then too.
- How does the experience stay consistent across loading, empty (first time), success, and error states? All four states must be handled in the same visual style as the existing previous weight and effort indicators.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When an active workout session is started from a planned workout that has been completed at least once, the system MUST retrieve the exercise order from the most recently completed session for that same planned workout.
- **FR-002**: For each exercise in the active session that was present in the most recently completed session, the system MUST display the position that exercise occupied in the previous session, formatted as `#x` (e.g., `#1`, `#3`).
- **FR-003**: The `#x` indicator MUST be scoped to the same planned workout — order data from other planned workouts that contain the same exercise MUST NOT be surfaced.
- **FR-004**: If no previous session exists for the planned workout, the system MUST NOT display any `#x` indicator for any exercise — consistent with the "no previous data" state for weight and effort.
- **FR-005**: If an exercise was not present in the most recently completed session (e.g., added after that session), the system MUST NOT display a `#x` indicator for it.
- **FR-006**: The `#x` indicator MUST be displayed as read-only reference information alongside the existing previous weight and effort reference data — it MUST NOT be an editable field.
- **FR-007**: The active session's editable fields (current weight, current effort) MUST remain fully functional regardless of the state of the `#x` indicator.
- **FR-008**: If previous order data cannot be loaded due to an error, the `#x` indicator MUST not appear, and the session MUST remain fully usable — consistent with the non-blocking error handling in `008-workout-exercise-history`.

### Security & Privacy Requirements

- **SR-001**: The system MUST enforce that users can only retrieve previous session order data for planned workouts and sessions they own — no cross-user data leakage.
- **SR-002**: All data retrieved for the previous order indicator MUST be validated and sanitized before display.

### User Experience Consistency Requirements

- **UX-001**: The `#x` indicator MUST be visually consistent with the existing previous weight and effort reference display (same label style, same read-only treatment), following the established pattern from `008-workout-exercise-history`.
- **UX-002**: Loading, empty (no previous session), success (indicator shown), and error states MUST all be defined and handled using the same visual patterns as the existing previous data indicators.
- **UX-003**: The `#x` format (hash symbol followed by the position number) MUST be used consistently for all exercises — no variation in format (e.g., no "1st", "No. 1", or "Position 1").

### Performance Requirements

- **PR-001**: The `#x` position data MUST be available within a time frame that does not block the user from starting to interact with the session view — consistent with the performance expectation from `008-workout-exercise-history`.
- **PR-002**: Retrieving exercise order from the previous session MUST NOT introduce unbounded queries — it is read-only and scoped to a single prior session.
- **PR-003**: Performance MUST be verified by confirming the active session view remains fully interactive while the previous order data loads.

### Key Entities

- **WorkoutSession**: A completed record of a performed workout, linked to a planned workout. The exercise order within this session is the source of the `#x` values shown in the next session.
- **LoggedExercise**: The per-exercise record within a WorkoutSession. Its position within the session (1-based) is the value displayed as `#x`.

## Assumptions

- The `#x` position is 1-based (first exercise is `#1`, not `#0`).
- Position is determined by the order exercises were recorded in the previous session, matching the same source already used for previous weight and effort data in `008-workout-exercise-history`.
- The `#x` indicator is fetched alongside (or as part of) the same data retrieval that powers the existing previous weight and effort display — no separate data fetch is required.
- The feature does not store any new data — it reads and presents an ordering that already exists in the completed session record.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users starting a repeated planned workout can see the `#x` previous position indicator for every exercise that was present in the last session, without leaving the active session view.
- **SC-002**: The `#x` position shown for each exercise is always sourced exclusively from the most recently completed session for the exact planned workout being run — zero instances of cross-workout data contamination.
- **SC-003**: The active session view remains fully interactive while previous order data loads — no blocking state is observed.
- **SC-004**: All four states (loading, first-time/empty, success with indicator, and error) are visually distinguishable and presented consistently alongside the existing previous weight and effort indicators.
- **SC-005**: The `#x` format is applied uniformly — every exercise that has a previous position shows exactly that format, with no variation in label or style.
