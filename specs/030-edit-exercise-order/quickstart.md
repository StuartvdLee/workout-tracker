# Quickstart: Edit Exercise Order in Current Workout

## Goal

Verify that a user can change the order of exercises while in a current workout without losing entered weight or effort data.

## Scenario

1. Create or open a workout with at least three exercises.
2. Start the workout so the current workout screen opens.
3. Confirm the top-right area of the current workout header shows **Edit order**.
4. Enter a weight and effort for the first exercise.
5. Select **Edit order**.
6. Confirm the exercise list collapses to name-only rows.
7. Confirm weight, effort, target, and previous-performance details are hidden.
8. Drag the first exercise below the last exercise.
9. Select **Done**.
10. Confirm the normal current workout view returns in the new order.
11. Confirm the weight and effort values entered before reordering are still associated with the same exercise.
12. Save the workout.
13. Confirm the saved session reflects the reordered exercise sequence.

## Keyboard Scenario

1. Start a workout with at least three exercises.
2. Select **Edit order**.
3. Focus the drag handle for an exercise.
4. Press `Space` to pick up the exercise.
5. Press `ArrowDown` or `ArrowUp` to move it.
6. Press `Enter` or `Space` to drop it.
7. Confirm the visible order changed and a screen-reader announcement is produced.

## Touch Scenario

1. Use a mobile-sized viewport or touch-capable browser.
2. Start a workout with at least three exercises.
3. Select **Edit order**.
4. Touch and drag a handle.
5. Confirm a drag clone follows the finger and rows shift live.
6. Release to drop and confirm the updated order.

## Validation Commands

From `src/WorkoutTracker.Web`:

```bash
npm test
npm run build
```

From `src`:

```bash
dotnet test WorkoutTracker.slnx --filter "FullyQualifiedName~WorkoutReorderTests"
```

Run broader `dotnet test WorkoutTracker.slnx` if targeted E2E or build validation reveals shared regressions.
