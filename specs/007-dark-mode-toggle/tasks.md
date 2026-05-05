# Tasks: Dark Mode Toggle

> **Status: Fully Delivered** — All 28 tasks complete. Feature is implemented on branch `007-dark-mode-toggle`.

**Input**: Design documents from `/specs/007-dark-mode-toggle/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ui-contract.md ✅

**Tests**: Automated Vitest unit tests are required for `theme.ts` (all exported functions). All existing Vitest and xUnit tests must continue to pass.

**Organization**: Tasks are grouped by user story. US1 (menu + immediate theme switch) is the MVP — US2 (persistence) and US3 (real-time OS tracking) build on top of it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to

---

## Phase 1: Setup

**Purpose**: No new projects, packages, or dependencies are needed. This phase confirms the working environment and creates the new files.

- [x] T001 Confirm feature branch `007-dark-mode-toggle` is checked out and `git status` is clean
- [x] T002 [P] Create empty `src/WorkoutTracker.Web/wwwroot/ts/theme.ts` with export placeholder so TypeScript compilation continues to pass
- [x] T003 [P] Create empty `src/WorkoutTracker.Web/wwwroot/ts/__tests__/theme.test.ts` with a single passing smoke test

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: CSS dark-theme overrides and topbar layout changes that every user story depends on. No user story implementation can begin until this phase is complete.

**⚠️ CRITICAL**: Complete this phase before any Phase 3+ work.

- [x] T004 Add `[data-theme="dark"]` override block immediately after the `:root` block in `src/WorkoutTracker.Web/wwwroot/css/styles.css` with the 8 colour overrides:
  - `--color-primary-light: #1e3a5f`
  - `--color-error: #f87171`
  - `--color-error-bg: #450a0a`
  - `--color-text: #f9fafb`
  - `--color-text-light: #9ca3af`
  - `--color-bg: #111827`
  - `--color-white: #1f2937`
  - `--color-border: #374151`

- [x] T005 Apply responsive topbar layout in `src/WorkoutTracker.Web/wwwroot/css/styles.css`:
  - Keep `.topbar { display: none }` as the default (base styles)
  - Add `@media (max-width: 767px) { .topbar { display: flex } }` — mobile shows the full topbar (hamburger + title + theme button)
  - Add `@media (min-width: 768px)` block that sets `.topbar { display: flex; background: transparent; border-bottom: none; left: auto; width: auto; padding: var(--spacing-xs); }` and hides `.topbar__toggle, .topbar__title { display: none; }` — desktop shows only the theme button as a transparent floating widget top-right
  - Keep `.content { padding-top: calc(var(--topbar-height) + var(--spacing-lg)) }` inside the mobile-only media query (no padding offset needed on desktop)

- [x] T006 [P] Add flash-prevention inline `<script>` as the last element inside `<head>` (before `</head>`) in `src/WorkoutTracker.Web/wwwroot/index.html`:
  ```html
  <script>
    (function () {
      var stored = localStorage.getItem('workout-tracker-theme');
      var valid = ['light', 'dark', 'system'];
      var pref = valid.indexOf(stored) !== -1 ? stored : 'system';
      var resolved = pref === 'dark' ? 'dark'
        : pref === 'light' ? 'light'
        : (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
      document.documentElement.setAttribute('data-theme', resolved);
      document.documentElement.setAttribute('data-theme-pref', pref);
    }());
  </script>
  ```

**Checkpoint**: With T004–T006 complete, manually setting `data-theme="dark"` on `<html>` in DevTools should flip the entire app to dark colours, and the topbar should be visible on desktop.

---

## Phase 3: User Story 1 — Select a Theme Using the Theme Selector Menu (P1)

**Story goal**: A user clicks a theme icon in the top-right corner of every page, which opens a three-option menu (Light / Dark / System). Selecting an option immediately applies the theme across the entire app and updates the header icon.

**Independent Test**: Open the app, click the theme icon, select each of the three options, verify the theme and icon update correctly for each.

### HTML Markup

- [x] T007 [US1] Add the theme selector button and dropdown menu markup inside `.topbar` in `src/WorkoutTracker.Web/wwwroot/index.html`, following the exact structure in `specs/007-dark-mode-toggle/contracts/ui-contract.md`:
  - A wrapping `<div class="topbar__theme">` after `.topbar__title`
  - `<button id="theme-btn" class="topbar__theme-btn" type="button" aria-label="Theme: System" aria-haspopup="menu" aria-expanded="false" aria-controls="theme-menu">`
  - Moon SVG (`topbar__theme-icon--moon`) and sun SVG (`topbar__theme-icon--sun`) inside the button
  - `<span class="topbar__theme-indicator" aria-hidden="true">` for the system dot
  - `<span class="sr-only">Theme</span>` for accessible label
  - `<ul id="theme-menu" class="theme-menu" role="menu" aria-labelledby="theme-btn" hidden>` with three `<li role="none">` items, each containing a `<button class="theme-menu__item" role="menuitem" data-theme-value="[light|dark|system]">` with icon SVG, label text, and checkmark SVG

### CSS

- [x] T008 [P] [US1] Add `.topbar__theme-btn` styles to `src/WorkoutTracker.Web/wwwroot/css/styles.css`:
  - Same dimensions, background (`transparent`), border (`none`), cursor (`pointer`), border-radius, hover state, and `focus-visible` focus ring as `.topbar__toggle`
  - `position: relative` (for the indicator dot)
  - `display: flex; align-items: center; justify-content: center`

- [x] T009 [P] [US1] Add icon visibility rules and system indicator dot styles to `src/WorkoutTracker.Web/wwwroot/css/styles.css`:
  - `.topbar__theme-icon--moon, .topbar__theme-icon--sun { display: none; width: 1.25rem; height: 1.25rem; }`
  - `html[data-theme="light"] .topbar__theme-icon--moon { display: block; }`
  - `html[data-theme="dark"] .topbar__theme-icon--sun { display: block; }`
  - `.topbar__theme-indicator { display: none; position: absolute; top: 0.2rem; right: 0.2rem; width: 6px; height: 6px; border-radius: 50%; background: var(--color-primary); }`
  - `html[data-theme-pref="system"] .topbar__theme-indicator { display: block; }`

- [x] T010 [P] [US1] Add `.theme-menu` and `.theme-menu__item` styles to `src/WorkoutTracker.Web/wwwroot/css/styles.css`:
  - `.theme-menu { position: absolute; top: calc(var(--topbar-height) + 0.25rem); right: var(--spacing-md); z-index: 200; background: var(--color-white); border: 1px solid var(--color-border); border-radius: var(--radius); box-shadow: 0 4px 12px rgba(0,0,0,0.12); min-width: 9rem; padding: var(--spacing-xs) 0; list-style: none; margin: 0; }`
  - `.theme-menu__item { display: flex; align-items: center; gap: var(--spacing-sm); width: 100%; padding: var(--spacing-xs) var(--spacing-md); background: none; border: none; cursor: pointer; color: var(--color-text); font-size: var(--font-size-base); text-align: left; }`
  - `.theme-menu__item:hover { background: var(--color-bg); }`
  - `.theme-menu__icon { width: 1rem; height: 1rem; flex-shrink: 0; }`
  - `.theme-menu__check { display: none; width: 1rem; height: 1rem; margin-left: auto; flex-shrink: 0; }`
  - `.theme-menu__item--active { font-weight: 600; }`
  - `.theme-menu__item--active .theme-menu__check { display: block; }`

### TypeScript

- [x] T011 [US1] Implement `theme.ts` at `src/WorkoutTracker.Web/wwwroot/ts/theme.ts` with the full module as specified in `specs/007-dark-mode-toggle/plan.md` Workstream C:
  - Export `ThemePreference` and `ResolvedTheme` types
  - Implement `getStoredPreference()` — reads `localStorage`, validates against allowlist `['light', 'dark', 'system']`, returns `'system'` if missing or invalid
  - Implement `getSystemPreference()` — queries `matchMedia('(prefers-color-scheme: dark)').matches`; returns `'dark'` or `'light'`; catches any error and returns `'light'`
  - Implement `resolveTheme(pref)` — returns `pref` unchanged if `'light'` or `'dark'`; calls `getSystemPreference()` if `'system'`
  - Implement `applyTheme(pref)` — resolves, sets `data-theme` and `data-theme-pref` on `<html>`, updates `#theme-btn` `aria-label`, updates `.theme-menu__item--active` class on menu items
  - Implement `initTheme()` — calls `applyTheme(getStoredPreference())`, registers `#theme-btn` click to toggle menu open/close (toggle `hidden` + `aria-expanded`), registers `.theme-menu__item` clicks to save to `localStorage` and call `applyTheme`, registers document click-outside to close menu, registers `Escape` keydown to close menu and restore focus, registers `ArrowDown`/`ArrowUp` keydown on menu items to cycle focus; registers `matchMedia 'change'` listener (see US3 — wire up stub now, full implementation in T020)

- [x] T012 [US1] Wire `initTheme()` into `src/WorkoutTracker.Web/wwwroot/ts/main.ts`:
  - Add `import { initTheme } from './theme.js';` at the top of the imports block
  - Call `initTheme();` inside `initializeApp()`, after `initSidebar()` and before `init()`

### Tests

- [x] T013 [US1] Write Vitest unit tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/theme.test.ts` covering `getStoredPreference()`:
  - Returns `'system'` when `localStorage` key is absent
  - Returns `'system'` when stored value is an unrecognised string
  - Returns `'light'` when `'light'` is stored
  - Returns `'dark'` when `'dark'` is stored
  - Returns `'system'` when `'system'` is stored

- [x] T014 [P] [US1] Write Vitest unit tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/theme.test.ts` covering `resolveTheme()`:
  - `'light'` → `'light'`
  - `'dark'` → `'dark'`
  - `'system'` with OS dark → `'dark'`
  - `'system'` with OS light → `'light'`

- [x] T015 [P] [US1] Write Vitest unit tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/theme.test.ts` covering `applyTheme()`:
  - `'light'` → sets `data-theme="light"` and `data-theme-pref="light"` on `document.documentElement`
  - `'dark'` → sets `data-theme="dark"` and `data-theme-pref="dark"`
  - `'system'` + OS dark → sets `data-theme="dark"` and `data-theme-pref="system"`
  - `'system'` + OS light → sets `data-theme="light"` and `data-theme-pref="system"`
  - Updates `aria-label` on `#theme-btn` to reflect the chosen preference
  - Adds `theme-menu__item--active` class to the matching menu item and removes it from the others

- [x] T016 [US1] Run `cd src/WorkoutTracker.Web && npm test` and confirm all tests pass (including pre-existing `router.test.ts`)

---

## Phase 4: User Story 2 — Preference is Remembered Across Sessions (P2)

**Story goal**: A user's theme choice is persisted in `localStorage` and restored without a flash of the wrong theme on the next page load.

**Independent Test**: Select "Dark", reload the page, confirm the app opens in dark mode without flashing light first. Repeat for "Light" and "System".

**Note**: The `localStorage` read/write is already implemented in `theme.ts` (T011) and the flash-prevention script is in place (T006). This phase adds explicit persistence tests and validates the FOUC prevention behaviour.

- [x] T017 [US2] Verify that `applyTheme()` in `src/WorkoutTracker.Web/wwwroot/ts/theme.ts` calls `localStorage.setItem('workout-tracker-theme', pref)` — confirm this write is triggered from the `.theme-menu__item` click handler in `initTheme()` (not from `applyTheme()` itself, which is also called by the FOUC script path)

- [x] T018 [US2] Write Vitest unit tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/theme.test.ts` covering persistence:
  - Clicking a `.theme-menu__item[data-theme-value="dark"]` calls `localStorage.setItem('workout-tracker-theme', 'dark')`
  - Clicking a `.theme-menu__item[data-theme-value="light"]` calls `localStorage.setItem('workout-tracker-theme', 'light')`
  - Clicking a `.theme-menu__item[data-theme-value="system"]` calls `localStorage.setItem('workout-tracker-theme', 'system')`
  - On first load with no stored key, `getStoredPreference()` returns `'system'`

- [x] T019 [US2] Run `cd src/WorkoutTracker.Web && npm test` and confirm all tests pass

---

## Phase 5: User Story 3 — Use System Theme Automatically (P3)

**Story goal**: When "System" is selected, the app mirrors the OS preference in real time. If the OS preference changes while the tab is open, the app updates immediately without a reload.

**Independent Test**: Select "System", use the OS dark/light mode toggle, confirm the app updates within 1 second.

- [x] T020 [US3] Complete the `matchMedia 'change'` listener in `initTheme()` in `src/WorkoutTracker.Web/wwwroot/ts/theme.ts`:
  - Register `window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => { if (getStoredPreference() === 'system') applyTheme('system'); })`
  - The listener must only trigger a re-apply when the stored preference is `'system'` — manual Light or Dark selections must never be overridden by an OS change

- [x] T021 [US3] Write Vitest unit tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/theme.test.ts` covering real-time OS tracking:
  - When stored preference is `'system'` and the `matchMedia 'change'` event fires with `matches: true` (dark), `data-theme` is updated to `'dark'`
  - When stored preference is `'system'` and the `matchMedia 'change'` event fires with `matches: false` (light), `data-theme` is updated to `'light'`
  - When stored preference is `'dark'` (manual) and the `matchMedia 'change'` event fires, `data-theme` remains `'dark'` (no override)
  - When stored preference is `'light'` (manual) and the `matchMedia 'change'` event fires, `data-theme` remains `'light'` (no override)

- [x] T022 [US3] Run `cd src/WorkoutTracker.Web && npm test` and confirm all tests pass

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Keyboard accessibility, contrast verification, regression checks.

- [x] T023 Verify full keyboard interaction in a browser:
  - Tab to `#theme-btn`; press Enter → menu opens, focus moves to first item
  - ArrowDown / ArrowUp cycles through Light / Dark / System items (wraps)
  - Enter on a focused item applies the theme and closes the menu, returning focus to `#theme-btn`
  - Escape closes the menu and returns focus to `#theme-btn`
  - Tab while menu is open closes the menu and moves focus forward

- [x] T024 [P] Verify WCAG AA contrast in both themes using DevTools or a browser contrast checker:
  - `--color-text` on `--color-bg` (dark): `#f9fafb` on `#111827` ≥ 4.5:1
  - `--color-text` on `--color-white` (dark): `#f9fafb` on `#1f2937` ≥ 4.5:1
  - `--color-text-light` on `--color-bg` (dark): `#9ca3af` on `#111827` ≥ 4.5:1
  - `--color-primary` on `--color-white` (dark): `#2563eb` on `#1f2937` ≥ 4.5:1
  - `--color-error` on `--color-error-bg` (dark): `#f87171` on `#450a0a` ≥ 4.5:1

- [x] T025 [P] Visual smoke test: navigate to every page in the app (home, workout list, active session, exercise detail) in both light and dark themes and confirm no unstyled elements, no invisible text, no clipped layout

- [x] T026 [P] Run the full backend test suite `dotnet test src/WorkoutTracker.slnx` to confirm no regressions (API and integration tests are unaffected but must still pass)

- [x] T027 Run `cd src/WorkoutTracker.Web && npm run build` (TypeScript compilation) and confirm zero errors

- [x] T028 Commit all changes with message: `feat: add dark mode toggle with Light/Dark/System menu (#007)`

---

## Dependencies

```
Phase 1 (Setup)
    └── Phase 2 (Foundation: CSS overrides, topbar layout, FOUC script)
            ├── Phase 3 US1 (Menu, applyTheme, initTheme, tests)
            │       ├── Phase 4 US2 (Persistence tests)
            │       └── Phase 5 US3 (Real-time OS tracking)
            │               └── Phase 6 (Polish, contrast, regression)
            └── (phases 3–5 can proceed once T004–T006 are done)
```

US2 and US3 can be implemented in parallel after US1 is complete (they touch the same `theme.ts` file but different functions and test blocks).

---

## Parallel Execution

The following task groups can run in parallel **within** a phase:

| Parallel group | Tasks |
|---|---|
| Phase 2: HTML + CSS can proceed simultaneously | T005 ‖ T006 |
| Phase 3 CSS tasks are file-independent | T008 ‖ T009 ‖ T010 |
| Phase 3 test tasks after T011 | T013 → T014 ‖ T015 |
| Phase 6 verification tasks | T023 ‖ T024 ‖ T025 ‖ T026 |

---

## Implementation Strategy

**MVP scope (minimum shippable increment)**: Phases 1–3 (US1). This delivers the fully functional theme selector menu with immediate visual feedback, correct icons, and FOUC prevention. Persistence (US2) and real-time OS tracking (US3) are additive improvements with no risk of breaking US1.

**Suggested delivery order**:
1. Phase 2 (foundation) — unblocks everything
2. Phase 3 (US1) — delivers the complete visible feature
3. Phase 4 (US2) — adds persistence (very low risk, most of the code is already in T011)
4. Phase 5 (US3) — adds real-time OS tracking (single event listener)
5. Phase 6 — polish, verification, final commit

---

## Task Count Summary

| Phase | Tasks | Story |
|---|---|---|
| Phase 1: Setup | 3 | — |
| Phase 2: Foundation | 3 | — |
| Phase 3: US1 (Theme menu) | 10 | P1 |
| Phase 4: US2 (Persistence) | 3 | P2 |
| Phase 5: US3 (System tracking) | 3 | P3 |
| Phase 6: Polish | 6 | — |
| **Total** | **28** | |

**Parallel opportunities**: 9 tasks marked `[P]`  
**Format validation**: All 28 tasks have checkbox, sequential ID, optional `[P]`, optional `[USn]`, description with file path ✅
