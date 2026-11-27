# Quickstart: Resume an In-Progress Workout

The verification record below distinguishes completed checks from the repeatable commands and manual scenarios. Azure SSO is configured separately by the user; this feature does not configure or require application-level identity.

## Prerequisites

- .NET 10 SDK, Node.js and the repository's installed frontend dependencies.
- PostgreSQL test database; set `TEST_DB_CONNECTION` for the API integration suite. The existing fixture otherwise defaults to a local test database.
- Docker and the AppHost's PostgreSQL secret as documented in [README.md](../../README.md); migrations must be applied to a disposable database.
- Playwright browser prerequisites for `WorkoutTracker.E2ETests`.

## Implementation verification

- After key removal, the frontend build and 109 Vitest cases passed. Against PostgreSQL 17.6 in an isolated temporary container, 106 session API tests passed, including concurrent retries, legacy-fence protection, rejected migration rollback, transaction retry and request bounds. The AppHost build and migration-model consistency check passed; an upgrade simulation retained an older rejection fence and removed obsolete key infrastructure.
- Before removing the fingerprint key, the complete Playwright history/reorder suite passed 121 tests using Chromium 153 headless on macOS 26.7.3, Apple M2 Pro. With one warm-up and a 25-exercise draft, 100/100 refreshes restored weights, effort, order and sets within 2 seconds (maximum 21.12 ms after DOM ready); 100 input events saved within 2 seconds (maximum 0.30 ms), with a separate failed-write check. Autosave made zero API requests; the observed completion and retry each made one request. The suite also passed 100 lost-response completion/retry trials and 100 discard trials against the browser test server, checking history and draft cleanup each time. After key removal, three focused browser retry/correction tests passed; the full 121-test suite was not rerun.
- Completion fingerprints use SHA-256 without a configured secret. Rejection responses allow correction only after a durable fingerprint fence commits; older incompatible rejection fences stay locked rather than being bypassed.
- The fingerprint transition cannot be rolled back safely. If an earlier keyed development revision is running during migration, completion may temporarily fail until the new API revision starts; pending drafts remain available for retry.
- The [transition security review](../../docs/code-review/2026-11-27-completion-fingerprint-transition-review.md) identified rollback and mixed-revision risks. Rollback was blocked and the [follow-up review](../../docs/code-review/2026-11-27-completion-fingerprint-rollback-recheck-review.md) found no remaining issue in that scope. The earlier keyed revision was a local development revision, not a production deployment; the mixed-revision interruption remains relevant if such a revision is ever deployed.
- The user confirmed that Azure SSO is configured manually; the application does not implement SSO or user identity. On 2026-11-27, unauthenticated HEAD requests to the deployed web root and `/api/workouts` returned `401` with a Bearer challenge; a request to the API's internal hostname returned `404`. The most recent successful deployment log recorded external web ingress and internal API ingress. Live Azure configuration was not independently inspected.

## Automated Checks

From the repository root:

```bash
cd src/WorkoutTracker.Web
npm run build
npm test
```

From the repository root, set `TEST_DB_CONNECTION` to a connection string for a **disposable** PostgreSQL database before running the API tests. The built test executables use the Microsoft.Testing.Platform in-process runner and its `-class` filter:

```bash
export TEST_DB_CONNECTION='<disposable PostgreSQL connection string>'
dotnet build src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj
src/WorkoutTracker.UnitTests/bin/Debug/net10.0/WorkoutTracker.UnitTests -class '*SessionApiTests'
dotnet build src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
src/WorkoutTracker.E2ETests/bin/Debug/net10.0/WorkoutTracker.E2ETests -class '*WorkoutHistoryTests' -class '*WorkoutReorderTests'
```

The API tests must exercise concurrent duplicate requests, history edits before replay, and history deletion with real PostgreSQL, not just the Playwright mock. Browser tests must exercise storage failures, cross-tab lock contention/release and unavailable locking, and a committed create with its response lost. Unit tests must cover version 1 and both sides of the weight, exercise-count and serialized-byte limits in [data-model.md](./data-model.md). Run broader regression suites when the targeted checks reveal a shared failure.

## Manual Journey

1. Start the app following [README.md](../../README.md). Select a workout and 3 or 5 sets; enter weights and effort, commit a changed exercise order, and wait for saved feedback.
2. Refresh, confirm the same workout, values, sets and order are restored, change values and refresh again. Existing history must remain visible.
3. Finish the workout and refresh; exactly one completed entry exists and no active draft returns. Repeat with explicit discard and verify no new history entry.
4. Disable or fault browser storage in a disposable browser profile. Confirm actionable failure feedback and retry without losing displayed values. Seed invalid draft data and confirm it is not silently discarded.
5. Simulate a create response lost after server commit. Refresh and retry the pending operation; confirm exactly one history entry. Verify failed draft removal is reported and retried.
6. Open the active draft in two tabs in the same browser profile. Confirm the second tab cannot edit or complete while the first owns the lock; close the first, retry in the second and confirm the draft resumes. Confirm a browser without locking receives an actionable error rather than proceeding unprotected.

## Performance and Repetition

The recorded 100-trial results above were obtained before the final fingerprint change. To repeat them, use the 25-exercise setup and measurement points in [plan.md](./plan.md#performance-budgets--measurement), and record the browser, machine, environment, trial timings, failures and revision.
