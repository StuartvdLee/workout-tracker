# Feature Specification: Randomize Workout Exercise Order

**Feature Branch**: `010-randomize-exercise-order`  
**Created**: 2026-05-08  
**Status**: Draft  
**Input**: User description: "If I start a workout now, the order of exercises is always the same. This means that I can do the same exercises well and I get tired during the same time, every time. I want there to be a way for me to indicate that I want to randomise the order of the exercises for that specific workout, when I start the workout (maybe before?)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shuffle Exercises Before Starting (Priority: P1)

A user who always follows the same exercise order wants variety in their training stimulus. When they are about to begin a workout, they are presented with an option to shuffle the exercise order for that session. They enable the option and the workout begins with exercises presented in a randomised sequence.

**Why this priority**: This is the core value of the feature. Without it the feature does not exist. Every other story builds on top of this action.

**Independent Test**: Can be fully tested by navigating to a workout with 3+ exercises, enabling the shuffle option, and verifying the exercises are presented in a different order than the template defines.

**Acceptance Scenarios**:

1. **Given** a user has a workout with 3 or more exercises, **When** they initiate that workout, **Then** they are presented with an option to randomise the exercise order before the session begins.
2. **Given** a user enables the randomise option, **When** the workout session starts, **Then** the exercises are presented in a shuffled order.
3. **Given** a user does not enable the randomise option, **When** the workout session starts, **Then** the exercises are presented in the original saved order.
4. **Given** a user has a workout with only 1 exercise, **When** they initiate the workout, **Then** the randomise option is disabled or hidden, and the single exercise is presented as normal with no error.

---

### User Story 2 - Preview Shuffled Order Before Committing (Priority: P2)

A user wants to see the shuffled exercise sequence before they commit to starting, so they can mentally prepare or decide to re-shuffle if the order does not suit them (e.g., two heavy compound exercises back-to-back).

**Why this priority**: Reduces friction and builds confidence. A user may want to re-shuffle rather than begin with an unfavourable sequence.

**Independent Test**: Can be fully tested by enabling shuffle on the pre-start screen and verifying the exercise list is visible and a re-shuffle action is available before the session becomes active.

**Acceptance Scenarios**:

1. **Given** a user has enabled the randomise option, **When** they are on the pre-start screen, **Then** they can see the full list of exercises in the shuffled order before the session begins.
2. **Given** a user can see the shuffled order, **When** they select a "Re-shuffle" action, **Then** the exercise list is reshuffled and the updated order is displayed.
3. **Given** a user can see the shuffled order and decides to proceed, **When** they start the session, **Then** the workout follows exactly the order shown in the preview.

---

### User Story 3 - Session Order Does Not Affect Workout Template (Priority: P3)

A user wants confidence that enabling shuffle will not permanently alter their saved workout. After completing or abandoning a randomised session, the workout template retains its original exercise order for future sessions.

**Why this priority**: Without this guarantee, users will be reluctant to use the randomise feature for fear of losing their carefully curated order.

**Independent Test**: Can be fully tested by completing a randomised workout session and then opening the workout in edit mode to confirm the exercise order matches the original template.

**Acceptance Scenarios**:

1. **Given** a user completes a workout session with randomisation enabled, **When** they view the workout template, **Then** the exercises are in the original pre-randomisation order.
2. **Given** a user abandons a randomised workout session mid-way, **When** they view the workout template, **Then** the exercises remain in the original order.
3. **Given** a user starts a subsequent workout session without enabling randomisation, **When** the session begins, **Then** the exercises are presented in the original saved order.

---

### Edge Cases

- What happens when a workout has exactly 2 exercises and shuffle is enabled (only one alternative order is possible)?
- How does the system behave if a shuffle produces the same sequence as the original (statistically possible with small exercise counts)?
- What happens if the user navigates away from the pre-start screen after enabling shuffle and then returns — is the shuffle state retained?
- How does the pre-start screen behave if exercises fail to load (loading, empty, error states)?
- What happens if a user loses connectivity immediately after starting a shuffled session — is the recorded exercise order preserved?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST present a randomise order control when a user initiates a workout that contains 2 or more exercises.
- **FR-002**: When randomisation is enabled, System MUST shuffle the exercise order using an unpredictable algorithm before the session begins.
- **FR-003**: The shuffled order MUST remain fixed for the duration of the session — exercises MUST NOT be re-ordered automatically while a session is active.
- **FR-004**: System MUST display the shuffled exercise sequence to the user on the pre-start screen before they commit to beginning the session.
- **FR-005**: System MUST provide a re-shuffle action on the pre-start screen that generates and displays a new random order without starting the session.
- **FR-006**: System MUST record the actual order in which exercises were performed in the session data, regardless of whether randomisation was used.
- **FR-007**: Completing or abandoning a randomised session MUST NOT modify the exercise order stored in the workout template.
- **FR-008**: For workouts with fewer than 2 exercises, the randomise control MUST be unavailable (disabled or hidden) and no shuffle logic is invoked.

### Security & Privacy Requirements

- **SR-001**: System MUST verify that the workout and its exercises belong to the authenticated user before allowing a session to start.
- **SR-002**: Session order data recorded on completion MUST be stored under the authenticated user's session record and not be accessible to other users.
- **SR-003**: The randomise toggle state MUST be treated as a session-scoped client preference and MUST NOT be persisted server-side between sessions unless explicitly designed to do so.

### User Experience Consistency Requirements

- **UX-001**: The randomise control MUST use interaction and visual patterns consistent with other pre-workout options already present in the application.
- **UX-002**: The pre-start screen MUST handle loading, empty (no exercises), success (order displayed), and error (failed to load exercises) states gracefully. *(Loading and error states are inherited from the workouts page — the "Start" button is only reachable after exercises have loaded into module state. The empty state (0 exercises) is addressed explicitly: see T027.)*
- **UX-003**: Labels such as "Randomise", "Shuffle", and "Re-shuffle" MUST be consistent in spelling and meaning throughout the pre-start flow and any in-session references.

### Performance Requirements

- **PR-001**: The shuffle operation and updated pre-start screen render MUST complete within the same time budget as the standard pre-start screen load.
- **PR-002**: Shuffling a workout with up to 50 exercises MUST introduce no perceptible delay compared to loading without shuffle.
- **PR-003**: The shuffle feature's impact on workout start time MUST be verified as part of standard pre-release testing.

### Key Entities

- **Workout Template**: The saved definition of a workout, including the ordered list of exercises. Must remain immutable as a result of a randomised session.
- **Workout Session**: A single instance of performing a workout. Records which exercises were performed and in what order, independently of the template.
- **Exercise Order**: An ordered sequence of exercises within either a template or a session. A session may hold an order that differs from its template.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can enable exercise randomisation within 2 interactions of initiating a workout.
- **SC-002**: For workouts with 3 or more exercises, the shuffled order differs from the original template order in at least 80% of randomisations.
- **SC-003**: 100% of completed workout sessions correctly record the actual exercise order performed, whether shuffled or not.
- **SC-004**: The workout template's exercise order is unchanged in 100% of cases after a randomised session is completed or abandoned.
- **SC-005**: The pre-start screen with shuffle enabled loads within the same time as the standard pre-start screen in 95% of cases.
- **SC-006**: Users can preview and re-shuffle the exercise order without leaving the pre-start screen.

## Assumptions

- The application already has a pre-start or confirmation step before a workout session becomes active, which is where the randomise option will be surfaced.
- Exercise order within a workout template is already a defined, persisted concept (as established in feature 006-reorder-exercises).
- Session data already records which exercises were performed; this feature extends that record to capture the order used.
- "Randomise" means a full shuffle of all exercises in the workout — partial or group-based shuffling (e.g., keeping warm-up exercises first) is out of scope for this feature.
- The randomise toggle defaults to off at the start of every session; the user must actively opt in each time.
