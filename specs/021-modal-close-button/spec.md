# Feature Specification: Modal Close Button

**Feature Branch**: `021-modal-close-button`  
**Created**: 2026-05-21  
**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Close Muscle Edit Modal Without Saving (Priority: P1)

A user opens the edit modal for a muscle group (e.g., to view its current name), then decides not to make any changes. Currently the only available actions are Save and Delete — there is no way to dismiss the modal without performing one of those destructive-or-save actions. They need a clear way to close the modal and return to the muscles list unchanged.

**Why this priority**: The Edit Muscle modal is the only modal in the application with no dismiss-without-action path. Without a close button, the user is trapped: they must either save changes they didn't intend to make or press Escape (which may not be discoverable). This is the most urgent usability gap.

**Independent Test**: Open the Muscles page, click Edit on any muscle, then click the X button in the top-right corner of the modal. The modal closes and no changes are saved.

**Acceptance Scenarios**:

1. **Given** a user has opened the Edit Muscle modal, **When** they click the X button in the top-right corner, **Then** the modal closes and the muscle data remains unchanged.
2. **Given** a user has typed new text into the name field of the Edit Muscle modal, **When** they click the X button, **Then** the modal closes, the muscle retains its original name, and no save request is sent.
3. **Given** the Edit Muscle modal is open, **When** the user presses Escape, **Then** the modal closes (same behaviour as clicking X).

---

### User Story 2 - Close Edit Exercise/Workout Modals via X Button (Priority: P2)

The Exercise and Workout edit modals already have a Cancel button, but the X button in the top-right corner provides a consistent and immediately recognisable dismiss affordance across all modals. Users who are accustomed to dismissing windows or dialogs with an X should find the same option here.

**Why this priority**: Edit Exercise and Edit Workout modals already have Cancel, so users can exit them. Adding X here is a consistency improvement, not a critical fix.

**Independent Test**: Open the Edit Exercise or Edit Workout modal, click the X in the top-right corner. The modal closes without saving. Identical outcome to clicking Cancel.

**Acceptance Scenarios**:

1. **Given** the Edit Exercise modal is open, **When** the user clicks the X button, **Then** the modal closes without saving any changes (same outcome as Cancel).
2. **Given** the Edit Workout modal is open, **When** the user clicks the X button, **Then** the modal closes without saving any changes (same outcome as Cancel).

---

### Edge Cases

- What happens if the user accidentally clicks the backdrop behind the modal? The X button should not change this existing behaviour — backdrop click behaviour stays as-is.
- What happens when a save or delete operation is in progress (submit button is loading/disabled)? The X button should also be disabled while an async operation is in progress, to prevent closing mid-request.
- What happens with keyboard navigation? The X button must be reachable via Tab and activatable via Enter/Space to maintain accessibility.
- How does the X button look in dark mode? The button must respect the existing dark-mode colour tokens.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Edit Muscle modal MUST include an X close button in the top-right corner of the modal header.
- **FR-002**: Clicking the X button MUST close the modal without saving any changes (equivalent to Cancel/dismiss).
- **FR-003**: The X button MUST be disabled while a save or delete network request is in progress.
- **FR-004**: The X button on the Edit Exercise modal MUST close that modal without saving changes (equivalent to the existing Cancel button).
- **FR-005**: The X button on the Edit Workout modal MUST close that modal without saving changes (equivalent to the existing Cancel button).
- **FR-006**: The X button MUST be keyboard-accessible (focusable via Tab, activatable via Enter and Space).
- **FR-007**: The X button MUST have an accessible label (e.g., `aria-label="Close"`) so screen-reader users understand its purpose.
- **FR-008**: The X button MUST be visually placed in the top-right corner of the modal, inside the modal boundary.

### Security & Privacy Requirements

- **SR-001**: No user data is transmitted when the X button is clicked; the close action is client-side only.
- **SR-002**: Disabling the X button during in-flight requests prevents partial or duplicate mutations.

### User Experience Consistency Requirements

- **UX-001**: The X button MUST use the same visual and interaction style across all affected modals (Edit Muscle, Edit Exercise, Edit Workout).
- **UX-002**: The X button appearance MUST be consistent with the existing modal design language (colours, border-radius, hover/focus states).
- **UX-003**: The X button MUST be visible in both light and dark mode.
- **UX-004**: The modals that already have a Cancel button (Edit Exercise, Edit Workout) retain their Cancel button; the X button is additive.

### Performance Requirements

- **PR-001**: The X button adds no network requests or computationally expensive operations; performance impact is negligible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every modal that previously had no dismiss-without-action path (Edit Muscle) now has at least one clear, labelled close affordance.
- **SC-002**: 100% of affected modals (Edit Muscle, Edit Exercise, Edit Workout) display an X button in the top-right corner.
- **SC-003**: Clicking the X button on any affected modal closes it without triggering a save or delete operation in all tested scenarios.
- **SC-004**: The X button is reachable via keyboard Tab order and activatable in all affected modals.
- **SC-005**: The X button is visually distinguishable and labelled for screen readers in all affected modals.

## Assumptions

- The fix targets edit-style modals (Edit Muscle, Edit Exercise, Edit Workout). Confirmation/alert modals (Delete, Discard) and choice modals (Pre-start, Effort) already provide adequate dismiss paths via their action buttons and are out of scope for this feature.
- "Close without saving" means no API call is made and the displayed data reverts to its prior state.
- The Escape key already dismisses some modals; this feature does not change that existing behaviour but the X button should be consistent with it.
