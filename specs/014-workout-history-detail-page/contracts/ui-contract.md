# UI Contract: Session Detail Page

**Feature**: `014-workout-history-detail-page`
**Route**: `/history/session?id=<sessionId>`
**Page module**: `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`

---

## Page Structure

### Root HTML Skeleton

```html
<div class="session-detail">

  <!-- Back navigation -->
  <button class="session-detail__back" type="button">← Back</button>

  <!-- Page header -->
  <div class="session-detail__header">
    <h1 class="session-detail__title">{workoutName}</h1>
    <p class="session-detail__date">{formattedDate}</p>
  </div>

  <!-- Loading state (shown while fetching) -->
  <div class="session-detail__loading" id="session-detail-loading">Loading...</div>

  <!-- Error state (shown on fetch failure) -->
  <div class="session-detail__error" id="session-detail-error" style="display:none;"></div>

  <!-- Table (shown on success with exercises) -->
  <div class="session-detail__table-wrapper" id="session-detail-table" style="display:none;">
    <table class="session-detail__table" aria-label="Session exercise details">
      <thead>
        <tr>
          <th class="session-detail__th session-detail__th--exercise" scope="col">Exercise</th>
          <th class="session-detail__th session-detail__th--value" scope="col">Weight</th>
          <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Weight</th>
          <th class="session-detail__th session-detail__th--value" scope="col">Effort</th>
          <th class="session-detail__th session-detail__th--prev" scope="col">Prev. Effort</th>
        </tr>
      </thead>
      <tbody id="session-detail-tbody">
        <!-- rows rendered by session-detail.ts -->
      </tbody>
    </table>
  </div>

  <!-- Empty state (shown on success with no exercises) -->
  <div class="session-detail__empty" id="session-detail-empty" style="display:none;">
    No exercises were logged for this session.
  </div>

</div>
```

### Table Row (one per exercise)

```html
<tr class="session-detail__row">
  <td class="session-detail__td session-detail__td--exercise">{exerciseName}</td>
  <td class="session-detail__td session-detail__td--value">{loggedWeight ?? "—"}</td>
  <td class="session-detail__td session-detail__td--prev">{previousWeight ?? "—"}</td>
  <td class="session-detail__td session-detail__td--value">{effort ?? "—"}</td>
  <td class="session-detail__td session-detail__td--prev">{previousEffort ?? "—"}</td>
</tr>
```

---

## CSS Classes

| Class | Purpose |
|-------|---------|
| `.session-detail` | Page root container; `max-width: var(--max-content-width)`, `padding-top: var(--spacing-xl)` |
| `.session-detail__back` | Back button; rendered as `<button>`, styled as a text link using `--color-primary` |
| `.session-detail__header` | Contains title + date; `margin-bottom: var(--spacing-lg)` |
| `.session-detail__title` | `h1`; `font-size: var(--font-size-xl)`, `font-weight: 700`, `color: var(--color-text)` |
| `.session-detail__date` | `p`; `font-size: var(--font-size-sm)`, `color: var(--color-text-light)` |
| `.session-detail__loading` | `color: var(--color-text-light)`; centred |
| `.session-detail__error` | `color: var(--color-text-light)` or danger colour; centred |
| `.session-detail__empty` | `color: var(--color-text-light)`; centred |
| `.session-detail__table-wrapper` | `background-color: var(--color-white)`, `border: 1px solid var(--color-border)`, `border-radius: var(--radius)`, `overflow: hidden` |
| `.session-detail__table` | `width: 100%`, `border-collapse: collapse` |
| `.session-detail__th` | `padding: var(--spacing-sm) var(--spacing-md)`, `font-size: var(--font-size-sm)`, `color: var(--color-text-light)`, `font-weight: 600`, `text-align: left`, `border-bottom: 1px solid var(--color-border)`, `background-color: var(--color-bg)` |
| `.session-detail__th--exercise` | `width: 35%` |
| `.session-detail__th--value` | Current session value column |
| `.session-detail__th--prev` | Previous session value column; `color: var(--color-text-light)` |
| `.session-detail__td` | `padding: var(--spacing-sm) var(--spacing-md)`, `font-size: var(--font-size-base)`, `color: var(--color-text)`, `border-bottom: 1px solid var(--color-bg)` |
| `.session-detail__td--exercise` | `font-weight: 500` |
| `.session-detail__td--value` | Current session value; `color: var(--color-text)` |
| `.session-detail__td--prev` | Previous session value; `color: var(--color-text-light)`, `font-size: var(--font-size-sm)` |

---

## History Entry Change

The `.history-session__header` button is simplified — the `aria-expanded`, `aria-controls`, toggle span (`▸`), and `toggleSession` event listener are all removed. On click, the entry calls `navigate('/history/session?id=' + sessionId)`.

```html
<!-- Simplified entry (no expand/collapse) -->
<div class="history-session" data-session-id="{sessionId}">
  <button class="history-session__header" type="button">
    <div class="history-session__info">
      <span class="history-session__workout-name">{workoutName}</span>
      <span class="history-session__date">{formattedDate}</span>
    </div>
    <span class="history-session__exercise-count">{exerciseLabel}</span>
  </button>
</div>
```

CSS changes:
- Remove `.history-session__toggle` and `.history-session--expanded .history-session__toggle` rules.
- Remove `.history-session__details` and `.history-session--expanded .history-session__details` rules.
- The `.history-session__exercise` rules remain (no longer used in history.ts but kept in CSS to avoid breaking the detail page if those classes are reused; alternatively they are cleanly removed since detail page uses `.session-detail__` BEM block).

> Clean approach: remove the detail-related CSS from history section entirely. The detail page uses its own `.session-detail__*` BEM block.

---

## ARIA / Accessibility

| Element | Attribute | Value |
|---------|-----------|-------|
| `<table>` | `aria-label` | `"Session exercise details"` |
| `<th>` cells | `scope` | `"col"` |
| Back button | `type` | `"button"` |
| Error container | `role` | `"alert"` |
| Error container | `aria-live` | `"polite"` |

---

## Navigation Behaviour

- Back button calls `navigate('/history')` via the router's `navigate()` function.
- If `?id=` is missing or not a valid GUID format, the page renders an error state: "Invalid session." with the back button.
- Browser back/forward (popstate) is handled by the existing router; no additional handling needed.
