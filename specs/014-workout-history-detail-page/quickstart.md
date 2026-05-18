# Quickstart: Workout History Detail Page

**Feature**: `014-workout-history-detail-page`

---

## What Changed

The History page entries no longer expand inline. Clicking any session entry now opens a dedicated **Session Detail** page showing a table of exercises with comparison data from your previous session of the same workout.

---

## User Flow

### Viewing a Past Session

1. Open the app and navigate to **History** from the sidebar.
2. You'll see your completed sessions listed, each showing the workout name and date.
3. **Tap any entry** — you are taken to the Session Detail page for that session.
4. The page shows a table with five columns:
   - **Exercise** — the name of each logged exercise
   - **Weight** — what you lifted in this session (or — if not recorded)
   - **Prev. Weight** — what you lifted the previous time you did this workout (or — if this is your first time)
   - **Effort** — your effort rating in this session (or —)
   - **Prev. Effort** — your effort rating last time (or —)
5. Use the **← Back** control at the top of the page to return to History.

---

## Example Table

| Exercise | Weight | Prev. Weight | Effort | Prev. Effort |
|----------|--------|--------------|--------|--------------|
| Bench Press | 80 KG | 77.5 KG | 7 — Hard | 6 — Moderate |
| Overhead Press | 50 KG | 50 KG | 8 — Very Hard | 7 — Hard |
| Tricep Dips | — | 0 KG | 6 — Moderate | — |

---

## States

- **Loading**: A loading indicator is shown while session data is being fetched.
- **Empty session**: If no exercises were logged, a message is displayed: "No exercises were logged for this session."
- **First session**: If this is your first time completing this workout, the Prev. Weight and Prev. Effort columns show — for all rows.
- **Error**: If data cannot be loaded, an error message is shown. You can still navigate back to History.
