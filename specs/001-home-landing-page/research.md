# Research: Home Landing Page

**Date**: 2026-03-14
**Feature**: 001-home-landing-page

## 1. .NET Aspire Project Structure

**Decision**: Use a standard .NET Aspire solution with four projects:
AppHost, ServiceDefaults, Api, and Web.

**Rationale**: Aspire provides built-in orchestration, service discovery,
and observability. The AppHost project wires all services together. The
Web project serves the static frontend (vanilla HTML/CSS/TypeScript)
using ASP.NET Core's static file middleware. The Api project handles
backend logic in future features.

**Alternatives considered**:
- Single monolithic project serving both API and static files — rejected
  because Aspire's multi-project model gives better separation of
  concerns and independent scaling.
- Separate SPA framework (React/Angular/Vue) — rejected per user
  requirement for vanilla HTML/CSS/TypeScript.

## 2. Frontend Approach: Vanilla HTML/CSS/TypeScript

**Decision**: Use plain HTML with a `<select>` element for the workout
dropdown, a `<button>` for "Start Workout", and TypeScript compiled to
JavaScript for client-side validation logic. CSS handles responsive
layout with media queries.

**Rationale**: The user explicitly requires vanilla HTML, CSS, and
TypeScript. No build framework is needed beyond the TypeScript compiler.
The page is entirely static for this feature — no API calls, no dynamic
content loading.

**Alternatives considered**:
- Web Components — more structured but adds complexity for a single page
  with three controls. Can be introduced later if component reuse becomes
  necessary.
- Server-side rendered Razor Pages — rejected because the user specified
  vanilla HTML/CSS/TypeScript for the frontend layer.

## 3. TypeScript Compilation

**Decision**: Use the TypeScript compiler (`tsc`) directly. Source files
live in `wwwroot/ts/` and compile to `wwwroot/js/`. A `tsconfig.json`
at the Web project root configures strict mode, ES2022 target, and
output directory.

**Rationale**: No bundler needed for a single TypeScript file. `tsc`
provides type safety with minimal tooling. The compiled JavaScript is
served as a static asset by the Web project.

**Alternatives considered**:
- esbuild or Vite for bundling — overkill for a single file; adds
  unnecessary dependency. Revisit when multiple TypeScript modules exist.

## 4. Testing Strategy

**Decision**: Playwright for E2E tests covering all user stories.
Tests run in the `WorkoutTracker.Tests` project using the xUnit test
runner with the `Microsoft.Playwright` NuGet package.

**Rationale**: Playwright provides cross-browser testing and integrates
well with .NET via the official NuGet package. E2E tests are the right
level for verifying dropdown selection, button clicks, error message
display, and responsive layout — all browser-based interactions.

**Alternatives considered**:
- Selenium — heavier, slower, and less modern than Playwright.
- Jest + jsdom for TypeScript unit tests — useful for isolated logic but
  cannot verify DOM behavior or responsive layout. May be added later
  for complex client-side logic.

## 5. Responsive Design Approach

**Decision**: Mobile-first CSS with a single breakpoint at 768 px. Base
styles target mobile (≤ 480 px), with a media query for wider viewports.
Controls are centered with max-width constraints on larger screens.

**Rationale**: The user primarily uses the app on mobile. A single
breakpoint keeps CSS simple while covering the spec requirement for
320 px–1920 px viewport range. Touch targets default to 44 × 44 pt
minimum via explicit sizing on the dropdown and button.

**Alternatives considered**:
- Multiple breakpoints (480, 768, 1024, 1440) — unnecessary complexity
  for a page with three vertically stacked elements.
- CSS frameworks (Tailwind, Bootstrap) — rejected to stay vanilla.

## 6. Static File Hosting in ASP.NET Core

**Decision**: The `WorkoutTracker.Web` project uses
`app.UseStaticFiles()` to serve `wwwroot/` content. A fallback route
maps `/` to `index.html`.

**Rationale**: Built-in ASP.NET Core middleware; zero additional
dependencies. Serves HTML, CSS, and compiled JS directly.

**Alternatives considered**:
- CDN/external hosting — unnecessary for a personal tracker; adds
  deployment complexity.
- Kestrel with reverse proxy — overkill at this stage; Aspire handles
  service routing.
