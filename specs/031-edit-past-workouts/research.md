# Research: Edit Past Workouts

## Decision 1: Add a dedicated session update endpoint

**Decision**: Add `PUT /api/sessions/{sessionId}` to update editable values on an existing completed workout session.

**Rationale**: Past-workout editing targets an existing `WorkoutSession`, not the planned workout template and not a newly completed session. Feature 014 already exposes `GET /api/sessions/{sessionId}` for session detail review, and feature 026 added session delete on the same resource. Adding a matching update endpoint keeps the resource model coherent.

**Alternatives considered**:

- Reuse `POST /api/workouts/{workoutId}/sessions`: rejected because it creates a new session instead of correcting an existing historical record.
- Add separate endpoints for exercise values and overall effort: rejected because FR-011 requires saving edited values together, and split endpoints could leave a partially updated workout.
- Edit via planned workout endpoint: rejected because planned workouts define templates, while this feature edits completed session data only.

## Decision 2: No database migration is required

**Decision**: Reuse existing nullable columns: `logged_exercise.logged_weight`, `logged_exercise.effort`, and `workout_session.overall_effort`.

**Rationale**: Features 005 and 016 already added the required storage and constraints for per-exercise effort and overall effort. This feature changes mutability of existing values, not the data model shape.

**Alternatives considered**:

- Add audit/version columns: rejected as outside the current specification, which asks for correcting values, not edit history.
- Add a separate corrections table: rejected because current comparisons and charts read the source session rows; a separate table would complicate every historical read path.

## Decision 3: Update by `loggedExerciseId` and preserve session structure

**Decision**: The update request identifies exercise rows by `loggedExerciseId`. The endpoint validates that every row belongs to the target session and updates only `loggedWeight` and `effort`.

**Rationale**: `loggedExerciseId` is the stable identity for a specific row in a completed session. Using it avoids ambiguity if an exercise appears more than once historically and preserves the requirement that date, workout identity, exercise order, and exercise membership are unchanged.

**Alternatives considered**:

- Update by `exerciseId`: rejected because duplicate exercise appearances could be ambiguous.
- Replace all logged exercises: rejected because the feature must not add/remove/reorder exercises and replacement increases the risk of losing row identity.
- Allow partial row updates only: rejected for the primary save path because the UI edits a full session snapshot and FR-011 requires one consistent update. The server may accept a subset for resilience, but every provided row must belong to the session.

## Decision 4: Reuse session-detail page edit mode

**Decision**: Add view/edit mode toggling to `session-detail.ts`, rendering editable inputs in the existing table and editable overall effort in the existing summary row.

**Rationale**: The user naturally reaches past workouts through history detail pages. Reusing the existing detail surface preserves context and avoids creating a parallel edit page with duplicated loading, chart, delete, and back-navigation behavior.

**Alternatives considered**:

- Add a separate `/history/session/edit` route: rejected because it duplicates detail-page data loading and navigation while offering no additional user value.
- Use an edit modal: rejected because a full session table with many exercise values is easier to review and correct inline on the detail page.

## Decision 5: Reuse established effort controls and discard-confirmation pattern

**Decision**: Use the existing 1-10 effort scale/labels for both exercise and overall effort. Use original-value snapshots plus the discard confirmation modal pattern established by feature 023 to protect unsaved changes.

**Rationale**: Feature 016 requires overall effort to match the per-exercise scale, and feature 023 already defines how edit flows warn before discarding changed values. Reusing both patterns keeps behavior predictable and minimizes new UI concepts.

**Alternatives considered**:

- Use numeric text boxes for effort: rejected because it would not match existing effort interaction patterns.
- Close edit mode immediately on cancel: rejected because this would violate the user's explicit need to avoid accidental historical changes.
- Add browser-native `confirm()`: rejected because the app already has accessible custom discard modals.

## Decision 6: Historical comparisons update by source-of-truth data

**Decision**: After saving edits, reload or re-render the session detail data from the updated session source; downstream "last time", previous comparison, and chart views naturally reflect corrected values through existing read endpoints.

**Rationale**: Feature 029 centralized historical comparison behavior around source session rows and bounded lookup rules. Updating the original rows keeps all consumers consistent without adding cache invalidation or derived correction logic.

**Alternatives considered**:

- Patch only visible DOM values after save: rejected because charts and comparison data would risk drifting from source state.
- Maintain client-side correction overlays: rejected because it duplicates server data and complicates every history consumer.

## Decision 7: Use backend integration plus Playwright coverage

**Decision**: Prove persistence and validation with `SessionApiTests.cs`, and prove user journeys with `WorkoutHistoryTests.cs` Playwright tests.

**Rationale**: Existing plans use backend integration tests for session endpoint behavior and Playwright for history/session-detail UI flows. This feature affects both API mutation semantics and browser edit interactions.

**Alternatives considered**:

- Use only API tests: rejected because cancel/discard/edit-mode accessibility and rendering behavior are user-facing.
- Use only E2E tests: rejected because server validation edge cases need precise, fast regression coverage.
