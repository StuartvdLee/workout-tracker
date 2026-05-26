# Feature Specification: Change Effort Slider Colours

**Feature Branch**: `024-change-effort-slider-colours`  
**Created**: 2026-05-26  
**Status**: Draft  
**Input**: User description: "I want to change the colours of the effort slider"

## Clarifications

### Session 2026-05-26

- Q: What exact colour mapping should effort levels 1-10 use? → A: 1 `#22C55E`, 2 `#4ADE80`, 3 `#84CC16`, 4 `#A3E635`, 5 `#EAB308`, 6 `#F59E0B`, 7 `#F97316`, 8 `#EA580C`, 9 `#EF4444`, 10 `#DC2626`.

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Updated Visual Feedback While Setting Effort (Priority: P1)

A user adjusts effort during an active workout and immediately sees the slider shown in the new colour system, helping them interpret intensity more clearly while choosing a value.

**Why this priority**: The core request is to change slider colours; this is the direct user value.

**Independent Test**: Set effort values across the full range and confirm each value displays its assigned new colour in the active workout flow.

**Acceptance Scenarios**:

1. **Given** a user is adjusting an effort slider, **When** they select any value in the effort range, **Then** the slider displays the new colour assigned to that value.
2. **Given** a slider currently shows an old colour mapping, **When** the updated feature is available, **Then** the slider uses the new colour mapping instead of the previous one.

---

### User Story 2 - Consistent Colours Across All Effort Sliders (Priority: P2)

A user sees the same effort colour mapping wherever effort sliders appear, so the meaning of colours stays predictable throughout the product.

**Why this priority**: Inconsistent colour mapping would confuse users and weaken the value of the change.

**Independent Test**: Visit each page/state where effort sliders are present and verify equal effort values render identical colours everywhere.

**Acceptance Scenarios**:

1. **Given** multiple effort sliders in the product, **When** each slider is set to the same value, **Then** they all show the same corresponding colour.
2. **Given** a user switches between different workout contexts, **When** they view effort sliders, **Then** colour meaning remains consistent.

---

### User Story 3 - Clear Fallback for Unset or Invalid Values (Priority: P3)

A user can still understand slider state when no effort value is selected yet or when an unexpected value appears, because the slider uses a defined neutral fallback style.

**Why this priority**: This protects usability in edge conditions and prevents confusing or broken visual states.

**Independent Test**: View sliders with unset values and simulated out-of-range values and confirm they use the defined fallback appearance.

**Acceptance Scenarios**:

1. **Given** an effort slider has no selected value, **When** it is displayed, **Then** it shows a neutral default style rather than a misleading effort colour.
2. **Given** an out-of-range effort value is encountered, **When** the slider renders, **Then** it shows the defined fallback style and remains usable.

---

### Edge Cases

- What happens when a slider loads with a previously saved effort value? It should immediately display the new mapped colour for that value.
- What happens when many sliders are visible at once in a long workout? Colour rendering should remain responsive and consistent for each slider.
- What happens if a user uses keyboard controls rather than drag/touch? The same colour mapping should apply for each changed value.
- What happens when the product is in loading or error states around workout data? Slider visuals should remain consistent with existing state handling patterns.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST apply this colour mapping to effort values 1-10: 1 `#22C55E`, 2 `#4ADE80`, 3 `#84CC16`, 4 `#A3E635`, 5 `#EAB308`, 6 `#F59E0B`, 7 `#F97316`, 8 `#EA580C`, 9 `#EF4444`, 10 `#DC2626`.
- **FR-002**: The system MUST display the mapped colour immediately when a user changes slider value.
- **FR-003**: The system MUST use the same colour mapping for every effort slider in scope of workout tracking flows.
- **FR-004**: The system MUST preserve all existing effort slider behavior except for the colour presentation change.
- **FR-005**: The system MUST define and apply a neutral fallback style for unset or invalid effort values.
- **FR-006**: The system MUST ensure the updated colour mapping appears for both newly entered and previously saved effort values.

### Security & Privacy Requirements

- **SR-001**: The feature MUST not introduce new collection, storage, or sharing of personal data.
- **SR-002**: Existing authorization boundaries for viewing and editing workouts MUST remain unchanged.
- **SR-003**: Any externally provided colour configuration inputs (if used) MUST be validated against allowed format and range before use.

### User Experience Consistency Requirements

- **UX-001**: The revised slider colours MUST align with the product's visual language and not reduce readability of the slider control.
- **UX-002**: Slider appearance in loading, empty, success, and error states MUST remain consistent with existing workout experience patterns.
- **UX-003**: The meaning of effort intensity represented by slider colour MUST remain consistent and understandable to users.

### Performance Requirements

- **PR-001**: Visual colour updates after slider interaction MUST appear immediate to users during normal use.
- **PR-002**: Rendering updated slider colours MUST not create noticeable slowdown when multiple sliders are present on screen.
- **PR-003**: The colour update behavior MUST remain stable during repeated slider interactions in a single session.

### Key Entities *(include if feature involves data)*

- **Effort Level**: Discrete intensity value used by the slider to represent workout effort.
- **Effort Colour Mapping**: Relationship between each effort level and its assigned display colour: 1 `#22C55E`, 2 `#4ADE80`, 3 `#84CC16`, 4 `#A3E635`, 5 `#EAB308`, 6 `#F59E0B`, 7 `#F97316`, 8 `#EA580C`, 9 `#EF4444`, 10 `#DC2626`.
- **Slider Visual State**: Current appearance of a slider, including mapped effort colour or fallback style.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of defined effort values display their new assigned colours in the active workout experience.
- **SC-002**: In at least 95% of slider interactions, the mapped colour is visible within 100 ms of the value change event.
- **SC-003**: 100% of in-scope effort slider instances use the same value-to-colour mapping.
- **SC-004**: At least 90% of user feedback in post-release validation indicates effort colours are clearer than before.
- **SC-005**: 100% of unset or invalid slider values display the defined neutral fallback style.

## Assumptions

- The effort slider already exists and supports a fixed discrete value range.
- The provided 10-value colour palette is final for this feature scope.
- This change is limited to effort sliders used in workout tracking flows and does not redefine broader theming rules.
