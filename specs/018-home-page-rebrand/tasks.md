# Tasks: Home Page Rebrand

**Input**: Design documents from `/specs/018-home-page-rebrand/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅

**Tests**: Existing E2E test assertions must be updated to reflect the new title and sidebar label. No new test files are required — the renamed values are already covered by the existing test structure once updated.

**Organization**: Single user story (P1) — the entire feature is one cohesive rename. No blocking foundational work is required; all necessary infrastructure already exists.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to

---

## Phase 1: User Story 1 — Updated Home Page Identity (Priority: P1) 🎯 MVP

**Goal**: The home page displays "Let's go!" as both the sidebar label and the page `<h1>`, and the Lucide flame icon replaces the house icon in the sidebar.

**Independent Test**: Navigate to the home page — confirm the sidebar shows the flame icon and "Let's go!" label, and the page heading reads "Let's go!".

### Tests for User Story 1

- [x] T001 [P] [US1] Update h1 assertion in `src/WorkoutTracker.E2ETests/E2E/HomeLandingPageSelectionTests.cs` — change `ToHaveTextAsync("Home")` to `ToHaveTextAsync("Let's go!")`
- [x] T002 [P] [US1] Update sidebar label assertion in `src/WorkoutTracker.E2ETests/E2E/SidebarNavigationTests.cs` — change `"Home"` to `"Let's go!"` in the expected labels array

### Implementation for User Story 1

- [x] T003 [P] [US1] Update `src/WorkoutTracker.Web/wwwroot/index.html` — replace the sidebar home link SVG (house paths) with the Lucide flame icon path (`M12 3q1 4 4 6.5t3 5.5a1 1 0 0 1-14 0 5 5 0 0 1 1-3 1 1 0 0 0 5 0c0-2-1.5-3-1.5-5q0-2 2.5-4`) and change the `<span class="sidebar__label">` text from `Home` to `Let's go!`
- [x] T004 [P] [US1] Update `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts` — change the `<h1 class="home-page__title">` inner text from `Home` to `Let's go!`

### Verification for User Story 1

- [x] T005 [US1] Build TypeScript to confirm no type errors: `cd src/WorkoutTracker.Web && npm run build`
- [x] T006 [US1] Run frontend unit tests to confirm no regressions: `cd src/WorkoutTracker.Web && npm test`
- [ ] T007 [US1] Run E2E test suite to confirm updated assertions pass and no regressions: `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`

**Checkpoint**: Home page shows "Let's go!" heading and flame icon in sidebar. Both E2E assertions pass.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (US1)**: No prerequisites — can start immediately

### Within User Story 1

- T001, T002, T003, T004 are all independent (different files) — run in parallel
- T005, T006, T007 verify the complete story after T003 and T004 are done

### Parallel Opportunities

```bash
# All implementation + test-update tasks can run in parallel (all different files):
T001: Update HomeLandingPageSelectionTests.cs
T002: Update SidebarNavigationTests.cs
T003: Update index.html (sidebar icon + label)
T004: Update home.ts (page h1)

# Then verify:
T005: npm run build
T006: npm test
T007: dotnet test WorkoutTracker.E2ETests
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete T001–T004 (all parallelisable — different files, no dependencies)
2. Run T005 (`npm run build`) to confirm TypeScript is clean
3. Run T006 (`npm test`) to confirm frontend unit tests pass
4. Run T007 (`dotnet test`) to confirm E2E assertions pass
5. **VALIDATE**: Load the app and confirm the home page shows "Let's go!" and the flame icon

---

## Notes

- [P] tasks operate on different files — safe to run in parallel
- The Lucide flame SVG uses a single `<path>` element; the house icon used `<path>` + `<polyline>` — replace both elements with the single flame path
- The apostrophe in "Let's go!" is a standard ASCII apostrophe (`'`) — no HTML entity encoding needed in UTF-8 HTML
- The `sidebar__icon` SVG wrapper attributes (`width="20"`, `height="20"`, `viewBox="0 0 24 24"`, `stroke="currentColor"`, etc.) remain unchanged — only the inner path(s) change
