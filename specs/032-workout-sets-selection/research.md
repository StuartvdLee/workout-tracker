# Research: Workout Sets Selection

**Feature**: `032-workout-sets-selection`
**Status**: Decisions implemented in PR #159

## Decision 1: Store sets once on `WorkoutSession`

**Decision**: Add nullable `int? Sets` to `WorkoutSession`, mapped to `workout_sessions.sets`, with a database check constraint allowing only `NULL`, `3`, or `5`.

**Rationale**: The selected value applies to every exercise in a performed workout. Session-level storage prevents duplicated values and conflicting per-exercise state. Nullable storage preserves compatibility with sessions created before this feature.

**Alternatives considered**:
- Store sets on each `LoggedExercise`: rejected because it duplicates one session-wide value and permits inconsistent values within a session.
- Backfill old sessions: rejected because the historical value is unknown.

## Decision 2: Carry sets from the start page in the active-session URL

**Decision**: Add a required `Sets` dropdown to `home.ts`, validate 3 or 5, and navigate using the existing active-session query-string pattern with `sets=3` or `sets=5` alongside `id` and optional `order`. `active-session.ts` parses and retains this value and includes it in the existing session save request.

**Rationale**: This reuses the established `id`/`order` navigation flow and adds no network request or client storage. The backend remains authoritative and validates the save payload.

**Alternatives considered**:
- Create the database session when Start is clicked: rejected because the current lifecycle persists only completed/saved sessions and premature creation would require abandoned-session cleanup.
- `sessionStorage`: rejected because URL state is already the project convention and is easier to test and reason about.
- Placeholder option with no preselected value: superseded; the start page and Workouts start modal now default to 3 (the most common choice) to remove an extra tap, while 5 remains selectable and server-side validation is unchanged.

## Decision 3: Extend existing contracts rather than add routes

**Decision**: Add `sets` to `SessionCreateRequest`, session create/list/detail responses, and `SessionUpdateRequest`. Add `sets` to previous-performance exercise entries and add `previousSets` to session-detail exercise entries. Existing Web proxy routes continue to forward the extended JSON bodies and responses.

**Rationale**: The data already flows through the session endpoints. Extending the current contracts avoids extra round trips and follows features 016, 029, and 031.

**Alternatives considered**:
- Add a sets-specific endpoint: rejected as unnecessary API surface and an extra request.
- Return one top-level previous sets value from previous-performance: rejected because feature 029 can select the latest usable comparison from a different historical session for each exercise.

## Decision 4: Associate previous sets with the selected historical exercise comparison

**Decision**: Extend `HistoricalSessionData` and `LatestExerciseComparison` propagation so every selected previous exercise entry carries the `Sets` value from the same historical session that supplied its weight/effort/sequence. Preserve the existing usability predicate: a row is selectable only when weight or effort is usable; Sets alone does not make the row selectable.

**Rationale**: This keeps `Last time`, `Previous Weight`, `Previous Effort`, and `Previous Sets` temporally consistent per exercise under the latest-usable-data behavior introduced by feature 029. Preserving the weight-or-effort predicate prevents a newer Sets-only row from suppressing fallback to older usable weight or effort.

**Alternatives considered**:
- Treat Sets alone as usable comparison data: rejected because it would change feature 029 semantics and could hide older usable weight or effort.
- Always use sets from the immediately preceding session: rejected because it could mismatch the historical session supplying an exercise's weight and effort.
- Use the newest workout-level sets value for every exercise: rejected for the same consistency reason.

## Decision 5: Repeat session sets in detail rows and synchronize editing

**Decision**: Add `Sets` and `Prev. Sets` columns to the session-detail table. View mode repeats the session-wide current sets value in each exercise row and shows the per-exercise comparison's previous sets. Edit mode uses the existing select style for current sets; all row controls represent one session value and remain synchronized. The update payload contains one top-level `sets` field.

**Rationale**: This satisfies the requested table presentation while preserving the invariant that sets is session-level. Synchronization avoids contradictory values.

**Alternatives considered**:
- Store or edit sets independently per row: rejected because it violates the feature rule.
- Put sets only in a summary row: rejected because the request explicitly requires sets and previous sets in the history table.

## Decision 6: Reuse current UX and validation patterns

**Decision**: Reuse `.workout-form__label` and `.workout-form__select` on the start page; existing active-session previous-value separators; session-detail table, no-data marker, edit controls, save/error handling, and discard-warning behavior. Validate allowed values both client-side and server-side.

**Rationale**: This meets the consistency requirement and minimizes new styling. Server validation protects persisted data from malformed or direct requests.

## Decision 7: Test at API and E2E levels

**Decision**: Add API integration coverage for creation, validation, nullable legacy data, previous-performance association, detail current/previous values, and historical updates. Add Playwright coverage for start-page selection/validation, active-session `Last time`, and session-detail view/edit behavior. Add dedicated selector unit tests following the nine-case matrix in `plan.md` **Selector Test Design**; because the selector and its projection records are `internal` to `WorkoutTracker.Api`, add `InternalsVisibleTo("WorkoutTracker.UnitTests")` rather than widening those types to `public`. The selector tests are pure in-memory and take no database fixture.

**Rationale**: These are cross-layer user journeys and critical data-selection rules, matching the constitution and prior plans.

For the graph extension, add an API integration test asserting `sets` is projected per `session-trends` data point, and Playwright tests asserting the sets series and legend entry render for both the overall and per-exercise selections and are absent when no session recorded sets. The E2E mock web host also needs a `session-trends` route: it previously had none, so the chart fell back to single-session data and multi-session chart behavior was never exercised.

No `NEEDS CLARIFICATION` markers remain.

## Decision 8: Show sets as background bars with no sets axis

**Decision**: Extend the existing `session-trends` response with one nullable session-level `sets` value per data point, and render it in `session-detail.ts` as vertical background bars, one per session, rising from the plot floor to a height proportional to the set count. Draw no sets axis, tick labels, or numeric value labels: the count is read from bar height alone. Bar height is measured from zero against a full-height reference of `6` sets, raised only when a session recorded more. Apply it to both the overall-effort selection and each per-exercise selection, add a `Sets` legend entry, and keep the chart's original `0 0 600 260` `viewBox` and `x=50`-`x=580` plot area.

**Rationale**: Sets is session-level, so it is meaningful under every chart selection, and bars let the user correlate changes in sets with changes in weight and effort at a glance. Bars occupy the chart background, so no horizontal room is needed for an extra axis and the weight and effort lines stay the visual focus. A count of 3 or 5 is a small magnitude that reads directly from relative bar height, so a numeric axis would add clutter without adding information. A fixed `6`-set reference keeps heights comparable across selections and workouts, while raising it for larger counts keeps every bar inside the plot area. Bars also differ in shape from the two line series, so sets stays distinguishable without relying on colour alone.

**Alternatives considered**:
- Plot sets as a third line/point series on an offset right-hand axis: rejected because a third axis crowded the chart, forced a wider `viewBox`, and gave a two-value measure more visual weight than the metrics being trended.
- Label each bar with its numeric set count: rejected because the labels collide at the 50-session cap, and relative bar height already conveys 3 versus 5.
- Append sets to the x-axis date labels: rejected because it crowds the date row and does not show the trend as a trend.
- Share the existing effort axis: rejected because the `0`-`10` effort scale compresses the 3-versus-5 difference and implies the two values are comparable.
- Scale bars to the observed min/max: rejected because a constant-sets workout would render every bar at full height and a small change would look dramatic. The fixed `6`-set reference keeps sessions comparable.
- Cap the bar reference at `6`: rejected because any count above six would overflow the plot area; the reference rises instead (`computeSetsBarMax` in `utils.ts`).
- Draw bars over the lines: rejected because the filled shapes would obscure the weight and effort series; bars are rendered first and filled at reduced opacity.
- Show sets only on per-exercise selections: rejected because sets applies to the whole session and is equally relevant to the overall-effort view.

## Decision 9: Omit the sets bars entirely when no session recorded sets

**Decision**: When every data point in range has `sets: null`, render no bars and no legend entry, and leave the overall-effort chart without a legend exactly as before. When only some data points lack sets, render a bar only for the sessions that recorded one.

**Rationale**: This matches how the chart already handles missing weight and effort, avoids advertising empty data on pre-feature workouts, and keeps `FR-015` verifiable with a simple element-count assertion.

**Alternatives considered**:
- Always draw the legend: rejected because it advertises data that does not exist for legacy workouts.
- Treat missing sets as zero: rejected because it fabricates data and a zero-height bar is indistinguishable from no bar while still implying a recorded value.

## Delivered Verification

The decisions were implemented and covered by the API, selector-unit, frontend, and Playwright suites. The delivered implementation also added defensive handling for missing legacy `sets`/`previousSets` fields and fixed responsive detail-table column sizing after visual verification.

Decisions 8 and 9 were implemented as an extension after PR #159 and are covered by an added `session-trends` API test and two added Playwright chart tests. Implementing them also required adding the missing `GET /api/workouts/{workoutId}/session-trends` mock route to `WebAppFixture.cs`.
