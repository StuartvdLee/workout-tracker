# Quickstart: Sidebar Icons for Workouts and Exercises

## 1. Implement

Edit `src/WorkoutTracker.Web/wwwroot/index.html`:

1. Locate the `<svg>` block inside the `<a ... data-page="workouts">` anchor and replace its entire contents with the `dumbbell` paths from [contracts/ui-contract.md](./contracts/ui-contract.md).
2. Locate the `<svg>` block inside the `<a ... data-page="exercises">` anchor and replace its entire contents with the `sport-shoe` paths from [contracts/ui-contract.md](./contracts/ui-contract.md).
3. Confirm the SVG wrapper attributes (`class`, `width`, `height`, `viewBox`, `fill`, `stroke`, `stroke-width`, `stroke-linecap`, `stroke-linejoin`, `aria-hidden`) are unchanged on both elements.
4. Do not touch any other sidebar icon SVG blocks.

## 2. Verify Build (no regressions)

From repository root:

```bash
cd src/WorkoutTracker.Web && npm run build && npm test
```

Expected: TypeScript compiles without errors; all Vitest tests pass.

## 3. Manual Smoke Check

1. Open the application in a browser.
2. Confirm the "Workouts" sidebar item displays the dumbbell icon (two weighted bar ends).
3. Confirm the "Exercises" sidebar item displays the sport shoe icon.
4. Confirm "Let's go!", "Muscles", and "History" sidebar icons are unchanged.
5. Toggle between light and dark themes; confirm both new icons inherit the correct colour.
6. Resize to a narrow viewport; confirm both icons remain correctly sized and aligned.

## 4. Ready for Task Breakdown

After checks pass, proceed with:

```text
/speckit.tasks
```
