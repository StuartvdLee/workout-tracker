# UI Contract: Workout Statistics Dashboard

## Navigation and periods

Expose the dashboard through the existing workout/history navigation and register a page route consistent with the current SPA. On entry, select the current local Monday–Sunday week. Offer labeled Week, Month and Year controls, Previous, Next, and Current. Changing kind selects its current period; Previous/Next traverse adjacent periods; disable Next at current and never display future periods. Current returns to the current period of the selected kind. Show the inclusive local range for a week, the month name for a month, or the year for a year.

Use native, keyboard-operable controls with programmatic selected/disabled states and visible focus. Announce loading and result/error changes appropriately without moving focus unnecessarily. Recompute boundaries on re-entry using the current browser local zone.

## Statistics and states

- Show **Completed workouts** and its exact count for a nonempty period.
- Show **Average workouts per week/month/year** for the selected period kind, using completed workouts from the first recorded workout through the end of the selected period divided by the number of local calendar periods, including periods without workouts. When no workouts have been recorded through that period, show an unavailable marker.
- If no workouts exist in the selected period, show a useful no-activity message.
- While loading, avoid presenting stale values as the newly selected period. On failure, show a clear error and Retry, not the empty state. A late prior response must not replace the current selection.
- Every visual statistic must have equivalent labeled text values without color dependence.

Follow existing typography, controls, visual tokens and history conventions. At narrow viewports controls and text remain readable without clipping essential content. A fresh read on return after add/edit/delete reflects current history.
