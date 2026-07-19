# Quickstart: Latest Exercise Data

**Feature**: `029-latest-exercise-data`  
**Date**: 2026-07-19

## Implementation Steps

1. Update `src/WorkoutTracker.Api/Program.cs` for `GET /api/workouts/{workoutId}/previous-performance`.
   - Keep the route and 404 behavior unchanged.
   - Query sessions for the requested planned workout newest-first using `EF.Property<DateTime>(ws, "CompletedAt")`.
   - Select the latest usable logged exercise independently per current workout exercise.
   - Treat non-blank `loggedWeight` or non-null `effort` as usable.
   - Skip rows with no comparison data, even when they include `sequence`.

2. Update `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
   - Extend `PreviousExerciseData` with `completedAt` if consumed.
   - Keep mapping by `exerciseId`.
   - Ensure sequence alone cannot produce a "Last time:" success display.
   - Preserve existing labels and error handling.

3. Update tests.
   - Add backend regression coverage in `SessionApiTests.cs` for fallback over skipped/empty latest rows in active-session and session-detail comparison endpoints.
   - Update existing previous-performance tests whose expectations assumed one immediately previous session.
   - Update `WebAppFixture.cs` so E2E mock behavior matches the API semantics.
   - Add Playwright coverage in `WorkoutHistoryTests.cs` for active-session fallback when the most recent session skipped an exercise.

## Verification

Run targeted validation:

```bash
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj --filter "PreviousPerformance|SessionDetail"
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj --filter WorkoutHistory
cd src/WorkoutTracker.Web && npm run build && npm test
```

Expected manual smoke flow:

1. Complete a workout with an exercise and recorded weight or effort.
2. Complete the same planned workout again while skipping that exercise or saving it without comparison data.
3. Start the same planned workout.
4. Confirm the exercise still shows the older meaningful "Last time:" data.
5. Open the newest session in history and confirm the previous weight/effort columns also show the older meaningful data.
