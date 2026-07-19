# Feature Specification: Edit Exercise Order

**Feature Branch**: `030-edit-exercise-order`  
**Created**: 2026-07-19  
**Status**: Draft  
**Input**: User description: "I want to be able to change the order of exercises in a current workout. In the top right of the current workout screen should be an \"Edit order\" button. This should \"collapse\" all exercises and only show their names (so the weight and effort should be hidden). I should be able to drag and drop exercises to change their order in the same way as when editing a workout on the \"Edit Workout\" screen."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enter order editing mode from current workout (Priority: P1)

As a user viewing a current workout, I want an obvious "Edit order" action in the top-right area so I can switch into a focused ordering mode without leaving the workout.

**Why this priority**: Users need a clear entry point before they can reorder exercises, and placing it consistently in the requested screen location makes the feature discoverable.

**Independent Test**: Can be tested by opening a current workout with multiple exercises, selecting "Edit order", and confirming the screen changes into ordering mode while staying on the current workout.

**Acceptance Scenarios**:

1. **Given** a current workout is open, **When** the user views the top-right area of the screen, **Then** an "Edit order" button is visible.
2. **Given** a current workout with exercises is open, **When** the user selects "Edit order", **Then** all exercise cards collapse into an order-editing view that shows exercise names only.
3. **Given** order-editing mode is active, **When** the user reviews the exercise list, **Then** weight, effort, and other workout-entry controls are hidden until the user exits order editing.

---

### User Story 2 - Reorder current workout exercises (Priority: P1)

As a user editing the order of a current workout, I want to drag and drop exercises into a new sequence so the workout follows the order I intend to perform.

**Why this priority**: Reordering is the core user value of the feature and must match the existing drag-and-drop experience users already know from editing a workout.

**Independent Test**: Can be tested by entering order-editing mode, dragging one exercise to a different position, and confirming the visible order changes accordingly.

**Acceptance Scenarios**:

1. **Given** order-editing mode is active for a current workout with at least two exercises, **When** the user drags an exercise to a new position, **Then** the exercise appears in the new position and the surrounding exercises shift accordingly.
2. **Given** order-editing mode is active, **When** the user reorders exercises, **Then** the interaction behavior is consistent with reordering exercises on the "Edit Workout" screen.
3. **Given** the user has changed the exercise order, **When** the current workout is subsequently viewed, **Then** exercises appear in the updated order.

---

### User Story 3 - Return to normal workout entry (Priority: P2)

As a user who has finished changing exercise order, I want to return to the normal current workout view so I can continue recording weight and effort.

**Why this priority**: Users must be able to resume the primary workout flow after reordering without losing context or workout data.

**Independent Test**: Can be tested by entering order-editing mode, changing or preserving the order, exiting the mode, and confirming the full workout-entry fields are restored.

**Acceptance Scenarios**:

1. **Given** order-editing mode is active, **When** the user exits order editing, **Then** the current workout returns to its normal expanded exercise view.
2. **Given** the user exits order-editing mode, **When** the normal view is restored, **Then** each exercise again shows its weight and effort information.
3. **Given** existing workout-entry data is present before reordering, **When** the user reorders exercises and returns to normal view, **Then** the data remains associated with the same exercises.

---

### Edge Cases

- If the current workout has no exercises, the order-editing action should not create an empty drag-and-drop workflow and should keep the empty state clear.
- If the current workout has exactly one exercise, entering order-editing mode should show the single exercise name without implying a reorder is possible.
- If the user starts dragging and cancels before dropping, the exercise order should remain unchanged.
- If order changes cannot be saved or retained, the user should receive clear feedback and the app should avoid presenting a successfully changed order.
- If the screen is narrow or touch-based, the collapsed ordering view should remain usable without exposing hidden weight or effort controls.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The current workout screen MUST display an "Edit order" button in the top-right area when a current workout is available.
- **FR-002**: Selecting "Edit order" MUST switch the current workout screen into an order-editing mode without navigating away from the current workout.
- **FR-003**: Order-editing mode MUST collapse each exercise so only the exercise name is shown as the primary visible content.
- **FR-004**: Order-editing mode MUST hide weight, effort, and other normal workout-entry controls for each exercise.
- **FR-005**: Users MUST be able to reorder exercises in the current workout by dragging and dropping them.
- **FR-006**: The reorder interaction MUST be consistent with the drag-and-drop reorder behavior used on the "Edit Workout" screen.
- **FR-007**: The changed order MUST be reflected in the current workout after the user reorders exercises.
- **FR-008**: Reordering exercises MUST preserve each exercise's existing workout-entry data, including weight and effort values, with the correct exercise.
- **FR-009**: Users MUST be able to exit order-editing mode and return to the normal current workout view.
- **FR-010**: The normal current workout view MUST restore exercise details and entry controls after order-editing mode ends.
- **FR-011**: The system MUST handle workouts with zero or one exercise without presenting a broken or misleading reorder experience.

### Security & Privacy Requirements

- **SR-001**: The feature MUST only change the order of exercises in the current workout the user is already allowed to view and edit.
- **SR-002**: The feature MUST NOT expose hidden workout-entry data while the collapsed order-editing view is active.
- **SR-003**: The feature MUST preserve existing workout data integrity when exercise order changes.

### User Experience Consistency Requirements

- **UX-001**: The "Edit order" label, placement, and interaction state MUST be understandable from the current workout screen without requiring navigation to another screen.
- **UX-002**: The collapsed exercise rows in order-editing mode MUST use visual and interaction patterns consistent with the "Edit Workout" reorder experience.
- **UX-003**: The feature MUST define clear normal, empty, single-exercise, active reordering, successful reorder, and reorder failure states.
- **UX-004**: The transition into and out of order-editing mode MUST keep the user on the current workout and preserve their context.

### Performance Requirements

- **PR-001**: Entering or exiting order-editing mode SHOULD visibly complete within 1 second for a typical current workout.
- **PR-002**: Dragging an exercise SHOULD feel responsive, with visible item movement during the interaction for workouts of up to 50 exercises.
- **PR-003**: Reordering exercises SHOULD complete without noticeable delay for typical current workouts.

### Key Entities *(include if feature involves data)*

- **Current Workout**: The workout session currently being viewed or performed; contains an ordered list of exercises and their in-progress workout-entry data.
- **Workout Exercise**: An exercise entry within the current workout; has a name, position in the workout, and associated workout-entry values such as weight and effort.
- **Exercise Order**: The user-defined sequence of workout exercises in the current workout.

### Assumptions

- The current workout screen already supports viewing and editing workout-entry values such as weight and effort.
- The "Edit Workout" screen already has an established drag-and-drop ordering pattern that should be reused from the user's perspective.
- Reordering a current workout changes only the exercise sequence, not the exercises themselves or their recorded values.
- The order should remain changed after the user returns to the normal current workout view.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of users can find and enter order-editing mode from the current workout screen within 10 seconds.
- **SC-002**: Users can reorder a workout containing 10 exercises in under 30 seconds.
- **SC-003**: 100% of reordered exercises retain their existing weight and effort data with the correct exercise after the order changes.
- **SC-004**: 90% of users who have previously used the "Edit Workout" reorder flow can successfully reorder exercises in the current workout without additional instruction.
- **SC-005**: Entering and exiting order-editing mode completes within 1 second for typical current workouts.
- **SC-006**: In order-editing mode, 100% of exercise rows hide weight and effort details and show exercise names.
