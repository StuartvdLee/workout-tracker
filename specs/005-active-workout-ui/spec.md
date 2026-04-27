# Feature Specification: Active Workout UI Redesign

**Feature Branch**: `005-active-workout-ui`  
**Created**: 2026-04-27  
**Status**: Draft  
**Increments**: [004-add-workouts](../004-add-workouts/spec.md)  
**Input**: User description: "Change the way that an active workout looks. Get rid of reps altogether since I always do the same number of reps. Weight should be specified in the UI as KG. Add an 'Effort' slider to each exercise in a workout."

## Summary of Changes

This spec refines the **active workout session view** introduced in `004-add-workouts` (User Story 6 / FR-024–FR-026). The changes are:

1. **Remove reps** from both the planned workout template and the active session logging view. The user performs a fixed number of reps every time, so capturing them adds no value.
2. **Explicit KG unit labelling** for weight fields. Weight is always in kilograms; the UI must make this clear with a "kg" suffix/label.
3. **Effort slider** per exercise in the active session view. After performing an exercise, the user rates perceived effort on a 1–10 scale with labelled bands (Easy / Moderate / Hard / All Out).

### Effort Scale

| Value | Label    |
|-------|----------|
| 1     | Easy     |
| 2     | Easy     |
| 3     | Easy     |
| 4     | Moderate |
| 5     | Moderate |
| 6     | Moderate |
| 7     | Hard     |
| 8     | Hard     |
| 9     | All Out  |
| 10    | All Out  |

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Log Weight (kg) for an Exercise in an Active Session (Priority: P1)

While performing an active workout session, the user logs the weight they lifted for each exercise. The weight field displays a "kg" label so the unit is always unambiguous. There is no reps field; that information is not tracked.

**Why this priority**: Weight logging is the primary numeric input during a session. Removing reps simplifies the form and eliminating unit ambiguity prevents data errors.

**Independent Test**: Can be fully tested by starting a workout session and verifying each exercise card shows a weight input labelled in kg with no reps input visible.

**Acceptance Scenarios**:

1. **Given** the user is in an active workout session, **When** they view an exercise card, **Then** a weight input field is shown with a visible "kg" label and no reps input is present.
2. **Given** the user enters a valid numeric weight, **When** they save the session, **Then** the weight is stored in kg and displayed correctly in workout history.
3. **Given** the user leaves the weight field empty, **When** they save the session, **Then** the session is saved successfully — weight is optional.
4. **Given** the user enters a non-numeric value in the weight field, **When** they attempt to save, **Then** a validation message indicates weight must be a number.

---

### User Story 2 - Rate Perceived Effort for an Exercise (Priority: P1)

After completing an exercise set, the user rates their perceived effort using an interactive 1–10 slider. The slider displays the current effort label (Easy / Moderate / Hard / All Out) alongside the numeric value. The effort rating is saved as part of the completed workout session.

**Why this priority**: Effort tracking replaces reps as the primary subjective quality signal for each set. It allows users to track fatigue trends and identify when to increase load, without requiring them to count and log reps.

**Independent Test**: Can be fully tested by starting a workout session, interacting with the effort slider on an exercise card, saving, and verifying the effort value appears in session history.

**Acceptance Scenarios**:

1. **Given** the user is in an active workout session, **When** they view an exercise card, **Then** an effort slider is displayed ranging from 1 to 10 with the current label shown (e.g., "6 — Moderate").
2. **Given** the user moves the slider to a value, **When** they view the slider label, **Then** the label updates immediately to reflect the effort band: 1–3 → Easy, 4–6 → Moderate, 7–8 → Hard, 9–10 → All Out.
3. **Given** the user sets an effort value and saves the session, **When** the session is persisted, **Then** the effort value (1–10) is stored alongside the exercise entry.
4. **Given** the user does not interact with the effort slider, **When** they save the session, **Then** the effort value defaults to no selection (null/unset) and the session is saved successfully.
5. **Given** the effort slider is at a Hard or All Out value (7–10), **When** displayed, **Then** the label uses a visually distinct colour or style to draw attention.

---

### User Story 3 - View Effort and Weight in Workout History (Priority: P2)

When the user reviews a completed workout session in history, they can see the weight (kg) and effort rating for each exercise. Reps are not shown. This gives a concise summary of session performance.

**Why this priority**: History must reflect the new data model. Without this, the effort data captured during sessions would be invisible to the user.

**Independent Test**: Can be fully tested by completing a session with weights and effort ratings, navigating to the History view, and verifying weight (kg) and effort labels appear for each exercise — with no reps column.

**Acceptance Scenarios**:

1. **Given** a completed session is displayed in history, **When** the user views the exercise details, **Then** weight is shown with a "kg" suffix and the effort label (e.g., "Moderate") is shown. No reps data is displayed.
2. **Given** an exercise was logged without a weight, **When** displayed in history, **Then** the weight field shows a dash or "—" (not blank).
3. **Given** an exercise was logged without an effort rating, **When** displayed in history, **Then** the effort field shows "—" (not blank).

---

### User Story 4 - Planned Workout Template: No Target Reps (Priority: P2)

When creating or editing a planned workout template, there is no target reps field. Only target weight (in kg) may be set per exercise. This keeps the template consistent with the session logging view.

**Why this priority**: Consistency between template and session is essential. If reps are not tracked during sessions, they should not appear in templates either.

**Independent Test**: Can be fully tested by creating a planned workout, verifying no reps input is available, setting a target weight, saving, and verifying the template displays only weight targets.

**Acceptance Scenarios**:

1. **Given** the user is creating or editing a planned workout, **When** they add an exercise, **Then** only a target weight field (labelled "kg") is available — no target reps field is present.
2. **Given** a planned workout with a target weight is displayed, **When** the user starts a session from it, **Then** the active session pre-fills the weight field with the target weight.

---

### Edge Cases

- What happens when the weight field contains a decimal value (e.g., 7.5 kg)? The system should accept it as a valid numeric entry.
- What happens when the user moves the effort slider to an extreme value (1 or 10)? Both endpoints must be selectable and labelled correctly.
- What happens if an old completed session was logged with reps (from the 004 release)? The history view should gracefully omit the reps column; any previously stored reps data remains in the database but is not displayed.
- What happens when the user completes a session and skips setting effort for some exercises? Those exercises are saved with effort = null and displayed as "—" in history.

---

## Requirements *(mandatory)*

### Functional Requirements

**Active Session View**

- **FR-001**: The active workout session view MUST NOT display a reps input field for any exercise.
- **FR-002**: Each exercise card in the active session MUST display a weight input field labelled with "kg".
- **FR-003**: Each exercise card in the active session MUST display an effort slider with a range of 1 to 10 (integer steps).
- **FR-004**: The effort slider MUST display a live label alongside the numeric value using the following mapping: 1–3 → "Easy", 4–6 → "Moderate", 7–8 → "Hard", 9–10 → "All Out".
- **FR-005**: Effort is optional; the slider MUST support an unset/null state (no effort selected by default).
- **FR-006**: The system MUST validate that weight, if provided, is a numeric value (integers and decimals accepted).
- **FR-007**: The system MUST store the logged effort value (1–10 or null) as part of the `LoggedExercise` record for each exercise in a completed session.
- **FR-008**: The system MUST store the logged weight value (numeric or null) labelled as kg in the `LoggedExercise` record.

**Planned Workout Templates**

- **FR-009**: The planned workout creation and edit forms MUST NOT display a target reps field for exercises.
- **FR-010**: The planned workout forms MUST display a target weight field per exercise, labelled with "kg".
- **FR-011**: When starting a session from a planned workout that has a target weight set, the active session MUST pre-fill the weight input with that target weight.

**History View**

- **FR-012**: The history detail view for completed sessions MUST display weight with a "kg" suffix and the effort label (Easy / Moderate / Hard / All Out) for each logged exercise.
- **FR-013**: The history view MUST NOT display a reps column or reps data.
- **FR-014**: Where weight or effort was not recorded for an exercise, the history view MUST display "—" in place of the missing value.

**Data Model**

- **FR-015**: The `LoggedExercise` entity MUST include an `effort` field (integer 1–10, nullable) in place of the previously defined `actualReps` field.
- **FR-016**: The `WorkoutExercise` (template join entity) MUST remove the `targetReps` field. The `targetWeight` field (numeric, nullable, in kg) is retained.

### User Experience Consistency Requirements

- **UX-001**: The effort slider MUST use a style consistent with the rest of the application's input components (colour palette, sizing, spacing, mobile touch targets).
- **UX-002**: The effort label (Easy / Moderate / Hard / All Out) MUST update in real-time as the user drags the slider — no button press required to see the current label.
- **UX-003**: Effort labels at the Hard (7–8) and All Out (9–10) bands SHOULD use a visually distinct accent colour (e.g., orange or red) to communicate intensity.
- **UX-004**: The "kg" unit label MUST appear as a suffix inside or adjacent to the weight input (e.g., an inline label or input adornment), not as a separate instructional paragraph.
- **UX-005**: The active session layout MUST remain mobile-friendly: weight input and effort slider MUST be comfortably operable by thumb on a small screen.
- **UX-006**: The removal of the reps field MUST not leave visual gaps or broken layouts; the exercise card layout MUST be designed around weight + effort only.

### Performance Requirements

- **PR-001**: Effort slider interactions MUST update the displayed label within 16 ms (one animation frame) with no perceptible lag.
- **PR-002**: No additional network round-trip is required to resolve effort labels — the mapping is client-side.

### Key Entity Changes

- **LoggedExercise** (updated): Removes `actualReps` (integer). Adds `effort` (integer 1–10, nullable). Retains `actualWeight` (decimal, nullable, in kg).
- **WorkoutExercise** (updated): Removes `targetReps`. Retains `targetWeight` (decimal, nullable, in kg).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No reps input appears anywhere in the active workout session view.
- **SC-002**: All weight fields display a "kg" label; the unit is unambiguous to a new user with no tooltip or external explanation needed.
- **SC-003**: The effort slider updates its label in real-time with no perceptible delay on both desktop and mobile.
- **SC-004**: 100% of effort values (1–10) map to the correct label when selected.
- **SC-005**: A completed session with weight and effort data for all exercises is saved and displayed correctly in history in under 5 seconds.
- **SC-006**: History entries for sessions completed before this feature was introduced display gracefully (reps hidden, effort shown as "—").
- **SC-007**: The active session view is usable with one hand on a mobile device — weight input and effort slider are reachable by thumb.

## Assumptions

- Weight is always in kilograms. There is no unit toggle (kg ↔ lb) in this release.
- The effort scale is fixed at 1–10 with the defined label bands; custom labels or different scale lengths are out of scope.
- The user always completes the same number of reps per set and finds reps tracking redundant. Rep data from any previous implementations is retained in the database but not surfaced in the UI.
- A single effort rating is captured per exercise per session (not per set/rep). If the user performs multiple sets of the same exercise in a session, this is out of scope for this release.
- The planned workout template no longer exposes a target reps field; any previously stored `targetReps` values are retained in the database but ignored by the UI.

## Clarifications

### Session 2026-04-27

- Q: Should there be a unit toggle (kg/lb)? → A: No — kg only for this release.
- Q: Should effort be required or optional? → A: Optional (null if not set).
- Q: Is effort per set or per exercise per session? → A: Per exercise per session (one rating per exercise, not per set).
