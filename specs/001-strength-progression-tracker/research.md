# Phase 0 Research: Strength Progression Tracker

## Decision 1: Architecture style for backend and frontend
- Decision: Use a split web architecture with ASP.NET Core Web API backend and React SPA frontend.
- Rationale: Aligns with provided constraints (.NET 10 backend, React frontend), supports independent scaling, and enforces a clean API contract between UI and data services.
- Alternatives considered:
  - Monolithic server-rendered app: rejected due to weaker separation between API and frontend concerns.
  - Backend-for-frontend per page: rejected as unnecessary complexity for initial scope.

## Decision 2: Data access strategy for PostgreSQL
- Decision: Use Entity Framework Core with Npgsql provider and explicit migrations.
- Rationale: Provides strong model mapping, migration management, and query composability while keeping code maintainable for evolving schemas.
- Alternatives considered:
  - Dapper-only repository: rejected for higher manual mapping overhead at this stage.
  - Raw SQL only: rejected because it increases maintenance burden and test complexity.

## Decision 3: Exercise normalization for progression comparison
- Decision: Store both display exercise name and normalized key (`normalizedExerciseName`) for grouping history/comparisons.
- Rationale: Resolves edge cases involving case and spacing differences and guarantees deterministic aggregation.
- Alternatives considered:
  - Case-insensitive query only: rejected because spacing and punctuation variants remain inconsistent.
  - User-maintained canonical dictionary only: rejected as too high-friction for MVP.

## Decision 4: Progression computation baseline
- Decision: Compute two comparisons for each new log entry: latest-vs-previous and latest-vs-best.
- Rationale: Matches feature requirements and provides immediate practical feedback without overcomplicating analytics.
- Alternatives considered:
  - Percentile and trendline analytics: rejected for initial release complexity.
  - Best-only comparison: rejected because users also need immediate prior-session context.

## Decision 5: Testing pyramid and tooling
- Decision: Backend uses xUnit unit/integration tests, frontend uses Vitest + React Testing Library, and E2E uses Playwright for P1/P2/P3 critical flows.
- Rationale: Enforces constitution test standards with fast feedback at lower levels and confidence in cross-stack journeys.
- Alternatives considered:
  - E2E-only testing: rejected due to slow execution and low defect localization.
  - Unit-only testing: rejected because contract and integration failures would be under-covered.

## Decision 6: API contract format
- Decision: Define REST JSON endpoints via OpenAPI 3.0 contract under `contracts/`.
- Rationale: Enables independent backend/frontend implementation and explicit validation of payload shapes.
- Alternatives considered:
  - GraphQL API: rejected due to additional complexity without clear MVP benefit.
  - Implicit contract by code only: rejected because it weakens integration reliability.

## Decision 7: Performance and indexing approach
- Decision: Add indexes on `(userId, normalizedExerciseName, performedAt)` and use paginated history endpoints.
- Rationale: Supports p95 response targets for save/history/comparison within expected data volume.
- Alternatives considered:
  - No indexing initially: rejected due to likely degradation as history grows.
  - Precomputed materialized views: rejected as premature optimization for MVP.

## Decision 8: Validation and error handling pattern
- Decision: Validate numeric and range constraints at API boundary and mirror validation in frontend forms with consistent error messages.
- Rationale: Prevents bad data persistence and maintains UX consistency with immediate user feedback.
- Alternatives considered:
  - Backend-only validation: rejected due to slower user feedback loops.
  - Frontend-only validation: rejected due to trust and data integrity risks.
