# Quickstart: Validate Workout Statistics

This is a post-implementation validation guide; these commands do not assert that the planned feature already exists. See [API contract](./contracts/api-contract.md), [UI contract](./contracts/ui-contract.md), and [data model](./data-model.md) for precise behavior.

## Prerequisites and setup

Use .NET 10 SDK, Node.js/npm, Docker and PostgreSQL as described in the repository [README](../../README.md). Configure the AppHost's PostgreSQL secret locally, never in source control. Configure the test database and browser prerequisites described by the test projects. From the repository root:

```sh
npm ci --prefix src/WorkoutTracker.Web
npm run build --prefix src/WorkoutTracker.Web
dotnet run --project src/WorkoutTracker.AppHost
```

Open the Web URL shown by Aspire. Use the existing workout UI or test fixtures to create completed sessions; do not use production or other users' data.

## Automated checks

Run from the repository root, with `TEST_DB_CONNECTION` set to an isolated PostgreSQL test database and Playwright browsers installed:

```sh
npm run build --prefix src/WorkoutTracker.Web
npm test --prefix src/WorkoutTracker.Web
dotnet build src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj
dotnet vstest src/WorkoutTracker.UnitTests/bin/Debug/net10.0/WorkoutTracker.UnitTests.dll
dotnet build src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
dotnet vstest src/WorkoutTracker.E2ETests/bin/Debug/net10.0/WorkoutTracker.E2ETests.dll
```

The repository's `global.json` selects Microsoft.Testing.Platform, while these existing test projects still use the VSTest adapter. Use `dotnet vstest` for execution unless the test projects are migrated to Microsoft.Testing.Platform.

The implemented tests must cover: local boundary inclusion/exclusion, Monday/year changes and spring/autumn DST; previous/current/next and switching kinds; distinct exercise per workout including duplicate entries; valid, missing, malformed and unsupported effort with exact contributor counts; no workouts versus unavailable statistics; fresh summaries after add/edit/delete; invalid ranges, overlong ranges and proxy errors; stale-response races; keyboard, text alternatives, focus and narrow-screen layout. API integration tests must exercise the actual database timestamp mapping, not only mocked dates.

## Verification recorded

On 2026-11-27, `npm test --prefix src/WorkoutTracker.Web` passed (115 tests), `npm run build --prefix src/WorkoutTracker.Web` passed, the complete .NET unit/API suite passed (187 tests), and the complete Playwright suite passed (353 tests). The API suite uses the isolated PostgreSQL test database; the Playwright fixture uses its in-memory API mock.

The planned 100-change benchmark against 1,000 workouts through the real Web/API/PostgreSQL stack has not been run. No p95 claim is made; complete the performance gate in the next section before release.

## End-to-end scenarios

1. With workouts in current and past periods, open the dashboard. Expect the current local week and correct inclusive range, workout count and the average per week since the first workout. Switch kinds; each shows its current calendar period and corresponding average.
2. Navigate backwards and return with Current; Next must be disabled at current. Exercise only keyboard controls and verify visible focus and selected state. At a narrow mobile viewport, no essential label or value is clipped.
3. Use a period before the first workout and a later period with no workouts. Expect an unavailable average in the first case and an average that includes empty periods in the second.
4. Add, edit and delete a contributing workout through existing flows; return to the same period and verify the source change is reflected. Force a failed request and retry; expect error, not empty or stale results.
5. Test sessions immediately before, at and after each boundary, including local midnight across both DST transitions in a DST-observing zone and a week spanning years. Each appears exactly in its correct local period.

## Security and scope checks

This feature follows the existing single-user workout-history scope and does not introduce sign-in, sharing, or user ownership. Verify that period details contain only completed sessions in the requested range and that the history aggregate contains only the count and first completion date before the period end. Verify invalid/overlong ranges fail safely, exercise names render as text, no workout values are logged, and no third-party dependencies or secrets are introduced. Existing deployment access controls are unchanged and are not verified by these feature tests.

## Performance gate

Seed an isolated representative database with 1,000 completed workouts distributed across multiple weeks, months and years, including a dense year, realistic exercise counts and mixed effort. Use a representative supported browser and the normal Web proxy/API/database stack; record hardware, browser, network and build configuration. After initial load, measure at least 100 sequential period changes across weekly, monthly and yearly views (including dense and empty periods), timing from control activation to the correct newly rendered summary. Include failed/timeout changes as failures, not discarded samples. Compute nearest-rank p95 (95th ordered duration for 100 successful samples) and the proportion completed correctly in under 2 seconds; both require at least 95% under 2 seconds. Verify counts as well as time. Record results and query/payload observations; if the budget fails, investigate indexes/query shape/rendering, retest, and do not release without an approved constitution-compliant mitigation.
