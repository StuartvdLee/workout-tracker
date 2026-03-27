# Tasks: Sidebar Navigation Layout

**Input**: Design documents from `/specs/002-sidebar-navigation/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for every user story and every bug fix.
Include the appropriate unit, integration, contract, or end-to-end coverage
needed to prove behavior before implementation is complete.

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story, with explicit work for security, user
experience consistency, and performance verification where applicable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web frontend**: `src/WorkoutTracker.Web/wwwroot/`
- **TypeScript source**: `src/WorkoutTracker.Web/wwwroot/ts/`
- **CSS**: `src/WorkoutTracker.Web/wwwroot/css/`
- **HTML shell**: `src/WorkoutTracker.Web/wwwroot/index.html`
- **E2E tests**: `src/WorkoutTracker.Tests/E2E/`

---

## Phase 1: Setup

**Purpose**: Create new file structure and directories for the sidebar navigation feature

- [x] T001 Create page module directory at `src/WorkoutTracker.Web/wwwroot/ts/pages/`
- [x] T002 [P] Create empty TypeScript module files: `src/WorkoutTracker.Web/wwwroot/ts/router.ts`, `src/WorkoutTracker.Web/wwwroot/ts/sidebar.ts`, `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts`, `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`, `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` — each exporting a placeholder `render` function that accepts `HTMLElement` and returns `void`
- [x] T003 Verify TypeScript compilation still succeeds after adding new files by running `cd src/WorkoutTracker.Web && npm run build`

**Checkpoint**: New file structure exists, project compiles clean

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented — the HTML shell, CSS layout, and client-side router

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Rework `src/WorkoutTracker.Web/wwwroot/index.html` to the new layout shell per the UI contract in `specs/002-sidebar-navigation/contracts/ui-contract.md`: replace the existing `<main class="app">` content with the sidebar (`aside.sidebar` containing `.sidebar__header` with app title, `nav.sidebar__nav` with three `a.sidebar__link[data-page]` items each containing an inline SVG icon and `span.sidebar__label`), the mobile backdrop (`div.sidebar__backdrop`), the mobile top bar (`header.topbar` with `button.topbar__toggle` hamburger icon), and the main content area (`main.content`). Set `data-page` attributes to `home`, `workouts`, `exercises`. Add all ARIA attributes per the contract (`aria-label="Main navigation"` on nav, `aria-label="Toggle navigation"` and `aria-expanded="false"` and `aria-controls` on toggle). SVG icons: house for Home, dumbbell for Workouts, list/checklist for Exercises — inline in each `a.sidebar__link`. Keep the `<script type="module" src="/js/main.js">` tag.
- [x] T005 Extend `src/WorkoutTracker.Web/wwwroot/css/styles.css` with sidebar layout styles: add new CSS custom properties for sidebar dimensions (`--sidebar-width`, `--topbar-height`). Add styles for `.sidebar` (fixed-position left panel, full height, fixed width), `.sidebar__header` (app title area), `.sidebar__nav`, `.sidebar__link` (flex row with icon + label, hover state, transition), `.sidebar__link--active` (accent background using `--color-primary`, font weight change, left border accent), `.sidebar__icon` (consistent size, `currentColor` fill), `.sidebar__label`, `.sidebar__backdrop` (semi-transparent overlay, hidden by default), `.sidebar__backdrop--visible`, `.topbar` (hidden on desktop, visible below 768px), `.topbar__toggle` (hamburger button styling), `.content` (margin-left equal to sidebar width on desktop, full width on mobile), `.page-placeholder`, `.page-placeholder__title`, `.page-placeholder__text`. Add responsive rules: below 768px the sidebar is off-canvas (translated left), `.sidebar--open` slides it in; above 768px the topbar is hidden and sidebar always visible. Preserve all existing `.workout-form*` and `.app__title` styles for backward compatibility.
- [x] T006 Implement the client-side router in `src/WorkoutTracker.Web/wwwroot/ts/router.ts`: export a `Router` class or module that maintains a route table mapping URL paths (`/`, `/workouts`, `/exercises`) to page module `render` functions. Implement `navigate(path: string)` that calls `history.pushState`, updates the content area by calling the matched page's `render()` on `main.content`, and updates sidebar active state. Listen to `popstate` for browser back/forward. On `init()`, read `window.location.pathname` to render the correct page (deep link support). Unknown routes redirect to `/`. Export a `getCurrentPath()` helper. All DOM queries should use the CSS classes from the UI contract.
- [x] T007 Verify TypeScript compilation succeeds and no regressions by running `cd src/WorkoutTracker.Web && npm run build`

**Checkpoint**: HTML shell renders sidebar + content area. Router handles navigation. CSS layout is complete. No page content yet.

---

## Phase 3: User Story 1 — Persistent Sidebar Navigation (Priority: P1) 🎯 MVP

**Goal**: Users can see the sidebar on every page, click menu items to switch pages, see the active state highlight, use the mobile toggle, and navigate via keyboard. This is the core navigation infrastructure.

**Independent Test**: Load the app on desktop and mobile viewports, click each menu item, verify active state updates and page content area changes. Verify deep linking by navigating directly to `/workouts`.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T008 [P] [US1] Create `src/WorkoutTracker.Tests/E2E/SidebarNavigationTests.cs` — Playwright E2E tests using `WebAppFixture` and `PlaywrightFixture`: test that sidebar is visible on page load with 3 menu items (Home, Workouts, Exercises) each having an icon and label; test clicking each menu item updates `main.content` and URL; test the active link gets `.sidebar__link--active` class and `aria-current="page"`; test deep linking by navigating directly to `/workouts` and `/exercises` and verifying correct active state; test unknown route redirects to Home; test browser back/forward updates active state correctly
- [ ] T009 [P] [US1] Create `src/WorkoutTracker.Tests/E2E/SidebarMobileTests.cs` — Playwright E2E tests at mobile viewport (375×667): test sidebar is hidden by default (off-canvas); test hamburger toggle button is visible; test clicking toggle opens sidebar (`.sidebar--open`); test `aria-expanded` toggles on the button; test selecting a menu item closes the sidebar and navigates; test clicking backdrop closes sidebar; test resizing from mobile to desktop (≥768px) makes sidebar always visible and hides topbar
- [ ] T010 [P] [US1] Create `src/WorkoutTracker.Tests/E2E/SidebarAccessibilityTests.cs` — Playwright E2E tests: test all sidebar links are reachable via Tab key; test links are activatable via Enter and Space; test `nav` has `aria-label="Main navigation"`; test active link has `aria-current="page"` and inactive links do not; test toggle button has `aria-label="Toggle navigation"` and `aria-controls` matching sidebar element ID

### Implementation for User Story 1

- [ ] T011 [US1] Implement sidebar behaviour module in `src/WorkoutTracker.Web/wwwroot/ts/sidebar.ts`: export `initSidebar(router)` that attaches click handlers to all `.sidebar__link` elements — on click, prevent default, call `router.navigate(link.dataset.page path)`, and on mobile close the sidebar. Export `updateActiveLink(path: string)` that adds `.sidebar__link--active` and `aria-current="page"` to the matching link and removes from others. Export `initMobileToggle()` that wires the `.topbar__toggle` button to toggle `.sidebar--open` on the sidebar and `.sidebar__backdrop--visible` on the backdrop, and updates `aria-expanded`. Wire backdrop click and Escape key to close. Wire a `matchMedia` listener for `(min-width: 768px)` to force-close mobile sidebar when resizing to desktop.
- [ ] T012 [US1] Rework `src/WorkoutTracker.Web/wwwroot/ts/main.ts` to be the application entry point that imports and initialises the router and sidebar: on `DOMContentLoaded`, import `Router` from `./router.js`, import `initSidebar` from `./sidebar.js`, create the router with route registrations (importing page render functions from `./pages/home.js`, `./pages/workouts.js`, `./pages/exercises.js`), call `initSidebar(router)`, and call `router.init()` to render the initial page based on current URL. Remove all existing inline home page logic (it moves to `pages/home.ts` in US2).
- [ ] T013 [US1] Run all E2E tests with `cd src && dotnet test` — verify SidebarNavigationTests, SidebarMobileTests, and SidebarAccessibilityTests pass. Fix any failures.

**Checkpoint**: Sidebar navigation fully functional — clicking items changes pages, active state works, mobile toggle works, keyboard accessible, deep linking works. Page content areas may be empty/placeholder until US2-US4.

---

## Phase 4: User Story 2 — Home Page Content (Priority: P2)

**Goal**: The existing workout selection form (dropdown + "Start Workout" button) renders correctly inside the new layout's content area, with all existing functionality preserved (dropdown population, validation, error handling).

**Independent Test**: Navigate to Home page, verify workout form loads, dropdown populates from API, validation works on submit, error messages display and clear correctly.

### Tests for User Story 2 ⚠️

> **NOTE: Update existing tests to work with new layout, ensure they FAIL before implementation fixes them**

- [ ] T014 [US2] Update all 6 existing E2E test files in `src/WorkoutTracker.Tests/E2E/HomeLandingPage*.cs` (`HomeLandingPageSelectionTests.cs`, `HomeLandingPageValidationTests.cs`, `HomeLandingPageAccessibilityTests.cs`, `HomeLandingPageResponsiveTests.cs`, `HomeLandingPagePerformanceTests.cs`, `HomeLandingPageRegressionTests.cs`) — scope selectors to work within the new `main.content` container. Update any selectors that reference `main.app` or assume the form is the only top-level content. The `#workout-form`, `#workout-select`, `#workout-error` IDs remain unchanged so ID-based selectors should still work. Verify responsive tests account for sidebar presence on desktop widths. Run tests to confirm they fail (home page module not yet implemented).

### Implementation for User Story 2

- [ ] T015 [US2] Implement the Home page module in `src/WorkoutTracker.Web/wwwroot/ts/pages/home.ts`: export a `render(container: HTMLElement)` function that creates the workout form HTML (the `<form class="workout-form" id="workout-form">` with select dropdown, error div, and submit button — same markup currently in `index.html`). After inserting the HTML, run the initialisation logic extracted from the original `main.ts`: call `populateWorkoutOptions()` to fetch `/api/workout-types` and populate the dropdown, attach submit handler for validation, attach change handler to clear errors. Export/inline the helper functions (`populateWorkoutOptions`, `handleStartWorkout`, `isValidWorkoutValue`, `showError`, `clearError`) as module-private functions. Ensure the `WorkoutType` interface and `loadedWorkoutTypeIds` Set are scoped to this module.
- [ ] T016 [US2] Run all E2E tests with `cd src && dotnet test` — verify all 6 existing `HomeLandingPage*` tests pass alongside the US1 sidebar tests. Fix any failures.

**Checkpoint**: Home page renders workout form correctly within the sidebar layout. All original functionality preserved. All existing + new tests green.

---

## Phase 5: User Story 3 & 4 — Placeholder Pages (Priority: P3)

**Goal**: The Workouts and Exercises pages display a heading and "coming soon" placeholder message when navigated to from the sidebar.

**Independent Test**: Click "Workouts" in sidebar → see "Workouts" heading and coming soon message. Click "Exercises" → see "Exercises" heading and coming soon message. Sidebar highlights the correct item.

### Tests for User Story 3 & 4 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T017 [P] [US3] Create `src/WorkoutTracker.Tests/E2E/WorkoutsPageTests.cs` — Playwright E2E tests: test navigating to Workouts page (via sidebar click and via direct URL `/workouts`) shows a heading containing "Workouts" and a paragraph with coming soon text; test sidebar "Workouts" link has active state; test page uses `.page-placeholder` wrapper with `.page-placeholder__title` and `.page-placeholder__text` elements
- [ ] T018 [P] [US4] Create `src/WorkoutTracker.Tests/E2E/ExercisesPageTests.cs` — Playwright E2E tests: test navigating to Exercises page (via sidebar click and via direct URL `/exercises`) shows a heading containing "Exercises" and a paragraph with coming soon text; test sidebar "Exercises" link has active state; test page uses `.page-placeholder` wrapper with `.page-placeholder__title` and `.page-placeholder__text` elements

### Implementation for User Story 3 & 4

- [ ] T019 [P] [US3] Implement the Workouts placeholder page in `src/WorkoutTracker.Web/wwwroot/ts/pages/workouts.ts`: export a `render(container: HTMLElement)` function that inserts a `<div class="page-placeholder">` containing an `<h1 class="page-placeholder__title">Workouts</h1>` and a `<p class="page-placeholder__text">` with a user-friendly coming soon message
- [ ] T020 [P] [US4] Implement the Exercises placeholder page in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`: export a `render(container: HTMLElement)` function that inserts a `<div class="page-placeholder">` containing an `<h1 class="page-placeholder__title">Exercises</h1>` and a `<p class="page-placeholder__text">` with a user-friendly coming soon message
- [ ] T021 [US3] Run all E2E tests with `cd src && dotnet test` — verify WorkoutsPageTests and ExercisesPageTests pass alongside all previous tests. Fix any failures.

**Checkpoint**: All 3 pages render correctly. Full navigation works end-to-end. All tests green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Edge case handling, performance verification, and final validation across all stories

- [ ] T022 Verify edge cases in `src/WorkoutTracker.Web/wwwroot/ts/router.ts` and `src/WorkoutTracker.Web/wwwroot/ts/sidebar.ts`: ensure rapid clicking between menu items only renders the last-clicked page (debounce or guard against concurrent renders); ensure Escape key closes mobile sidebar; ensure resize from mobile to desktop auto-closes mobile overlay without losing current page
- [ ] T023 [P] Verify no cumulative layout shift (CLS) by confirming in `src/WorkoutTracker.Web/wwwroot/css/styles.css` that the sidebar has a fixed width and `main.content` uses a stable margin-left that does not animate or shift on page load
- [ ] T024 [P] Run TypeScript compilation in strict mode (`cd src/WorkoutTracker.Web && npm run build`) and confirm zero errors and zero warnings
- [ ] T025 Run the full E2E test suite with `cd src && dotnet test` as a final regression check — all tests must pass: SidebarNavigationTests, SidebarMobileTests, SidebarAccessibilityTests, HomeLandingPageSelectionTests, HomeLandingPageValidationTests, HomeLandingPageAccessibilityTests, HomeLandingPageResponsiveTests, HomeLandingPagePerformanceTests, HomeLandingPageRegressionTests, WorkoutsPageTests, ExercisesPageTests
- [ ] T026 Run quickstart.md verification checklist from `specs/002-sidebar-navigation/quickstart.md` — confirm all items pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — sidebar navigation must exist first
- **User Story 2 (Phase 4)**: Depends on Phase 3 — router and sidebar must work before home page content
- **User Story 3 & 4 (Phase 5)**: Depends on Phase 3 — can run in parallel with Phase 4
- **Polish (Phase 6)**: Depends on Phases 3, 4, and 5

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational — core navigation infrastructure
- **US2 (P2)**: Depends on US1 — needs router and sidebar working to render home content
- **US3 (P3)**: Depends on US1 — needs router to navigate to workouts page. Independent of US2 and US4.
- **US4 (P3)**: Depends on US1 — needs router to navigate to exercises page. Independent of US2 and US3.

### Within Each User Story

- Tests written first and verified to fail
- Implementation follows
- All tests verified green before moving to next story

### Parallel Opportunities

- T002 files can all be created in parallel (different files)
- T008, T009, T010 (US1 test files) can be written in parallel
- T017, T018 (US3/US4 test files) can be written in parallel
- T019, T020 (US3/US4 implementation) can be written in parallel
- Phase 4 (US2) and Phase 5 (US3/US4) can run in parallel after Phase 3 completes
- T023, T024 (polish) can run in parallel

---

## Parallel Example: User Story 1

```bash
# Write all US1 test files in parallel:
Task: T008 "SidebarNavigationTests.cs"
Task: T009 "SidebarMobileTests.cs"
Task: T010 "SidebarAccessibilityTests.cs"
```

## Parallel Example: User Stories 3 & 4

```bash
# Write both test files in parallel:
Task: T017 "WorkoutsPageTests.cs"
Task: T018 "ExercisesPageTests.cs"

# Implement both pages in parallel:
Task: T019 "pages/workouts.ts"
Task: T020 "pages/exercises.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (HTML shell, CSS, router)
3. Complete Phase 3: User Story 1 (sidebar behaviour, mobile toggle, keyboard nav)
4. **STOP and VALIDATE**: Sidebar navigation works with basic page switching
5. Deploy/demo if ready — pages show placeholder content via router

### Incremental Delivery

1. Setup + Foundational → Shell and router ready
2. Add US1 → Sidebar fully functional → Test independently (MVP!)
3. Add US2 → Home page form works in new layout → Test independently
4. Add US3 + US4 → Placeholder pages complete → Test independently
5. Polish → Edge cases, performance, final regression
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. One developer: US1 (sidebar core)
3. Once US1 is done:
   - Developer A: US2 (home page)
   - Developer B: US3 + US4 (placeholder pages)
4. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Verify tests fail before implementing
- Include security, UX consistency, and performance verification in story tasks
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- The existing `WebAppFixture.cs` requires no changes — `MapFallbackToFile("index.html")` already supports client-side routes
- The existing `Program.cs` requires no changes — static file serving and fallback routing are already configured
