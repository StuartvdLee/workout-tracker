# Quickstart: Sidebar History Icon Change

## 1. Implement

Edit `src/WorkoutTracker.Web/wwwroot/index.html`:

1. Locate the `<svg>` block inside the `<a ... data-page="history">` anchor.
2. Replace the entire contents of that `<svg>` element with the `history` paths from [contracts/ui-contract.md](./contracts/ui-contract.md).
3. Confirm the SVG wrapper attributes (`class`, `width`, `height`, `viewBox`, `fill`, `stroke`, `stroke-width`, `stroke-linecap`, `stroke-linejoin`, `aria-hidden`) are unchanged.
4. Do not touch any other sidebar icon SVG blocks.

## 2. Verify Build (no regressions)

From repository root:

```bash
cd src/WorkoutTracker.Web && npm run build && npm test
```

Expected: TypeScript compiles without errors; all Vitest tests pass.

## 3. Manual Smoke Check

1. Open the application in a browser.
2. Confirm the "History" sidebar item displays the Lucide history icon (circular back-arrow with clock hands).
3. Confirm "Let's go!", "Workouts", "Exercises", and "Muscles" sidebar icons are unchanged.
4. Toggle between light and dark themes; confirm the new icon inherits the correct colour.
5. Resize to a narrow viewport; confirm the icon remains correctly sized and aligned.

## 4. Ready for Task Breakdown

After checks pass, proceed with:

```text
/speckit.tasks
```
