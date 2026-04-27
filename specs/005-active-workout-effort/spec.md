# Feature Specification: Active Workout UI — Effort Tracking

**Feature Branch**: `005-active-workout-effort`  
**Created**: 2026-04-27  
**Status**: Draft  
**Input**: User description: "Change the way that an active workout looks. Remove reps altogether since the user always does the same number of reps. Weight should be specified in the UI as KG. Add an Effort slider to each exercise in a workout with range 1-10: Easy (1-3), Moderate (4-6), Hard (7-8), All Out (9-10)."

## Context

This feature refines the **active workout session view** introduced in `004-add-workouts`. It removes reps tracking (which is not needed because the user's rep count is fixed per exercise), makes the weight unit explicit as kilograms, and adds a per-exercise **Effort** slider so the user can capture how hard a set felt on a 10-point scale with named intensity bands.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Log Weight (KG) Without Reps (Priority: P1)

The user starts an active workout session from a planned workout. For each exercise, the only performance field shown is **weight in KG**. There is no reps input field anywhere on the active session view. The user enters the weight lifted (or leaves it blank if bodyweight/not applicable) and moves on to the next exercise.

**Why this priority**: Removing reps is the primary simplification. Everything else in the session view depends on this streamlined layout being in place first.

**Independent Test**: Can be fully tested by starting an active session and confirming there is no reps input field visible for any exercise, while a weight field labelled "KG" is present for every exercise. Saves correctly without any reps data.

**Acceptance Scenarios**:

1. **Given** the user opens an active workout session, **When** the session view loads, **Then** no reps input fields are shown — only the weight field and the Effort slider (Story 2) per exercise.
2. **Given** the user is on an active session, **When** they view the weight field for any exercise, **Then** the field is labelled or suffixed with "KG" to make the unit unambiguous.
3. **Given** the user enters a weight value in the KG field and saves the session, **Then** the completed session record stores the weight with the KG unit and no reps value.
4. **Given** the user leaves the weight field empty and saves the session, **Then** the session is saved successfully with no weight recorded for that exercise (weight is optional).
5. **Given** a previously saved workout session is viewed in history, **When** the user inspects an exercise entry, **Then** weight is displayed with the "KG" label and no reps column is shown.

---

### User Story 2 — Rate Effort Per Exercise (Priority: P1)

For every exercise in an active workout session, the user sees an **Effort slider** ranging from 1 to 10. The slider displays a named intensity band beneath the current value using these groupings:

| Value | Label    |
|-------|----------|
| 1–3   | Easy     |
| 4–6   | Moderate |
| 7–8   | Hard     |
| 9–10  | All Out  |

The user drags or taps the slider to the value that best describes how difficult the set felt. The label updates instantly as the slider moves. If the user does not interact with the slider, no effort value is recorded (effort is optional).

**Why this priority**: The Effort slider is the primary new capability introduced by this feature and must be present alongside the weight-only layout to deliver value.

**Independent Test**: Can be fully tested by starting an active session, setting the Effort slider for one exercise to each boundary value (1, 3, 4, 6, 7, 8, 9, 10), confirming the correct label appears, saving, and verifying the saved session stores the effort value.

**Acceptance Scenarios**:

1. **Given** the user is in an active workout session, **When** the session view loads, **Then** every exercise row includes an Effort slider with a visible range of 1 to 10.
2. **Given** the user moves the slider to a value between 1 and 3, **Then** the label "Easy" is displayed immediately beneath or alongside the slider.
3. **Given** the user moves the slider to a value between 4 and 6, **Then** the label "Moderate" is displayed.
4. **Given** the user moves the slider to a value between 7 and 8, **Then** the label "Hard" is displayed.
5. **Given** the user moves the slider to a value of 9 or 10, **Then** the label "All Out" is displayed.
6. **Given** the user has not interacted with the slider for an exercise, **When** the session is saved, **Then** no effort value is stored for that exercise (effort is optional and absence is valid).
7. **Given** the user sets the Effort slider to a value and then saves the session, **Then** the completed session record stores the exact numeric effort value (1–10) for that exercise.
8. **Given** a previously saved session is viewed in history, **When** the user views an exercise entry that had an effort value recorded, **Then** the effort value and its corresponding label (Easy/Moderate/Hard/All Out) are displayed.

---

### User Story 3 — View Effort Data in Workout History (Priority: P2)

When the user views a completed workout session in the History page, each exercise entry shows the weight (in KG) and, if recorded, the effort rating with its label. Reps are never shown. This gives the user a clear, compact record of each session's performance and perceived intensity.

**Why this priority**: Persisting and displaying effort data closes the loop — the feature has no value if recorded data is not surfaced in history. This depends on Stories 1 and 2 being complete.

**Independent Test**: Can be fully tested by completing a session with mixed effort data (some exercises rated, some left blank), navigating to History, opening the session detail, and confirming each exercise shows weight-KG and effort label (or nothing for unrated exercises), with no reps column anywhere.

**Acceptance Scenarios**:

1. **Given** a completed session contains exercises with effort ratings, **When** the user views the session in History, **Then** each rated exercise displays its effort value and label (e.g., "7 — Hard").
2. **Given** a completed session contains an exercise where effort was not recorded, **When** the user views the session in History, **Then** no effort value is shown for that exercise and no placeholder or error is displayed.
3. **Given** a completed session is displayed in History, **When** the user reviews it, **Then** no reps column or reps value appears anywhere in the session details.
4. **Given** a completed session is displayed in History, **When** the user reviews weight values, **Then** all weights are shown with the "KG" label.

---

### Edge Cases

- What happens when the user moves the Effort slider rapidly between values? The label must update smoothly and reflect the final resting value without lag or missed updates.
- What happens when the user enters a non-numeric value in the weight field? The system must reject non-numeric input and display an inline validation message; the session cannot be saved while invalid data is present.
- What happens when the weight field is left empty for all exercises? The session should save successfully — weight is optional.
- What happens when the user's network drops mid-session? The active session view should retain all entered weight and effort values locally so nothing is lost if the user was mid-input when the connection dropped.
- What happens on a small mobile screen where the slider and weight field must coexist? Both controls must be fully usable and accessible at minimum mobile breakpoints without overlap or truncation.
- What happens when an existing completed workout session (saved before this feature) is viewed in History? Old sessions that have reps recorded should suppress those reps values in the display (show nothing or a dash) rather than erroring; weight is shown with KG where available.
- What happens when the user tries to save while another save is already in progress? The Save button must be disabled until the current operation completes to prevent duplicate submissions.

## Requirements *(mandatory)*

### Functional Requirements

**Active Session View — Layout Changes**

- **FR-001**: The active workout session view MUST NOT display any reps input field for any exercise.
- **FR-002**: The active workout session view MUST display a weight input field for each exercise, clearly labelled or suffixed with "KG".
- **FR-003**: The weight field MUST accept numeric values only; non-numeric input MUST trigger an inline validation message and prevent saving.
- **FR-004**: The weight field MUST be optional; a session MUST be saveable with no weight value entered for any or all exercises.

**Effort Slider**

- **FR-005**: The active workout session view MUST display an Effort slider for each exercise, with a range of 1 to 10 (integer steps).
- **FR-006**: The Effort slider MUST display an intensity label that updates in real time as the slider is adjusted, using the following bands:
  - 1–3: **Easy**
  - 4–6: **Moderate**
  - 7–8: **Hard**
  - 9–10: **All Out**
- **FR-007**: The Effort slider MUST be optional; a session MUST be saveable without the user interacting with any slider.
- **FR-008**: When saved, the system MUST persist the numeric effort value (1–10) for each exercise that the user rated.
- **FR-009**: When saved, exercises where the user did not interact with the slider MUST be stored with no effort value (null/absent — not defaulted to any value).

**History Display**

- **FR-010**: The History session detail view MUST NOT display any reps column or reps value for any exercise.
- **FR-011**: The History session detail view MUST display the weight for each exercise with the "KG" label where a weight was recorded.
- **FR-012**: The History session detail view MUST display the effort value and its corresponding label (e.g., "7 — Hard") for each exercise where effort was recorded.
- **FR-013**: The History session detail view MUST gracefully handle exercises with no effort recorded (display nothing, not an error or placeholder).
- **FR-014**: Existing completed sessions saved before this feature was introduced MUST continue to display correctly in History; any previously stored reps data MUST be hidden from the UI (not surfaced).

### Security & Privacy Requirements

- **SR-001**: System MUST validate and sanitize all user inputs on the active session form (weight values, effort slider values) before persistence.
- **SR-002**: Effort and weight data for a session MUST only be accessible to the user who created that session; no cross-user data leakage.

### User Experience Consistency Requirements

- **UX-001**: The Effort slider MUST follow the existing interaction and visual patterns of the application; if no slider pattern exists, the design introduced here becomes the standard for future features.
- **UX-002**: The weight field and Effort slider MUST be clearly grouped per exercise so the user can unambiguously see which controls belong to which exercise.
- **UX-003**: The active session view MUST define the following states for the save action: default (ready to save), loading (save in progress, button disabled), success (session saved, user redirected to history or confirmation), and error (user-friendly message, inputs preserved).
- **UX-004**: On mobile-sized viewports, the weight input and Effort slider for each exercise MUST remain fully usable without horizontal scrolling or control overlap.
- **UX-005**: The intensity label (Easy / Moderate / Hard / All Out) MUST be visible at all times while the slider is being adjusted, not only on hover or focus.

### Performance Requirements

- **PR-001**: The active session view MUST load and be interactive within 3 seconds on a slow 3G connection.
- **PR-002**: The Effort slider label MUST update within 50 milliseconds of any slider movement so the interaction feels immediate.
- **PR-003**: Saving a completed session MUST provide visible loading feedback within 200 milliseconds of the user pressing Save.

### Key Entities

- **LoggedExercise** *(updated)*: Extends the entity defined in `004-add-workouts`. The `reps` field is removed from the active session UI (data model field may remain for backward compatibility but is no longer surfaced). Gains a new optional `effort` attribute: an integer between 1 and 10 representing perceived exertion for that exercise in the session.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of active workout session views contain no reps input field.
- **SC-002**: 100% of weight fields in the active session view are labelled with "KG".
- **SC-003**: The Effort slider label updates correctly for all 10 possible values, with each of the four intensity bands (Easy, Moderate, Hard, All Out) displayed at their correct thresholds.
- **SC-004**: Users can complete logging an exercise (set weight and effort) in under 15 seconds per exercise, without confusion about which control does what.
- **SC-005**: 100% of sessions saved with effort values persist those values correctly and display them in History with the correct label.
- **SC-006**: Sessions saved without any effort values save successfully — 0% error rate for sessions where the slider was not touched.
- **SC-007**: 100% of completed sessions in History display weight with the "KG" label and no reps column.
- **SC-008**: The active session view loads within 3 seconds on a slow 3G connection.
- **SC-009**: Existing pre-feature workout history entries continue to display without errors after this change is deployed.

## Assumptions

- The fixed rep count per exercise is not stored or displayed anywhere in the active session view; it is assumed to be known to the user and does not need to be surfaced during logging.
- "Effort" in this context maps to the concept of Rate of Perceived Exertion (RPE) and is entirely subjective. The system does not validate whether a logged effort value is physiologically consistent with the weight lifted.
- The `reps` field on the `LoggedExercise` data entity (defined in `004-add-workouts`) may remain in the underlying data model for backward compatibility with previously saved sessions. However, it MUST NOT be shown in any part of the UI going forward.
- Weight is assumed to always be in KG. There is no requirement for unit switching (e.g., lbs) in this release.
- Effort is captured once per exercise per session (not per set). The user performs their sets and then rates the overall effort for that exercise.
- The Effort slider defaults to an unset/no-value state rather than a specific starting position (e.g., it does not snap to 5 or any other default on load).
- This feature does not introduce target effort values on the PlannedWorkout template; effort is only recorded on completed sessions.
