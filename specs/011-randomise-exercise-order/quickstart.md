# Quickstart: Randomise Exercise Order UX Simplification

**Feature**: `011-randomise-exercise-order`  
**Date**: 2026-05-09

---

## What This Feature Does

Simplifies how users choose to randomise exercise order when starting a workout:

- **Homepage**: An iOS-style toggle ("Randomise exercise order") appears directly on the form when a multi-exercise workout is selected. The user flips it on or off before clicking "Start Workout". No modal involved.
- **Workouts page**: Clicking "Start" on a workout opens a minimal modal asking "Randomise exercise order?" with "Yes" and "No" buttons. That's it.
- **Re-shuffle removed**: The "Re-shuffle" button no longer exists anywhere.

---

## How to Run the App

```bash
cd src/WorkoutTracker.AppHost
dotnet run
```

The Aspire dashboard URL is printed to the console. Navigate there and open the Workout Tracker web URL.

---

## How to Run Tests

**Backend integration tests** (requires PostgreSQL running):
```bash
dotnet test src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj
```

**Frontend unit tests**:
```bash
cd src/WorkoutTracker.Web
npm test
```

---

## Files Changed in This Feature

| File | What changed |
|------|-------------|
| `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` | Removed pre-start modal; added inline `workout-form__randomise` toggle row; updated `PlannedWorkout` interface to include `exerciseCount`; simplified start flow |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` | Replaced complex pre-start modal with Yes/No modal (No left, Yes right); removed shuffle toggle, exercise preview, and re-shuffle logic |
| `src/WorkoutTracker.Web/wwwroot/ts/prestart-modal.ts` | Removed `PrestartExercisePreview` interface and `renderPrestartExercisePreview` export |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | Removed reshuffle and exercise-list CSS; added `workout-form__randomise*` styles for homepage toggle; added `prestart-modal__yes-btn` / `prestart-modal__no-btn` styles (both `flex: 1`) |
| `src/WorkoutTracker.Web/wwwroot/ts/__tests__/prestart-modal.test.ts` | Removed test for deleted `renderPrestartExercisePreview` function |
| `src/WorkoutTracker.E2ETests/E2E/WorkoutsPageTests.cs` | Updated button selector `#prestart-start` → `#prestart-no`; added T024 (`PrestartModal_ClickYes_NavigatesWithOrderParam`) and T025 (`HomeToggle_Enabled_NavigatesWithOrderParam`); added `CreateTwoExerciseWorkoutViaApiAsync` helper |
| `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs` | Updated button selector `#prestart-start` → `#prestart-no` |

No backend changes. No migration. No new source files.

---

## Key Design Decisions

1. **Homepage toggle is inline** — not in a modal. The spec explicitly asks for a toggle visible before clicking "Start Workout".
2. **`PlannedWorkout` interface extended** — `exerciseCount` was already returned by `GET /api/workouts` but not typed in `home.ts`. Adding it enables show/hide of the toggle without an extra API call.
3. **Homepage navigates directly when toggle is off** — eliminates one `GET /api/workouts/{id}` call vs. feature 010's approach. The active session page fetches workout detail itself.
4. **Workouts page exercises are already in memory** — the Workouts page loads the full exercises array on page load, so "Yes" shuffles in memory with no extra API call.
5. **`prestart-modal.ts` trimmed, not deleted** — `trapModalTabKey` remains reusable for the workouts page modal.
