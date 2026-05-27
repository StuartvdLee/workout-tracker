# Research: Sidebar History Icon Change

## Context

This feature intentionally reuses decisions and patterns already validated in prior planning artifacts:
- `specs/027-sidebar-icons/plan.md` (inline SVG swap pattern, single-file change)

## Decision 1: Use Lucide inline SVG paths (no new dependency)

**Decision**: Inline the SVG `<path>` elements from the Lucide `history` icon directly into `index.html`, matching the pattern used by all existing sidebar icons.

**Rationale**: All five existing sidebar icons already use inline SVG with `stroke="currentColor"`. Adding an external icon library would introduce a new network dependency and deviate from the established pattern. Inlining keeps the bundle unchanged and the approach consistent.

**Alternatives considered**:
- **Import Lucide as an npm package**: rejected; requires a build-step change and introduces a runtime dependency for a purely static change.
- **Use an `<img>` or `<use>` reference to an SVG sprite**: rejected; existing pattern uses inline SVG exclusively.

## Decision 2: Preserve all existing SVG wrapper attributes unchanged

**Decision**: Keep `class="sidebar__icon"`, `width="20"`, `height="20"`, `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`, `stroke-width="2"`, `stroke-linecap="round"`, `stroke-linejoin="round"`, and `aria-hidden="true"` identical to the current icon.

**Rationale**: These attributes govern layout sizing, theme inheritance, accessibility, and visual consistency. Modifying any of them risks layout regressions or accessibility breakage beyond the stated scope.

**Alternatives considered**:
- **Adopt Lucide's default 24×24 presentation size**: rejected; existing sidebar icons render at 20×20 and the CSS class `.sidebar__icon` is calibrated to that size.

## Decision 3: No automated tests added; existing suite is sufficient

**Decision**: No new Vitest or Playwright tests for this change. The existing frontend build (`npm run build`) and test suite (`npm test`) validate that nothing is broken after the HTML edit.

**Rationale**: The change is a single SVG path replacement in static HTML with no TypeScript logic, no new components, and no altered behaviour. The constitution's "Tests Prove Behavior" applies to behavioural changes; a path string swap has no behaviour to prove beyond visual correctness, validated by manual smoke check.

**Alternatives considered**:
- **Snapshot test the rendered sidebar HTML**: brittle and low-value for a static SVG path swap.
- **E2E visual regression screenshot**: no visual regression tooling is currently set up; adding it is out of scope.
