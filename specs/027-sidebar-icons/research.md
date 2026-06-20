# Research: Sidebar Icons for Workouts and Exercises

## Context

This feature intentionally reuses decisions and patterns already validated in prior planning artifacts:
- `specs/024-change-effort-slider-colours/plan.md` (frontend-only static change pattern)
- `specs/026-delete-session/plan.md` (HTML + minimal TypeScript change pattern)

## Decision 1: Use Lucide inline SVG paths (no new dependency)

**Decision**: Inline the SVG `<path>` elements from Lucide 1.16.0 directly into `index.html`, matching the pattern used by all existing sidebar icons.

**Rationale**: All five existing sidebar icons already use inline SVG with `stroke="currentColor"`. Adding an external icon library would introduce a new network dependency and deviate from the established pattern. Inlining keeps the bundle unchanged and the approach consistent.

**Alternatives considered**:
- **Import Lucide as an npm package and reference icons by component**: rejected; requires a build-step change and introduces a runtime dependency for a purely static change.
- **Use an `<img>` or `<use>` reference to an SVG sprite**: rejected; existing pattern uses inline SVG exclusively and `<use>` requires a sprite file to maintain.

## Decision 2: Preserve all existing SVG wrapper attributes unchanged

**Decision**: Keep `class="sidebar__icon"`, `width="20"`, `height="20"`, `viewBox="0 0 24 24"`, `fill="none"`, `stroke="currentColor"`, `stroke-width="2"`, `stroke-linecap="round"`, `stroke-linejoin="round"`, and `aria-hidden="true"` identical to the current icons.

**Rationale**: These attributes govern layout sizing (20×20), theme inheritance (currentColor), accessibility (aria-hidden), and visual consistency (stroke style). Modifying any of them would risk layout regressions or accessibility breakage beyond the stated scope.

**Alternatives considered**:
- **Adopt Lucide's default 24×24 presentation size**: rejected; existing sidebar icons render at 20×20 and the CSS class `.sidebar__icon` is calibrated to that size.
- **Remove aria-hidden now that icons are more descriptive**: rejected; icons remain decorative (labels are present); removing aria-hidden is a separate accessibility decision outside scope.

## Decision 3: No automated tests added; existing suite is sufficient

**Decision**: No new Vitest or Playwright tests for this change. The existing frontend build (`npm run build`) and test suite (`npm test`) validate that nothing is broken after the HTML edit.

**Rationale**: The change is two SVG path replacements in static HTML with no TypeScript logic, no new components, and no altered behavior. The constitution requirement "Tests Prove Behavior" applies to behavioral changes; a path string swap has no behavior to prove beyond visual correctness, which is validated by manual smoke check.

**Alternatives considered**:
- **Snapshot test the rendered sidebar HTML**: considered, but adding a snapshot test for static SVG paths would be brittle and low-value for this change type.
- **E2E visual regression screenshot**: considered, but no visual regression tooling is currently set up and adding it is out of scope.
