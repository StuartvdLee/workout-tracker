# Implementation Plan: Add Exercises

**Branch**: `003-add-exercises` | **Date**: 2026-07-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-add-exercises/spec.md`

## Summary

Replace the placeholder Exercises page with a fully functional exercise management interface. Users can create, view, and edit exercises with a required name and optional targeted muscle associations. The backend introduces new API endpoints, a Muscle entity with seed data, and an ExerciseMuscle junction table. The frontend replaces the exercises placeholder with a form (supporting create and edit modes) and a scrollable exercise list. All data persists in PostgreSQL via Entity Framework Core. The existing predefined muscle list (11 muscles) is stored as seeded database rows for referential integrity and API-driven display.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
**Storage**: PostgreSQL via Entity Framework Core — extending existing schema with Muscle entity, ExerciseMuscle junction table, and constraints on Exercise
**Testing**: xUnit 3.2.2 + Microsoft Playwright 1.58.0 for E2E tests; mock API endpoints in WebAppFixture
**Target Platform**: Web browser (mobile-first, responsive to desktop)
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: Exercises page loads and displays list within 3 seconds on slow 3G (PR-001); visual feedback within 200ms of form submission (PR-002)
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing CSS custom properties must be extended, not replaced; existing tests must continue to pass
**Scale/Scope**: Single-user personal tracker; 1 page reworked, 4 new API endpoints, 1 new EF migration, ~15 new E2E test cases

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced via `tsconfig.json` (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`). CSS follows BEM naming convention established in `styles.css`. C# uses .NET nullable reference types (`<Nullable>enable</Nullable>`) and snake_case DB naming convention via `UseSnakeCaseNamingConvention()`. All new code must follow these established patterns. ✅ No deviations required.
- **Testing**: Playwright E2E tests cover all four user stories: create exercise with name, assign muscles, view exercise list, and edit exercise. Tests cover validation (empty name, whitespace-only, max length, duplicate names), form state transitions (create ↔ edit mode), empty state, and error handling. The existing `WebAppFixture` mock API pattern is extended with exercise and muscle endpoints. ✅ Tests are mandatory, not optional.
- **Security**: User-provided exercise names must be validated on both client (TypeScript) and server (API). Name is trimmed, checked for emptiness, capped at 150 characters, and checked for case-insensitive uniqueness. Muscle IDs are validated against the known predefined set (whitelist). No authentication required (single-user app). No secrets introduced. No third-party dependencies added. ✅ Input validation on both tiers; no new trust boundaries.
- **User Experience Consistency**: The exercise form follows the same patterns as the Home page workout form: BEM CSS classes, inline error messages with `role="alert"` and `aria-live="polite"`, `novalidate` with custom validation, existing design tokens for spacing, colours, and typography. The exercise list uses the same `--max-content-width` constraint. Empty state follows the placeholder pattern. Touch targets meet `--min-touch-target` (44px). Edit mode is visually distinct with a different submit label and a cancel button. ✅ All existing patterns followed.
- **Performance**: Page load budget is 3 seconds on slow 3G (consistent with Home page). API calls for exercises and muscles are parallelised on page load. The exercise list renders synchronously after data fetch — no pagination needed for initial scope (personal tracker with modest exercise count). Form submission provides immediate visual feedback via loading state. ✅ Budgets defined; verification via Playwright timing assertions.

## Project Structure

### Documentation (this feature)

```text
specs/003-add-exercises/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── api-contract.md  # REST API endpoints
│   └── ui-contract.md   # HTML/CSS/ARIA contracts
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Infrastructure/
├── Data/
│   ├── Models/
│   │   ├── Exercise.cs              # MODIFIED: add MaxLength, navigation to ExerciseMuscle
│   │   ├── Muscle.cs                # NEW: predefined muscle entity
│   │   └── ExerciseMuscle.cs        # NEW: junction table (Exercise ↔ Muscle)
│   ├── Migrations/
│   │   └── [timestamp]_AddMusclesAndExerciseConstraints.cs  # NEW: migration
│   └── WorkoutTrackerDbContext.cs   # MODIFIED: add DbSets, entity config, seed data

src/WorkoutTracker.Api/
└── Program.cs                       # MODIFIED: add exercise and muscle API endpoints

src/WorkoutTracker.Web/
├── wwwroot/
│   ├── css/
│   │   └── styles.css               # MODIFIED: add exercise form, list, muscle chips styles
│   └── ts/
│       └── pages/
│           └── exercises.ts          # MODIFIED: replace placeholder with full exercise page

src/WorkoutTracker.Tests/
├── E2E/
│   └── ExercisesPageTests.cs        # MODIFIED: replace placeholder tests with full coverage
└── Infrastructure/
    └── WebAppFixture.cs             # MODIFIED: add mock exercise and muscle API endpoints
```

**Structure Decision**: The existing .NET Aspire solution structure is preserved. New entities are added to the Infrastructure project's Models directory following the established pattern. The API endpoints are added to the existing `Program.cs` minimal API file. The exercises page module is updated in-place. No new projects or structural changes needed.

## Complexity Tracking

> No constitution violations. The feature uses existing patterns and technologies throughout.
