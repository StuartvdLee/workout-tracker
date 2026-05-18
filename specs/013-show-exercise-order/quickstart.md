# Quickstart: Previous Exercise Order Indicator in Active Workout

**Feature**: `013-show-exercise-order`  
**Date**: 2026-05-18

## Prerequisites

Same as all other features in this project:

- .NET 10 SDK
- Node.js (for TypeScript build)
- Docker (for PostgreSQL in tests)
- `TEST_DB_CONNECTION` environment variable set (e.g., `Host=localhost;Port=5432;Database=workout_tracker_test;Username=postgres;Password=postgres`)

## Running the App

```bash
cd src/WorkoutTracker.AppHost
dotnet run
```

Aspire will start the API, Web, and PostgreSQL containers. Open the Aspire dashboard URL to navigate to the app.

## Running Backend Tests

```bash
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj
```

Requires a running PostgreSQL instance pointed to by `TEST_DB_CONNECTION`.

## Running Frontend Tests

```bash
cd src/WorkoutTracker.Web
npm test
```

## Building TypeScript

```bash
cd src/WorkoutTracker.Web
npm run build
```

## Verifying This Feature Manually

1. Start the app with `dotnet run` in `WorkoutTracker.AppHost`.
2. Create a planned workout with 3+ exercises.
3. Complete a session for that workout (save it from the active session page).
4. Start a new session for the same planned workout.
5. Each exercise row should show `#1`, `#2`, `#3`, etc. in the "Last time" display — e.g., `Last time: #2 · 80 KG · 7 — Hard`.

## Key Files Changed

| File | Change |
|------|--------|
| `src/WorkoutTracker.Api/Program.cs` | Add `le.Sequence` to `previous-performance` projection |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` | Add `sequence` to `PreviousExerciseData`; prepend `#x` in render |
| `src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs` | Add `int? Sequence` to `PreviousExerciseDataDto`; new sequence test |
