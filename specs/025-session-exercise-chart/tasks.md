---
description: "Delivered task status for 025-session-exercise-chart"
---

# Tasks: Session Exercise Chart (Delivered)

**Feature**: 025-session-exercise-chart  
**Status**: Complete

## Foundational

- [x] Add integration coverage for `GET /api/workouts/{workoutId}/session-trends` in `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs`
- [x] Implement `GET /api/workouts/{workoutId:guid}/session-trends` in `src/WorkoutTracker.Api/Program.cs`
- [x] Add web proxy route in `src/WorkoutTracker.Web/Program.cs`
- [x] Add chart utility helpers to `src/WorkoutTracker.Web/wwwroot/ts/utils.ts`
- [x] Add Vitest coverage for chart utility helpers in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts`
- [x] Add `.session-chart__*` styles in `src/WorkoutTracker.Web/wwwroot/css/styles.css`

## Session Detail Chart

- [x] Render chart section in `src/WorkoutTracker.Web/wwwroot/ts/pages/session-detail.ts`
- [x] Populate dropdown with:
  - [x] `overall` → "Overall Session Effort"
  - [x] `exercise:{exerciseId}` → `{exerciseName}` (one option per exercise)
- [x] Render overall mode as a single spikey polyline (effort axis 0–10)
- [x] Render exercise mode as combined lines:
  - [x] weight line (blue)
  - [x] effort line (red)
- [x] Keep chart hidden for ad-hoc sessions (`plannedWorkoutId === null`)
- [x] Preserve chart functionality via fallback data when trends fetch fails
- [x] Keep line rendering straight/spikey (polyline/path with line segments, not smoothed curves)

## E2E Coverage

- [x] Chart section visible when planned workout has session history
- [x] Dropdown switches between overall and exercise chart modes
- [x] Empty state appears when selected exercise has no plottable data
- [x] Overall effort chart renders on initial load

## Verification

- [x] `dotnet build src/WorkoutTracker.slnx`
- [x] `cd src/WorkoutTracker.Web && npm run build`
- [x] `cd src/WorkoutTracker.Web && npm test`
- [x] `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj`
- [x] `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`

## Notes on Delivered Behavior

- `GET /api/workouts/{workoutId}/session-trends` returns `{ dataPoints: [...] }` (object wrapper), not a bare array.
- `GET /api/sessions/{sessionId}` includes `exerciseId` in each exercise row; this is required for exercise-series mapping.
- Exercise chart mode uses dual y-axes:
  - left axis: dynamic weight range
  - right axis: effort 0–10
