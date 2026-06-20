# Feature Specification: Randomise Exercise Order UX Simplification

**Feature Branch**: `011-randomise-exercise-order`  
**Created**: 2026-05-09  
**Status**: Complete  
**Input**: User description: "I want to change the randomise exercise order. When starting a workout from the homepage, I just want to have a toggle that says 'Randomise exercise order'. Would be nice if it's a toggle like on iOS instead of a checkbox. When starting a workout from the Workouts page, I just want a modal that asks Randomise exercise order? with a Yes and No button. Clicking Yes randomises the order and starts the workout. Clicking No doesn't randomise the order and starts the workout. Take out all Re-shuffle functionality"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start Workout from Homepage with Toggle (Priority: P1)

A user on the homepage selects a workout and sees a persistent iOS-style toggle labelled "Randomise exercise order" directly on the page. The user flips the toggle on or off before clicking "Start Workout". The workout starts with the exercise order honoured accordingly. No modal is shown; the toggle state is the sole control.

**Why this priority**: The homepage is the primary entry point for starting a workout. Simplifying the flow by moving the toggle out of a modal and onto the page reduces friction and makes the feature immediately discoverable.

**Independent Test**: Can be fully tested by navigating to the homepage, toggling "Randomise exercise order" to the on position, clicking "Start Workout", and confirming the active session presents exercises in a different order than the original. Toggling off and repeating should preserve the original order.

**Acceptance Scenarios**:

1. **Given** the homepage is displayed with a workout selected, **When** the user views the start area, **Then** an iOS-style toggle labelled "Randomise exercise order" is visible and off by default.
2. **Given** the toggle is off, **When** the user clicks "Start Workout", **Then** the workout starts with exercises in their defined order.
3. **Given** the toggle is on, **When** the user clicks "Start Workout", **Then** the workout starts with exercises in a randomised order.
4. **Given** the selected workout has only one exercise, **When** the start area is displayed, **Then** the "Randomise exercise order" toggle is hidden.

---

### User Story 2 - Start Workout from Workouts Page with Yes/No Modal (Priority: P2)

A user on the Workouts page clicks the start button for a workout. A focused modal appears asking "Randomise exercise order?" with two buttons: "Yes" and "No". Clicking "Yes" randomises the exercise order and immediately starts the workout. Clicking "No" starts the workout in its defined order. The modal contains no other controls.

**Why this priority**: The Workouts page offers a different interaction pattern where a confirmation step is expected. The simplified modal preserves intentional confirmation while eliminating unnecessary options.

**Independent Test**: Can be fully tested by navigating to the Workouts page, clicking start on any workout, selecting "Yes" in the modal, and confirming the active session shows a randomised order. Repeating with "No" confirms the original order is preserved.

**Acceptance Scenarios**:

1. **Given** a workout with 2 or more exercises on the Workouts page, **When** the user clicks the start button, **Then** a modal appears containing only the question "Randomise exercise order?" and "Yes" / "No" buttons.
2. **Given** the modal is open, **When** the user clicks "Yes", **Then** the modal closes and the workout starts immediately with a randomised exercise order.
3. **Given** the modal is open, **When** the user clicks "No", **Then** the modal closes and the workout starts immediately in the defined exercise order.
4. **Given** a workout with only one exercise on the Workouts page, **When** the user clicks the start button, **Then** the modal is skipped and the workout starts directly (randomisation is irrelevant).
5. **Given** the modal is open, **When** the user presses Escape, **Then** the modal closes without starting the workout.

---

### User Story 3 - Remove Re-shuffle Functionality (Priority: P3)

All "Re-shuffle" controls are removed from every part of the application. No button, link, or affordance for re-shuffling exercises exists after this change.

**Why this priority**: Removal of an existing feature is the lowest-risk story and ensures the UI is uncluttered before the new patterns are validated.

**Independent Test**: Can be fully tested by inspecting every workout-start flow and active session screen and confirming no "Re-shuffle" button or equivalent control is present.

**Acceptance Scenarios**:

1. **Given** any workout start flow on the homepage, **When** the page is loaded, **Then** no "Re-shuffle" button is present.
2. **Given** any workout start flow on the Workouts page, **When** the modal is displayed, **Then** no "Re-shuffle" button is present.
3. **Given** an active workout session started with randomised order, **When** the session screen is displayed, **Then** no "Re-shuffle" button or control is present.

---

### Edge Cases

- What happens when a workout has only one exercise? The randomise control (toggle or modal) is hidden or skipped, and the workout starts directly.
- What happens if randomisation produces the same order as the original? The workout starts normally; no error or retry is triggered.
- How does the homepage toggle behave when the user switches the selected workout to a different one? The toggle resets to off for the newly selected workout.
- What is the state of the homepage toggle on page load? It defaults to off every time the page loads.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The homepage MUST display an iOS-style toggle labelled "Randomise exercise order" in the workout start area, outside of any modal.
- **FR-002**: The homepage toggle MUST default to the off state on every page load and whenever a different workout is selected.
- **FR-003**: When the homepage toggle is on and the user starts a workout, the system MUST randomise the exercise order before navigating to the active session.
- **FR-004**: When the homepage toggle is off and the user starts a workout, the system MUST preserve the defined exercise order.
- **FR-005**: The homepage MUST hide the "Randomise exercise order" toggle when the selected workout has fewer than 2 exercises.
- **FR-006**: The Workouts page MUST show a modal when the user initiates a workout start, containing only the question "Randomise exercise order?" and "Yes" / "No" buttons.
- **FR-007**: Clicking "Yes" in the Workouts page modal MUST randomise the exercise order and start the workout immediately.
- **FR-008**: Clicking "No" in the Workouts page modal MUST start the workout immediately in the defined exercise order.
- **FR-009**: The Workouts page MUST skip the modal and start the workout directly when the workout has fewer than 2 exercises.
- **FR-010**: Pressing Escape while the Workouts page modal is open MUST close the modal without starting the workout.
- **FR-011**: All "Re-shuffle" buttons and controls MUST be removed from every page and flow in the application.

### Security & Privacy Requirements

- **SR-001**: The randomise toggle state is a client-side UI preference and MUST NOT be persisted to the server or exposed in API calls beyond what is already used to pass exercise order to the active session.
- **SR-002**: Exercise order passed to the active session MUST be validated to contain the same set of exercises as the original workout, preventing injection of unexpected exercises.

### User Experience Consistency Requirements

- **UX-001**: The iOS-style toggle on the homepage MUST follow the same visual language as any other toggle controls in the product, providing clear on/off states with an animated transition.
- **UX-002**: The Workouts page modal MUST reuse the existing modal visual pattern (backdrop, focus trap, keyboard accessibility) already used elsewhere in the product.
- **UX-003**: Labels and terminology ("Randomise exercise order", "Yes", "No") MUST be used consistently across both flows.
- **UX-004**: The homepage start area MUST clearly associate the toggle with the "Start Workout" action so users understand the toggle affects the upcoming session.

### Performance Requirements

- **PR-001**: The randomisation of exercise order MUST complete instantly (imperceptible delay) before the navigation to the active session begins.
- **PR-002**: Removing the Re-shuffle button and simplifying the modals MUST result in no increase to page load size or time compared to the current implementation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can initiate a randomised workout from the homepage in 2 interactions or fewer (toggle on + click Start Workout).
- **SC-002**: Users can initiate a randomised workout from the Workouts page in 2 interactions or fewer (click start + click Yes).
- **SC-003**: 100% of "Re-shuffle" controls are absent from all pages and flows.
- **SC-004**: The "Randomise exercise order" toggle on the homepage is hidden for all workouts with fewer than 2 exercises.
- **SC-005**: The Workouts page randomise modal contains exactly 2 action buttons ("Yes" and "No") and no other interactive controls.
- **SC-006**: All affected flows maintain consistent labelling, visual patterns, and keyboard accessibility.
