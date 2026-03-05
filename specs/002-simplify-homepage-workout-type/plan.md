# Implementation Plan: Simplified Homepage Session Start

**Branch**: `002-simplify-homepage-workout-type` | **Date**: 2026-03-05 | **Spec**: [/specs/002-simplify-homepage-workout-type/spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-simplify-homepage-workout-type/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Deliver a simplified homepage focused on starting a workout session with a single required
"Workout Type" dropdown (`Push`, `Pull`, `Legs`) and one "Start Session" button. The frontend
removes legacy navigation/content and enforces required-field validation, while the backend stores
the selected workout type and captures session start date-time automatically at creation time.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# (.NET 10) backend, TypeScript (React) frontend  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core (Npgsql provider), React, React Router, existing form/state validation library  
**Storage**: PostgreSQL  
**Testing**: xUnit + integration tests (backend), Vitest + React Testing Library (frontend), Playwright for end-to-end journeys  
**Target Platform**: Modern desktop/mobile web browsers, Linux container or VM hosting for API
**Project Type**: Web application (frontend + backend API)  
**Performance Goals**: Match spec budgets: successful start-session p95 <=2s, homepage render p95 <=1s  
**Constraints**: No technology deviation from feature 001 stack, maintain accessibility and UX consistency, preserve existing non-homepage flows  
**Scale/Scope**: Single-homepage journey update; session creation path for one user with existing app data volumes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Gate Assessment (Pre-Research)

- **Code Quality Gate**: PASS — existing backend/frontend linting and static checks remain merge
  blockers; changes are limited to homepage/session-start paths.
- **Testing Gate**: PASS — add/adjust automated tests at backend integration, frontend integration,
  and E2E levels for required selection, hidden legacy elements, and timestamp creation behavior.
- **UX Consistency Gate**: PASS — reuse existing controls/messages and enforce keyboard/label/error
  accessibility for the simplified homepage flow.
- **Performance Gate**: PASS — explicit p95 targets from spec are measurable via integration + E2E
  timing checks and existing performance scripts.
- **Evidence Gate**: PASS — PR must include test outputs, homepage screenshots, and timing evidence
  for render and session-start API latency.

### Post-Design Gate Assessment (After Phase 1)

- **Code Quality Gate**: PASS — design keeps existing architecture boundaries and introduces no new
  frameworks or cross-cutting complexity.
- **Testing Gate**: PASS — data model, contract, and quickstart define concrete regression coverage
  for validation and automatic timestamp behavior.
- **UX Consistency Gate**: PASS — design specifies exact visible homepage elements and required
  accessibility behaviors with no unapproved deviations.
- **Performance Gate**: PASS — contract/data model maintain lightweight payloads and existing query
  paths; validation method and thresholds are documented.
- **Evidence Gate**: PASS — implementation checklist requires test logs, API contract checks,
  screenshots, and performance outputs before review approval.

## Project Structure

### Documentation (this feature)

```text
specs/002-simplify-homepage-workout-type/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── Api/
│   │   ├── Contracts/
│   │   ├── Controllers/
│   │   └── Program.cs
│   ├── Application/
│   │   └── Sessions/
│   └── Infrastructure/
│       ├── Persistence/
│       └── Repositories/
└── tests/
    ├── integration/
    └── unit/

frontend/
├── src/
│   ├── pages/
│   ├── features/
│   │   ├── sessions/
│   │   ├── exercises/
│   │   └── progression/
│   └── services/
└── tests/
    └── integration/

e2e/
└── tests/
```

**Structure Decision**: Use the existing web application split (`backend/`, `frontend/`, `e2e/`)
to implement the homepage simplification while preserving current deployment and testing patterns.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |
