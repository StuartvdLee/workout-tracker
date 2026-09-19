# Feature Specification: Workout Sets Selection

**Feature Branch**: `032-workout-sets-selection`
**Created**: 2026-09-19
**Status**: Implemented and verified in PR #159
**Input**: User description: "I want to add the number of sets to my workout. I want to select the number of sets right before I start my workout. So on the \"Let's go!\" page, add a dropdown called \"Sets\" below the \"Select your workout\" dropdown. Make sure it's in the same visual style. The number of sets will either be 3 or 5. The number of sets will apply to all exercises in a workout. In the \"current workout\" view/page, add the number of sets to the \"Last time\" information like this: \"3 sets\" or \"5 sets\" depending on the number of sets. On the \"History\" view of a previous workout, add the number of sets to the table as well as the previous number of sets. Do this in the same way as weight and effort"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Choose sets before starting a workout (Priority: P1)

A user on the "Let's go!" page selects their workout and then picks the number of sets (3 or 5) from a "Sets" dropdown directly below the workout dropdown, before starting the session. The chosen sets value applies to every exercise in that session and is stored with the session.

**Why this priority**: Without capturing sets at start, no other part of the feature has data to display. This is the foundation.

**Independent Test**: Open the "Let's go!" page, select a workout and a sets value, start the workout, and confirm the session is created and stores the selected sets value.

**Acceptance Scenarios**:

1. **Given** the user is on the "Let's go!" page, **When** the page loads, **Then** a dropdown labelled "Sets" appears directly below the "Select your workout" dropdown, styled identically to it, offering only the values 3 and 5.
2. **Given** the user has selected a workout and a sets value, **When** they start the workout, **Then** the session begins and the selected sets value is recorded for the whole session.
3. **Given** the user has selected a workout but no sets value, **When** they attempt to start the workout, **Then** the system prevents the start and shows a validation message in the same style as the existing workout-selection validation message.
4. **Given** the user has selected no workout and no sets value, **When** they attempt to start the workout, **Then** the existing workout-selection validation behaviour still applies.

---

### User Story 2 - See latest comparable session's sets during the current workout (Priority: P2)

While logging a current workout, for each exercise the user sees the "Last time" information extended with the sets from the latest prior session containing usable comparison data for that exercise, shown as "3 sets" or "5 sets".

**Why this priority**: Sets context during the workout is the main day-to-day value of storing sets, but it depends on sets data existing (US1).

**Independent Test**: With at least one prior session that recorded sets, start a new session of the same workout and confirm the "Last time" line for each exercise includes the sets text alongside the existing weight and effort values.

**Acceptance Scenarios**:

1. **Given** the latest prior session containing usable weight or effort for an exercise recorded 5 sets, **When** the user views that exercise in the current workout, **Then** the "Last time" line includes "5 sets" from that same source session in the existing separated-value format.
2. **Given** the selected latest usable comparison session has no recorded sets (pre-existing data), **When** the user views that exercise, **Then** the "Last time" line shows the available weight or effort without any sets text, placeholder, or error.
3. **Given** no prior session contains usable weight or effort for the exercise, **When** the user views that exercise, **Then** the existing "First session — no previous data" message is shown unchanged.

---

### User Story 3 - See sets and previous sets in workout history (Priority: P3)

When viewing a previous workout in History, the user sees a "Sets" column and a "Prev. Sets" column in the exercise table, presented and behaving consistently with the existing weight and effort columns, including in edit mode.

**Why this priority**: Historical review is valuable but secondary to capturing and using sets in the live workout flow.

**Independent Test**: Open the history detail page for a session that recorded sets and confirm both the sets and previous sets columns render correctly, and that editing follows the same pattern as weight and effort.

**Acceptance Scenarios**:

1. **Given** a session recorded with sets, **When** the user opens its history detail view, **Then** the table shows a "Sets" column with the recorded value and a "Prev. Sets" column with the value from the latest prior session containing usable weight or effort for each exercise; different exercises may use different source sessions.
2. **Given** an exercise has no prior session containing usable weight or effort, **When** the user views the history detail table, **Then** the "Prev. Sets" cell shows the same no-data indicator used for previous weight and previous effort.
3. **Given** the user edits a past session, **When** they change the sets value and save, **Then** the new sets value is persisted for the whole session and reflected on reload, consistent with how weight and effort edits behave.
4. **Given** a session recorded before this feature existed, **When** the user views it, **Then** the sets cells show the standard no-data indicator rather than an error.

---

### Edge Cases

- What happens when a session was recorded before sets existed? Sets values are treated as absent and rendered with the existing no-data indicator (history) or omitted (current workout "Last time").
- What happens if a sets value outside the allowed set (3 or 5) is submitted? The system rejects it and the session is not created or updated.
- What happens when the workout list fails to load? The sets dropdown remains usable but starting a workout is blocked by the existing workout-selection error handling.
- What happens when the user encounters a slow network, slow device, or delayed backend response? The sets dropdown follows the same loading and disabled-state behaviour as the workout dropdown, and starting a workout remains blocked until the request resolves.
- How does the experience stay consistent across loading, empty, success, and failure states? Sets reuses the existing select, validation-message, table-cell, and no-data patterns without introducing new visual treatments.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The "Let's go!" page MUST present a dropdown labelled "Sets" positioned directly below the "Select your workout" dropdown, using the same label, select, and spacing styles.
- **FR-002**: The "Sets" dropdown MUST offer exactly two selectable values, 3 and 5, with no value pre-selected and a placeholder option consistent with the workout dropdown.
- **FR-003**: Users MUST be able to start a workout only when both a workout and a sets value are selected; otherwise a validation message is shown in the existing style.
- **FR-004**: The selected sets value MUST apply to all exercises in the started session and be stored once per session rather than per exercise.
- **FR-005**: The system MUST persist the sets value with the workout session so it is available in later sessions and in history.
- **FR-006**: The current workout view MUST include sets from the latest prior session containing usable weight or effort for that exercise in the "Last time" information, formatted as "3 sets" or "5 sets", using the same source session and separator as the displayed weight and effort values.
- **FR-007**: The current workout view MUST omit the sets portion of "Last time" when the selected latest usable comparison session has no recorded sets, without showing an error or placeholder text.
- **FR-008**: The history detail view of a previous workout MUST include a "Sets" column and a "Prev. Sets" column in the exercise table, ordered and styled consistently with the weight and effort columns.
- **FR-009**: The history detail view MUST show the established no-data indicator when a sets or previous-sets value is unavailable.
- **FR-010**: Editing a past session MUST allow changing the session sets value using the same interaction pattern as editing weight and effort, and MUST persist the change for all exercises in that session.
- **FR-011**: Existing sessions recorded before this feature MUST continue to display and be editable without error when no sets value exists.

### Security & Privacy Requirements

- **SR-001**: The system MUST validate that any submitted sets value is one of the permitted values (3 or 5) on the server, rejecting all other input.
- **SR-002**: The system MUST apply the same access rules to sets data as to existing session data, so users can only read and modify sets on sessions they are permitted to access.
- **SR-003**: Sets data introduces no secrets, sensitive personal data, or third-party integrations; no new data-handling controls are required beyond existing session handling.

### User Experience Consistency Requirements

- **UX-001**: The sets dropdown, table columns, and "Last time" text MUST reuse the existing select, table, and summary-line patterns without introducing new visual styles.
- **UX-002**: Loading, empty (no sets recorded), success, and error states MUST be defined for the "Let's go!" page, the current workout view, and the history detail view, reusing existing state treatments.
- **UX-003**: The term "Sets" MUST be used consistently in labels, column headers, and summary text, with the "Prev. Sets" header matching the existing "Prev." header convention.

### Performance Requirements

- **PR-001**: For sessions containing up to 25 exercises, the p95 duration of starting a workout and loading a history detail view MUST regress by no more than 10% or 100 milliseconds, whichever allowance is greater, compared with the pre-feature baseline measured in the same environment.
- **PR-002**: Sets data adds a single value per session and introduces no additional per-exercise lookups, repeated queries, or new hot paths.
- **PR-003**: Performance MUST be verified with at least 20 warmed measurements per affected flow and by confirming that starting a workout and loading history detail issue no additional data-fetch round trips compared with the current behaviour.

### Key Entities *(include if feature involves data)*

- **Workout Session**: An instance of a performed workout; gains a single sets attribute (3 or 5, or absent for historical sessions) that applies to all of its exercises.
- **Logged Exercise**: An exercise recorded within a session; its displayed sets value derives from its parent session's sets attribute, and its "previous sets" derives from the latest prior session selected as the usable weight-or-effort comparison for that exercise.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can select a workout and a sets value and start a session in no more than two additional interactions compared to today.
- **SC-002**: 100% of newly started sessions have a recorded sets value of 3 or 5.
- **SC-003**: For every exercise whose latest usable weight-or-effort comparison session recorded sets, the "Last time" line shows the sets value from that same source session in 100% of cases.
- **SC-004**: 100% of history detail views for sessions predating this feature render without errors and show the standard no-data indicator for sets.
- **SC-005**: 100% of affected flows use the existing select, table, validation, and no-data patterns with no new visual treatments introduced.
- **SC-006**: Across at least 20 warmed measurements for sessions with up to 25 exercises, the p95 duration of starting a workout and opening history detail regresses by no more than 10% or 100 milliseconds, whichever allowance is greater, with no additional data-fetch round trips.

## Delivery Verification

- **Implemented**: PR #159 (`https://github.com/StuartvdLee/workout-tracker/pull/159`).
- **Verified**: Release build passed; frontend tests passed (92); backend tests passed (167); Playwright E2E tests passed (280); TypeScript build passed; whitespace validation passed.
- **Verified behavior**: New sessions require and persist Sets 3 or 5; active-workout comparisons and history current/previous Sets render correctly; legacy missing Sets is omitted or shown with the existing no-data marker; session-level editing remains synchronized.
- **Not completed**: The manual pre-feature versus feature p95 baseline procedure in PR-001/SC-006 was not run because there is no separate pre-feature baseline commit for this branch. Automated request-count, query-count, selector-bound, and local latency smoke budgets passed.
- **Tooling note**: The repository has no separate npm `lint` script; TypeScript compilation and the existing frontend test suite were run instead.
