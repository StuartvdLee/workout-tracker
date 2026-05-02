# Feature Specification: Reorder Exercises in a Workout

**Feature Branch**: `006-reorder-exercises`  
**Created**: 2026-05-02  
**Status**: Draft  
**Input**: User description: "I want to be able to change the order of exercises. This could be necessary when creating a new workout and the user is adding exercises but in the wrong order. They should be able to drag exercises in the order they desire before saving the exercise. The other situation is when editing a workout. The user should be able to rearrange the order of exercises here."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reorder Exercises While Creating a Workout (Priority: P1)

While building a new planned workout by adding exercises, a user realises their exercises are listed in the wrong order. Before saving, they drag exercises up or down to arrange them in the desired sequence. The reordered list is reflected immediately in the UI and is saved when the user submits the new workout.

**Why this priority**: This is the most impactful scenario — a user creating a brand-new workout from scratch needs to correct ordering mistakes before committing the workout. Without this, the only remedy is to delete and re-add exercises, which is frustrating.

**Independent Test**: Can be fully tested by opening the new workout form, adding at least three exercises, dragging one exercise to a different position, saving the workout, and verifying the exercises are persisted in the reordered sequence.

**Acceptance Scenarios**:

1. **Given** the user is creating a new workout and has added two or more exercises, **When** they drag an exercise to a new position in the list, **Then** the exercise list immediately reflects the new order.
2. **Given** the user has reordered exercises in the new workout form, **When** they save the workout, **Then** the exercises are persisted in the order shown in the form.
3. **Given** the user has dragged an exercise to a new position, **When** they drag it back to its original position, **Then** the original order is restored and is saved correctly.
4. **Given** the workout form contains only one exercise, **When** the user views the exercise list, **Then** no drag affordance or reorder UI is needed and no visual clutter is introduced.

---

### User Story 2 - Reorder Exercises While Editing an Existing Workout (Priority: P2)

A user opens an existing planned workout to edit it and decides to change the order of the exercises already saved to that workout. They drag exercises to rearrange them within the edit view. When the workout is saved, the new order is persisted, replacing the previous exercise sequence.

**Why this priority**: Editing an existing workout is a natural follow-on to creation. Users whose workout needs evolve over time must be able to refine exercise ordering without recreating the entire workout.

**Independent Test**: Can be fully tested by opening an existing workout that contains at least three exercises in the edit view, dragging one exercise to a different position, saving, and verifying the exercises are stored in the new order.

**Acceptance Scenarios**:

1. **Given** the user opens an existing workout in edit mode, **When** they view the exercise list, **Then** exercises are displayed in their currently saved order with a visible drag handle affordance.
2. **Given** the user is editing a workout, **When** they drag an exercise to a new position, **Then** the list reorders immediately to reflect the change.
3. **Given** the user has reordered exercises in the edit view, **When** they save the workout, **Then** the exercises are persisted in the updated order.
4. **Given** the user has reordered exercises in the edit view, **When** they cancel without saving, **Then** the previously saved order is preserved and no changes are committed.
5. **Given** the user saves a reordered workout, **When** they subsequently open it to view or edit, **Then** the exercises appear in the order they last saved.

---

### Edge Cases

- What happens when the user starts dragging an exercise and the list scrolls (i.e., a long exercise list)?  The drag interaction must support scrolling so all positions are reachable.
- What happens if a network error occurs while saving a reordered workout? The user should see a clear error message and the form should remain open with the reordered state intact so they can retry.
- What happens if the user reorders exercises and then adds a new exercise? The newly added exercise should appear at the bottom of the current (possibly reordered) list, and further dragging should still be possible.
- What happens when the user removes an exercise after reordering? The remaining exercises should retain their relative order without gaps or numbering errors in the persisted positions.
- How does the experience stay consistent on touch devices (phones/tablets) where a touch-based drag must work in place of a mouse drag?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to drag an exercise to a new position within the exercise list while creating a new planned workout.
- **FR-002**: Users MUST be able to drag an exercise to a new position within the exercise list while editing an existing planned workout.
- **FR-003**: The exercise list MUST update its displayed order immediately when a drag-and-drop reorder action is completed, with no page refresh required.
- **FR-004**: The reordered exercise sequence MUST be saved and persisted when the user submits or saves the workout form.
- **FR-005**: Each exercise item in the list MUST display a clear visual drag handle that communicates to the user that reordering is possible.
- **FR-006**: Reorder interactions MUST work on both pointer (mouse) and touch input devices.
- **FR-007**: Cancelling the edit of an existing workout after reordering MUST leave the previously saved exercise order unchanged.
- **FR-008**: Adding or removing exercises after reordering MUST preserve the relative order of remaining exercises.

### Security & Privacy Requirements

- **SR-001**: The system MUST validate that the submitted exercise order belongs to the workout being saved, preventing a user from reordering exercises in another user's workout.
- **SR-002**: The system MUST sanitize and validate all order-position values submitted to the backend, rejecting invalid or out-of-range values.

### User Experience Consistency Requirements

- **UX-001**: The drag handle affordance MUST follow the same visual style used elsewhere in the application (e.g., consistent icon, spacing, colour tokens).
- **UX-002**: The exercise list MUST provide clear visual feedback during a drag operation (e.g., a placeholder showing where the item will be dropped, the dragged item visually elevated or highlighted).
- **UX-003**: If a save fails after reordering, the error state MUST be communicated with a message consistent with how other save errors are displayed in the workout forms.
- **UX-004**: The reorder affordance MUST NOT be shown when there is only one exercise in the list, avoiding visual noise for a no-op interaction.

### Performance Requirements

- **PR-001**: Reordering interactions MUST feel instantaneous — the list MUST reorder within 100 ms of the user completing a drag.
- **PR-002**: Saving a reordered workout MUST complete within the same time budget as saving any other workout change, with no additional latency perceived by the user.
- **PR-003**: Performance of the reorder interaction MUST not degrade for workout lists up to 20 exercises.

### Key Entities

- **PlannedWorkout**: A user-created workout template composed of an ordered list of exercises. This feature adds the concept of a defined sequence (position) to each exercise slot within a workout.
- **WorkoutExercise**: The association between a PlannedWorkout and an Exercise. This entity must carry an explicit ordering value (position) so that the sequence can be stored, updated, and retrieved reliably.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can reorder exercises within a workout in under 30 seconds, regardless of whether they are creating or editing a workout.
- **SC-002**: 100% of exercise order changes made in the UI are accurately reflected in the saved workout when retrieved.
- **SC-003**: The reorder interaction responds within 100 ms of the user releasing a dragged item, as verified by manual and automated interaction testing.
- **SC-004**: Cancelling an edit after reordering results in zero changes to the previously stored exercise order, verified by confirming the saved state is unchanged.
- **SC-005**: The drag-and-drop interaction works correctly across all supported input types (pointer and touch), with no functional gaps between interaction modes.
- **SC-006**: 100% of affected create and edit flows consistently display loading, success, and error states using the same patterns as existing workout form interactions.

## Assumptions

- Only **planned workout** exercise order is in scope. Reordering exercises within an active workout session (WorkoutSession) is out of scope for this feature.
- Exercise order is a property of the **WorkoutExercise** association (the exercise slot within a workout), not of the Exercise entity itself.
- The existing edit-workout modal/view (from feature 004) is the surface where the edit-time reordering will be exposed; no new page or navigation path is introduced.
- A workout must have at least two exercises for the reorder affordance to be visible and actionable.
- Touch-based drag is expected to work on mobile browsers; a native mobile app is out of scope.
