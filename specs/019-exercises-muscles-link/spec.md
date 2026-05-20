# Feature Specification: Exercises Muscles Link

**Feature Branch**: `019-exercises-muscles-link`  
**Created**: 2026-05-20  
**Status**: Delivered  
**Input**: User description: "I want to add a link to the Muscles page on the Exercises page. It should be placed after the 'Targeted muscles (optional)' text."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navigate to Muscles Page from Exercises (Priority: P1)

A user is on the Exercises page, filling in the form to add or edit an exercise. They see the "Targeted muscles (optional)" label followed by a small inline link. Clicking the link takes them directly to the Muscles page where they can add, edit, or delete muscles. They can then navigate back to the Exercises page to continue creating their exercise.

**Why this priority**: This is the sole purpose of the feature. Without the link, users who want to add a new muscle while building an exercise must manually discover the Muscles page in the sidebar — the link removes that friction.

**Independent Test**: Can be fully tested by visiting the Exercises page, locating the link after the "Targeted muscles (optional)" label, clicking it, and verifying it navigates to the Muscles page.

**Acceptance Scenarios**:

1. **Given** the user is on the Exercises page (Add Exercise form), **When** they look at the "Targeted muscles (optional)" label, **Then** they see a link immediately after it.
2. **Given** the user is on the Exercises page (Edit Exercise modal), **When** they look at the "Targeted muscles (optional)" label, **Then** they see the same link immediately after it.
3. **Given** the user clicks the link, **When** the navigation occurs, **Then** they are taken to the Muscles page.
4. **Given** the user is on a mobile-sized screen, **When** they view the Exercises page, **Then** the link is still visible and usable next to the label.

---

### Edge Cases

- What happens if the user is mid-way through filling in the exercise form and clicks the link? They lose their unsaved form data — this is acceptable given the link is a standard anchor navigation; no special handling required.
- What happens on slow networks? The link is static HTML — it renders immediately regardless of API state.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Exercises page MUST display a link after the "Targeted muscles (optional)" label in both the Add Exercise form and the Edit Exercise modal.
- **FR-002**: Clicking the link MUST navigate the user to the Muscles page (`/muscles`).
- **FR-003**: The link text MUST read **"Manage"**.
- **FR-004**: The link MUST appear in both the Add form and the Edit modal to ensure a consistent experience.

### Security & Privacy Requirements

- **SR-001**: The link is a standard internal navigation link — no additional input validation or authorization logic is required beyond what already governs the Muscles page.

### User Experience Consistency Requirements

- **UX-001**: The link MUST use existing link or secondary-action styling consistent with the rest of the form UI — it should not look like a primary button.
- **UX-002**: The link MUST be visually associated with the "Targeted muscles (optional)" label, appearing inline or immediately adjacent.
- **UX-003**: Terminology used in the link text MUST be consistent with the label "muscles" used elsewhere in the product.

### Performance Requirements

- **PR-001**: The link is static HTML and introduces no performance overhead.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The link is present and functional on the Exercises page within both the Add form and the Edit modal.
- **SC-002**: 100% of form contexts (Add and Edit) display the link in the correct position.
- **SC-003**: Clicking the link successfully navigates to the Muscles page in all tested browsers.

## Assumptions

- **Link text**: Confirmed as **"Manage"** — short, action-oriented, and consistent with existing terminology.
- **Navigation behaviour**: The link uses standard page navigation (not opening in a new tab), consistent with how the sidebar navigates between pages.
- **Styling**: The link should use existing anchor/inline-link styling from the stylesheet, not a button style.
- **No "back" logic**: No automatic return-to-exercise navigation is added; users use the sidebar or browser back button.
