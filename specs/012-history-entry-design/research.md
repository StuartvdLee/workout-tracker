# Research: History Page Entry Redesign

**Branch**: `012-history-entry-design` | **Date**: 2026-05-10

## Q1: Does the API need changes to support the new layout?

**Decision**: No API changes required.

**Rationale**: `GET /api/sessions` already returns `workoutName` and `completedAt` on every `WorkoutSession` object. The current `renderSession()` function already receives both fields — it just doesn't format a full date, only a time-of-day. The new layout reuses the same data with a different formatting function.

**Alternatives considered**: None — the data is already present.

---

## Q2: What date format should be used for the secondary line?

**Decision**: `Intl.DateTimeFormat("en-GB", { day: "numeric", month: "long", year: "numeric" })` combined with the existing `formatTime()` output, separated by " · ".

Example output: `"10 May 2026 · 2:30 PM"`

**Rationale**: 
- The weekday (e.g., "Monday") was initially considered relevant but dropped at user request — the date alone provides sufficient temporal context.
- `en-GB` is consistent with feature 009 (`last-workout-hint`) which also used `en-GB` locale for dates.
- The existing `formatTime()` function already produces a clean "2:30 PM" string using `en-US` locale — retaining it avoids duplication and provides precision when multiple sessions occur on the same day.
- The " · " separator (middle dot with spaces) is a common typographic convention for combining date and time in compact secondary labels.

**Alternatives considered**:
- Including weekday ("Monday, 10 May 2026"): considered initially, dropped — user found it unnecessary.
- Date only (no time): rejected — the time provides precision when two sessions occur on the same day.
- `toLocaleDateString()` without locale: rejected — produces inconsistent output across browsers and OS locales.
- Relative dates kept for recent entries ("Yesterday"): rejected — this was the explicit problem being solved.

---

## Q3: How does the header layout accommodate the stacked name + date?

**Decision**: Wrap `workout-name` and `date` in a `<div class="history-session__info">` flex-column container that owns the `flex: 1; min-width: 0` properties previously on `workout-name`. The exercise count and toggle remain as inline flex siblings to the right.

**Rationale**: The existing `.history-session__header` is `display: flex; align-items: center; justify-content: space-between`. Adding a column-direction wrapper on the left side achieves the stacked layout without restructuring the outer flex row. This is the same pattern used in many mobile-first list components (e.g., iOS UITableViewCell primary/secondary label).

**Alternatives considered**:
- CSS Grid on the header: more powerful but overkill for a two-row, two-column layout.
- Absolute positioning for the date: would break natural flow and responsive behaviour.

---

## Q4: Which E2E tests are affected?

**Decision**: One test must be removed and replaced; all others are unaffected.

| Test | Selector Used | Impact |
|------|--------------|--------|
| `HistoryPage_DateGrouping_ShowsToday` | `.history-group__date-label` | **REMOVE** — CSS class deleted |
| `HistoryPage_WithSessions_ShowsSessions` | `.history-session` | ✅ Unaffected |
| `HistoryPage_SessionExpandCollapse` | `.history-session__header` | ✅ Unaffected |
| `HistoryPage_SessionDetails_ShowsExerciseData` | `.history-session__exercise-name`, `.history-session__exercise-data` | ✅ Unaffected |
| `HistoryPage_EmptyState_ShowsMessage` | `#history-empty` | ✅ Unaffected |
| `HistoryPage_LoadingState_ShownInitially` | `#history-loading` | ✅ Unaffected |
| `HistoryPage_HasH1Heading` | `.history-page__title` | ✅ Unaffected |

**New tests added**:
- `HistoryPage_NoGroupHeaders_FlatList` — asserts `.history-group__date-label` count = 0
- `HistoryPage_EntryShowsDateBelowName` — asserts `.history-session__date` is visible with non-empty text

---

## Q5: Any CSS variable or token concerns?

**Decision**: Use existing tokens only — no new design tokens needed.

- `.history-session__date` uses `var(--color-text-light)` and `var(--font-size-sm)` — identical to the existing `.history-session__time` and `.history-session__exercise-count` styles.
- `gap: 2px` (hardcoded) is used for the small spacing between name and date within the info block. A spacing token (`var(--spacing-xs)`) could be used but `2px` is a tighter visual separation appropriate for a label-sublabel relationship. This is consistent with how `gap: 2px` appears in other list-item designs in the codebase.
