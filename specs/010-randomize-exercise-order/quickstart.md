# Quickstart: Randomize Workout Exercise Order

**Feature**: `010-randomize-exercise-order`  
**Date**: 2026-05-08

## What This Feature Does

When you start a workout, you can now choose to randomise the order in which exercises are presented. This introduces variety in your training stimulus — you won't always be fresh for the same exercises or fatigued by the same ones at the end.

The randomisation is:
- **Opt-in per session** — the toggle defaults to off every time; you choose when to shuffle.
- **Preview first** — you see the shuffled order before the session starts, and can re-shuffle if you want a different sequence.
- **Non-destructive** — your saved workout template is never changed. The next session starts from the original order unless you shuffle again.

---

## User Flow

### Standard start (no shuffle)

1. Go to **Workouts**.
2. Click **Start** on any workout.
3. A pre-start dialog appears showing the exercises in their saved order, with a "Randomise order" toggle (off by default).
4. Click **Start Workout** — the session begins in the saved order, exactly as before.

### Shuffled start

1. Go to **Workouts**.
2. Click **Start** on a workout that has 2 or more exercises.
3. The pre-start dialog appears.
4. Toggle **Randomise order** on.
5. The exercise list immediately reorders into a random sequence.
6. *(Optional)* Click **Re-shuffle** to generate a different random order.
7. When you're happy with the sequence, click **Start Workout**.
8. The active session shows exercises in the shuffled order.

### Changing your mind

- Click **Cancel** in the pre-start dialog at any point to return to the workout list without starting.
- Pressing **Escape** or clicking outside the dialog also cancels.

### Single-exercise workout

If a workout has only one exercise, the "Randomise order" toggle is not shown — there is nothing to shuffle. The dialog still appears so you can confirm before starting.

---

## Changes to Existing Screens

### Workouts Page (`/workouts`)

**Before**: Clicking "Start" immediately navigated to the active session.

**After**: Clicking "Start" opens a pre-start dialog. You confirm start from within the dialog.

The rest of the workouts page (create form, edit, delete) is unchanged.

### Active Session Page (`/active-session`)

**Before**: Exercises were always displayed in the template's saved order.

**After**: If a shuffle was applied in the pre-start dialog, exercises are displayed in the shuffled order. There is no visual indicator that the order is shuffled — the exercises simply appear in the chosen sequence.

Saving a session records the actual exercise order alongside each logged entry. This does not change the workout template.

### All Other Screens

No changes.

---

## Technical Walkthrough

### Frontend: Pre-start modal in `workouts.ts`

A new modal is added to the workouts page HTML alongside the existing edit and delete modals. It follows the same `[name]-modal-backdrop` / `[name]-modal` BEM pattern.

```
workouts.ts
  openPreStartModal(workout)    ← called on "Start" button click
  closePreStartModal()          ← called on Cancel / Escape / backdrop click
  renderExercisePreview(order)  ← renders <ol> items for given exercise order
  handleShuffleToggle()         ← Fisher-Yates shuffle via utils.shuffle()
  handleConfirmStart()          ← navigate with ?order=... if shuffle on
```

### Frontend: `shuffle()` in `utils.ts`

A new pure function added alongside the existing `reorder()` helper:

```typescript
export function shuffle<T>(arr: readonly T[]): T[] {
  const result = [...arr];
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}
```

Covered by Vitest unit tests in `__tests__/utils.test.ts`.

### Frontend: Order parameter in `active-session.ts`

`active-session.ts` reads an optional `order` query parameter:

```typescript
const orderParam = params.get("order");
exerciseOrder = orderParam
  ? orderParam.split(",").map(id => id.trim()).filter(id => id.length > 0)
  : null;
```

If `exerciseOrder` is non-null, the exercise list from the API is reordered in memory before rendering using `applyOrder()` from `utils.ts`:

```typescript
if (workout !== null && exerciseOrder !== null) {
  workout = { ...workout, exercises: applyOrder(workout.exercises, exerciseOrder) };
}
```

`applyOrder<T>()` is a generic utility in `utils.ts` (alongside `shuffle`) — it matches each ID from the order array against the exercise list, appending any unrecognised exercises at the end as a safety fallback:

```typescript
export function applyOrder<T extends { readonly exerciseId: string }>(
  exercises: readonly T[],
  order: readonly string[]
): T[] {
  const validOrder = order.filter(id => exercises.some(e => e.exerciseId === id));
  if (validOrder.length === 0) return [...exercises];
  const orderedExercises: T[] = [];
  const remaining = [...exercises];
  for (const id of validOrder) {
    const idx = remaining.findIndex(e => e.exerciseId === id);
    if (idx !== -1) orderedExercises.push(remaining.splice(idx, 1)[0]);
  }
  orderedExercises.push(...remaining);
  return orderedExercises;
}
```

When saving the session, each exercise is assigned its display index as `sequence`:

```typescript
displayedExercises.map((ex, index) => ({
  exerciseId: ex.exerciseId,
  loggedWeight: ...,
  effort: ...,
  sequence: index,
}))
```

### Backend: `SessionLoggedExerciseItem` extension

`Program.cs` — one new nullable field on the existing DTO:

```csharp
internal sealed class SessionLoggedExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public string? Notes { get; set; }
    public int? Effort { get; set; }
    public int? Sequence { get; set; }  // NEW
}
```

The session creation handler maps it to the entity:

```csharp
new LoggedExercise
{
    ExerciseId    = item.ExerciseId,
    LoggedWeight  = item.LoggedWeight,
    Notes         = item.Notes,
    Effort        = item.Effort,
    Sequence      = item.Sequence,   // NEW
}
```

### Backend: Migration

```
dotnet ef migrations add AddSequenceToLoggedExercise \
  --project src/WorkoutTracker.Infrastructure \
  --startup-project src/WorkoutTracker.Api
```

Adds one nullable `int` column `sequence` to `logged_exercise`.

---

## Files Changed Summary

| File | Change |
|------|--------|
| `src/WorkoutTracker.Web/wwwroot/ts/utils.ts` | Add `shuffle<T>()` and `applyOrder<T>()` functions |
| `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts` | Add `shuffle` and `applyOrder` tests |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` | Pre-start modal, shuffle toggle, re-shuffle, conditional navigation |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` | Pre-start modal (same UX), fetch workout detail before navigating |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` | Read `?order=` param, reorder exercises, send `sequence` in save |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | Pre-start modal styles (`prestart-modal-backdrop`, `prestart-modal`, etc.) |
| `src/WorkoutTracker.Api/Program.cs` | `SessionLoggedExerciseItem` gains `Sequence`; handler maps it to entity; 201 response includes `Sequence` |
| `src/WorkoutTracker.Infrastructure/Data/Models/LoggedExercise.cs` | Add `int? Sequence` property |
| `src/WorkoutTracker.Infrastructure/Data/Migrations/…_AddSequenceToLoggedExercise.cs` | New migration |
