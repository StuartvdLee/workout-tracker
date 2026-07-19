# Research: Latest Exercise Data

**Feature**: `029-latest-exercise-data`  
**Date**: 2026-07-19

## Decision: Keep the existing previous-performance endpoint

**Decision**: Reuse `GET /api/workouts/{workoutId}/previous-performance` and change its semantics from "immediately previous session" to "latest usable data per exercise".

**Rationale**: Feature 008 already introduced this endpoint as the active-session "Last time" contract, and the Web project already proxies the same route. Keeping the route preserves the current fetch path in `active-session.ts` and avoids creating duplicate API concepts for the same UI.

**Alternatives considered**:
- Add `GET /api/workouts/{workoutId}/latest-exercise-data`: rejected because it duplicates the previous-performance contract and would require unnecessary proxy/frontend rewiring.
- Compute fallback entirely on the frontend from session history: rejected because the active-session page does not fetch full history and should not own historical query semantics.

## Decision: Define usable data as weight or effort

**Decision**: A historical logged exercise is usable for "Last time" when it has a non-blank `loggedWeight` or a non-null `effort`. `sequence` may be returned as context for the selected row, but sequence alone MUST NOT make an entry usable.

**Rationale**: The bug occurs when skipped or empty exercise entries in a recent workout hide older meaningful data. Sequence describes order, not performance. Existing partial-data behavior should remain useful when only weight or only effort was recorded.

**Alternatives considered**:
- Require both weight and effort: rejected because existing behavior and tests support partial previous data.
- Treat sequence as usable data: rejected because it can produce a "Last time" display without performance information.
- Treat any logged exercise row as usable: rejected because skipped rows are the root cause.

## Decision: Select data independently per exercise

**Decision**: For each exercise in the planned workout, select the newest usable logged exercise from completed sessions for that same planned workout. Different exercises in the same response may come from different historical sessions.

**Rationale**: The issue asks for latest available data for an exercise, not for a single latest session. If exercise A was completed yesterday and exercise B was skipped yesterday but completed last week, each should display its own latest meaningful data.

**Alternatives considered**:
- Find the newest session that has any usable data and return all rows from that session: rejected because one skipped exercise in that session would still remain blank despite older data.
- Find the newest session where all current exercises have usable data: rejected because it hides newer valid data for exercises that were completed more recently.

## Decision: Preserve planned-workout scoping

**Decision**: Latest available data only considers sessions for the requested planned workout.

**Rationale**: Feature 008 explicitly scoped previous performance to the planned workout, and existing tests verify that the same exercise in a different workout does not contribute data. This feature fixes skipped entries without expanding comparison scope.

**Alternatives considered**:
- Search all sessions containing the same exercise regardless of planned workout: rejected because it would change established product semantics and could show data from a different routine context.

## Decision: Use a bounded newest-first read

**Decision**: Query workout sessions for the planned workout in descending `CompletedAt` order, project only fields needed for selection, and stop once all current workout exercises have selected usable data or a documented cap is reached.

**Rationale**: Feature 025 established capped historical reads for trend data. This feature can follow the same principle to avoid unbounded work for users with long history while keeping the query scoped to one planned workout.

**Alternatives considered**:
- Load all sessions for the planned workout: rejected because the constitution requires avoiding unbounded work.
- Add a denormalized latest-performance table: rejected because this is a read-path behavior change and does not require schema changes.
