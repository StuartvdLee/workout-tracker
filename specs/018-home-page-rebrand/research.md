# Research: Home Page Rebrand

## Decision 1: Flame Icon SVG path

**Decision**: Use the official Lucide `flame` icon inline SVG path directly in `index.html`, matching the existing inline SVG pattern used for all other sidebar icons.

**Rationale**: The project does not use a Lucide runtime library — all sidebar icons are raw inline SVG paths. Embedding the path directly requires zero new dependencies and loads with the initial HTML.

**SVG path** (from https://github.com/lucide-icons/lucide/blob/main/icons/flame.svg):
```
<path d="M12 3q1 4 4 6.5t3 5.5a1 1 0 0 1-14 0 5 5 0 0 1 1-3 1 1 0 0 0 5 0c0-2-1.5-3-1.5-5q0-2 2.5-4" />
```

**Icon element** (with existing sidebar attributes):
```html
<svg class="sidebar__icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
  <path d="M12 3q1 4 4 6.5t3 5.5a1 1 0 0 1-14 0 5 5 0 0 1 1-3 1 1 0 0 0 5 0c0-2-1.5-3-1.5-5q0-2 2.5-4" />
</svg>
```

**Alternatives considered**: None — inline SVG is the established project pattern.

---

## Decision 2: Sidebar label and page h1 text

**Decision**: Both the sidebar label and the page `<h1>` use the exact string `Let's go!` (with apostrophe and exclamation mark, as specified).

**Rationale**: The spec is explicit about the exact string. The apostrophe is a standard Unicode character (`'`, U+2019 or the ASCII `'`) — no HTML entity encoding needed in UTF-8 HTML.

**Alternatives considered**: None — spec is unambiguous.

---

## Decision 3: Test updates

**Decision**: Update two existing E2E tests in-place:
- `HomeLandingPageSelectionTests.HomePage_DisplaysTitle_WorkoutTracker`: change `ToHaveTextAsync("Home")` → `ToHaveTextAsync("Let's go!")`
- `SidebarNavigationTests.Sidebar_MenuItems_HaveIconsAndLabels`: change `"Home"` → `"Let's go!"` in the expected labels array

**Rationale**: These are the only tests that assert the current label. No new test file is needed — the renamed label is already covered by the existing test structure.

**Alternatives considered**: Adding a new dedicated test for the rebrand — rejected as over-engineering for a pure rename. The existing tests become the regression guard automatically once updated.
