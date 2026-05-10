# Data Model: History Page Entry Redesign

**Branch**: `012-history-entry-design` | **Date**: 2026-05-10

## Overview

No database schema changes. No new API contracts. No localStorage additions.

This feature is a **pure presentational change** — the same `WorkoutSession` data already returned by `GET /api/sessions` is reformatted for display.

---

## Frontend Visual State Model

### History Entry (per `WorkoutSession`)

| State | Visible Elements |
|-------|-----------------|
| Collapsed (default) | Workout name (bold), date + time (muted), exercise count, expand chevron |
| Expanded | All of the above + exercise detail rows |

### Data Used Per Entry

| Field | Source | Displayed As |
|-------|--------|-------------|
| `workoutName` | `WorkoutSession.workoutName` | Bold primary text in `.history-session__workout-name` |
| `completedAt` | `WorkoutSession.completedAt` (ISO 8601 string) | Formatted date + time in `.history-session__date` |
| `loggedExercises.length` | `WorkoutSession.loggedExercises` array | Exercise count in `.history-session__exercise-count` |
| `loggedExercises[*]` | Exercise detail objects | Expanded detail rows in `.history-session__details` |

### Date Formatting Rule

```
formatDate(completedAt):
  datePart = Intl.DateTimeFormat("en-GB", {
    day: "numeric", month: "long", year: "numeric"
  }).format(new Date(completedAt))
  → "10 May 2026"

  timePart = Intl.DateTimeFormat("en-US", {
    hour: "numeric", minute: "2-digit", hour12: true
  }).format(new Date(completedAt))
  → "2:30 PM"

  result = `${datePart} · ${timePart}`
  → "10 May 2026 · 2:30 PM"
```

---

## Removed State

The following grouping state is **removed** and no longer exists:

- `getDateLabel(isoDate)` → `"Today" | "Yesterday" | "X days ago"` (deleted)
- `groups: { label: string; sessions: WorkoutSession[] }[]` accumulator (deleted)
- `.history-group` DOM wrapper elements (deleted)
- `.history-group__date-label` DOM label elements (deleted)
