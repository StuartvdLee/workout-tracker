# Feature Specification: Add Targeted Muscles In-App

**Feature Branch**: `015-manage-targeted-muscles`  
**Created**: 2026-05-18  
**Status**: Implemented  
**Input**: User description: "I want to be able to add targeted muscles in the app itself. When a targeted muscle is added, it should immediately be displayed without a manual refresh. The order of targeted muscles should stay alphabetised"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add a New Muscle (Priority: P1)

A user is on the exercises page, setting up a new exercise or editing an existing one. They notice the muscle they want to target (e.g. "Hip Flexors") is not in the existing list of toggleable muscles. They can enter a name and add that muscle directly from within the app, and it immediately appears as a selectable toggle in the muscle list — sorted alphabetically alongside the existing muscles — without any page reload.

**Why this priority**: This is the core value of the feature. Without it, users are blocked from associating exercises with muscles not already in the pre-seeded list.

**Independent Test**: Can be fully tested by opening the exercise form, typing a new muscle name in the add-muscle input, submitting it, and verifying the new toggle appears in correct alphabetical position without a page reload.

**Acceptance Scenarios**:

1. **Given** the exercises page is open and the user is creating or editing an exercise, **When** the user enters a muscle name that does not already exist and confirms the addition, **Then** the new muscle toggle appears in the targeted muscles list, sorted alphabetically, without a page refresh.
2. **Given** a newly added muscle appears in the targeted muscles list, **When** the user selects it as a target for the exercise and saves, **Then** the exercise is saved with that muscle correctly associated.
3. **Given** the exercises page is freshly loaded, **When** muscles are fetched, **Then** any previously added custom muscles appear in the list alongside the pre-seeded ones, in alphabetical order.

---

### User Story 2 - Prevent Duplicate Muscles (Priority: P2)

A user attempts to add a muscle name that already exists in the list. The system prevents the duplicate and informs the user with a clear message, rather than silently creating a duplicate entry or failing without explanation.

**Why this priority**: Without duplicate prevention, the targeted muscles list would become cluttered and unreliable. This is a data integrity concern closely tied to the core feature.

**Independent Test**: Can be fully tested by attempting to add a muscle whose name matches an existing one (exact or case-insensitive), verifying no duplicate is created and an appropriate message is shown.

**Acceptance Scenarios**:

1. **Given** "Biceps" already exists in the muscle list, **When** the user attempts to add a muscle named "Biceps" (or "biceps"), **Then** no duplicate is created and the user sees an error message indicating the muscle already exists.
2. **Given** a duplicate attempt is rejected, **When** the user corrects the name to something unique and submits, **Then** the new muscle is added successfully.

---

### Edge Cases

- What happens when the user submits an empty or whitespace-only muscle name? The system should reject it with a clear validation message and not create an entry.
- What happens when the muscle name is extremely long? The system should enforce a reasonable maximum length and inform the user.
- What happens when the network is slow or the save request fails? The user should see a clear error message; the muscle list should not show the muscle until it is confirmed saved.
- What does the add-muscle interaction look like in both the "create exercise" and "edit exercise" flows? Both must offer the same capability.
- What is shown when the targeted muscles list is empty (no muscles seeded)? The add-muscle control must still be accessible and functional.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to add a new muscle by entering a name directly on the exercises page, without navigating away.
- **FR-002**: When a new muscle is successfully added, it MUST appear immediately in the targeted muscles list without requiring a page refresh.
- **FR-003**: The targeted muscles list MUST remain sorted alphabetically at all times, including after a new muscle is inserted.
- **FR-004**: The system MUST prevent muscles with duplicate names from being created (comparison MUST be case-insensitive).
- **FR-005**: The system MUST reject attempts to add a muscle with an empty or whitespace-only name.
- **FR-006**: Newly added muscles MUST be persisted and available across sessions and page reloads.
- **FR-007**: The add-muscle capability MUST be available in both the create-exercise and edit-exercise flows.
- **FR-008**: Newly added muscles MUST be immediately selectable as a target for the current exercise being created or edited.

### Security & Privacy Requirements

- **SR-001**: The system MUST validate and sanitize muscle name input to prevent injection or malformed data.
- **SR-002**: Muscle creation MUST be subject to the same authorization rules as other data-write operations in the app.

### User Experience Consistency Requirements

- **UX-001**: The add-muscle interaction MUST reuse or visually align with existing input and action patterns on the exercises page.
- **UX-002**: The feature MUST define clear loading, success, and error states: while saving, after a successful save, and if the save fails.
- **UX-003**: Terminology (e.g. "Targeted muscles", muscle toggle labels) MUST remain consistent with the existing exercises page copy.

### Performance Requirements

- **PR-001**: A new muscle MUST appear in the list within 1 second of a successful save under normal network conditions.
- **PR-002**: Adding a muscle is a low-frequency, low-volume operation; no special scaling concerns are anticipated.
- **PR-003**: Performance will be verified manually and via integration tests confirming the add-and-display round-trip completes promptly.

### Key Entities

- **Muscle**: A named body area or muscle group. Has a unique name (case-insensitive). Can be associated with one or more exercises as a targeted muscle. Has no user-specific ownership — all muscles are shared across the app.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can add a new muscle and see it in the sorted list within 1 second, without a page reload.
- **SC-002**: 100% of add-muscle attempts with a duplicate name (case-insensitive) are rejected with a visible error message.
- **SC-003**: 100% of add-muscle attempts with an empty or whitespace-only name are rejected with a visible validation message.
- **SC-004**: Newly added muscles persist across page reloads and appear in alphabetical order alongside existing muscles.
- **SC-005**: The add-muscle control is accessible and functional in both the create-exercise and edit-exercise flows.
- **SC-006**: All affected states (loading, success, error) are handled consistently with the existing exercises page patterns.

## Assumptions

- Muscles are shared across all users of the app (there is no per-user muscle list).
- There is no need to edit or delete a muscle name once created; that is out of scope for this feature.
- The maximum muscle name length is assumed to be 100 characters unless the user specifies otherwise.
- Case-insensitive duplicate checking is the correct default (e.g., "biceps" and "Biceps" are treated as the same muscle).
