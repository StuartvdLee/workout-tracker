<!--
Sync Impact Report
- Version change: template (unratified) -> 1.0.0
- Modified principles:
  - Template principle slot 1 -> I. Code Quality Is Mandatory
  - Template principle slot 2 -> II. Tests Prove Behavior
  - Template principle slot 3 -> III. Security By Default
  - Template principle slot 4 -> IV. Consistent User Experience
  - Template principle slot 5 -> V. Performance Is a Feature
- Added sections:
  - Delivery Standards
  - Review & Release Workflow
- Removed sections:
  - None
- Templates requiring updates:
  - ✅ .specify/templates/plan-template.md
  - ✅ .specify/templates/spec-template.md
  - ✅ .specify/templates/tasks-template.md
  - ⚠ pending: .specify/templates/commands/*.md (directory not present)
- Follow-up TODOs:
  - None
-->
# Workout Tracker Constitution

## Core Principles

### I. Code Quality Is Mandatory
All production code MUST be readable, cohesive, and intentionally designed.
Changes MUST follow established project patterns, use descriptive names, avoid
duplication, and improve any touched code rather than merely adding to its
entropy. Reviews MUST reject speculative abstractions, dead code, and changes
that bypass linting, typing, formatting, or equivalent quality gates.

Rationale: workout-tracker will evolve through repeated feature additions. A
high quality bar keeps the codebase maintainable, easier to review, and safer
to extend without regressions.

### II. Tests Prove Behavior
Every behavior change MUST be backed by automated tests at the appropriate
level before the change is considered complete. New user journeys MUST include
integration or end-to-end coverage, critical domain logic MUST include unit
coverage, and bug fixes MUST ship with a regression test that fails before the
fix and passes after it. Manual verification MAY supplement automated tests,
but it MUST NOT replace them.

Rationale: test coverage is the executable proof that the product still works
as intended and that future changes can be made with confidence.

### III. Security By Default
Security requirements MUST be addressed during design, implementation, and
review. Inputs MUST be validated, secrets MUST be excluded from source control,
privileged operations MUST enforce explicit authorization, and dependencies or
third-party integrations MUST be introduced with a documented trust and data
handling review. When a tradeoff exists, the safer default MUST be chosen and
any exception MUST be justified in writing.

Rationale: security failures are expensive and trust-eroding. Making security
default behavior prevents rushed features from creating avoidable risk.

### IV. Consistent User Experience
User-facing changes MUST preserve a coherent experience across flows, screens,
copy, and interaction patterns. New interfaces MUST align with existing visual
and behavioral conventions or explicitly define a replacement standard before
implementation. Error states, empty states, loading states, and success
feedback MUST be deliberate and consistent wherever similar user actions occur.

Rationale: consistency reduces user confusion, lowers support burden, and makes
the product feel reliable even as it grows.

### V. Performance Is a Feature
Features MUST define measurable performance expectations before implementation
and MUST be validated against them before release. Changes MUST avoid
unbounded work, unnecessary network or storage overhead, and avoidable render
or interaction latency. When performance budgets cannot be met immediately, the
gap MUST be recorded with a mitigation plan before merging.

Rationale: performance directly shapes user trust and product usability.
Treating it as a first-class requirement prevents slow degradation over time.

## Delivery Standards

- Every feature specification MUST define security, user experience, and
  performance expectations alongside functional requirements.
- Every implementation plan MUST document the quality gates, test strategy,
  performance budgets, and any constitution exceptions before build work starts.
- Every task breakdown MUST include explicit work for automated testing,
  security validation, experience consistency checks, and performance
  verification when a change affects those concerns.
- Definition of done for any change includes passing validation, updated
  documentation when behavior changes, and evidence that the affected user
  journey still works end to end.

## Review & Release Workflow

- Pull requests MUST describe the user-visible or operational impact, the tests
  executed, and any security or performance considerations.
- Reviewers MUST verify constitution compliance, including code quality,
  coverage depth, security posture, UX consistency, and stated performance
  expectations.
- Releases MUST NOT include known constitution violations unless they are
  documented in an approved exception with owner, scope, and follow-up plan.
- When a change introduces a new pattern, the corresponding spec, plan, and
  task templates MUST be updated in the same stream of work if future features
  need to follow that pattern.

## Governance

- This constitution supersedes conflicting local habits and template defaults.
- Amendments MUST be proposed as repository changes that include the
  constitution diff, a clear rationale, and any required template or workflow
  updates.
- Semantic versioning governs this document: MAJOR for incompatible governance
  changes or principle removals, MINOR for new principles or materially
  expanded guidance, and PATCH for clarifications that do not change expected
  behavior.
- Compliance review is mandatory in feature planning and pull request review.
  The Constitution Check in plans and the release review checklist MUST confirm
  adherence or document approved exceptions.
- Ratification and amendment dates MUST use ISO format (`YYYY-MM-DD`).

**Version**: 1.0.0 | **Ratified**: 2026-03-14 | **Last Amended**: 2026-03-14
