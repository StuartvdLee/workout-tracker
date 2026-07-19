# Feature Specification: Latest Exercise Data

**Feature Branch**: `029-latest-exercise-data`  
**Created**: 2026-07-19  
**Status**: Draft  
**Input**: User description: "GitHub issue #128 - Last time should get previous available data. Last time data on a current workout now gets data from the previous workout. That sometimes leaves the data empty (when an exercise is skipped for instance). Instead, it should get the latest available data for an exercise"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See latest available exercise data during a workout (Priority: P1)

As a person performing a workout, I want the "last time" information for each exercise to show the most recent recorded data for that exercise, even if the exercise was skipped in one or more recent workouts, so I can compare today's effort against meaningful prior performance.

**Why this priority**: This is the core user value. Empty "last time" information removes the comparison cue users rely on during active workouts.

**Independent Test**: Can be tested by starting a workout that contains an exercise skipped in the immediately previous workout but completed earlier, then confirming the earlier completed data is shown as the last available data.

**Acceptance Scenarios**:

1. **Given** an exercise has completed data from an older workout and was skipped in the immediately previous workout, **When** the user starts a workout containing that exercise, **Then** the "last time" area shows the older completed data for that exercise.
2. **Given** an exercise was completed in the immediately previous workout, **When** the user starts a workout containing that exercise, **Then** the "last time" area shows data from that immediately previous completion.
3. **Given** multiple historical workouts contain completed data for an exercise, **When** the user views the "last time" information, **Then** the data comes from the most recent completed occurrence of that exercise.

---

### User Story 2 - Understand when no prior exercise data exists (Priority: P2)

As a person performing a workout, I want a clear empty state when an exercise has never been completed before, so I know the absence of "last time" data is expected rather than a missing-history problem.

**Why this priority**: A clear distinction between "no prior completion exists" and "a recent workout skipped the exercise" prevents confusion and makes the feature trustworthy.

**Independent Test**: Can be tested by adding or selecting an exercise with no completed historical data and confirming the "last time" area communicates that no prior data is available.

**Acceptance Scenarios**:

1. **Given** an exercise has no completed historical data, **When** the user views that exercise in a current workout, **Then** the "last time" area shows a clear no-prior-data state.
2. **Given** an exercise only has skipped or incomplete historical entries, **When** the user views that exercise in a current workout, **Then** the "last time" area does not present those entries as usable prior performance data.

---

### User Story 3 - Keep historical comparisons stable across workout changes (Priority: P3)

As a person reviewing workout activity, I want the chosen previous comparison data to stay consistent when workouts include reordered, skipped, or partially completed exercises, so comparison information remains reliable across normal workout variations.

**Why this priority**: Workout routines often vary over time. Stable comparison behavior improves confidence after the primary lookup behavior is corrected.

**Independent Test**: Can be tested with workout history containing completed, skipped, reordered, and partially completed exercise entries, then confirming each current exercise resolves to the newest usable prior data for the same exercise.

**Acceptance Scenarios**:

1. **Given** an exercise appears in different positions across past workouts, **When** the current workout displays "last time" data, **Then** the displayed data is matched by exercise identity rather than workout position.
2. **Given** the latest historical entry for an exercise is incomplete and an older entry is complete, **When** the current workout displays "last time" data, **Then** the older complete entry is used.

---

### Edge Cases

- An exercise appears in the previous workout but was skipped, while an older workout has completed data.
- An exercise has never been completed before.
- An exercise has only skipped or incomplete historical entries.
- Multiple historical workouts contain the same exercise; the most recent completed occurrence must be selected.
- Workout history is loading slowly or temporarily unavailable; the user should see a loading or error state rather than misleading blank data.
- Exercises are reordered between workouts; lookup must still refer to the same exercise.
- Historical data exists for other exercises in the workout but not for the selected exercise.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST determine "last time" data for a current workout exercise from the most recent completed historical occurrence of the same exercise.
- **FR-002**: System MUST skip historical occurrences where the exercise was skipped and continue searching older history for completed data.
- **FR-003**: System MUST skip historical occurrences that do not contain usable performance data for comparison.
- **FR-004**: System MUST show the immediately previous completed occurrence when it contains usable data.
- **FR-005**: System MUST show a clear no-prior-data state when no completed historical occurrence exists for the exercise.
- **FR-006**: System MUST match historical data to the exercise itself, not to its position in a workout or session.
- **FR-007**: System MUST keep previous exercise comparison data consistent across active workout and workout review contexts where the same comparison is shown.
- **FR-008**: System MUST avoid showing blank "last time" data when older completed data exists for the exercise.

### Security & Privacy Requirements

- **SR-001**: System MUST only use workout history that the current user is authorized to view.
- **SR-002**: System MUST not expose another user's workout history in "last time" comparisons.
- **SR-003**: System MUST handle missing, malformed, or incomplete historical workout data without exposing diagnostic details to users.

### User Experience Consistency Requirements

- **UX-001**: The "last time" area MUST preserve existing labels, layout, and interaction patterns except where copy is needed for the no-prior-data state.
- **UX-002**: The feature MUST define distinct loading, no-prior-data, success, and error states for the "last time" display.
- **UX-003**: The no-prior-data state MUST clearly indicate that no completed prior data exists for that exercise.
- **UX-004**: The success state MUST make the selected prior data readable without requiring the user to inspect workout history manually.

### Performance Requirements

- **PR-001**: For 95% of workout views, "last time" information for visible exercises MUST appear within 1 second of the workout becoming available.
- **PR-002**: The feature MUST remain responsive for users with at least 2 years of regular workout history.
- **PR-003**: Loading delayed historical comparison data MUST not block users from continuing the current workout.

### Key Entities

- **Exercise**: A movement or activity that can appear in workouts and has a stable identity across workout sessions.
- **Workout Session**: A completed or active workout containing one or more exercise entries.
- **Exercise History Entry**: A historical occurrence of an exercise within a workout, including whether it was completed, skipped, or incomplete and any recorded performance data.
- **Last Available Exercise Data**: The newest usable completed historical data for a specific exercise, selected for display in the current workout context.

### Assumptions

- "Usable performance data" means completed exercise data that is currently meaningful in the existing "last time" display.
- Skipped exercises and incomplete entries should not be treated as valid comparison data.
- If no completed historical data exists, the product should show an intentional empty state rather than a blank or missing value.
- The feature applies to the existing user-owned workout history available in the product.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In test histories where the previous workout skipped an exercise but older completed data exists, the correct older data is shown for 100% of affected exercises.
- **SC-002**: Users see a clear no-prior-data state for 100% of exercises with no completed historical data.
- **SC-003**: At least 95% of workout views show available "last time" comparison data within 1 second of the workout becoming available.
- **SC-004**: Users can identify whether prior exercise data exists without opening workout history in at least 90% of usability checks.
- **SC-005**: No workout scenario shows blank "last time" data when a completed historical entry exists for the same exercise.
