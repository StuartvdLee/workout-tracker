<!--
Sync Impact Report
- Version change: template-unversioned -> 1.0.0
- Modified principles:
	- Placeholder Principle 1 -> I. Code Quality Is a Release Gate
	- Placeholder Principle 2 -> II. Testing Standards Are Mandatory
	- Placeholder Principle 3 -> III. UX Consistency Is Non-Negotiable
	- Placeholder Principle 4 -> IV. Performance Requirements Are Defined and Verified
- Added sections:
	- Engineering Standards
	- Delivery Workflow & Quality Gates
- Removed sections:
	- Placeholder Principle 5 section
- Templates requiring updates:
	- ✅ updated: .specify/templates/plan-template.md
	- ✅ updated: .specify/templates/spec-template.md
	- ✅ updated: .specify/templates/tasks-template.md
	- ✅ not applicable (directory absent): .specify/templates/commands/*.md
	- ✅ not applicable (file absent): README.md
	- ✅ not applicable (file absent): docs/quickstart.md
- Follow-up TODOs:
	- None
-->

# Workout Tracker Constitution

## Core Principles

### I. Code Quality Is a Release Gate
All production code changes MUST pass repository linting, formatting, and static checks before
merge. Code MUST favor clear names, cohesive modules, and removal of dead or duplicate logic.
Non-trivial design decisions MUST be captured in feature planning artifacts so reviewers can
verify intent and maintainability.

Rationale: Enforcing objective quality gates reduces defects, keeps the codebase readable, and
prevents drift into unmaintainable patterns.

### II. Testing Standards Are Mandatory
Every behavior change MUST include automated tests at the appropriate level (unit, integration,
or end-to-end). Every bug fix MUST include a regression test that fails before the fix and passes
afterward. Merge to the default branch MUST be blocked when required tests fail.

Rationale: Mandatory automated tests create confidence in frequent changes and prevent known
failures from reappearing.

### III. UX Consistency Is Non-Negotiable
All user-facing changes MUST use approved UI patterns, interaction behaviors, and terminology
defined for this product. Accessibility expectations (keyboard navigation, visible focus,
semantic labels, and adequate contrast) MUST be met for affected screens. Intentional deviations
MUST be documented in the spec and approved during review.

Rationale: Consistent and accessible experiences reduce user confusion, support costs, and
rework across features.

### IV. Performance Requirements Are Defined and Verified
Each feature specification MUST define measurable performance targets for impacted user journeys
or system operations. Changes on performance-sensitive paths MUST include evidence from profiling,
benchmarks, or production-like measurements. Regressions beyond agreed budgets MUST be fixed or
formally waived before release.

Rationale: Explicit budgets and evidence-based validation prevent hidden slowdowns and preserve
product responsiveness as scope grows.

## Engineering Standards

- Plans and specs MUST identify quality, test, UX, and performance impacts before implementation.
- Pull requests MUST remain focused in scope and include rationale for any trade-off decisions.
- Tooling and framework choices MUST prioritize maintainability and team operability over novelty.
- Definition of Done MUST include updated tests, validated UX behavior, and measured performance
	where applicable.

## Delivery Workflow & Quality Gates

- Spec Phase: include independently testable user scenarios and measurable outcomes.
- Plan Phase: complete Constitution Check with pass/fail evidence for each core principle.
- Implementation Phase: follow task ordering that preserves testability and independent delivery.
- Review Phase: reviewers MUST confirm constitutional compliance before approval.
- Release Phase: performance and UX validation artifacts MUST be attached for affected changes.

## Governance

This constitution is the source of truth for engineering practices in this repository and
supersedes conflicting process guidance.

- Amendment process: propose changes via pull request that includes rationale, impact assessment,
	and updates to affected templates and guidance files.
- Approval policy: at least one maintainer approval is required for any constitutional amendment.
- Versioning policy: semantic versioning is mandatory for this document.
	- MAJOR: backward-incompatible principle removals or redefinitions.
	- MINOR: new principle or materially expanded guidance.
	- PATCH: clarifications, wording improvements, and non-semantic refinements.
- Compliance review: every feature plan and pull request MUST include an explicit constitution
	compliance check; unresolved violations MUST be tracked with owner and due date.

**Version**: 1.0.0 | **Ratified**: 2026-02-27 | **Last Amended**: 2026-02-27
