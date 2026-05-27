# Research: Session Exercise Chart

**Feature**: 025-session-exercise-chart  
**Phase**: 0 — Outline & Research  
**Date**: 2026-05-26

## R-001: Chart Rendering Approach

**Decision**: Inline SVG, drawn programmatically in TypeScript — no external library.

**Rationale**: The project has zero external JS runtime dependencies (vanilla TypeScript, no npm runtime packages). Introducing a charting library (Chart.js, etc.) would add bundle weight, a build step change, and a dependency maintenance burden inconsistent with the project's philosophy. SVG is natively supported in all modern browsers, integrates cleanly with the existing CSS token system, is accessible (text labels in DOM), and is fully responsive via `viewBox` + `preserveAspectRatio`. The chart requirements (overall single-series mode plus exercise combined dual-series mode, with line + dots and date labels) are straightforward enough that hand-authored SVG is proportionate.

**Alternatives considered**:
- **Chart.js via CDN**: rejected — introduces CDN dependency, versioning risk, inconsistent with project's no-CDN pattern.
- **Canvas API**: rejected — less accessible (no DOM text nodes for screen readers), harder to integrate with CSS tokens, harder to write unit tests for coordinate math with jsdom.
- **D3.js**: rejected — significant bundle size and API surface area for a single line chart.

---

## R-002: API Endpoint Shape

**Decision**: New endpoint `GET /api/workouts/{workoutId}/session-trends`.

**Rationale**: The session detail page already holds `plannedWorkoutId` from the existing `GET /api/sessions/{sessionId}` response. A dedicated trends endpoint scoped to the planned workout keeps the query bounded, returns exactly what the chart needs, and follows the existing endpoint-per-concern pattern (cf. `GET /api/workouts/{workoutId}/previous-performance`). Reusing `GET /api/sessions` (which returns all sessions) would require client-side filtering and return unnecessary data.

**Response shape**:
```json
{
  "dataPoints": [
    {
      "completedAt": "2026-05-01T10:00:00Z",
      "overallEffort": 7,
      "exercises": [
        {
          "exerciseId": "guid",
          "exerciseName": "Bench Press",
          "loggedWeight": "80",
          "effort": 6
        }
      ]
    }
  ]
}
```

**Ordering**: Results returned chronologically (oldest first) for natural left-to-right chart rendering.

**Cap**: `Take(50)` most recent sessions (ordered descending, then reversed to chronological). A workout done twice weekly for 6 months produces ~52 sessions — capping at 50 keeps the query and SVG bounded while covering a full training cycle.

---

## R-003: Shadow Property Access Pattern

**Decision**: Always use `EF.Property<DateTime>(ws, "CompletedAt")` when ordering by or selecting `CompletedAt`.

**Rationale**: `CompletedAt` is configured as a shadow property on `WorkoutSession` with `HasDefaultValueSql("now()")`. It has no CLR property. All existing endpoints follow this pattern (lines 717, 744, 775, 805 of `Program.cs`). The new trends endpoint must follow this pattern or the code will not compile / ordering will be wrong.

---

## R-004: Dropdown Population Source

**Decision**: Populate dropdown from the *current session's exercises* (already loaded with `GET /api/sessions/{sessionId}`).

**Rationale**: The current session is the user's anchor — they are reviewing *this* session and want to understand how each exercise in *this* session has trended. Using the union of all historical exercises would include exercises removed from the workout long ago, producing confusing sparse series. The frontend already has the current session's exercise list; no additional fetch is needed for dropdown population. Each exercise appears once in the dropdown; selecting it renders both weight and effort lines together.

---

## R-005: Weight Parsing

**Decision**: Use `Number(value)` to convert `loggedWeight` strings to numeric values for chart plotting.

**Rationale**: `loggedWeight` is a free-text field (max 100 chars). Typical values are "80", "80.5". `Number('60abc')` returns `NaN` (detectable), whereas `parseFloat('60abc')` silently returns `60`, producing a wrong data point without any indication of an error. Sessions with `NaN` weight are excluded from the weight series with no plotting error. Sessions where the weight is a compound string (e.g., "80/85/90") would be excluded — this is acceptable because such entries are not meaningful as a single numeric trend value.

---

## R-006: Y-Axis Range Policy

**Decision**: Effort series use a fixed Y-axis of 0–10. Weight series use a dynamic Y-axis (data min/max with padding).

**Rationale**: Effort is bounded 1–10 by a DB check constraint. A fixed axis prevents visual inflation of small differences (e.g., 7–8 appearing as a full-height swing). Weight has no fixed upper bound and varies per exercise — dynamic axis is appropriate. When all weight data points are equal, `min === max`, the normalisation denominator would be zero; handle this by treating the range as `[value - 1, value + 1]` to show a flat line centred on the value.

---

## R-007: Trends Fetch Timing

**Decision**: Fetch trends data eagerly, immediately after `plannedWorkoutId` becomes available from the session detail response.

**Rationale**: `plannedWorkoutId` is not in the page URL — it is only known after `GET /api/sessions/{sessionId}` resolves. Lazy-on-first-interaction introduces a visible delay at the moment the user opens the dropdown. Eager-post-session-load starts the trends request as soon as the session data arrives, so by the time the user scrolls to the chart and interacts with the dropdown, the data is often already cached in a pending Promise. If `plannedWorkoutId` is null (ad-hoc session), the chart section is not rendered at all.

---

## R-008: Chart Section Rendering on Ad-Hoc Sessions

**Decision**: Do not render the chart section if `session.plannedWorkoutId` is null.

**Rationale**: Ad-hoc sessions (not linked to a planned workout) have no comparable historical sessions — there is nothing to trend. Rendering an empty chart or an error would be confusing. Omitting the section entirely is the clearest UX and avoids unnecessary API calls.

---

## Resolved Clarifications

All NEEDS CLARIFICATION items from Technical Context have been resolved. No open items remain.
