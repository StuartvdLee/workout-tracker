# Feature Specification: Sidebar Icons for Workouts and Exercises

**Feature Branch**: `027-sidebar-icons`  
**Created**: 2026-05-27  
**Status**: Draft  
**Input**: User description: "I want to change the icons in the sidebar for Workouts and Exercises"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Updated Sidebar Icons (Priority: P1)

A user navigates the application and sees refreshed, more recognisable icons next to the "Workouts" and "Exercises" sidebar items. The new icons better represent the content they link to, making navigation clearer and more intuitive.

**Why this priority**: The icons are the primary visual cue for navigation; updating them is the entire scope of this feature.

**Independent Test**: Open the application, look at the sidebar — the Workouts and Exercises entries display new icons that differ from the current dumbbell/barbell and list icons respectively.

**Acceptance Scenarios**:

1. **Given** I open the application, **When** I view the sidebar, **Then** the Workouts item displays a new icon that is distinct from the current barbell-style icon.
2. **Given** I open the application, **When** I view the sidebar, **Then** the Exercises item displays a new icon that is distinct from the current bulleted-list icon.
3. **Given** the sidebar is visible, **When** I inspect the icons, **Then** all other sidebar items (Let's go!, Muscles, History) retain their existing icons unchanged.
4. **Given** the application is in dark mode, **When** I view the sidebar, **Then** the new icons render correctly using the current colour token (no hardcoded colours).
5. **Given** the application is in light mode, **When** I view the sidebar, **Then** the new icons render correctly using the current colour token.

---

### Edge Cases

- What happens when the icon SVG path is malformed? The icon should fall back gracefully; accessibility attributes (`aria-hidden="true"`) must be preserved.
- How does the experience stay consistent across loading, empty, success, and failure states? Icons are static assets and are unaffected by loading state.
- Are the icons still visible and appropriately sized on small screens (mobile sidebar)? The 20×20 viewBox and `sidebar__icon` class must be preserved to ensure consistent sizing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The sidebar item labelled "Workouts" MUST display the Lucide `dumbbell` icon (https://lucide.dev/icons/dumbbell), replacing the current barbell-style icon.
- **FR-002**: The sidebar item labelled "Exercises" MUST display the Lucide `sport-shoe` icon (https://lucide.dev/icons/sport-shoe), replacing the current bulleted-list icon.
- **FR-003**: All other sidebar icons (Let's go!, Muscles, History) MUST remain unchanged.
- **FR-004**: The new icons MUST maintain the existing `sidebar__icon` CSS class, `width="20"`, `height="20"`, `viewBox="0 0 24 24"` attributes, and `aria-hidden="true"` to preserve layout and accessibility.
- **FR-005**: The new icons MUST use `stroke="currentColor"` so they inherit the correct colour in both light and dark themes without hardcoding colour values.

### Security & Privacy Requirements

- **SR-001**: No user data or inputs are involved; this change is purely presentational with no security implications.

### User Experience Consistency Requirements

- **UX-001**: New icons MUST follow the same inline SVG pattern used by all other sidebar icons (stroke-based, 24×24 viewBox, currentColor).
- **UX-002**: Icon visual style (stroke-width, stroke-linecap, stroke-linejoin) MUST match the existing sidebar icons to maintain a cohesive look.
- **UX-003**: The labels "Workouts" and "Exercises" remain unchanged; only the SVG paths are updated.

### Performance Requirements

- **PR-001**: Icon changes are inline SVG and introduce no additional network requests or measurable performance impact.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The Workouts sidebar icon displays the Lucide `dumbbell` icon, visually distinct from the previous barbell-style icon and clearly representing fitness/workouts.
- **SC-002**: The Exercises sidebar icon displays the Lucide `sport-shoe` icon, visually distinct from the previous bulleted-list icon and clearly representing exercises/movement.
- **SC-003**: All five sidebar navigation items render their icons correctly in both light and dark themes with zero visual regressions.
- **SC-004**: The sidebar layout is pixel-consistent with the previous layout — no alignment, sizing, or spacing changes.
- **SC-005**: 100% of existing sidebar accessibility attributes are preserved after the change.

## Assumptions

- SVG paths are inlined directly from the Lucide library (no new library dependency is introduced).
- The change affects only `src/WorkoutTracker.Web/wwwroot/index.html` (the single HTML shell that contains the sidebar).

## Clarifications

### Session 2026-05-27

- Q: Which icon should be used for the Workouts sidebar item? → A: Lucide `dumbbell` (https://lucide.dev/icons/dumbbell)
- Q: Which icon should be used for the Exercises sidebar item? → A: Lucide `sport-shoe` (https://lucide.dev/icons/sport-shoe)
