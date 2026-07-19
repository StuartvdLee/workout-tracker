# Quickstart: Edit Past Workouts

## Goal

Verify that a user can correct saved historical workout values from the session detail page and that all history/comparison views use the corrected data.

## Prerequisites

- PostgreSQL test database available through `TEST_DB_CONNECTION`.
- Existing workout with at least one completed session.
- Preferably two completed sessions of the same planned workout to verify previous-comparison behavior.

## Implementation Walkthrough

1. Add `PUT /api/sessions/{sessionId}` to `src/WorkoutTracker.Api/Program.cs`.
2. Add a `SessionUpdateRequest` DTO and logged-exercise update item DTO.
3. Validate:
   - session exists
   - `overallEffort` is null or 1-10
   - each provided `loggedExerciseId` belongs to the session
   - each exercise `effort` is null or 1-10
   - each `loggedWeight` is null or at most 100 characters
   - duplicate `loggedExerciseId` values are rejected
4. Update only `WorkoutSession.OverallEffort`, `LoggedExercise.LoggedWeight`, and `LoggedExercise.Effort`.
5. Return the updated session-detail response shape after save.
6. Add matching `PUT /api/sessions/{sessionId:guid}` proxy route in `src/WorkoutTracker.Web/Program.cs`.
7. Update `session-detail.ts`:
   - Add Edit button after successful load.
   - Capture original editable values when entering edit mode.
   - Render editable weight and effort controls.
   - Render editable overall effort.
   - Add Save/Cancel and discard-confirmation behavior.
   - Re-render updated view mode from the save response.
8. Add CSS for session-detail edit controls, action row, disabled/saving state, and error messages.

## Manual Verification

1. Open History.
2. Open a completed workout detail page.
3. Select Edit.
4. Change one exercise weight.
5. Change one exercise effort.
6. Change or fill Overall Effort.
7. Save changes.
8. Confirm the detail page returns to view mode and shows the updated values.
9. Navigate away and reopen the same session; confirm values persist.
10. Open a later workout of the same planned workout; confirm previous/comparison values reflect the corrected historical data where applicable.

## Cancel/Discard Verification

1. Open a completed workout detail page.
2. Select Edit.
3. Change a weight or effort value.
4. Select Cancel.
5. Confirm the discard modal appears.
6. Select Keep editing and confirm the changed value remains.
7. Select Cancel again, then Discard.
8. Confirm the detail page returns to view mode with original saved values.

## Validation Verification

1. Try saving an exercise effort outside 1-10 through an API/integration test; expect `400`.
2. Try clearing a weight through the UI; expect the request payload to send `loggedWeight: null` and view mode to show the no-data indicator.
3. Try saving overall effort outside 1-10 through an API/integration test; expect `400`.
4. Try saving a weight longer than 100 characters; expect `400`.
5. Try updating a `loggedExerciseId` from another session; expect `400`.
6. Try updating a missing session ID; expect `404`.

## Automated Tests

Recommended targeted commands once implemented:

```bash
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj --filter Session
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj --filter WorkoutHistory
```

If frontend helper functions are extracted, also run:

```bash
cd src/WorkoutTracker.Web && npm test
```
