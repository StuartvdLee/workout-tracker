# Quickstart: Sticky Exercise Column

**Feature**: `033-sticky-exercise-column`
**Branch**: `033-sticky-exercise-column`

## Implementation Order

1. Update the existing session-detail table styles so the Exercise header and exercise-name cells remain anchored to the left edge of `.session-detail__table-wrapper`.
2. Give the fixed header and body cells the existing light-grey header-row surface colour.
3. Add explicit layering and a subtle trailing divider so scrolling statistics disappear behind the fixed column; remove the header-cell bottom border while retaining body-row dividers.
4. Use automatic table layout with a shrink-to-content first column (`width: 1%` on the first header/body cells), `white-space: nowrap`, trailing padding, and `max-width: min(20rem, 55vw)`. Names beyond the cap truncate with an ellipsis and retain full `aria-label`/`title` values; preserve horizontal overflow plus both view/edit table markup.
5. Add focused Playwright regression coverage in `WorkoutHistoryTests.cs` for position, scroll reach, opacity/layering, and edit mode.

## Validation Commands

From the repository root:

```bash
# Frontend static build and unit regression tests
cd src/WorkoutTracker.Web && npm run build && npm test

# Focused browser tests for session-detail table behavior
dotnet run --project src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj -- -noLogo -maxThreads 1 --filter SessionDetailPage

# Full E2E regression suite when targeted tests pass
dotnet run --project src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj -- -noLogo -maxThreads 1

# Formatting/whitespace validation
git diff --check
```

If the repository's xUnit runner requires executable-project invocation in the current environment, use the established equivalent:

```bash
dotnet run --project src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj -- -noLogo -maxThreads 1
```

## Regression Baseline Isolation

After adding the new Playwright tests, prove they fail before the sticky-column implementation without disturbing the current uncommitted stylesheet:

1. Compute the branch-point commit with `git merge-base HEAD main`.
2. Create a detached temporary worktree at that commit.
3. Export and review a patch containing only the new changes to `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
4. Apply that test-only patch in the temporary worktree and run the focused session-detail tests there; record the expected failure.
5. Remove the temporary worktree.
6. Run the same tests in the current worktree and record the passing result.

Do not stash, reset, overwrite, or discard `src/WorkoutTracker.Web/wwwroot/css/styles.css` during this procedure.

## Automated Verification Checklist

- The table overflows at the test viewport.
- Horizontal scrolling moves statistic columns.
- At zero, 25%, 50%, 75%, and maximum offsets, Exercise header and first-column cells remain within 2 CSS pixels of the wrapper's left edge.
- Returning to zero leaves no offset artifact.
- Fixed header/body backgrounds are opaque and use the shared light-grey header-row surface.
- Fixed cells layer above ordinary statistic cells.
- The header row has no bottom border; body-row separators remain.
- A 150-character exercise name is capped, ellipsized, and exposes its full value through `aria-label` and `title`.
- Maximum horizontal scroll exposes the complete last column, including with a maximum-length exercise name.
- View mode and edit mode both preserve the fixed column.
- Existing seven-column headers and exercise-row assertions continue to pass.

## 50-Row Scroll-to-Paint Protocol

1. Load a past workout table containing exactly 50 exercise rows.
2. In the browser, measure from assigning each target `scrollLeft` value until two consecutive animation frames have completed.
3. Sample 20 interactions across the zero, 25%, 50%, 75%, and maximum offsets, using four samples per offset.
4. Pass when at least 19 of 20 samples complete within 100 milliseconds.
5. Sort the 20 durations and record the p95 value using the 19th ordered sample.
6. Repeat the protocol in resolved light and dark themes.
7. Record the date, browser/environment, all 20 samples, p95, and pass/fail result below.

### Performance Verification Record

- Date: 2026-09-19
- Environment: Playwright Chromium, headless, macOS; 50 exercise rows; two animation frames per sample
- Light theme samples (ms): 6.90, 14.60, 16.60, 18.60, 16.90, 14.50, 16.60, 16.70, 16.70, 16.50, 16.80, 16.70, 16.60, 16.80, 16.60, 16.60, 16.70, 16.60, 16.70, 16.60
- Light theme p95: 16.90 ms
- Dark theme samples (ms): 4.20, 16.70, 16.70, 16.60, 16.70, 16.70, 16.60, 16.70, 16.70, 16.70, 16.50, 16.70, 16.70, 16.70, 16.60, 16.70, 16.70, 16.70, 18.20, 15.10
- Dark theme p95: 16.70 ms
- Result: PASS — 20/20 samples completed within 100 ms in both themes
- Input observations: Programmatic horizontal scrolling, zero/intermediate/maximum boundaries, view mode, and edit mode passed. Physical trackpad/touch hardware was not available in the headless test environment.

## Manual Smoke Check

1. Open a past workout containing 50 exercises.
2. Use zero, 25%, 50%, 75%, and maximum horizontal scroll offsets.
3. Repeat with a mouse/trackpad, touch gesture, horizontal scrollbar, and keyboard where supported.
4. Confirm exercise names remain visible and statistics disappear behind them.
5. Confirm the last statistic column can be read completely.
6. Enter edit mode and repeat the scroll check.
7. Repeat in light and dark themes; system preference is covered by verifying both resolved theme states.
8. Return the table to the zero offset and confirm its original appearance with no offset artifact.

## Delivered Implementation Notes

- Runtime changes are limited to the existing session-detail CSS and browser regression tests.
- The first column uses native sticky positioning, `table-layout: auto`, `width: 1%` shrink-to-content hints, `white-space: nowrap`, trailing padding, and a `min(20rem, 55vw)` maximum with accessible full-name attributes.
- The fixed column uses the same light-grey theme surface as the header row; the header/body divider is removed, while body-row dividers remain.
- View-mode and edit-mode coverage, theme checks, long-name sizing, final-column reachability, and the 50-row performance budget were completed.
