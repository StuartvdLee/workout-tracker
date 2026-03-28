# Research: Sidebar Navigation Layout

**Feature**: 002-sidebar-navigation
**Date**: 2026-03-27

## R1: Client-Side Routing Approach

**Decision**: History API with `pushState` / `popstate`

**Rationale**: The ASP.NET Core backend already configures `app.MapFallbackToFile("index.html")` which returns `index.html` for any unmatched route. This means clean URLs like `/workouts` and `/exercises` already resolve to the SPA shell without any server changes. The History API provides clean, bookmarkable URLs that feel native to users.

**Alternatives considered**:
- **Hash-based routing** (`#/workouts`): Simpler but produces uglier URLs and doesn't leverage the existing server-side fallback. Hash fragments are also invisible to the server for analytics or future SSR.
- **Full page reloads**: Would require multiple HTML files and duplicate the sidebar markup. Violates the SPA architecture and the spec requirement for instant page switches.

## R2: SVG Icon Strategy

**Decision**: Inline SVG elements directly in `index.html` within each navigation link.

**Rationale**: The project uses zero external dependencies for the frontend — no icon fonts, no CSS icon libraries, no bundler. Inline SVGs are the lightest approach: no additional HTTP requests, full CSS styling control (colour inherits from `currentColor`), and no build tooling changes. Three small icons add negligible HTML weight (~500 bytes total).

**Alternatives considered**:
- **Icon font** (e.g., Font Awesome): Adds an external dependency and a large font file download for just 3 icons. Contradicts the zero-dependency frontend principle.
- **External SVG sprite sheet**: Adds a separate file and an HTTP request. Unnecessary overhead for 3 icons.
- **CSS background images**: Cannot inherit text colour, harder to animate, less accessible.

## R3: Mobile Sidebar Pattern

**Decision**: Off-canvas sidebar that slides in from the left, triggered by a hamburger button in a top header bar. The sidebar overlays content on mobile rather than pushing it.

**Rationale**: Overlay pattern is the most common mobile navigation pattern and avoids content reflow. The hamburger icon is universally recognised. A semi-transparent backdrop behind the sidebar provides visual separation and allows closing by tapping outside.

**Alternatives considered**:
- **Bottom navigation bar**: Common on mobile but would require a completely different layout for mobile vs desktop, increasing complexity for 3 items.
- **Push sidebar**: Pushes content to the right, which causes layout reflow and potential CLS — violates PR-002.
- **Dropdown menu**: Less discoverable and feels less like a native app navigation.

## R4: Page Module Architecture

**Decision**: Each page is a TypeScript ES module exporting a `render(container: HTMLElement)` function and optionally an `init()` function for setup logic. The router calls `render()` when navigating to a page.

**Rationale**: ES modules are natively supported by the project's `tsconfig.json` (target ES2022, module ES2022). Each page module is self-contained, testable, and follows single-responsibility. The existing `main.ts` code for the home page form can be extracted into `pages/home.ts` with minimal changes.

**Alternatives considered**:
- **Single file with conditionals**: All page logic in `main.ts` with `if/else` blocks. Becomes unmaintainable as pages grow. Violates the code quality constitution principle.
- **Web Components**: Too much ceremony for simple pages. Adds complexity without proportional benefit for 3 pages.
- **HTML template elements**: Could work but would require all page HTML to live in `index.html`, making it bloated.

## R5: Existing Test Update Strategy

**Decision**: Update existing E2E test selectors to scope within the new content area container rather than searching the full page. No test logic changes needed — only selector scope adjustments.

**Rationale**: The 6 existing test classes query elements like `#workout-form`, `#workout-select`, `#workout-error` by ID. These IDs remain unchanged. However, some tests may use broader selectors (e.g., `main.app`) that need updating to reflect the new DOM structure. The `WebAppFixture` requires no changes because `MapFallbackToFile` already handles client-side routes.

**Alternatives considered**:
- **Rewriting all tests**: Wasteful and risky. The existing tests are comprehensive and well-structured.
- **Wrapping tests in a base class with shared setup**: Over-engineering for selector-only changes.
