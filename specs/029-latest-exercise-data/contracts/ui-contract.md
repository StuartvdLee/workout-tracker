# UI Contract: Previous Exercise Comparison Display

**Feature**: `029-latest-exercise-data`  
**Date**: 2026-07-19

## Active Session Surface

The active session page continues to render previous exercise data inside each `.active-session__exercise-item` using:

- `.active-session__exercise-previous`
- `.active-session__previous-label`
- `.active-session__previous-value`
- `.active-session__previous-empty`
- `.active-session__previous-error`

No new CSS block is required.

## States

### Loading

The active session fetches workout details and previous-performance data concurrently. While the page is loading, the existing page-level loading state remains in place.

### Success

When an exercise has returned latest available data:

```text
Last time: [optional sequence] · [optional weight] · [optional effort label]
```

Rules:

- Preserve the existing label text: `Last time:`.
- Include weight when `loggedWeight` is present.
- Include effort as `[effort] — [effort label]` using the existing `getEffortLabel()` helper.
- Include sequence only when returned, but sequence alone must not create a success state.
- Write all dynamic values with `textContent`.

### No Prior Data

When an exercise has no returned entry:

```text
First session — no previous data
```

This copy is retained for visual consistency, but its meaning is now "no usable previous data for this exercise in this planned workout".

### Error

When the previous-performance fetch fails or cannot be parsed:

```text
Could not load previous data
```

The workout itself remains usable and the user can continue logging.

## Matching Rule

The UI maps previous data by `exerciseId`. It MUST NOT infer previous data from exercise position or sequence.

## Accessibility

The display remains read-only reference text inside the exercise item. Existing page-level alert regions for blocking load/save errors are unchanged.

## Session Detail Review Surface

The session detail page continues to render previous comparison data in the existing table columns:

- `.session-detail__cell--prev` under `Prev. Weight (kg)`
- `.session-detail__cell--prev` under `Prev. Effort`
- `.session-detail__no-data` for missing previous values

Rules:

- `previousWeight` and `previousEffort` must come from the latest usable prior row for the same exercise before the reviewed session.
- A skipped or empty immediately prior row must not force `—` when older usable data exists.
- Missing data remains rendered as `—` using the existing `.session-detail__no-data` style.
- Dynamic text remains escaped via existing session-detail escaping helpers.
