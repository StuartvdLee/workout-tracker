# Implementation Plan: Sidebar Navigation Layout

**Branch**: `002-sidebar-navigation` | **Date**: 2026-03-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-sidebar-navigation/spec.md`

## Summary

Rework the single-page workout tracker application to use a persistent sidebar navigation layout with three sections — Home, Workouts, and Exercises. The Home page retains the existing workout selection form. Workouts and Exercises are placeholder pages. Client-side routing via the History API drives page switching without full reloads. The sidebar is always visible on desktop (≥768px) and collapsible behind a hamburger toggle on mobile. Each menu item includes an inline SVG icon. All existing Playwright E2E tests must continue to pass after the layout change.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, vanilla TypeScript (no JS frameworks), CSS custom properties
**Storage**: PostgreSQL via Entity Framework Core (unchanged by this feature)
**Testing**: xUnit 3.0 + Microsoft Playwright 1.58.0 for E2E tests
**Target Platform**: Web browser (desktop and mobile viewports)
**Project Type**: Web application (SPA with static file serving)
**Performance Goals**: Page switches under 100ms perceived time, zero CLS from sidebar
**Constraints**: No external JS/CSS libraries; vanilla TypeScript only; existing design token system must be extended, not replaced
**Scale/Scope**: 3 pages, 1 sidebar, 1 mobile toggle — small surface area; ~6 existing E2E test files to update

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode is enforced via `tsconfig.json` (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`). CSS follows BEM-like naming (`app__title`, `workout-form__select`). All warnings are errors in the .NET build (`TreatWarningsAsErrors`). The new sidebar and router code must follow the same conventions. No new linting tools needed — existing `tsc` compilation enforces type safety.
- **Testing**: Every user story requires Playwright E2E coverage. The existing 6 test classes covering the home page must be updated to account for the new layout (sidebar present, content area scoped). New test classes are needed for: sidebar navigation behaviour, active state highlighting, mobile toggle, keyboard accessibility, and placeholder pages. Tests run via `dotnet test` from the `src/` directory after TypeScript compilation.
- **Security**: This feature introduces no new inputs, APIs, or data flows. Navigation is entirely client-side. The existing `/api/workout-types` proxy and input validation are unchanged. No new trust boundaries or secret handling required.
- **User Experience Consistency**: The sidebar must use existing CSS custom properties (`--color-primary`, `--font-family`, `--spacing-*`, `--radius`, etc.). Active state uses `--color-primary` as the accent. The 768px mobile breakpoint matches the existing responsive breakpoint in `styles.css`. Placeholder pages follow the same typography and spacing patterns as the Home page.
- **Performance**: Page switches are purely DOM manipulation — no network requests. The sidebar is rendered once and persists across navigations. SVG icons are inlined in HTML (no icon font or image requests). CLS is prevented by giving the sidebar a fixed width that does not change. Performance verified via Playwright timing assertions in E2E tests.

## Project Structure

### Documentation (this feature)

```text
specs/002-sidebar-navigation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
├── wwwroot/
│   ├── index.html                  # Reworked: sidebar + content area shell, inline SVG icons
│   ├── css/
│   │   └── styles.css              # Extended: sidebar layout, nav items, active states, mobile toggle, page styles
│   └── ts/
│       ├── main.ts                 # Reworked: initialises router, sets up sidebar event listeners
│       ├── router.ts               # NEW: History API router — registers routes, handles popstate, renders pages
│       ├── pages/
│       │   ├── home.ts             # NEW: Home page module — contains existing workout form logic
│       │   ├── workouts.ts         # NEW: Workouts placeholder page
│       │   └── exercises.ts        # NEW: Exercises placeholder page
│       └── sidebar.ts              # NEW: Sidebar behaviour — active state, mobile toggle, keyboard nav
├── Program.cs                      # Unchanged (MapFallbackToFile already supports client-side routing)
└── tsconfig.json                   # Unchanged

src/WorkoutTracker.Tests/
├── E2E/
│   ├── HomeLandingPage*.cs         # Updated: scoped selectors to content area within new layout
│   ├── SidebarNavigationTests.cs   # NEW: navigation between pages, active state, deep linking
│   ├── SidebarMobileTests.cs       # NEW: mobile toggle, collapse on selection, responsive breakpoints
│   ├── SidebarAccessibilityTests.cs # NEW: keyboard nav, ARIA attributes, focus management
│   ├── WorkoutsPageTests.cs        # NEW: placeholder content renders correctly
│   └── ExercisesPageTests.cs       # NEW: placeholder content renders correctly
└── Infrastructure/
    └── WebAppFixture.cs            # Unchanged (fallback routing already configured)
```

**Structure Decision**: The existing project layout is preserved. New TypeScript modules are added under `wwwroot/ts/` following ES module conventions with a `pages/` subdirectory for page-specific code. This keeps the vanilla TypeScript approach with no bundler — the `tsconfig.json` already outputs ES2022 modules. The router, sidebar, and page modules are separate files for single-responsibility and testability.

## Complexity Tracking

> No constitution violations. The feature uses existing patterns and technologies throughout.
