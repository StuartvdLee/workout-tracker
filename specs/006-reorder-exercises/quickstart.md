# Quickstart: Reorder Exercises in a Workout

**Feature**: `006-reorder-exercises`  
**Date**: 2026-05-02

## What Changed

You can now drag exercises into any order you like when creating or editing a planned workout. Previously, exercises were locked to the order you added them. Now, each exercise in the selected list has a drag handle (⠿) on the left that you can drag to rearrange.

---

## Reordering While Creating a Workout

### Step 1 — Open the Workouts Page

Navigate to `/workouts`. The create workout form is at the top of the page.

### Step 2 — Add Exercises

Select exercises from the "Add exercise" dropdown. Each selected exercise appears in the "Selected exercises" list below.

### Step 3 — Reorder by Dragging

Each exercise item in the list has a **grip icon** (⠿) on the left side.

- **Mouse/trackpad**: Click and hold the grip icon, drag the exercise up or down to the desired position, then release.
- **Touch (mobile)**: Press and hold the grip icon, then drag up or down. The page scroll is temporarily paused while you drag.

The list updates immediately to show the new order.

### Step 4 — Save the Workout

Click **Create Workout**. The exercises are saved in the order shown in the list.

---

## Reordering While Editing an Existing Workout

### Step 1 — Open the Edit Modal

Click the pencil (edit) button on any workout in the list. The edit modal opens with the workout's current exercises displayed in their saved order.

### Step 2 — Drag to Rearrange

Use the same grip icon (⠿) to drag exercises into the desired order within the modal.

### Step 3 — Save or Cancel

- Click **Save Changes** to persist the new order.
- Click **Cancel** (or press Escape, or click outside the modal) to discard any reordering changes and keep the original order.

---

## Keyboard Reordering

If you prefer not to use drag-and-drop, you can reorder using the keyboard:

1. **Tab** to the grip icon button on the exercise you want to move.
2. Press **Space** to "pick up" the exercise.
3. Use **↑** / **↓** arrow keys to move the exercise up or down one position at a time.
4. Press **Space** or **Enter** to drop it in the current position.
5. Press **Escape** to cancel and return it to where it was.

A screen-reader announcement confirms each move (e.g., "Bench Press moved to position 2 of 4").

---

## Notes

- If a workout has only **one exercise**, no drag handle is shown — there is nothing to reorder.
- Adding a new exercise after reordering places it at the **bottom** of the current list. You can drag it up from there.
- Removing an exercise after reordering preserves the relative order of the remaining exercises.
- If saving fails (e.g., network error), the list retains your reordered state so you can retry without starting over.
