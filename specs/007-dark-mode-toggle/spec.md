# Feature Specification: Dark Mode Toggle

**Feature Branch**: `007-dark-mode-toggle`  
**Created**: 2026-05-05  
**Status**: Delivered  
**Input**: User description: "I want to add a dark mode to the app. When in light mode, a little moon icon should be displayed in the top right corner, indicating to the user that dark mode is available. When switching to dark mode, the moon icon should turn into a little sun, indicating to the user that light mode is available. All text, fields and buttons should be clearly visible in both light and dark mode"

## Clarifications

### Session 2026-05-05

- Q: What icon/visual represents the active state in the header when "System" mode is selected? → A: Show a monitor icon to represent the System preference directly, regardless of the resolved OS theme.
- Q: Does "System" mode track OS preference changes in real time during an active session? → A: Yes — the app updates immediately when the OS theme changes while the tab is open.
- Q: What should the default theme be for a first-time visitor with no saved preference? → A: "System" — the app inherits the OS preference on first visit, falling back to light mode if the OS preference cannot be detected.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Select a Theme Using the Theme Selector Menu (Priority: P1)

A user clicks the theme icon in the top right corner of the app, which opens a small menu with three options: **Light**, **Dark**, and **System**. Selecting an option immediately applies the chosen theme across the entire app. The icon in the header updates to reflect the active preference: sun for Light, moon for Dark, and monitor for System.

**Why this priority**: This is the core deliverable of the feature. The menu is the primary interaction surface for all theme choices.

**Independent Test**: Can be fully tested by opening the app, clicking the theme icon, selecting each of the three menu options in turn, and verifying that the theme and icon update correctly for each selection.

**Acceptance Scenarios**:

1. **Given** any state, **When** the user clicks the theme icon, **Then** a menu with three options (Light, Dark, System) appears.
2. **Given** the menu is open, **When** the user selects "Light", **Then** the app applies the light colour scheme, the menu closes, and the header shows a sun icon.
3. **Given** the menu is open, **When** the user selects "Dark", **Then** the app applies the dark colour scheme, the menu closes, and the header shows a moon icon.
4. **Given** the menu is open, **When** the user selects "System", **Then** the app applies the colour scheme matching the OS preference, the menu closes, and the header shows a monitor icon.
5. **Given** either theme is active, **When** the theme is applied, **Then** all text, input fields, and buttons are clearly visible with sufficient contrast.

---

### User Story 2 - Preference is Remembered Across Sessions (Priority: P2)

A user selects "Dark" from the theme menu. When they close the browser tab and return to the app later, dark mode is still active — they do not need to open the menu again.

**Why this priority**: Without persistence, the selector is inconvenient and feels unfinished. Persisting preference dramatically improves the user experience at low implementation cost.

**Independent Test**: Can be fully tested by selecting a theme option, reloading or reopening the app, and confirming the previously selected option is still active.

**Acceptance Scenarios**:

1. **Given** the user has selected "Dark", **When** they reload the page, **Then** the app opens in dark mode.
2. **Given** the user has selected "Light", **When** they reload the page, **Then** the app opens in light mode.
3. **Given** the user has selected "System", **When** they reload the page, **Then** the app opens with the OS-inferred theme.
4. **Given** a first-time visitor with no saved preference, **When** the app loads, **Then** the app defaults to the "System" option.

---

### User Story 3 - Use System Theme Automatically (Priority: P3)

A user selects "System" from the theme menu. The app mirrors the OS-level light/dark preference. If the user later changes their OS theme, the app updates accordingly.

**Why this priority**: Provides a "set and forget" option for users who want the app to match their OS at all times. Adds polish but is not blocking.

**Independent Test**: Can be fully tested by selecting "System", changing the OS preference, and confirming the app updates to match.

**Acceptance Scenarios**:

1. **Given** the user has selected "System" and the OS is in dark mode, **When** the app loads, **Then** the dark colour scheme is applied and the header shows a monitor icon.
2. **Given** the user has selected "System" and the OS is in light mode, **When** the app loads, **Then** the light colour scheme is applied and the header shows a monitor icon.
3. **Given** "System" is the active selection, **When** the OS preference changes during an active session, **Then** the app immediately updates its theme to match the new OS preference without a page reload.

---

### Edge Cases

- What happens when a stored preference value is invalid or corrupted? The app falls back to "System" (the default), which itself falls back to light mode if the OS preference cannot be detected.
- What happens if the theme icon fails to render (e.g., icon asset missing)? The menu trigger must still be functional and accessible; a visible fallback label or button must appear.
- How does the app behave when switching themes during an in-progress form interaction? Theme switches instantly without disrupting the form state or losing entered data.
- Are all states (loading spinner, empty states, error messages) themed correctly in both light and dark themes?
- What if the OS/system preference cannot be detected (e.g., unsupported browser)? The app falls back to light mode when "System" is selected and detection fails.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST display a theme selector icon in the top right corner of every page.
- **FR-002**: Clicking the theme selector icon MUST open a menu with exactly three options: **Light**, **Dark**, and **System**.
- **FR-003**: Selecting "Light" MUST immediately apply the light colour scheme across the entire app and display a sun icon in the header.
- **FR-004**: Selecting "Dark" MUST immediately apply the dark colour scheme across the entire app and display a moon icon in the header.
- **FR-005**: Selecting "System" MUST apply the colour scheme matching the user's current OS-level preference.
- **FR-006**: When "System" is the active selection, the app MUST update its theme immediately if the OS preference changes during an active session, without requiring a page reload.
- **FR-007**: When "System" is active and the OS preference cannot be detected, the app MUST fall back to light mode.
- **FR-008**: All text, input fields, and buttons MUST meet accessible contrast requirements in both light and dark themes.
- **FR-009**: The selected theme option MUST be persisted locally so that it is restored when the user returns to the app.
- **FR-010**: When no saved preference exists, the app MUST default to the "System" option.
- **FR-011**: The theme selector icon MUST be visible and accessible on every page of the app.
- **FR-012**: When "System" is the active selection, the header MUST display a monitor icon.
- **FR-013**: The currently active theme option MUST be visually indicated inside the menu (e.g., a checkmark or highlighted row).

### Security & Privacy Requirements

- **SR-001**: The user's theme preference is stored locally on the user's device and is not transmitted to any server or third party.
- **SR-002**: No user-identifiable data is collected or stored as part of this feature.

### User Experience Consistency Requirements

- **UX-001**: The theme selector icon MUST follow the existing visual style and sizing conventions used elsewhere in the app for action icons.
- **UX-002**: The theme transition MUST be immediate — no full page reload should occur when selecting a theme option.
- **UX-003**: The theme selector menu MUST be accessible via keyboard navigation and screen readers. The trigger button MUST carry an appropriate accessible label reflecting the active selection (e.g., "Theme: Light", "Theme: System"). Each menu option MUST be individually focusable and activatable via keyboard.
- **UX-004**: All existing pages and components (forms, buttons, navigation, modals, lists) MUST be visually correct in both light and dark themes — no unstyled or illegible elements.
- **UX-005**: The menu MUST close when the user selects an option, clicks outside the menu, or presses Escape.

### Performance Requirements

- **PR-001**: Theme switching MUST be instantaneous — users must perceive no delay between selecting an option and the full visual change applying.
- **PR-002**: Loading and applying the saved preference on startup MUST not introduce any visible flash of the wrong theme (no "flash of unstyled content" for the wrong theme).
- **PR-003**: The feature introduces no server-side operations, so no backend performance targets are applicable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can change the active theme in two interactions (open menu + select option), with no perceived delay in the theme applying.
- **SC-002**: The correct preference icon (sun for Light, moon for Dark, monitor for System) is displayed on 100% of app pages.
- **SC-003**: All text, fields, and buttons pass accessibility contrast requirements (WCAG AA minimum) in both light and dark themes.
- **SC-004**: The user's theme selection is correctly restored on 100% of return visits where a preference was previously saved.
- **SC-005**: No visible "flash of wrong theme" occurs when the app loads with a saved or system-inferred preference.
- **SC-006**: The theme selector menu is fully operable via keyboard and is correctly announced by screen readers.
- **SC-007**: When "System" is active, any OS theme change is reflected in the app within one second.

## Assumptions

- The app currently uses a single, consistent visual theme (light mode) across all pages. No partial dark styling already exists.
- "Top right corner" refers to the application's main header/navigation bar, visible on all pages.
- Local browser storage is an acceptable mechanism for persisting the user's theme selection without requiring authentication.
- WCAG AA contrast ratio (minimum 4.5:1 for normal text) is the target for accessibility compliance.
- The default theme for first-time visitors is "System" (resolves to OS preference, falls back to light if detection fails).
