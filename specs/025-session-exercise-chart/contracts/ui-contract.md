# UI Contract: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Date**: 2026-05-27

## Overview

A chart section is appended below the overall-effort row on session detail, only when `plannedWorkoutId` is non-null.

## DOM Structure (implemented)

```html
<div class="session-chart" id="session-chart">
  <div class="session-chart__header">
    <label class="session-chart__label" for="session-chart-select">Show:</label>
    <select class="session-chart__select" id="session-chart-select"></select>
  </div>
  <div id="session-chart-body">
    <!-- one of: loading, empty, error, or rendered chart -->
  </div>
</div>
```

## Dropdown Options

| Value pattern | Display text | Notes |
|---|---|---|
| `overall` | Overall Session Effort | always first/default |
| `exercise:{exerciseId}` | `{exerciseName}` | one option per exercise in current session |

## Rendering Modes

### 1) Overall mode
- Single spikey polyline (straight segments) using overall effort values.
- Fixed y-axis range 0–10.

### 2) Exercise mode (combined)
- Two lines in the same chart:
  - weight line: blue (`.session-chart__line--weight`)
  - effort line: red (`.session-chart__line--effort`)
- Weight uses left y-axis (dynamic min/max, degenerate handled via ±1).
- Effort uses right y-axis (fixed 0–10).
- Legend is shown above chart for this mode.

## State Behavior

1. Initial fallback render uses current session data so dropdown is enabled immediately.
2. Trends fetch runs in background and replaces chart data on success.
3. If trends fetch fails, fallback chart remains visible.
4. Empty state is shown when selected series has no plottable data.

## Accessibility

- `label[for="session-chart-select"]` ↔ `#session-chart-select`.
- Error markup uses `role="alert"` and `aria-live="polite"` when shown.
