# Quickstart: Workout Sets Selection

**Feature**: `032-workout-sets-selection`
**Branch**: `032-workout-sets-selection`
**Delivery**: Implemented in PR #159

## User Flows

### Start a workout with sets

1. Open the `Let's go!` page.
2. Select a workout.
3. Leave `Sets` at its default of 3, or select 5.
4. Start the workout.
5. Complete and save it; the selected value is stored once on the workout session.

### Compare with last time

1. Start a workout that has historical data.
2. Confirm each exercise's `Last time` line includes the sets from the same historical session as its selected weight/effort comparison.
3. Confirm a newer Sets-only row does not suppress fallback to older usable weight or effort.
4. Confirm legacy data omits sets without showing an error.

### Review and edit history

1. Open a completed workout from History.
2. Confirm `Sets` and `Prev. Sets` columns appear.
3. Enter edit mode and change Sets in any row.
4. Confirm all Sets controls synchronize.
5. Save and reload; every row displays the updated session-wide value.

## Implementation Order

1. Add `WorkoutSession.Sets`, EF configuration, and migration.
2. Extend API create/update/read DTOs and server validation.
3. Extend historical comparison records/selector and response projections.
4. Add the start-page Sets dropdown and query-string handoff.
5. Parse sets in active session, render previous sets, and include current sets in save.
6. Extend session-detail view/edit tables and synchronized session-level state.
7. Update integration, unit, and Playwright tests.
8. Verify no additional HTTP requests are introduced.

## Validation Commands

```bash
# Build backend and frontend
dotnet build src/WorkoutTracker.slnx
cd src/WorkoutTracker.Web && npm run build

# Frontend unit tests and strict TypeScript check
# (The project currently has no separate npm lint script.)
cd src/WorkoutTracker.Web && npm test && npm run build

# Backend integration tests (requires TEST_DB_CONNECTION)
dotnet run --project src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj -- -noLogo -maxThreads 1

# E2E tests (requires the Aspire app/test prerequisites)
dotnet run --project src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj -- -noLogo -maxThreads 1
```

## Verification Checklist

- Start page offers only 3 and 5 with no default.
- Missing or invalid sets cannot create a new session.
- New sessions persist one sets value.
- Active-session `Last time` displays `3 sets` or `5 sets` without another fetch.
- Detail table shows current and previous sets, including legacy no-data states.
- Editing any row updates one synchronized session value.
- Database and API reject values other than 3 or 5; update omission preserves, explicit null clears, and 3/5 replaces the stored value.
- Tier 1 deterministic budgets PB-01 to PB-07 pass: no added HTTP round trips on start or detail, exactly two database round trips per historical endpoint, the 200-session scan bound is honoured, and exactly one top-level `sets` field is transported per session.
- Tier 2 ceilings PB-08 to PB-10 pass in the local test environment.
- Automated Tier 1 budgets PB-01 to PB-07 pass: request counts, query counts, selector bound, and one top-level Sets field are covered.
- Automated Tier 2 local latency ceilings PB-08 to PB-10 pass.
- Tier 3 comparative run is **not recorded**: the branch has no separate pre-feature baseline commit. Run it before release using the procedure in `plan.md` and record both p95 values, the delta, the machine, and both commit SHAs.
- Existing workout start, random order, save, history, edit, legacy-field, and table-layout regression behaviors pass.

## Delivered Validation Evidence

- Release solution build: passed.
- Frontend tests: 92 passed.
- Backend tests: 167 passed.
- Playwright E2E tests: 281 passed.
- TypeScript build: passed.
- No separate npm lint script exists in the repository; TypeScript compilation was used as the frontend static check.
- `git diff --check`: passed.

## Additional Start Path

The Workouts-page Start action also collects Sets in its pre-start modal. All direct navigation paths include `sets=3|5`, including single-exercise workouts and both randomized and original-order starts.
