# Phase 0 Research: Simplified Homepage Session Start

## Decision 1: Technology stack continuity
- Decision: Reuse the exact stack from feature 001: ASP.NET Core Web API + EF Core/Npgsql backend, React + TypeScript frontend, existing test tooling (xUnit, Vitest/RTL, Playwright).
- Rationale: User explicitly requested no technology deviation; this also minimizes delivery risk and onboarding overhead.
- Alternatives considered:
  - Introduce new UI/form library for homepage form: rejected due to unnecessary change and inconsistency.
  - Introduce new API layer abstraction: rejected because existing layering already supports this scope.

## Decision 2: Session start timestamp source of truth
- Decision: Capture session `startedAt` server-side at successful session creation time.
- Rationale: Requirement removes date/time input from homepage while still requiring persisted timestamp; server-generated time is deterministic and avoids client clock skew.
- Alternatives considered:
  - Client-provided timestamp hidden from UI: rejected due to potential drift and tampering.
  - Database default only without service-level control: rejected because application-level consistency checks are clearer.

## Decision 3: Workout type domain constraint
- Decision: Restrict allowed workout types to enum-like values `Push`, `Pull`, and `Legs` in API validation and UI options.
- Rationale: Spec requires exactly these three options and required selection prior to session creation.
- Alternatives considered:
  - Free-text workout type: rejected due to invalid/fragmented data risk.
  - Configurable list from settings table: rejected as out of MVP scope.

## Decision 4: Homepage simplification scope
- Decision: Remove homepage links `Session`, `History`, `Progression` and remove content at/under `Add Exercise Entry`, replacing with minimal start-session controls only.
- Rationale: Aligns directly with P1/P3 scope and improves focus on one primary journey.
- Alternatives considered:
  - Keep links but visually de-emphasize: rejected because requirement is explicit removal.
  - Keep Add Exercise Entry collapsed: rejected because requirement removes it entirely.

## Decision 5: Validation behavior pattern
- Decision: Apply existing required-field validation pattern on submit attempt, with inline error cleared once a valid selection is made.
- Rationale: Preserves UX consistency and accessibility conventions already established in the app.
- Alternatives considered:
  - Disable button until selection exists: rejected because spec calls out error message on invalid submit.
  - Toast-only errors: rejected because inline field association is clearer and more accessible.

## Decision 6: API contract impact
- Decision: Document `POST /sessions` contract for this feature so request requires `workoutType` and response returns server-generated `startedAt`.
- Rationale: Frontend and backend can implement independently with a clear behavioral contract and testable payload expectations.
- Alternatives considered:
  - Keep contract implicit in code only: rejected because it weakens cross-layer verification.
  - Add a new endpoint for simplified flow: rejected as unnecessary API surface growth.

## Decision 7: Performance verification approach
- Decision: Validate homepage render and start-session latency using existing integration/E2E tests plus lightweight timing evidence in PR.
- Rationale: Meets constitution performance gate and aligns with already-used tooling.
- Alternatives considered:
  - Dedicated load test framework for this feature only: rejected as disproportionate for this scope.
  - No explicit timing evidence: rejected due to constitution requirements.
