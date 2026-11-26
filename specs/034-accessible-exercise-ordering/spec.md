# Feature Specification: Accessible Exercise Ordering While Scrolling

**Feature Branch**: `stuartvdlee-accessible-exercise-ordering`

**Created**: 2026-11-26

**Status**: Implemented; manual verification outstanding (see [tasks.md](./tasks.md))

**Input**: [Issue #202](https://github.com/StuartvdLee/workout-tracker/issues/202) — Make exercise ordering accessible while scrolling through a workout.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enter order editing from anywhere in a workout (Priority: P1)

As a person recording a workout, I want the exercise-order control to remain available as I scroll, so I can change the order without returning to the top.

**Why this priority**: Access to ordering throughout a long workout directly resolves the reported interruption.

**Independent Test**: Open a workout with enough exercises to scroll, scroll to the middle and bottom, and activate the order control at each position.

**Acceptance Scenarios**:

1. **Given** a current workout with exercises is displayed, **When** the user scrolls from the top to the middle or bottom, **Then** the order control remains visible and operable without scrolling back to the top.
2. **Given** the user is at any scroll position in the workout, **When** they activate the order control, **Then** the existing order-editing view opens for the same workout.
3. **Given** the user has entered workout values, **When** they open order editing from a scrolled position, **Then** those values remain associated with their respective exercises.

### User Story 2 - Recognize and access the order control (Priority: P1)

As a person using the workout screen, including with a keyboard or assistive technology, I want a recognizable order icon with an accessible name so I can identify and activate the control.

**Why this priority**: The replacement control must be understandable and accessible to everyone who can use the existing action.

**Independent Test**: Identify and activate the icon using a pointer, touch, keyboard, and a screen reader on supported devices.

**Acceptance Scenarios**:

1. **Given** the workout is in its normal entry view, **When** the order control is shown, **Then** it uses a recognizable exercise-reordering icon rather than a visible text button and has an accessible name identifying its action.
2. **Given** the control is available, **When** a keyboard user navigates to it and activates it, **Then** focus is visible and the same order-editing view opens.
3. **Given** a screen reader encounters the control, **When** it announces the control, **Then** its name identifies the edit-order action and its role identifies it as a button.
4. **Given** a mobile viewport, **When** the user scrolls through a workout, **Then** a prominent order control floats above the full-width workout without shifting it sideways, and workout controls remain reachable by scrolling.

### User Story 3 - Preserve the existing editing flow (Priority: P1)

As a person changing exercise order, I want the existing ordering, completion, and cancellation behavior to remain intact so my workout and entered data are not lost.

**Why this priority**: A more accessible entry point must not change the established workout editing experience.

**Independent Test**: Enter order editing from a scrolled position, reorder exercises, finish editing, and separately cancel both unchanged and changed orders.

**Acceptance Scenarios**:

1. **Given** order editing is opened from a scrolled position, **When** the user reviews the exercises, **Then** the existing collapsed ordering view is shown and normal workout-entry controls are hidden.
2. **Given** the user changes the exercise sequence, **When** they select Done, **Then** the normal workout view returns with the new sequence and the original values attached to the same exercises.
3. **Given** the user has changed the sequence, **When** they select Cancel, **Then** the existing discard confirmation appears and the original sequence is restored only if they confirm.
4. **Given** the user has not changed the sequence, **When** they select Cancel, **Then** order editing ends without a discard confirmation.

### Edge Cases

- A workout with no exercises retains its existing empty state and does not offer a misleading order action.
- A workout with one exercise retains the existing single-exercise ordering behavior.
- Short workouts that do not require scrolling still show an operable order control.
- During loading or a blocking workout-load failure, the order control remains hidden. When a workout with exercises is loaded, non-blocking previous-performance or session-save failures do not hide the control in the normal view; it must not interfere with an active modal.
- On narrow screens and at supported zoom levels, the floating control remains visible and workout controls remain reachable by scrolling.
- Scrolling by touch, keyboard, pointer, or scrollbar does not prevent use of the control.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The current workout's order control MUST remain visible and operable throughout vertical scrolling in the normal workout-entry view when the existing order action is available.
- **FR-002**: The control MUST replace the visible "Edit order" text button with a recognizable icon representing a change of order.
- **FR-003**: The icon control MUST expose an accessible name identifying the edit-order action and a button role, and support pointer, touch, and keyboard activation.
- **FR-004**: Activating the control at any scroll position MUST open the existing order-editing view for the current workout without navigation away from it.
- **FR-005**: The existing collapsed ordering view, reorder interactions, Done action, Cancel action, and discard confirmation behavior MUST remain unchanged.
- **FR-006**: Entering or exiting order editing MUST preserve existing workout values with their corresponding exercises; Done MUST retain the edited order in the current workout.
- **FR-007**: The control MUST remain hidden during loading, blocking workout-load failure, empty workouts, and active order editing. In normal view it MUST remain available for workouts with one or more exercises, including after non-blocking previous-performance or session-save failures; modal interactions MUST retain existing behavior.
- **FR-008**: On mobile, the control MUST float above the workout without narrowing its content; workout entries and actions MUST remain reachable by scrolling. On desktop, the control MUST NOT cover workout controls. Navigation and modal content MUST remain above the control in the feature verification matrix defined in Assumptions.

### Security & Privacy Requirements

- **SR-001**: The change MUST NOT introduce new data collection or change existing access permissions for workouts or exercise data.
- **SR-002**: Changing the control's presentation MUST NOT alter the integrity of existing workout-entry data.

### User Experience Consistency Requirements

- **UX-001**: The icon and its presentation MUST fit existing application conventions; the floating mobile control MUST stand out clearly against the workout in both themes.
- **UX-002**: The control MUST retain a visible keyboard focus indication and a usable target for supported input methods.
- **UX-003**: Existing loading, empty, error, and ordering feedback MUST remain unchanged.

### Performance Requirements

- **PR-001**: For a workout with 50 exercises, across 20 evenly spaced scroll positions from top to bottom, the control MUST be visible and hit-testable at every position; at least 19 of 20 scroll-to-render samples MUST complete within 100 milliseconds under normal local conditions. A manual continuous-scroll check MUST show no visible flicker.
- **PR-002**: Opening and closing order editing for a workout with 50 exercises MUST visibly complete within 1 second in at least 19 of 20 attempts under normal conditions.

### Key Entities

- **Current workout**: The workout being recorded, containing an ordered list of exercises and associated entered values.
- **Exercise order**: The sequence of exercises in the current workout, editable through the existing ordering flow.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At the top, middle, and bottom of a scrollable 50-exercise workout, the control is visible and opens order editing without a return to the top in every tested position.
- **SC-002**: In all tested supported themes, screen sizes, and input methods, the icon control remains identifiable and operable, and workout controls remain reachable; mobile content retains its full width.
- **SC-003**: A screen reader announces a meaningful action name and button role, and keyboard users can focus and activate the control in every tested scroll position.
- **SC-004**: In all tested Done and Cancel scenarios, the resulting exercise sequence and associated entered values match the existing behavior.
- **SC-005**: At least 90% of participants in a usability check identify and open exercise ordering from a scrolled position without assistance within 10 seconds.
- **SC-006**: At least 19 of 20 attempts to enter and exit order editing with 50 exercises complete visibly within 1 second each under normal conditions.
- **SC-007**: In a 50-exercise workout, the control remains visible and hit-testable at all 20 sampled scroll positions, at least 19 of 20 scroll-to-render samples complete within 100 milliseconds, and a continuous-scroll check shows no visible flicker.

## Assumptions

- The feature applies to the current workout screen, not workout-template editing or past workout details.
- Existing exercise ordering, saving, cancellation, permissions, and empty-state behavior are dependencies and are not being redesigned.
- This feature's minimum verification matrix, not a claim about a repository-wide support policy, is 320×568, 375×667, 768×768, and 1024×768 browser viewports at 100% zoom, plus a 1280×960 browser window at 200% zoom (approximately 640×480 CSS pixels). Check both light and dark themes and the pointer, keyboard, touch and screen-reader interactions applicable to each device. Validate additional environments required by any established project support policy.
- The exact icon and presentation are design choices for planning, provided they satisfy the accessibility and usability requirements above.
