# Quickstart: Sidebar Navigation Layout

**Feature**: 002-sidebar-navigation
**Date**: 2026-03-27

## Prerequisites

- .NET 10 SDK
- Node.js (for TypeScript compilation)
- Playwright browsers installed (`pwsh bin/Debug/net10.0/playwright.ps1 install` or `npx playwright install`)

## Build

From the repository root:

```bash
cd src/WorkoutTracker.Web
npm install
npm run build
```

Or build the entire solution (which triggers TypeScript compilation automatically):

```bash
cd src
dotnet build
```

## Run Locally

Start the full Aspire orchestration (requires Docker for PostgreSQL):

```bash
cd src
dotnet run --project WorkoutTracker.AppHost
```

The web app will be available at the URL shown in the Aspire dashboard (typically `https://localhost:7xxx`).

## Run Tests

Build TypeScript first, then run the test suite:

```bash
cd src/WorkoutTracker.Web && npm run build && cd ..
dotnet test
```

## Key Files for This Feature

| File | Purpose |
| ---- | ------- |
| `src/WorkoutTracker.Web/wwwroot/index.html` | HTML shell with sidebar and content area |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | All styles including sidebar layout |
| `src/WorkoutTracker.Web/wwwroot/ts/main.ts` | Application entry point — initialises router and sidebar |
| `src/WorkoutTracker.Web/wwwroot/ts/router.ts` | Client-side History API router |
| `src/WorkoutTracker.Web/wwwroot/ts/sidebar.ts` | Sidebar behaviour (active state, mobile toggle) |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` | Home page with workout form logic |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts` | Workouts placeholder page |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` | Exercises placeholder page |

## Development Workflow

1. Start TypeScript watch mode: `cd src/WorkoutTracker.Web && npm run watch`
2. In a separate terminal, run the Aspire host: `cd src && dotnet run --project WorkoutTracker.AppHost`
3. Make changes to `.ts` files — they auto-compile to `wwwroot/js/`
4. Refresh the browser to see changes

## Verification Checklist

- [ ] Sidebar visible on desktop, collapsible on mobile
- [ ] All three menu items navigate to correct pages
- [ ] Active state highlights the current page
- [ ] Home page form loads workout types and validates correctly
- [ ] Workouts and Exercises show placeholder content
- [ ] Deep linking works (e.g., navigating directly to `/workouts`)
- [ ] All existing E2E tests pass
- [ ] New E2E tests pass for sidebar behaviour
