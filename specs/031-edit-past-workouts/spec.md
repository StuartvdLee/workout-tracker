# Feature Specification: Edit Past Workouts

**Feature Branch**: `031-edit-past-workouts`  
**Created**: 2026-07-19  
**Status**: Draft  
**Input**: User description: "I want to be able to edit past workouts in case I filled in wrong values or forgot to fill in values for weight and effort. I also want to be able to edit the overall workout effort"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Edit Exercise Values in a Past Workout (Priority: P1)

As a person reviewing a completed workout, I want to correct or add the weight and effort values for exercises in that workout, so my history remains accurate when I entered a wrong value or forgot to record one.

**Why this priority**: Correcting per-exercise weight and effort is the main user need. Without this, historical performance data and later comparisons remain wrong or incomplete.

**Independent Test**: Can be fully tested by opening a completed workout from history, changing one exercise's weight and effort values, saving the changes, reopening the workout, and verifying the corrected values are shown everywhere that workout data appears.

**Acceptance Scenarios**:

1. **Given** a completed workout has an exercise with an incorrect weight value, **When** the user edits the workout, updates the weight, and saves, **Then** the workout shows the corrected weight when viewed again.
2. **Given** a completed workout has an exercise with an incorrect effort value, **When** the user edits the workout, updates the effort, and saves, **Then** the workout shows the corrected effort when viewed again.
3. **Given** a completed workout has an exercise with no recorded weight or effort, **When** the user edits the workout and fills in the missing values, **Then** the workout shows the newly entered values when viewed again.
4. **Given** the user changes multiple exercise values in one edit session, **When** the user saves, **Then** all changed values are saved together and displayed consistently.

---

### User Story 2 - Edit Overall Workout Effort (Priority: P2)

As a person reviewing a completed workout, I want to correct or add the overall workout effort rating, so the session-level summary reflects how hard the workout actually felt.

**Why this priority**: Overall workout effort is part of the completed workout record and appears in session review. If it is wrong or missing, the workout summary and comparisons are misleading even if the exercise rows are correct.

**Independent Test**: Can be tested by opening a completed workout with a missing or incorrect overall effort, editing the overall effort value, saving, and verifying the session detail page shows the updated overall effort and uses it in the appropriate comparison context.

**Acceptance Scenarios**:

1. **Given** a completed workout has no overall effort recorded, **When** the user edits the workout, selects an overall effort, and saves, **Then** the workout detail shows the selected overall effort.
2. **Given** a completed workout has an incorrect overall effort, **When** the user edits the workout, changes the overall effort, and saves, **Then** the workout detail shows the corrected overall effort.
3. **Given** the user edits both exercise values and the overall effort before saving, **When** the user saves, **Then** all edited values are saved as one consistent update.

---

### User Story 3 - Avoid Accidental Historical Changes (Priority: P3)

As a person editing a past workout, I want clear controls for saving or cancelling my changes, so I do not accidentally alter historical data.

**Why this priority**: Editing past workout history changes records the user may rely on for progress tracking. The experience must make the edit state clear and protect against accidental changes.

**Independent Test**: Can be tested by opening a completed workout, entering edit mode, making changes, cancelling, and verifying the original values remain unchanged; then repeating the flow and saving to verify only explicit saves persist changes.

**Acceptance Scenarios**:

1. **Given** the user is viewing a completed workout, **When** they choose to edit it, **Then** the page clearly enters an editable state with visible save and cancel actions.
2. **Given** the user has changed values while editing, **When** they cancel, **Then** no changes are saved and the original workout values remain visible.
3. **Given** the user has changed values while editing, **When** saving fails, **Then** the user sees a clear error and their unsaved changes remain available for retry.
4. **Given** the user exits the edit flow with unsaved changes, **When** the product normally protects against unsaved changes elsewhere, **Then** this edit flow uses the same protection pattern.

---

### Edge Cases

- A completed workout has exercises with missing weight, missing effort, or both; the user can fill either field independently.
- A completed workout has no exercises; the edit flow does not show exercise inputs but still allows editing the overall workout effort when applicable.
- The user enters an invalid weight or effort value; the invalid value is rejected with a clear message and is not saved.
- The user clears a previously recorded optional weight or effort value; the cleared value is saved as intentionally unset (`null`) and displays with the same no-data indicator used elsewhere rather than reverting to the old value.
- The user opens a workout created before overall effort existed; the overall effort edit control starts empty and can be filled.
- Saving takes longer than expected or fails; the user sees a loading or error state and can retry without losing their changes.
- Edited values affect "last time", previous workout comparison, and session detail displays that depend on historical workout data; those views reflect the corrected data after save.
- A user navigates directly to a past workout detail page and edits from there; the same edit capabilities and safeguards are available as from normal history navigation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to enter an edit mode from a completed workout detail view.
- **FR-002**: Users MUST be able to edit the weight value for each exercise entry in a completed workout.
- **FR-003**: Users MUST be able to edit the effort value for each exercise entry in a completed workout.
- **FR-004**: Users MUST be able to add missing weight and effort values to exercise entries that were saved without those values.
- **FR-005**: Users MUST be able to clear optional weight and effort values when they want the completed workout to record those fields as empty.
- **FR-006**: Users MUST be able to edit the overall workout effort for a completed workout.
- **FR-007**: Users MUST be able to add an overall workout effort to completed workouts that were saved without one.
- **FR-008**: Exercise effort and overall workout effort values MUST use the existing 1-10 effort scale and labels used elsewhere in the product.
- **FR-009**: The system MUST validate edited weight values according to the same rules used when recording a workout.
- **FR-010**: The system MUST validate edited exercise effort and overall workout effort values as either empty or an integer from 1 through 10.
- **FR-011**: The system MUST save all edited values for a workout together so the workout is not left partially updated.
- **FR-012**: The system MUST show the updated values when the user returns to the workout detail view after saving.
- **FR-013**: The system MUST ensure workout history, session detail displays, "last time" data, and previous workout comparisons use the corrected historical values after an edit is saved.
- **FR-014**: The system MUST provide a cancel action that discards unsaved edits and restores the last saved workout values.
- **FR-015**: The system MUST prevent invalid edits from being saved and explain which value needs correction.
- **FR-016**: The system MUST preserve the existing workout date, workout identity, exercise order, and exercise membership when editing values; this feature is limited to per-exercise weight, per-exercise effort, and overall workout effort.
- **FR-017**: The system MUST handle save failures by keeping the user's unsaved edits visible and offering a retry path.

### Security & Privacy Requirements

- **SR-001**: System MUST only allow edits to completed workouts that the current user is authorized to view and modify.
- **SR-002**: System MUST validate all edited values before saving and reject malformed, out-of-range, or unsafe input.
- **SR-003**: System MUST not expose another user's workout history or identifiers while loading, editing, or saving a past workout.
- **SR-004**: Error messages MUST avoid exposing diagnostic details or internal identifiers that are not already visible to the user.

### User Experience Consistency Requirements

- **UX-001**: The edit mode MUST reuse existing workout detail layout, input styling, effort controls, and button patterns where applicable.
- **UX-002**: The edit flow MUST clearly distinguish view mode from edit mode so users know when changes are pending.
- **UX-003**: The edit flow MUST define visible loading, success, validation error, save error, and cancel states.
- **UX-004**: Save and cancel actions MUST remain easy to find while editing, including on smaller screens.
- **UX-005**: Field labels and empty-value indicators MUST remain consistent with the existing workout detail page and effort summary language.
- **UX-006**: The product MUST protect users from losing unsaved edits using the same unsaved-changes pattern used elsewhere in the application.

### Performance Requirements

- **PR-001**: For 95% of completed workouts with up to 25 exercises, edit mode MUST become usable within 1 second of the user choosing to edit.
- **PR-002**: For 95% of saves involving up to 25 edited exercise entries, the user MUST receive success or actionable failure feedback within 2 seconds.
- **PR-003**: Editing and saving a past workout MUST not make the workout history page or session detail page noticeably slower for users with at least 2 years of regular workout history.

### Key Entities

- **Workout Session**: A completed workout record with a date, workout identity, ordered exercise entries, and an optional overall effort value.
- **Session Exercise Entry**: A recorded exercise within a completed workout, including the exercise identity, order, optional weight value, and optional effort value.
- **Overall Workout Effort**: An optional session-level effort rating using the same 1-10 scale as exercise effort.
- **Edited Workout Values**: The pending changes a user makes to per-exercise weight, per-exercise effort, and overall workout effort before saving or cancelling.

### Assumptions

- Editing past workouts starts from the existing completed workout detail page.
- This feature does not include changing the workout date, renaming the workout, adding or removing exercises, reordering exercises, or deleting the workout.
- Weight remains optional, matching the current ability to have missing values.
- Effort values remain optional, but any provided effort value must use the established 1-10 scale.
- Corrected historical values should immediately become the source of truth for future comparison displays.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can correct or add a past exercise weight value in under 30 seconds from opening the workout detail page in 90% of usability checks.
- **SC-002**: Users can correct or add a past exercise effort value in under 30 seconds from opening the workout detail page in 90% of usability checks.
- **SC-003**: Users can correct or add an overall workout effort in under 30 seconds from opening the workout detail page in 90% of usability checks.
- **SC-004**: 100% of saved edits display the corrected values when the workout detail page is reopened.
- **SC-005**: 100% of invalid weight or effort edits are prevented from saving and show an actionable validation message.
- **SC-006**: 100% of cancelled edit sessions leave the original saved workout values unchanged.
- **SC-007**: Historical comparison displays that depend on edited workouts reflect the corrected values after save in 100% of covered test scenarios.
- **SC-008**: At least 95% of edit-mode entries and save attempts meet the response targets defined in the Performance Requirements.
