# Feature Specification: Workout Overall Effort

**Feature Branch**: `016-workout-overall-effort`  
**Created**: 2026-05-19  
**Status**: Implemented  
**Input**: User description: "I want to fill in an overall effort when logging a workout. At the end of a workout, when I press 'Save Workout', I want a pop up to appear asking me for the workout effort. Use the same scale as for individual exercises. The overall effort should also be visible on the workout's history page but I don't know how to display it yet. Come up with three ideas for me"

## Clarifications

### Session 2026-05-19

- Q: Which display option should be used for the Workout History page? → A: Option C — inline text below the exercise count, right-aligned on the card.
- Q: Should overall effort also be visible on the session detail page (when opening a past workout)? → A: Yes — include in scope. Display option to be decided (see Session Detail Display Ideas).
- Q: Which display option should be used for the session detail page, and should it compare to the previous workout? → A: Option 3 (summary line below the exercises table) plus a comparison to the overall effort from the most recent prior session of the same workout.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rate Overall Workout Effort on Save (Priority: P1)

After completing all exercises in a workout, the user presses "Save Workout". A modal/pop-up appears prompting them to rate how hard the overall session felt on a scale of 1–10 (the same scale used for individual exercises: 1–3 Easy, 4–6 Moderate, 7–8 Hard, 9–10 All Out). The user sets their effort rating and confirms. The rating is saved alongside the completed workout session.

**Why this priority**: This is the core of the feature — capturing the data. Without it, nothing else is possible. It directly interrupts the existing save flow and must feel natural and frictionless.

**Independent Test**: Complete a workout session, press "Save Workout", interact with the effort pop-up, confirm, and verify the session is saved with the overall effort value recorded.

**Acceptance Scenarios**:

1. **Given** a user has an active workout session in progress, **When** they press "Save Workout", **Then** a modal pop-up appears before the workout is saved, prompting them to rate their overall effort.
2. **Given** the effort pop-up is shown, **When** the user sets a rating between 1 and 10 and confirms, **Then** the workout session is saved with the selected overall effort value.
3. **Given** the effort pop-up is shown, **When** the user dismisses or skips the rating (e.g., via a "Skip" or "Save without rating" option), **Then** the workout session is saved without an overall effort value (effort remains unset).
4. **Given** the effort pop-up is shown, **When** the user interacts with the effort control, **Then** the label ("Easy", "Moderate", "Hard", "All Out") updates in real time to match the selected value, consistent with the per-exercise effort slider behaviour.

---

### User Story 2 - View Overall Effort on Workout History Page (Priority: P2) *(Removed post-delivery)*

> **Note**: This user story was implemented as specified, then removed after initial delivery at the user's request — the effort display on the history card was considered too cluttered. The overall effort is **not** shown on the Workout History page. It remains accessible via the Session Detail page (User Story 3).

**Why removed**: The history card is compact by design. Adding a second line of metadata made it feel cluttered. The session detail page already provides the effort with comparison context, which is more useful.

---

### User Story 3 - View Overall Effort on Session Detail Page (Priority: P3)

When the user opens a past workout session from the history page, a summary line below the exercises table shows the overall effort for that session alongside the overall effort from the most recent prior session of the same workout (for comparison). This gives the user a quick sense of whether they pushed harder or eased off compared to last time.

**Why this priority**: Completes the per-session view with session-level context. The comparison mirrors the existing per-exercise "Prev. Effort" pattern, so users already understand the mental model.

**Independent Test**: Open a past session that was saved with an overall effort rating, where a prior session of the same workout also exists. Verify a summary line below the table shows both the current and previous overall effort. Open a session with no prior workout or no previous effort — verify graceful display.

**Acceptance Scenarios**:

1. **Given** a session was saved with an overall effort of 8, and the most recent prior session of the same workout had an overall effort of 6, **When** the user opens that session on the detail page, **Then** a summary line below the exercises table shows the current overall effort (e.g., "8 · All Out") and the previous overall effort (e.g., "6 · Moderate").
2. **Given** a session was saved without an overall effort, **When** the user opens that session on the detail page, **Then** the summary row is always shown; the overall effort position displays "—" (consistent with how missing per-exercise previous data is shown).
3. **Given** a session was saved with an overall effort, but no prior session for the same workout exists (or the prior session has no overall effort), **When** the user opens that session on the detail page, **Then** the previous effort position shows "—" (consistent with how missing per-exercise previous data is displayed).
4. **Given** a session detail page is shown, **When** the user views per-exercise effort columns in the table, **Then** the overall session effort summary line is visually distinct from the per-exercise effort data and not confused with it.
5. **Given** an ad-hoc session (not linked to a planned workout), **When** the user opens that session on the detail page, **Then** no previous comparison is shown (as no "previous instance of the same workout" can be determined).

---

### Edge Cases

- What happens if the user force-closes the browser or navigates away while the effort pop-up is open? The workout should either not be saved yet (pop-up is pre-save), or the system should handle it gracefully without data loss.
- What happens if the user skips the effort rating? The session must still be saved successfully; effort is optional.
- How does the history page behave for sessions logged before this feature existed (no effort data)? Cards must display without an effort indicator and without errors — no effort is shown on the history page regardless of whether a session has one.
- What happens on a slow network when the save request fails after the pop-up is dismissed? The user should receive a clear error and be able to retry without losing their session data.
- How does the overall effort summary row on the session detail page behave for ad-hoc sessions (not linked to a planned workout)? The current effort shows (or "—" if unrated); the previous effort always shows "—" since no previous instance of the same workout can be determined.
- How does the overall effort on the session detail page remain visually distinct from the per-exercise effort columns in the table?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a modal pop-up when the user presses "Save Workout", before the session is persisted, prompting them to rate their overall workout effort.
- **FR-002**: The effort pop-up MUST use the same 1–10 scale and effort labels (Easy, Moderate, Hard, All Out) as the per-exercise effort control.
- **FR-003**: Users MUST be able to skip or dismiss the effort pop-up without rating, and the workout MUST still be saved successfully without an effort value.
- **FR-004**: System MUST persist the overall effort value (1–10, optional) as part of the workout session record.
- **FR-005**: The effort control in the pop-up MUST show the descriptive label (e.g., "Hard") updating in real time as the user adjusts the rating.
- **FR-006**: ~~System MUST display the overall effort for each session on the Workout History page.~~ **Removed post-delivery** — effort is not displayed on the Workout History page (removed after initial implementation due to visual clutter). The API still returns `overallEffort` in `GET /api/sessions` responses for forward compatibility.
- **FR-007**: Sessions without an overall effort MUST display gracefully on the session detail page without showing a blank or broken indicator (the summary row always shows, using "—" for absent values).
- **FR-008**: The effort value MUST be validated to be either absent (null) or an integer between 1 and 10, inclusive.
- **FR-009**: System MUST display the overall effort for a session on the session detail page as a summary row below the exercises table, always visible, showing both the current session's overall effort and the overall effort of the most recent prior session for the same workout (for comparison). When a value is absent, "—" is shown.
- **FR-010**: When the most recent prior session has no overall effort recorded, or when no prior session for the same workout exists, the previous overall effort position in the summary row MUST display "—" (consistent with how missing per-exercise previous data is shown). For ad-hoc sessions not linked to a planned workout, the previous position also shows "—".

### Security & Privacy Requirements

- **SR-001**: System MUST validate and sanitize the overall effort value on the server side; any value outside the 1–10 range or of an unexpected type MUST be rejected.
- **SR-002**: Access to save a session and its effort rating MUST be restricted to the authenticated user who owns the session.

### User Experience Consistency Requirements

- **UX-001**: The effort control in the pop-up MUST visually and behaviourally match the existing per-exercise effort slider (same scale, same real-time label updates, same "not rated" initial state).
- **UX-002**: The pop-up MUST provide clear affordances for both confirming with a rating and skipping without one. Button labels must be unambiguous (e.g., "Save" vs "Skip").
- **UX-003**: The session detail page effort summary row MUST be visually consistent and must not break existing layouts. The Workout History page is unaffected by this feature.
- **UX-004**: The pop-up MUST be accessible: keyboard navigable, screen-reader labelled, and dismissible via standard mechanisms (e.g., Escape key).
- **UX-005**: The overall effort summary line on the session detail page MUST be clearly visually distinct from the per-exercise effort columns in the exercises table, so users cannot confuse session-level with exercise-level data.
- **UX-006**: The previous overall effort in the session detail summary line MUST use the same "—" placeholder as the existing per-exercise previous data when no value is available, ensuring a consistent no-data pattern across the page.

### Performance Requirements

- **PR-001**: The effort pop-up MUST appear immediately (no perceptible delay) when "Save Workout" is pressed; it must not require a network call before rendering.
- **PR-002**: Saving a session with an overall effort value MUST not increase the save latency perceptibly compared to saving without one.
- **PR-003**: The history page MUST continue to load and render all session cards, including the effort display, within the same time budget as the current implementation.

### Key Entities

- **WorkoutSession**: An existing entity representing a completed workout. Gains an optional overall effort attribute (integer 1–10). Relationships to logged exercises are unchanged.

## Session Detail Display Ideas

The following three options were proposed for the session detail page. **Option 3 has been selected.**

### Option 1 — In the Session Header (alongside the date)

Display "Overall effort: 7 · Hard" as an extra line in the session header area, immediately below the date.

---

### Option 2 — Summary Banner between Header and Table

A dedicated strip between the page header and the exercises table, showing "Overall session effort: 7 · Hard".

---

### ✅ Option 3 — Summary Line below the Table *(Selected)*

A summary line after the exercises table showing the current overall effort alongside the previous session's overall effort (e.g., "Overall effort: 8 · All Out  ·  Previous: 7 · Hard"). If no previous session exists or the previous session has no overall effort, the previous position shows "—".

*Best for*: Natural reading flow — the user reviews all exercises first, then sees the overall session rating and comparison as a conclusion, mirroring the "current vs. previous" pattern already established in the exercise table columns.

---

## Assumptions

- The effort pop-up is a blocking modal: the workout is not saved until the user either rates and confirms or explicitly skips.
- The overall effort is optional; the feature must not prevent saving a workout for users who choose not to rate.
- "Same scale as individual exercises" means 1–10 integer, with the labels: 1–3 Easy, 4–6 Moderate, 7–8 Hard, 9–10 All Out.
- The history page does **not** display overall effort (removed after initial implementation — considered too cluttered).
- The session detail page displays a summary row below the exercises table showing both current and previous overall effort (Option 3 confirmed). The row is always visible; absent values show "—". "Previous" means the most recent prior completed session for the same planned workout (same `PlannedWorkoutId`). Ad-hoc sessions (no `PlannedWorkoutId`) show "—" for previous. Missing previous effort shows "—".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete the "Save Workout" flow, including the effort pop-up, in under 15 seconds from pressing the save button.
- **SC-002**: 100% of workout sessions saved via the updated flow either include a valid effort value (1–10) or explicitly record no effort — no sessions are saved in an ambiguous state.
- **SC-003**: The history page continues to load all session cards within the same time budget as before this feature was introduced.
- **SC-004**: Sessions saved before this feature was introduced display correctly on both the history page and session detail page with no visible errors or broken layout.
- **SC-005**: 100% of effort controls in the pop-up, history page, and session detail page use consistent labels and value ranges matching the existing per-exercise effort scale.
- **SC-006**: The session detail summary line comparison displays correctly for sessions with and without a prior session, and for sessions where the prior session has no overall effort recorded.
