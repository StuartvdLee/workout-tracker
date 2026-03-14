# Implementation Plan: Home Landing Page

**Branch**: `001-home-landing-page` | **Date**: 2026-03-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-home-landing-page/spec.md`

## Summary

Build the initial home/landing page for Workout Tracker. The page displays
the application title, a dropdown to select a workout type (Push, Pull, or
Legs), and a "Start Workout" button with client-side validation. No backend
interaction is required for this feature; workout types are hardcoded. The
page must be mobile-first and responsive across viewports from 320 px to
1920 px.

## Technical Context

**Language/Version**: C# on .NET 10 (backend), vanilla TypeScript (frontend)
**Primary Dependencies**: .NET Aspire (latest), ASP.NET Core minimal API
**Storage**: PostgreSQL (not used in this feature — no data persistence yet)
**Testing**: xUnit + Playwright (E2E/integration for frontend behavior)
**Target Platform**: Web browser (mobile-first, responsive to desktop)
**Project Type**: Web application (Aspire-orchestrated)
**Performance Goals**: Interactive within 3 seconds on slow 3G; Lighthouse
mobile score ≥ 90
**Constraints**: No JavaScript frameworks; vanilla HTML/CSS/TypeScript only
for the frontend
**Scale/Scope**: Single-user personal tracker; 1 screen in this feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: C# follows `dotnet format` and .NET coding conventions.
  TypeScript uses strict mode with no `any` types. CSS uses a consistent
  naming convention (BEM). All code passes linting before merge. ✅ No
  deviations required.
- **Testing**: Playwright E2E tests cover all three user stories: valid
  selection acceptance, validation error on empty selection, and responsive
  layout verification. Unit tests cover the TypeScript validation logic.
  ✅ Tests are mandatory, not optional.
- **Security**: Minimal attack surface — no user-provided free text, no
  authentication, no secrets, no third-party integrations. The dropdown
  has a fixed set of values; the button triggers client-side validation
  only. ✅ No security concerns for this feature.
- **User Experience Consistency**: This is the first page and establishes
  the foundational patterns: vertically stacked centered layout, error
  message styling (colored text near the control), touch-friendly sizing
  (44 × 44 pt minimum). Future pages MUST follow these patterns. ✅ New
  standard defined in spec UX-001 through UX-004.
- **Performance**: Budget is 3 seconds to interactive on slow 3G and
  Lighthouse ≥ 90 on mobile. Verified via Lighthouse CI or manual audit.
  No remote data calls; static content only. ✅ Budget defined and
  verification method stated.

## Project Structure

### Documentation (this feature)

```text
specs/001-home-landing-page/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (none for this feature)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── WorkoutTracker.AppHost/                # Aspire orchestrator
│   ├── Program.cs
│   └── WorkoutTracker.AppHost.csproj
│
├── WorkoutTracker.ServiceDefaults/        # Shared service configuration
│   ├── Extensions.cs
│   └── WorkoutTracker.ServiceDefaults.csproj
│
├── WorkoutTracker.Api/                    # Backend C# API (minimal for now)
│   ├── Program.cs
│   └── WorkoutTracker.Api.csproj
│
├── WorkoutTracker.Web/                    # Frontend — vanilla HTML/CSS/TS
│   ├── wwwroot/
│   │   ├── index.html                     # Home landing page
│   │   ├── css/
│   │   │   └── styles.css                 # Application styles
│   │   ├── ts/
│   │   │   └── main.ts                    # Validation and interaction logic
│   │   └── js/                            # Compiled TypeScript output
│   │       └── main.js
│   ├── Program.cs                         # Static file server
│   └── WorkoutTracker.Web.csproj
│
└── WorkoutTracker.Tests/                  # Test project
    ├── E2E/
    │   └── HomePageTests.cs               # Playwright E2E tests
    ├── Unit/
    │   └── (TypeScript unit tests via a
    │       separate test runner if needed)
    └── WorkoutTracker.Tests.csproj
```

**Structure Decision**: .NET Aspire solution with four projects inside a
`src/` directory — AppHost (orchestrator), ServiceDefaults (shared config),
Api (backend, minimal for now), and Web (static file server for vanilla
HTML/CSS/TS frontend). Tests live in a dedicated test project using
Playwright for E2E coverage. All projects are under `src/` to keep the
repository root clean.

## Complexity Tracking

> No constitution violations. No entries required.
