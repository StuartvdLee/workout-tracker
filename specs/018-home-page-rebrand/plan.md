# Implementation Plan: Home Page Rebrand

**Branch**: `018-home-page-rebrand` | **Date**: 2026-05-20 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/018-home-page-rebrand/spec.md`

## Summary

The home page currently displays a house icon in the sidebar and an `<h1>` reading "Home" on the page itself. This feature renames both to "Let's go!" and replaces the sidebar icon with the Lucide flame icon (`path d="M12 3q1 4 4 6.5t3 5.5..."`). Changes are confined to three files: `index.html` (sidebar nav), `home.ts` (page `<h1>`), and two E2E test files that assert the current label text.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, vanilla TypeScript (no JS frameworks)  
**Storage**: N/A — no data model changes  
**Testing**: xUnit 3.2.2 + Playwright E2E tests (`WebAppFixture` + `PlaywrightFixture`)  
**Target Platform**: Web browser (mobile-first, responsive)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: No measurable change — icon is inline SVG, same render path as existing icons  
**Constraints**: Inline SVG only (no external icon library); strict TypeScript; BEM CSS naming; existing test suite must continue to pass after label updates  
**Scale/Scope**: Two source file changes (`index.html`, `home.ts`) + two E2E test file updates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Changes are purely cosmetic string/SVG substitutions in existing files. No new abstractions, no dead code, no bypass of linting or TypeScript strict mode. The flame SVG path is inline, matching the pattern of all other sidebar icons. ✅

- **Testing**: Two existing E2E tests assert the old label text and must be updated — `HomeLandingPageSelectionTests.HomePage_DisplaysTitle_WorkoutTracker` (asserts h1 = "Home") and `SidebarNavigationTests.Sidebar_MenuItems_HaveIconsAndLabels` (asserts sidebar label list includes "Home"). Both are updated in-place (no new test files needed — existing coverage is sufficient for a pure rename). ✅

- **Security**: No user input, no data handling, no new dependencies. The flame SVG path is a static string. No security concerns. ✅

- **User Experience Consistency**: The flame icon uses the same inline SVG attributes (`width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"`) as all other sidebar icons. The sidebar label "Let's go!" uses the same `<span class="sidebar__label">` element. The `<h1>` uses the same `class="home-page__title"` already in place. Light/dark mode compatibility is automatic via `stroke="currentColor"`. ✅

- **Performance**: Inline SVG — no extra network request. The single `<path>` element for the flame icon is smaller than the two-element house icon it replaces. No measurable change in load time. ✅

## Project Structure

### Documentation (this feature)

```text
specs/018-home-page-rebrand/
├── plan.md              # This file
├── research.md          # Phase 0 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

*No `data-model.md`, `quickstart.md`, or `contracts/` — pure UI rename with no API surface or data model.*

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── index.html                           # MODIFIED: sidebar home link — flame SVG + "Let's go!" label
    └── ts/
        └── pages/
            └── home.ts                      # MODIFIED: h1 text "Home" → "Let's go!"

src/WorkoutTracker.E2ETests/
└── E2E/
    ├── HomeLandingPageSelectionTests.cs     # MODIFIED: update h1 assertion "Home" → "Let's go!"
    └── SidebarNavigationTests.cs            # MODIFIED: update sidebar label list "Home" → "Let's go!"
```

**Structure Decision**: Web application pattern (ASP.NET Core + Aspire + vanilla TypeScript SPA). Consistent with all prior features.
