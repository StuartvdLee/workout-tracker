# Feature Specification: Sidebar History Icon Change

**Feature Branch**: `028-sidebar-history-icon`  
**Created**: 2026-05-27  
**Status**: Draft  
**Input**: User description: "I want to change the icon in the sidebar for History"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Updated History Icon in Sidebar (Priority: P1)

A user navigating the app sees a new, more recognisable icon next to the "History" sidebar link. The new icon better conveys the concept of viewing past workout sessions.

**Why this priority**: The icon is the primary visual cue for the History navigation item. It must communicate "past records" clearly at a glance.

**Independent Test**: Open the app, view the sidebar, and confirm the History link displays the new icon instead of the current clock icon.

**Acceptance Scenarios**:

1. **Given** the app is open, **When** the user views the sidebar, **Then** the History link shows the new icon (not the current clock icon).
2. **Given** the sidebar is visible on a small screen (collapsed), **When** the user opens the sidebar, **Then** the History icon is rendered correctly at the standard size.
3. **Given** dark mode is enabled, **When** the user views the sidebar, **Then** the History icon adapts to the active colour scheme like all other sidebar icons.

---

### Edge Cases

- What happens if the icon SVG paths are malformed? The icon must degrade gracefully (show nothing or a fallback) without breaking layout.
- How does the icon look at different zoom levels? It must remain recognisable at standard zoom and 150%+ accessibility zoom.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The History sidebar link MUST display a new SVG icon in place of the current clock icon (`circle` + `polyline` clock design).
- **FR-002**: The new icon MUST use the same dimensions (`width="20" height="20" viewBox="0 0 24 24"`) as all other sidebar icons.
- **FR-003**: The new icon MUST use the same stroke styling (`fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"`) as all other sidebar icons so it inherits the active/inactive colour states.
- **FR-004**: The new icon MUST include `aria-hidden="true"` to preserve existing accessibility behaviour.
- **FR-005**: The icon change MUST NOT alter the link text, href, or any other attribute of the History sidebar entry.
- **FR-006**: The replacement icon MUST be the Lucide "history" icon — a circular arrow (undo arc) with clock hands, conveying the concept of looking back in time. The icon uses three paths: the arc with arrow tip, the arrowhead corner, and the clock hands.

### User Experience Consistency Requirements

- **UX-001**: The new icon MUST visually align with the style of the other sidebar icons (Lucide-style, single-colour, stroke-based).
- **UX-002**: No loading, empty, or error states apply — this is a static visual change.
- **UX-003**: The icon label "History" remains unchanged; only the icon graphic changes.

### Performance Requirements

- **PR-001**: The change is a static SVG path replacement with no performance impact.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The History sidebar item displays the new icon on every page of the app.
- **SC-002**: The new icon is visually consistent with all other sidebar icons (same size, same stroke style, same colour behaviour).
- **SC-003**: No regression in sidebar layout, accessibility, or dark mode rendering is introduced by the change.
- **SC-004**: The change passes a visual check in both light and dark mode.

## Assumptions

- The replacement icon will be chosen from the same icon library style used elsewhere in the app (Lucide-compatible, stroke-based SVG).
- No icon change is required for the topbar or any other location — only the sidebar `<a data-page="history">` entry is in scope.
- The "History" label text is not part of this change.
