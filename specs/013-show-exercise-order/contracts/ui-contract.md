# UI Contract: Previous Exercise Order Indicator in Active Workout

**Feature**: `013-show-exercise-order`  
**Date**: 2026-05-18

## Overview

This contract documents the change to the active session exercise card's "previous performance" section. A `#x` position indicator is prepended to the "Last time" value string. All other UI elements are unchanged from feature 008.

---

## Active Session Exercise Card — Updated Previous Section

The `active-session__exercise-previous` section is unchanged in structure. Only the content of `active-session__previous-value` changes when `sequence` is non-null.

### HTML Structure (unchanged)

```html
<div class="active-session__exercise-previous" id="previous-{exerciseId}">
  <!-- Content determined by state — see State Variants below -->
</div>
```

---

## State Variants for `active-session__exercise-previous`

### State 0: Loading (previous performance fetch in progress)

Inherited unchanged from feature 008. While the `Promise.allSettled` fetch for `/api/workouts/{workoutId}/previous-performance` is in progress, the `active-session__exercise-previous` section is rendered empty — no child elements are inserted. The section retains its natural block height via the existing CSS, preventing layout shift when content arrives.

```html
<!-- Section is empty while the fetch is in progress -->
<div class="active-session__exercise-previous" id="previous-{exerciseId}">
</div>
```

*No new implementation required: this placeholder behaviour is fully provided by the `Promise.allSettled` fetch strategy established in feature 008 and is unchanged by feature 013. The `#x` indicator is inserted alongside weight and effort once the fetch resolves — it does not introduce an additional loading window.*

### State 1: First Session (no previous session for this planned workout)

Unchanged from feature 008.

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-empty">First session — no previous data</span>
</div>
```

### State 2: Previous Data — Position, Weight, and Effort

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">#2 · 80 KG · 7 — Hard</span>
</div>
```

### State 3: Previous Data — Position and Weight Only (effort null)

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">#2 · 80 KG</span>
</div>
```

### State 4: Previous Data — Position and Effort Only (weight null)

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">#2 · 7 — Hard</span>
</div>
```

### State 5: Previous Data — Position Only (weight and effort null)

When `sequence` is set but both `loggedWeight` and `effort` are null (user completed the session without logging anything for this exercise):

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">#2</span>
</div>
```

*This is a change from feature 008 behaviour, where State 5 showed "First session — no previous data". With sequence available, the fact that the exercise appeared at position `#2` is informative even if no weight/effort was recorded.*

### State 6: Previous Data — Weight and/or Effort, No Sequence (null sequence)

When `sequence` is null (session saved before sequence tracking), the display falls back to feature 008 behaviour. The `#x` indicator is simply absent.

```html
<!-- Weight and effort available, no sequence -->
<div class="active-session__exercise-previous">
  <span class="active-session__previous-label">Last time:</span>
  <span class="active-session__previous-value">80 KG · 7 — Hard</span>
</div>
```

### State 7: Error (fetch failed)

Unchanged from feature 008.

```html
<div class="active-session__exercise-previous">
  <span class="active-session__previous-error">Could not load previous data</span>
</div>
```

---

## TypeScript Interface Changes

```typescript
// Updated interface in active-session.ts
interface PreviousExerciseData {
  readonly exerciseId: string;
  readonly loggedWeight: string | null;
  readonly effort: number | null;
  readonly sequence: number | null;  // NEW: 0-based position from previous session
}

// PreviousPerformance interface unchanged:
interface PreviousPerformance {
  readonly hasPreviousSession: boolean;
  readonly completedAt: string | null;
  readonly exercises: PreviousExerciseData[];
}
```

---

## Rendering Logic Change

The `parts` array in `renderExerciseInputs` is updated to prepend `#x` when `sequence` is non-null:

```typescript
// Before (feature 008):
const parts: string[] = [];
if (entry.loggedWeight !== null) parts.push(`${entry.loggedWeight} KG`);
if (entry.effort !== null) parts.push(`${entry.effort} — ${getEffortLabel(entry.effort)}`);

// After (feature 013):
const parts: string[] = [];
if (entry.sequence !== null) parts.push(`#${entry.sequence + 1}`);
if (entry.loggedWeight !== null) parts.push(`${entry.loggedWeight} KG`);
if (entry.effort !== null) parts.push(`${entry.effort} — ${getEffortLabel(entry.effort)}`);
```

The rest of the rendering logic (label/value spans, empty fallback, error state) is unchanged.

---

## CSS Classes

No new CSS classes are required. The `#x` indicator is rendered inside the existing `active-session__previous-value` span, inheriting `--color-text-muted` at `0.85rem`.

| Class | Status | Description |
|-------|--------|-------------|
| `active-session__exercise-previous` | Unchanged | Container for previous performance section |
| `active-session__previous-label` | Unchanged | "Last time:" read-only label |
| `active-session__previous-value` | Unchanged | Value text — now includes `#x` prefix when sequence is available |
| `active-session__previous-empty` | Unchanged | "First session" / no-data fallback |
| `active-session__previous-error` | Unchanged | Error message ("Could not load previous data") |

---

## Interaction Rules

Unchanged from feature 008. The previous data section is read-only, has no tab index, and does not auto-populate input fields.

---

## Unchanged UI Elements

The following active session UI elements are not modified by this feature:
- Session title bar
- Save Workout button and saving state
- Cancel button
- Discard modal
- Weight input (number field, KG label)
- Effort slider (range, value display, band label)
- Error and API error message areas
- The `active-session__exercise-previous` DOM structure and element IDs
