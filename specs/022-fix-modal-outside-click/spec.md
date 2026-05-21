# Feature Specification: Fix Effort Modal Outside-Click Behaviour

**Feature Branch**: `022-fix-modal-outside-click`  
**Created**: 2026-05-21  
**Status**: Delivered  

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Clicking Outside the Effort Modal Closes It Without Saving (Priority: P1)

After completing a workout the user taps "Save Workout", which opens the Overall Workout Effort modal. If the user changes their mind and clicks somewhere outside the modal (on the backdrop), the modal currently saves the session immediately with no effort rating. The user expects clicking outside to dismiss the modal — the same way the X button works — so they can remain on the active session page and decide what to do next.

**Why this priority**: This is a data-integrity bug. A user who accidentally taps outside the modal has their workout saved prematurely and without an effort rating, with no opportunity to correct it. It is the only scenario in the spec.

**Independent Test**: Start and save a workout. When the Overall Workout Effort modal appears, click anywhere on the semi-transparent backdrop (outside the modal card). Verify the modal closes and the session is **not** saved; the user remains on the active session page.

**Acceptance Scenarios**:

1. **Given** the Overall Workout Effort modal is open, **When** the user clicks on the backdrop area outside the modal card, **Then** the modal closes and the active session is not saved — the user remains on the active session page.
2. **Given** the Overall Workout Effort modal is open, **When** the user clicks the X button in the modal header, **Then** the modal closes and the active session is not saved (existing behaviour, unchanged).
3. **Given** the Overall Workout Effort modal is open and the user clicks the backdrop to close it, **When** they subsequently click "Save Workout" again, **Then** the effort modal reopens and the user can complete the flow normally.

---

### Edge Cases

- What happens when the user clicks the backdrop while a save request is already in flight? If the modal's Save button is disabled during a request, the backdrop click must also be suppressed so a concurrent save cannot be triggered.
- What happens on mobile where a tap outside can be ambiguous? The same touch event on the backdrop element (not the modal card) must close without saving.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When the user clicks the backdrop (outside the modal card) while the Overall Workout Effort modal is open, the modal MUST close without triggering a save of the active session.
- **FR-002**: The backdrop click MUST produce the same outcome as clicking the X close button: the modal closes and the user stays on the active session page.
- **FR-003**: The Save and Skip buttons inside the modal MUST continue to work as before — Save records the effort and saves the session; Skip saves the session with no effort rating.
- **FR-004**: If a save request is already in progress (e.g., triggered by the Save button), a backdrop click MUST be ignored to prevent a concurrent or duplicate save.

### Security & Privacy Requirements

- **SR-001**: No data is transmitted when the user dismisses the modal via backdrop click; the dismiss action is client-side only.

### User Experience Consistency Requirements

- **UX-001**: The backdrop-click behaviour MUST match the X button behaviour: modal closes, session not saved, user stays on active session page.
- **UX-002**: No loading or error states are introduced by this change; dismiss is instantaneous.
- **UX-003**: The terminology and visual state of the active session page after dismissal MUST remain unchanged.

### Performance Requirements

- **PR-001**: Dismissing the modal via backdrop click MUST be instantaneous (no network request is made, so there is no latency budget to define beyond the DOM event handling).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of backdrop-click interactions on the Overall Workout Effort modal result in the modal closing without saving the session.
- **SC-002**: 0 unintended session saves are triggered by clicking outside the effort modal.
- **SC-003**: Users who dismiss via backdrop click can subsequently re-open the effort modal by clicking "Save Workout" again and complete the save flow successfully.
- **SC-004**: The existing Save and Skip button behaviours are unaffected (verified by existing or updated test coverage).
