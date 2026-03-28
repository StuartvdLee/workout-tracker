# Quickstart: Add Exercises

**Feature**: 003-add-exercises
**Date**: 2026-07-15

## Prerequisites

- .NET 10 SDK
- Node.js (for TypeScript compilation)
- Docker (for PostgreSQL via Aspire)
- Playwright browsers installed (`node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium`)

## Build

From the repository root:

```bash
cd src/WorkoutTracker.Web
npm install
npm run build
```

Or build the entire solution (which triggers TypeScript compilation automatically):

```bash
cd src
dotnet build
```

## Run Locally

Start the full Aspire orchestration (requires Docker for PostgreSQL):

```bash
cd src
dotnet run --project WorkoutTracker.AppHost
```

The web app will be available at the URL shown in the Aspire dashboard (typically `https://localhost:7xxx`). The database migration runs automatically in development mode, creating the muscles table and seeding the 11 predefined muscles.

## Run Tests

Build TypeScript first, then run the test suite:

```bash
cd src/WorkoutTracker.Web && npm run build && cd ..
dotnet test
```

## Key Files for This Feature

| File | Purpose |
| ---- | ------- |
| `src/WorkoutTracker.Infrastructure/Data/Models/Exercise.cs` | Modified: max length + muscle navigation |
| `src/WorkoutTracker.Infrastructure/Data/Models/Muscle.cs` | New: predefined muscle entity |
| `src/WorkoutTracker.Infrastructure/Data/Models/ExerciseMuscle.cs` | New: junction entity |
| `src/WorkoutTracker.Infrastructure/Data/WorkoutTrackerDbContext.cs` | Modified: new DbSets, config, seed data |
| `src/WorkoutTracker.Infrastructure/Data/Migrations/*_AddMusclesAndExerciseConstraints.cs` | New: schema migration |
| `src/WorkoutTracker.Api/Program.cs` | Modified: exercise + muscle API endpoints |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` | Modified: full exercise page (replaces placeholder) |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | Modified: exercise form + list + muscle toggle styles |
| `src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs` | Modified: full E2E test coverage |
| `src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs` | Modified: mock exercise + muscle endpoints |

## Development Workflow

1. Start TypeScript watch mode: `cd src/WorkoutTracker.Web && npm run watch`
2. In a separate terminal, run the Aspire host: `cd src && dotnet run --project WorkoutTracker.AppHost`
3. Make changes to `.ts` files — they auto-compile to `wwwroot/js/`
4. Make changes to `.cs` files — restart the Aspire host to pick up API/model changes
5. Refresh the browser to see changes

## Verification Checklist

### Exercise Creation (User Story 1)
- [ ] Navigate to Exercises page via sidebar
- [ ] Enter an exercise name and submit — exercise appears in list
- [ ] Submit with empty name — validation error shown
- [ ] Submit with whitespace-only name — validation error shown
- [ ] Submit with a name that already exists (different case) — duplicate error shown
- [ ] Submit with a name exceeding 150 characters — max length error shown
- [ ] After successful save, form clears and is ready for next entry

### Muscle Selection (User Story 2)
- [ ] Muscle toggle buttons display all 11 predefined muscles
- [ ] Clicking a muscle toggles it on (highlighted) / off
- [ ] Creating an exercise with selected muscles saves the associations
- [ ] Creating an exercise without selecting muscles succeeds
- [ ] Selected muscles display with the exercise in the list

### Exercise List (User Story 3)
- [ ] All created exercises appear in the list with names
- [ ] Exercises with muscles show muscle chips alongside the name
- [ ] Exercises without muscles show only the name
- [ ] When no exercises exist, a friendly empty state is displayed

### Exercise Editing (User Story 4)
- [ ] Clicking edit on an exercise populates the form with its data
- [ ] Form shows "Update Exercise" button and "Cancel" button in edit mode
- [ ] Modifying name and/or muscles and submitting updates the exercise in the list
- [ ] Clicking cancel clears the form and returns to create mode
- [ ] Same validation rules apply in edit mode (empty name, duplicates, max length)
- [ ] Changing name to own current name (no actual change) is allowed

### Cross-Cutting
- [ ] All existing E2E tests still pass
- [ ] New E2E tests pass
- [ ] Mobile responsive layout works (sidebar toggle, form usable on small viewport)
- [ ] Keyboard navigation works (tab through form, toggle muscles, submit)
