# Feature Specification: Resume an In-Progress Workout After a Refresh

**Feature Branch**: `stuartvdlee-resume-workout-refresh`

**Created**: 2026-11-27

**Status**: Implemented locally; not deployed

**Input**: GitHub issue [#199](https://github.com/StuartvdLee/workout-tracker/issues/199): "Resume an in-progress workout after a refresh"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Restore an Active Workout (Priority: P1)

As someone recording a workout, I want my in-progress workout and entered values saved as I work so I can continue after refreshing the page without re-entering my progress.

**Why this priority**: Losing entered workout data on refresh is the central problem this feature addresses.

**Independent Test**: Select the sets, start a workout, change exercise values, refresh the page, and verify the same workout, values and selected sets are available to continue.

**Acceptance Scenarios**:

1. **Given** a user has selected sets, started a workout and entered exercise values, **When** the progress has been saved and the user refreshes the page, **Then** the same active workout is restored with those values and selections unchanged.
2. **Given** a user has restored an active workout, **When** they edit its values and refresh again, **Then** the latest saved values are restored rather than the earlier values.
3. **Given** the user has no active workout, **When** they open or refresh the workout page, **Then** no active draft is created and existing workout history remains available.

---

### User Story 2 - Finish or Discard a Workout (Priority: P2)

As someone recording a workout, I want the in-progress draft removed when I finish or explicitly discard it so it is not restored later or duplicated in my history.

**Why this priority**: Cleanup keeps the resume feature consistent with the user's decision to finish or abandon the workout and protects workout history from duplicates.

**Independent Test**: Complete an active workout and verify one completed history entry and no restored draft; repeat with an explicit discard and verify no completed entry is added.

**Acceptance Scenarios**:

1. **Given** an active workout has been completed successfully, **When** the user refreshes or returns to the workout page, **Then** the completed workout is not restored as active and exactly one completed history entry exists.
2. **Given** an active workout exists, **When** the user explicitly discards it, **Then** the draft is removed, it is not restored after refresh, and no completed history entry is added.
3. **Given** a completion action is repeated or the page is refreshed during completion, **When** completion is finalized, **Then** the workout appears only once in history.

---

### User Story 3 - Recover from Save or Restore Problems (Priority: P2)

As someone recording a workout, I want clear feedback when workout progress cannot be saved or restored so I know whether my progress is safe and what I can do next.

**Why this priority**: The feature must not create false confidence or silently lose progress when persistence is unavailable or saved data cannot be used.

**Independent Test**: Simulate save unavailability and invalid saved data, then verify the app communicates the problem, offers an appropriate next action, and leaves existing workout history intact.

**Acceptance Scenarios**:

1. **Given** an active workout cannot be saved, **When** the user changes a value, **Then** the user receives clear feedback that the change may not survive a refresh and can retry the save without losing the values still present on the page.
2. **Given** saved workout data is invalid or cannot be restored, **When** the user opens the workout page, **Then** the app explains that the active workout could not be restored, provides a safe way to continue or clear the unusable draft, and preserves existing workout history.
3. **Given** the user retries after a temporary save problem and saving succeeds, **When** the page is refreshed, **Then** the latest saved workout values are restored.

### Edge Cases

- A saved draft is missing required values, contains unsupported values, or cannot be read.
- Saving becomes unavailable while a user is actively editing; the entered values remain available on the current page and the user is told they may not survive a refresh.
- The user refreshes immediately after changing a value; the latest successfully saved values are restored, and any unsaved change is not presented as safely persisted.
- Completion is repeated, interrupted by a refresh, or retried after a response is delayed; only one completed history entry is created.
- The user discards a workout and then refreshes; the discarded draft does not reappear.
- No active draft exists while older completed workout history is present; history remains unchanged.
- A different workout is already in progress, another tab owns the draft, or browser locking is unavailable.
- A completion response is lost, the workout template changes, or the completed session is later deleted.
- The draft exceeds a supported limit, the selected workout is missing, or draft removal fails.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST automatically save an active workout as the user changes it, including entered exercise values, committed exercise order and the sets selected before starting.
- **FR-002**: The system MUST restore the most recently saved active workout and its saved values after a page refresh in the same browser context.
- **FR-003**: The system MUST preserve the active workout's identity and progress when the user resumes and continues editing it.
- **FR-004**: The system MUST remove the active draft after successful workout completion.
- **FR-005**: The system MUST create no more than one completed history entry for an active workout, including when completion is repeated or interrupted and retried.
- **FR-006**: The system MUST remove an active draft after the user explicitly discards it and MUST NOT add a completed history entry for the discarded workout.
- **FR-007**: The system MUST report save failures clearly, keep the current page's entered values available, and provide a way to retry saving.
- **FR-008**: The system MUST report when saved workout data cannot be read or restored and provide a safe way to continue or clear the unusable draft.
- **FR-009**: The system MUST preserve existing completed workout history when an active workout is saved, restored, completed, discarded, or cannot be restored.
- **FR-010**: The system MUST NOT indicate that a workout change is safely saved when saving has failed.

### User Experience Requirements

- **UX-001**: The active workout MUST be clearly distinguishable from completed workout history and immediately usable after restoration.
- **UX-002**: Save and restore failures MUST use clear, actionable language that explains whether progress may be lost and what the user can do next.
- **UX-003**: Completion and discard MUST provide feedback consistent with the existing workout flow; discard MUST be an explicit user action.

### Security and Privacy Requirements

- **SP-001**: The feature MUST preserve the deployment's existing access protections and MUST NOT require a separate application-level user identity or multi-user draft isolation.
- **SP-002**: Discarding an active workout MUST remove its resumable draft without deleting or altering completed workout history.

### Performance Requirements

- **PF-001**: In at least 95 of 100 representative refresh trials, the latest saved active workout MUST be available to continue within 2 seconds after the workout page is ready.
- **PF-002**: A changed workout value MUST be saved within 2 seconds under normal operating conditions, or the user MUST receive a clear save-failure indication.

### Key Entities *(include if feature involves data)*

- **Active Workout Draft**: A user's unfinished workout, including its identity, exercise values, selected sets, and most recently saved progress.
- **Completed Workout**: A finalized workout represented in existing workout history; one active draft can produce at most one completed workout.
- **Workout History**: The user's existing completed workout records, which remain available and unchanged by draft recovery or discard.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100 consecutive refresh tests performed after a successful save, all entered exercise values and selected sets are restored without requiring re-entry.
- **SC-002**: In at least 95 of 100 representative trials, a user's latest saved workout is ready to continue within 2 seconds after the workout page is ready.
- **SC-003**: In 100 completion and retry tests, each successfully completed workout produces exactly one history entry and no active draft after refresh.
- **SC-004**: In 100 discard tests, the discarded workout is not restored and no completed history entry is created.
- **SC-005**: In save and restore failure tests, users receive actionable feedback in every case, and existing completed history remains unchanged.

## Assumptions

- The application has a single user who signs in through Azure SSO; multi-user access and application-level user identity are outside this feature's scope.
- Azure SSO and deployment access controls are assumed to protect the application and its API; this feature does not introduce or replace those controls.
- Restoration is required in the same browser context; continuing across different devices or browsers is outside this feature's scope. Saved drafts may be accessible to anyone using that browser profile.
- The existing workout entry, completion, discard, and history flows remain the source of truth for workout behavior.
- A value is considered safely saved only after the save succeeds; the feature does not promise that changes made after the last successful save survive an immediate refresh.
- Existing workout history is not migrated, replaced, or removed as part of this feature.
