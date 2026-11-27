# Phase 0 Research: Workout Statistics Dashboard

## Reporting periods and time zones

**Decision**: The browser computes local calendar starts for Monday weeks, months and years using local date construction and calendar arithmetic, then sends their UTC instants as `[startUtc, endUtc)`; end is the start of the next local period, not start plus a fixed number of hours. Display inclusive local dates through the day before end. Fetch again when selection changes or the page is re-entered, including after a local time-zone change.

**Rationale**: History already renders timestamps in browser local time; the database `CompletedAt` is an EF shadow timestamp. Independent local midnights preserve DST transitions and avoid server-zone assumptions. Test Monday/year boundaries, spring and autumn transitions, and exact endpoints. This uses the browser's current zone, not a stored user preference.

**Alternatives considered**: Server-local calendar or fixed UTC-day arithmetic (wrong at zone/DST boundaries); introducing a saved user time-zone setting (outside scope).

## Aggregation and history freshness

**Decision**: Add a read-only bounded-range summary API, projected from existing completed session rows, with exercise IDs deduplicated per session and valid session `OverallEffort` observations ordered chronologically. Request fresh data for each selected period and page re-entry. No materialized aggregate or migration.

**Rationale**: 014 established history/API conventions; 031 updates source rows and 029 favors deriving results from current history. This avoids lifetime-history transfers and invalidation problems while limiting each request to at most one year's range. The 1,000-workout target must be measured, not inferred.

**Alternatives considered**: Repeatedly download all `/api/sessions` history (unbounded data transfer); cache or materialize summaries (invalidation after edits/deletes and complexity); reuse the 029 latest-exercise selection algorithm (different question).

## Exercise activity and effort

**Decision**: Count one appearance per exercise identity per completed workout, even with repeated logged entries. Use only session-wide `OverallEffort` integers 1–10, not per-exercise effort, for the labeled workout effort trend and contributor count. Order observations by completion instant then stable session ID; display their local dates and values. With no valid observations, show unavailable, not zero.

**Rationale**: The spec requires counts of workouts in which exercises appear and permits an effort summary or trend. Feature 016 established the nullable overall workout scale; mixing per-exercise and workout ratings would create inconsistent denominators. Feature 029 establishes treating missing values as unavailable while keeping other usable data.

**Alternatives considered**: Count logged rows or sets (inflates workout appearances); average mixed exercise and session scores (not comparable); fill missing values with zero (fabricated).

## Presentation and validation

**Decision**: A dashboard page uses existing responsive page/navigation conventions with native buttons, selected-state semantics, labeled statistics and a textual chronological effort list. A chart is optional only if paired with identical text values; 032's SVG techniques can be reused without a library. Verify keyboard and viewport geometry in Playwright, as in 033; use Vitest for calendar logic and API tests for aggregation.

**Rationale**: This supports the spec without requiring charts or new dependencies. Loading, no-workouts, no-effort, and failed-load states are distinct.

**Alternatives considered**: Chart-only and color-only output (inaccessible); relying solely on DOM unit tests for actual layout (insufficient); inventing other metrics or insights (outside scope).

## Security and privacy

**Decision**: Treat completed history as the product's existing single-user dataset and preserve its current Web/API access scope. The summary endpoint reads no data beyond completed sessions already exposed by history and filters them to the requested period. Validate UTC ranges and cap them; use parameterized EF queries, safe output encoding, generic errors and no new service or secrets. This feature adds no sharing, cross-user aggregation, sign-in or ownership model.

**Rationale**: Existing history endpoints use the same application-wide session dataset. This feature does not broaden that data scope; it adds a bounded summary of those existing records. Existing deployment access controls are outside this feature and remain unchanged; repository configuration does not prove SSO or ownership filtering, so the plan does not claim either.

**Alternatives considered**: Add a speculative multi-user identity and ownership system (outside this feature); introduce new sign-in controls separate from the existing history access path (not requested).
