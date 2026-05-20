# Implementation Plan: Exercises Muscles Link

**Branch**: `019-exercises-muscles-link` | **Date**: 2026-05-20 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/019-exercises-muscles-link/spec.md`

## Summary

The Exercises page has two places where a "Targeted muscles (optional)" label appears — the Add Exercise form and the Edit Exercise modal. This feature adds a small inline "Manage" link inside each label, separated by a dash (e.g. `Targeted muscles (optional) – Manage`). Clicking the link navigates to the Muscles page (`/muscles`) via the existing SPA router. No backend, API, or data model changes are required. The change touches one TypeScript file (`exercises.ts`), one CSS file (`styles.css`), and one E2E test file (`ExercisesPageTests.cs`).

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, vanilla TypeScript (no JS frameworks)  
**Storage**: N/A — no data model changes  
**Testing**: Playwright E2E tests (`WebAppFixture` + `PlaywrightFixture`)  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: No measurable change — the link is static HTML with a single event listener  
**Constraints**: Inline SPA navigation via `navigate()` from `router.ts`; strict TypeScript; BEM CSS naming; existing tests must continue to pass  
**Scale/Scope**: One TypeScript file modified, one CSS file modified, one E2E test file updated

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Changes are additive — two small HTML string additions in `exercises.ts` and one CSS rule. A click listener using `navigate('/muscles')` from the already-imported router keeps navigation consistent with the sidebar pattern. No new abstractions, no dead code, no TypeScript `any`. BEM: new class `.exercise-form__manage-link`. ✅

- **Testing**: Three new E2E tests in `ExercisesPageTests.cs`:
  - `MusclesLink_IsVisibleInAddForm` — asserts the "Manage" link is visible inside the "Targeted muscles (optional)" label in the Add Exercise form.
  - `MusclesLink_NavigatesToMusclesPage` — clicks the link and asserts the browser navigates to `/muscles`.
  - `MusclesLink_IsVisibleInEditModal` — opens the Edit modal for an existing exercise and asserts the link is present.
  No unit tests needed — the behaviour is pure navigation with no logic. ✅

- **Security**: The link is a static internal anchor — no user input, no data handling, no new API surface. No security concerns. ✅

- **User Experience Consistency**: The link uses the same inline navigation mechanism as the sidebar (SPA `navigate()`). Styling follows BEM. The link appears in the same relative position in both the Add form and the Edit modal, giving a consistent experience across both contexts. ✅

- **Performance**: A single `addEventListener('click', ...)` — negligible overhead. No extra network requests. ✅

## Project Structure

### Documentation (this feature)

```text
specs/019-exercises-muscles-link/
├── plan.md              # This file
├── research.md          # Phase 0 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

*No `data-model.md`, `quickstart.md`, or `contracts/` — pure frontend UI addition with no API surface or data model changes.*

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                      # MODIFIED: add .exercise-form__manage-link rule
    └── ts/
        └── pages/
            └── exercises.ts                # MODIFIED: add "Manage" anchor (inside label, dash-separated)
                                            #           in both Add form and Edit modal label groups;
                                            #           import navigate from ../router.js;
                                            #           initMusclesLinks() attaches click handlers

src/WorkoutTracker.E2ETests/
└── E2E/
    └── ExercisesPageTests.cs               # MODIFIED: add MusclesLink_IsVisibleInAddForm,
                                            #           MusclesLink_NavigatesToMusclesPage,
                                            #           MusclesLink_IsVisibleInEditModal
```

**Structure Decision**: Web application pattern (ASP.NET Core + Aspire + vanilla TypeScript SPA). Consistent with all prior features.

## Complexity Tracking

> No constitution violations.
