# Feature Specification: Effort Slider Colour Feedback

**Feature Branch**: `020-effort-slider-colours`  
**Created**: 2026-05-20  
**Status**: Implemented  
**Input**: User description: "I want to add colours to the effort sliders. Ideally, the slider should change colour while sliding. Or it should immediately become the colour when letting go of the slider on a value."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-Time Colour Feedback While Sliding (Priority: P1)

A user is logging a workout set and adjusting the effort slider. As they drag the slider thumb from value to value, the slider's colour changes immediately to reflect the current effort level — green for easy, blue/purple for moderate, dark purple for hard, and deep purple for all-out. This allows the user to intuitively gauge their effort level visually before committing to a value.

**Why this priority**: This is the primary feature request and delivers the most immediate visual feedback value. The effort scale is a key data point in the active workout experience, and colour coding makes the scale self-explanatory.

**Independent Test**: Can be fully tested by opening an active workout session, grabbing an effort slider, and dragging it across the range — confirming that the slider's colour updates at each step along the way.

**Acceptance Scenarios**:

1. **Given** a user is on an active workout session, **When** they drag an effort slider from value 1 to 10, **Then** the slider colour transitions through the defined colour palette step by step as they drag.
2. **Given** the slider is at value 1 (Easy), **When** the user views it, **Then** the slider displays the colour `#267252`.
3. **Given** the slider is at value 5 (Moderate), **When** the user views it, **Then** the slider displays the colour `#2E3C80`.
4. **Given** the slider is at value 10 (All Out), **When** the user views it, **Then** the slider displays the colour `#8A3666`.
5. **Given** the slider is being dragged, **When** the thumb passes over each integer value, **Then** the colour updates to the colour defined for that value without delay.

---

### User Story 2 - Colour Applied on Release (Priority: P2)

A user adjusts the effort slider and releases the thumb at a chosen value. The slider immediately reflects the colour for that effort level. This ensures that even if real-time colour updating during drag is not available (e.g., on touch devices with limited feedback), the correct colour is always shown after the interaction completes.

**Why this priority**: This is the minimum viable behaviour — colour must at least be correct after a value is committed. It ensures correctness even in environments where continuous drag feedback may be limited.

**Independent Test**: Can be fully tested by tapping or clicking an effort slider to set a specific value and confirming the slider displays the correct colour for that value upon release.

**Acceptance Scenarios**:

1. **Given** a user releases the effort slider at value 3 (Easy), **When** the interaction ends, **Then** the slider displays the colour `#0E6577`.
2. **Given** a user releases the effort slider at value 7 (Hard), **When** the interaction ends, **Then** the slider displays the colour `#68448C`.
3. **Given** the effort slider is set to any value between 1 and 10, **When** the page loads or the value is restored, **Then** the slider displays the correct colour for that value.

---

### User Story 3 - Colour Persists on Re-Render (Priority: P3)

A user is in an active workout session and has already rated effort for one or more exercises. If the exercise list is re-rendered mid-session (e.g., after reordering exercises), the effort sliders rebuild with the previously entered values intact and their colours immediately visible — no re-interaction required.

**Why this priority**: The exercise list can be re-rendered during a session (e.g., drag-to-reorder). Without colour restoration, re-rendered sliders would silently lose their colour state even though the effort value itself is preserved.

**Independent Test**: Can be tested by setting an effort value on a per-exercise slider, triggering a re-render of the exercise list (e.g., via exercise reorder), and confirming the rebuilt slider renders with the correct palette colour for the previously entered value without requiring the user to touch it again.

**Acceptance Scenarios**:

1. **Given** an effort value of 9 (All Out) was entered earlier in the session, **When** the exercise list is re-rendered, **Then** the effort slider shows the colour `#8A417D` immediately on render.
2. **Given** an effort value of 4 (Moderate) was entered earlier in the session, **When** the exercise list re-renders, **Then** the effort slider shows the colour `#356089` without requiring any interaction.

---

### Edge Cases

- What happens when the slider has no value set (e.g., default/neutral state)? The slider should display a neutral/default colour (e.g., the app's standard slider colour) until a value is chosen.
- What happens when the slider renders on a slow device? The colour must still be correct upon render completion — no flash of incorrect colour.
- How does the colour update stay consistent across drag (pointer/touch), keyboard navigation (arrow keys), and direct value entry (if supported)?
- What happens if a slider value is outside the 1–10 range due to a data issue? The slider must gracefully fall back to the nearest defined colour or the default colour.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each effort slider MUST display a colour corresponding to its current integer value (1–10) using the following defined colour palette:
  - 1 (Easy): `#267252`
  - 2 (Easy): `#127368`
  - 3 (Easy): `#0E6577`
  - 4 (Moderate): `#356089`
  - 5 (Moderate): `#2E3C80`
  - 6 (Moderate): `#4C3D8A`
  - 7 (Hard): `#68448C`
  - 8 (Hard): `#71398B`
  - 9 (All Out): `#8A417D`
  - 10 (All Out): `#8A3666`
- **FR-002**: The slider colour MUST update in real-time as the user drags the slider thumb across the range.
- **FR-003**: The slider colour MUST update immediately when the user releases the slider at a value (if real-time update is not triggered during drag).
- **FR-004**: The slider colour MUST be correct when the exercise list is re-rendered mid-session with previously entered effort values (e.g., after exercise reorder).
- **FR-005**: All effort sliders in the application that use the 1–10 effort scale MUST use this colour system consistently.
- **FR-006**: The slider colour change MUST apply to the slider's filled track and/or thumb element — whichever visual element represents the current value — so the colour is clearly associated with the chosen value.

### Security & Privacy Requirements

- **SR-001**: No user data is transmitted as part of the colour-feedback feature; this is a purely visual enhancement with no new data inputs or outputs.
- **SR-002**: Colour values are statically defined and not user-configurable, eliminating injection risk.

### User Experience Consistency Requirements

- **UX-001**: The colour system MUST be used consistently across all effort sliders used for logging per-set effort during active workouts. History views and other read-only displays of effort values are out of scope for this feature.
- **UX-002**: The slider MUST display a visually distinct colour at every one of the 10 defined values — no two adjacent values may render identically.
- **UX-003**: The colour change behaviour (real-time or on-release) MUST be identical across pointer (mouse/stylus) and touch interactions.
- **UX-004**: When no effort value has been set, the slider MUST display a neutral appearance consistent with the app's existing default slider style.

### Performance Requirements

- **PR-001**: Colour updates during drag MUST appear instantaneous to the user — no perceivable lag between moving the slider thumb and the colour changing.
- **PR-002**: The colour system MUST NOT introduce any new network requests or data-loading steps; all colour values are statically defined.
- **PR-003**: The colour rendering MUST be verified across common browsers and device types used by the application's target audience.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 10 effort values (1–10) display their designated colour when the slider is set to that value — verified by manual and automated visual inspection across all effort slider instances in the application.
- **SC-002**: Slider colour updates are perceived as immediate during drag interactions — no user-reported lag in colour change when moving the thumb.
- **SC-003**: 100% of effort sliders in the application use the defined colour palette consistently — no slider renders a colour outside the defined set for a given value.
- **SC-004**: The correct colour is displayed for previously entered effort values when the exercise list is re-rendered mid-session — verified for all effort slider instances that restore state.
- **SC-005**: The colour system is consistent across all supported interaction methods (mouse drag, touch drag, keyboard navigation) — all produce the same colour output for the same value.

## Assumptions

- Effort sliders already exist in the application and accept integer values 1–10.
- The application already has an "effort" concept with labels (Easy, Moderate, Hard, All Out) corresponding to value ranges.
- The colour palette provided by the user (10 exact hex values) is final and does not require further design input.
- "All effort sliders" refers to sliders used for logging per-set effort during active workouts; if the application has other slider types, they are out of scope.
- The slider control used in the application supports applying a colour to the filled track or thumb element without requiring a full component replacement.
