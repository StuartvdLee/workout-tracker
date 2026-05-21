# Feature Specification: Unsaved Changes Warning in Edit Modals

**Feature Branch**: `023-unsaved-changes-warning`  
**Created**: 2026-05-21  
**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Warning on Dismiss When Changes Exist (Priority: P1)

A user opens an edit modal (for a workout, exercise, or muscle), modifies one or more fields, and then attempts to close the modal by clicking Cancel, the × icon, or outside the modal. Instead of the modal closing immediately and discarding changes, the user sees a confirmation warning asking whether they want to discard their unsaved changes.

**Why this priority**: This is the core behaviour that prevents accidental data loss. Without it, all other stories have no context.

**Independent Test**: Open an edit modal, change any field, click Cancel — a warning must appear. If the user confirms, changes are discarded and the modal closes. This alone constitutes a complete, demonstrable feature.

**Acceptance Scenarios**:

1. **Given** the user has opened an edit modal and changed at least one field, **When** the user clicks Cancel, **Then** a warning dialog or inline prompt is displayed asking them to confirm discarding unsaved changes.
2. **Given** the warning is shown, **When** the user confirms they want to discard, **Then** the modal closes and all changes are lost.
3. **Given** the warning is shown, **When** the user chooses to stay / keep editing, **Then** the warning is dismissed and the edit modal remains open with all changes intact.
4. **Given** the user has opened an edit modal and changed at least one field, **When** the user clicks the × (close) icon, **Then** the same warning is displayed as in scenario 1.
5. **Given** the user has opened an edit modal and changed at least one field, **When** the user clicks outside the modal (on the backdrop), **Then** the same warning is displayed as in scenario 1.

---

### User Story 2 - No Warning When No Changes Have Been Made (Priority: P2)

A user opens an edit modal, makes no changes (or reverts all changes back to the original values), and then tries to close it. The modal closes immediately without any warning.

**Why this priority**: A false-positive warning on every close would be extremely annoying and degrade UX. The no-warning path is equally important to the warning path.

**Independent Test**: Open an edit modal without changing anything, click Cancel — the modal must close instantly with no warning.

**Acceptance Scenarios**:

1. **Given** the user has opened an edit modal and made no changes, **When** the user clicks Cancel, the × icon, or outside the modal, **Then** the modal closes immediately with no warning.
2. **Given** the user has opened an edit modal, changed a field, and then reverted it back to its original value, **When** the user attempts to close, **Then** the modal closes immediately with no warning.

---

### User Story 3 - Consistent Behaviour Across All Editable Entity Types (Priority: P3)

The warning behaviour is consistently applied across every edit modal in the application: editing a workout, editing an exercise, and editing a muscle.

**Why this priority**: Consistency prevents confusion. A user who learns the behaviour on the workout modal expects it everywhere.

**Independent Test**: Repeat the P1 test on each of the three entity edit modals (workout, exercise, muscle) and verify identical behaviour in each case.

**Acceptance Scenarios**:

1. **Given** the user is editing a workout, **When** they make a change and attempt to close, **Then** the warning appears as per Story 1.
2. **Given** the user is editing an exercise, **When** they make a change and attempt to close, **Then** the warning appears as per Story 1.
3. **Given** the user is editing a muscle, **When** they make a change and attempt to close, **Then** the warning appears as per Story 1.

---

### Edge Cases

- What happens when a user opens the edit modal but only interacts with a field without actually changing the value (e.g., clicks into a text field and clicks out again without typing)? No warning should appear.
- What happens if the user rapidly clicks outside the modal multiple times while the warning is visible? The warning must not stack or appear more than once.
- What happens when the network is slow and saving is in progress when the user clicks Cancel? The warning should still appear if there are unsaved field changes independent of any in-flight save.
- How does the warning behave on mobile/touch devices where "clicking outside" maps to a tap on the backdrop?
- What if the user presses the Escape key on the keyboard while editing? The same warning must apply as clicking Cancel or the × icon.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST detect whether any editable field in an open edit modal has a value that differs from the value it had when the modal was opened.
- **FR-002**: When the user attempts to close an edit modal (via Cancel, ×, backdrop click, or Escape key) and unsaved changes exist, the system MUST present a confirmation prompt before closing.
- **FR-003**: The confirmation prompt MUST offer at least two actions: one to discard changes and close the modal, and one to return to the edit modal.
- **FR-004**: When the user confirms discarding changes, the system MUST close the modal and restore all field values to the state they were in when the modal was opened.
- **FR-005**: When the user chooses to continue editing, the system MUST dismiss the warning and return focus to the edit modal with all in-progress changes intact.
- **FR-006**: When no changes have been made (or all changes have been reverted to original values), the system MUST close the modal immediately without showing the warning.
- **FR-007**: The unsaved-changes detection and warning MUST be applied consistently to all edit modals: workout edit, exercise edit, and muscle edit.
- **FR-008**: Pressing the Escape key while an edit modal has unsaved changes MUST trigger the same warning as other close actions.

### Security & Privacy Requirements

- **SR-001**: The system MUST validate and sanitize all user inputs in edit modals consistent with existing input handling. The warning feature itself does not introduce new data handling concerns.
- **SR-002**: The system MUST enforce existing authorization rules; the warning behaviour does not alter access control.

### User Experience Consistency Requirements

- **UX-001**: The warning prompt MUST reuse existing modal or dialog patterns already present in the application, or explicitly define the new pattern it introduces.
- **UX-002**: The warning prompt MUST have clearly labelled actions that communicate intent without ambiguity (e.g., "Discard changes" and "Keep editing").
- **UX-003**: Terminology used in the warning (e.g., "unsaved changes", "discard") MUST remain consistent across all entity types.
- **UX-004**: The feature MUST define all relevant states: no-changes (no prompt), changes-present (prompt shown), user-confirms-discard (modal closes), user-cancels-discard (returns to edit).

### Performance Requirements

- **PR-001**: Change detection MUST complete in negligible time (imperceptible to the user) as the comparison is against in-memory values captured at modal open time.
- **PR-002**: The warning prompt MUST appear immediately (no perceptible delay) after the close action is triggered.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of close actions (Cancel, ×, backdrop, Escape) on edit modals with unsaved changes trigger the warning prompt.
- **SC-002**: 100% of close actions on edit modals with no unsaved changes close the modal immediately without a warning.
- **SC-003**: The warning prompt appears within one animation frame of the close action — no perceptible delay.
- **SC-004**: The feature behaves identically across all three entity edit modals (workout, exercise, muscle).
- **SC-005**: A user who accidentally triggers the close action is able to return to editing with all in-progress changes intact 100% of the time.

## Assumptions

- Edit modals are rendered client-side and already have access to the current form field values; capturing "original" values at modal open time is straightforward.
- The application currently has a shared modal or dialog component that can be extended to support the unsaved-changes check, rather than requiring separate implementations per entity.
- "Outside click" (backdrop dismissal) is already a supported close mechanism for edit modals in the current application.
- The Escape key is a standard close mechanism for modals; extending it to trigger the warning is in scope.
