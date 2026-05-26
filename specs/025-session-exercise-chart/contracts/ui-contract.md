# UI Contract: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Date**: 2026-05-26

## Overview

A chart section is appended below the Overall Effort row on the session detail page, but **only when `session.plannedWorkoutId` is non-null**. Ad-hoc sessions (no planned workout) show no chart section.

---

## DOM Structure

```html
<!-- Appended inside #session-detail-content, after the overall-effort row -->
<div class="session-chart">
  <div class="session-chart__header">
    <label class="session-chart__label" for="session-chart-select">Show history for</label>
    <select class="session-chart__select" id="session-chart-select" aria-label="Select chart data">
      <option value="overall-effort">Overall Session Effort</option>
      <!-- One pair per exercise in the current session: -->
      <option value="exercise:{exerciseId}:weight">{exerciseName} – Weight</option>
      <option value="exercise:{exerciseId}:effort">{exerciseName} – Effort</option>
    </select>
  </div>

  <!-- Loading state (while trends fetch is in flight) -->
  <div class="session-chart__loading" id="session-chart-loading">Loading chart…</div>

  <!-- Error state -->
  <div class="session-chart__error" id="session-chart-error" style="display:none;" role="alert" aria-live="polite"></div>

  <!-- Chart container — holds the rendered SVG -->
  <div class="session-chart__container" id="session-chart-container" style="display:none;" aria-label="Chart: {selected series label}"></div>

  <!-- Empty state — shown when selected series has no data points -->
  <div class="session-chart__empty" id="session-chart-empty" style="display:none;">No data available for this selection.</div>
</div>
```

---

## Dropdown Options

Options are built from the session already loaded in memory:

| Value pattern                       | Display text                | Condition                                 |
|-------------------------------------|-----------------------------|-------------------------------------------|
| `"overall-effort"`                  | Overall Session Effort      | Always first option                       |
| `"exercise:{id}:weight"`            | `{exerciseName} – Weight`   | One per exercise in current session       |
| `"exercise:{id}:effort"`            | `{exerciseName} – Effort`   | One per exercise in current session       |

Exercise options are ordered by `sequence` (same order as the exercises table). The first option is selected by default.

---

## SVG Line Chart Structure

The chart is rendered as an inline SVG inside `#session-chart-container`:

```html
<svg class="session-chart__svg"
     viewBox="0 0 600 260"
     preserveAspectRatio="xMidYMid meet"
     role="img"
     aria-label="{series label} over time">

  <!-- Y-axis gridlines and tick labels -->
  <g class="session-chart__y-axis">
    <line class="session-chart__axis-line" x1="50" y1="20" x2="50" y2="220" />
    <!-- Repeated per tick: -->
    <line class="session-chart__gridline" x1="50" y1="{y}" x2="580" y2="{y}" />
    <text class="session-chart__tick-label" x="44" y="{y+4}" text-anchor="end">{value}</text>
  </g>

  <!-- X-axis line and date labels -->
  <g class="session-chart__x-axis">
    <line class="session-chart__axis-line" x1="50" y1="220" x2="580" y2="220" />
    <!-- Repeated per data point (or thinned if many): -->
    <text class="session-chart__date-label" x="{x}" y="240" text-anchor="middle">{date}</text>
  </g>

  <!-- Data series -->
  <g class="session-chart__series">
    <polyline class="session-chart__line"
              points="{x1,y1 x2,y2 ...}" />
    <!-- One circle per data point: -->
    <circle class="session-chart__point" cx="{x}" cy="{y}" r="4" />
  </g>
</svg>
```

### viewBox Layout

| Region      | x range  | y range  | Purpose              |
|-------------|----------|----------|----------------------|
| Left margin | 0–50     | —        | Y-axis labels        |
| Plot area   | 50–580   | 20–220   | Chart lines + points |
| Bottom      | —        | 220–260  | X-axis date labels   |

---

## Chart Math (Extracted to `utils.ts`)

### `normaliseValue(value: number, min: number, max: number): number`

Maps a data value to a Y coordinate within the plot area (20–220, top-to-bottom inversion):

```
If min === max: return 120 (midpoint — flat line case)
plotY = 220 - ((value - min) / (max - min)) * 200
```

Degenerate case: when all values are equal, clamp range to `[value - 1, value + 1]` before calling.

### `buildYTicks(min: number, max: number, tickCount: number): number[]`

Returns `tickCount` evenly spaced tick values between `min` and `max`, inclusive. For effort series, called with `min=0, max=10, tickCount=6`.

### `buildXLabels(dates: string[], maxLabels: number): (string | null)[]`

Returns an array of the same length as `dates`. Labels are shown at evenly spaced intervals up to `maxLabels`; intermediate items are `null` (no label rendered). Formatted as `"DD MMM"` (e.g., "01 Apr").

---

## States

| State    | Element visible                          | Trigger                                              |
|----------|------------------------------------------|------------------------------------------------------|
| Loading  | `#session-chart-loading`                 | Trends fetch in flight after session detail loads    |
| Success  | `#session-chart-container` (with SVG)   | Trends data available and selected series has points |
| Empty    | `#session-chart-empty`                  | Selected series has zero plottable data points       |
| Error    | `#session-chart-error`                  | Trends fetch fails (network error, non-200 response) |

Only one state is visible at a time. Transitions are driven by toggling `display:none`.

---

## CSS Classes (`.session-chart__*`)

| Class                          | Purpose                                          |
|--------------------------------|--------------------------------------------------|
| `.session-chart`               | Outer container; card-style, margin-top spacing  |
| `.session-chart__header`       | Row containing label + select                    |
| `.session-chart__label`        | "Show history for" label text                    |
| `.session-chart__select`       | Dropdown; reuses existing `<select>` token styles|
| `.session-chart__loading`      | Loading text; uses `--color-text-light`          |
| `.session-chart__error`        | Error text; uses `--color-error`                 |
| `.session-chart__empty`        | Empty state text; uses `--color-text-light`      |
| `.session-chart__container`    | SVG wrapper; `width: 100%; overflow-x: auto`     |
| `.session-chart__svg`          | `width: 100%; height: auto`                      |
| `.session-chart__axis-line`    | Axis border lines; `--color-border`              |
| `.session-chart__gridline`     | Horizontal gridlines; `--color-border` at 50% opacity |
| `.session-chart__tick-label`   | Y-axis value labels; `--color-text-light`, small font |
| `.session-chart__date-label`   | X-axis date labels; `--color-text-light`, small font |
| `.session-chart__line`         | Polyline; `--color-primary`, stroke-width 2, no fill |
| `.session-chart__point`        | Circle data points; fill `--color-primary`       |

---

## Accessibility

- The `<svg>` element has `role="img"` and `aria-label` describing the current series.
- Error and empty containers use `role="alert"` with `aria-live="polite"`.
- The `<select>` has an associated `<label>` via `for`/`id`.
- Tick and date labels are SVG `<text>` elements — readable by assistive technology.

---

## Behaviour on Dropdown Change

1. User selects a new option.
2. If trends data is still loading, wait for it (dropdown is not disabled — user selection is remembered).
3. Filter the loaded `SessionTrendItem[]` to the selected series, build `ChartPoint[]`.
4. If zero points: show empty state.
5. Else: render SVG and show chart container.
6. Update `aria-label` on `#session-chart-container` to reflect new series name.
