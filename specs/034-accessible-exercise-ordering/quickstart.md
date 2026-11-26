# Quickstart: Accessible Exercise Ordering While Scrolling

## Prerequisites

- .NET 10 SDK, Node.js, installed project dependencies and Playwright browser dependencies.
- For interactive application use, the PostgreSQL/Aspire setup in `README.md`.

## Automated validation

From the repository root:

```bash
cd src/WorkoutTracker.Web
npm test
npm run build
```

From `src`:

```bash
dotnet test --project WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj -- --filter-class '*WorkoutReorderTests'
```

The focused suite covers the automated scroll positions and regression scenarios in [plan.md](./plan.md). The geometry regression was observed failing on the pre-change implementation and passing after implementation. Run broader suites if shared regressions appear. See [tasks.md](./tasks.md) for the recorded results and outstanding manual checks.

## Manual end-to-end checks

1. Start the application using the instructions in `README.md`. Create a workout long enough to scroll, start the workout, and enter weight and effort for an exercise.
2. At the top, middle, and bottom of the page, locate the reorder icon. On mobile, confirm it floats prominently above full-width content and fields/actions remain reachable by scrolling; on desktop, confirm it does not cover controls. Activate it from each position; the existing collapsed editor opens.
3. Reorder an exercise and select Done. Confirm the new sequence and the previously entered values remain attached to the same exercise. Separately verify Cancel without edits and both outcomes of the changed-order confirmation.
4. Repeat the viewport/zoom matrix in [spec.md](./spec.md), in light and dark themes, using keyboard and touch where applicable. Verify focus visibility, Enter/Space activation, the fixed mobile top bar, and modal layering. Use a screen reader to confirm the button is announced as **Edit order**, button.
5. Test short, empty, single-exercise and 50-exercise workouts, including long names; verify the action is hidden on blocking load failure but available after non-blocking previous-performance and session-save failures. Record the 20 scroll, entry and exit samples and continuous-scroll flicker check described in [plan.md](./plan.md).
6. Conduct and record the independent discoverability check described in the plan; do not infer its result from automated tests.
