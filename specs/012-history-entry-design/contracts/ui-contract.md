# UI Contract: History Session Entry

**Branch**: `012-history-entry-design` | **Date**: 2026-05-10

## Overview

Defines the HTML structure, CSS classes, and ARIA attributes for a single history session entry in the redesigned History page.

---

## HTML Structure

```html
<!-- Flat list container — no .history-group wrappers -->
<div id="history-list">

  <!-- One .history-session per WorkoutSession, in DESC completedAt order -->
  <div class="history-session" data-session-id="{workoutSessionId}">

    <button
      class="history-session__header"
      type="button"
      aria-expanded="false"
      aria-controls="session-details-{workoutSessionId}"
    >
      <!-- Left: stacked name + date -->
      <div class="history-session__info">
        <span class="history-session__workout-name">{workoutName}</span>
        <span class="history-session__date">{formattedDate}</span>
      </div>

      <!-- Right: exercise count + expand chevron -->
      <span class="history-session__exercise-count">{N} exercise(s)</span>
      <span class="history-session__toggle">▸</span>
    </button>

    <div
      class="history-session__details"
      id="session-details-{workoutSessionId}"
      style="display:none;"
    >
      <!-- Exercise rows — unchanged from current implementation -->
      <div class="history-session__exercise">
        <span class="history-session__exercise-name">{exerciseName}</span>
        <span class="history-session__exercise-data">{data}</span>
      </div>
    </div>

  </div>

</div>
```

---

## CSS Classes

| Class | Element | Role |
|-------|---------|------|
| `.history-session` | `<div>` | Card container for one session |
| `.history-session__header` | `<button>` | Clickable expand/collapse trigger; outer flex row |
| `.history-session__info` | `<div>` | Flex-column wrapper for name + date (owns `flex: 1; min-width: 0`) |
| `.history-session__workout-name` | `<span>` | Bold primary label |
| `.history-session__date` | `<span>` | Muted secondary label — date + time |
| `.history-session__exercise-count` | `<span>` | Muted exercise count badge |
| `.history-session__toggle` | `<span>` | Chevron icon; rotates 180° when expanded |
| `.history-session--expanded` | modifier on `.history-session` | Applied on expand; drives toggle rotation + details visibility |
| `.history-session__details` | `<div>` | Collapsible exercise detail panel |
| `.history-session__exercise` | `<div>` | One exercise row |
| `.history-session__exercise-name` | `<span>` | Exercise name |
| `.history-session__exercise-data` | `<span>` | Weight, effort, notes |

### Removed Classes (no longer emitted)

| Class | Reason |
|-------|--------|
| `.history-group` | Grouping by date label removed |
| `.history-group__date-label` | Relative date header removed |
| `.history-session__time` | Merged into `.history-session__date` |

---

## CSS Specifications

```css
.history-session__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  cursor: pointer;
  min-height: var(--min-touch-target);
  transition: background-color 0.15s ease;
  background-color: transparent;
  border: none;
  width: 100%;
  text-align: left;
}

.history-session__info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.history-session__workout-name {
  font-size: var(--font-size-base);
  font-weight: 600;
  color: var(--color-text);
}

.history-session__date {
  font-size: var(--font-size-sm);
  color: var(--color-text-light);
}
```

---

## ARIA Contract

| Attribute | Element | Value |
|-----------|---------|-------|
| `aria-expanded` | `.history-session__header` | `"false"` (collapsed) / `"true"` (expanded) |
| `aria-controls` | `.history-session__header` | `"session-details-{workoutSessionId}"` |
| `id` | `.history-session__details` | `"session-details-{workoutSessionId}"` |

---

## Date Format

| Token | Format | Example |
|-------|--------|---------|
| Date part | `en-GB`, day numeric + month long + year numeric | `10 May 2026` |
| Time part | `en-US`, hour numeric + minute 2-digit + hour12 | `2:30 PM` |
| Combined | `{datePart} · {timePart}` | `10 May 2026 · 2:30 PM` |

All values are HTML-escaped via `escapeHtml()` before insertion.
