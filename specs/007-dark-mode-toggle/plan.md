# Implementation Plan: Dark Mode Toggle

**Branch**: `007-dark-mode-toggle` | **Date**: 2026-05-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-dark-mode-toggle/spec.md`

## Summary

Add a three-option theme selector (Light / Dark / System) to the app. This is a **pure frontend change**: dark theme colours are implemented by redefining the existing CSS custom properties under a `[data-theme="dark"]` attribute on `<html>`, and a new `theme.ts` module manages reading/writing the `localStorage` preference, resolving the effective theme, applying it to the DOM, and listening for live OS preference changes. The topbar (currently mobile-only) is made always visible with the theme selector button right-aligned, providing a persistent "top right corner" entry point on all viewport sizes. No backend, API, or database changes are required.

## Technical Context

**Language/Version**: TypeScript 5.9.3 (frontend — primary change); C# on .NET 10 (backend — no changes)  
**Primary Dependencies**: Vanilla TypeScript (no JS frameworks or libraries); `window.matchMedia` Web API for OS preference detection; `localStorage` for preference persistence  
**Storage**: `localStorage` key `workout-tracker-theme` with values `'light' | 'dark' | 'system'` — no database changes  
**Testing**: Vitest frontend unit tests (add tests for `theme.ts`); xUnit backend integration tests (no changes needed)  
**Target Platform**: Web browser — all modern browsers supporting CSS custom properties and `matchMedia`; no IE11 support required  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Theme switch perceived as instantaneous (< 16 ms, one paint frame); no flash of wrong theme on load  
**Constraints**: No external JS/CSS frameworks or libraries (vanilla TypeScript only); existing tests must continue to pass; inline `<script>` in `<head>` required to prevent FOUC  
**Scale/Scope**: Changes touch 3 files (`index.html`, `styles.css`, `main.ts`) + 1 new module (`theme.ts`) + 1 new test file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`) enforced via `tsconfig.json`. CSS follows BEM naming convention — all new classes use `topbar__theme-btn`, `theme-menu`, `theme-menu__item` naming. The new `theme.ts` module follows the existing `initSidebar()` / `init()` pattern in `main.ts` — a single exported `initTheme()` function, called from `initializeApp()`. A small inline `<script>` in `<head>` is the single justified exception to the "no inline scripts" implicit convention; it is required for FOUC prevention and cannot be deferred. ✅ No structural deviations.

- **Testing**:
  - **Backend xUnit integration tests**: No changes needed — the API contract is entirely unchanged. All existing tests must continue to pass.
  - **Frontend Vitest unit tests**: The `getStoredPreference()`, `resolveTheme()`, and `applyTheme()` functions in `theme.ts` MUST have unit tests covering: each explicit preference (light, dark, system); OS dark + system → dark; OS light + system → light; corrupt/missing preference → falls back to 'system'; real-time OS change triggers `applyTheme`. This is consistent with the `reorder()` utility test pattern from feature 006.
  - **Regression**: All existing Vitest tests (`router.test.ts`) must continue to pass.
  - ✅ Tests treated as mandatory, not optional.

- **Security**:
  - `localStorage` stores only one of three string literals (`'light'`, `'dark'`, `'system'`). The inline script and `theme.ts` both validate the stored value against this allowlist before use; any unrecognised value is ignored and `'system'` is used.
  - The `data-theme` attribute is set to either `'light'` or `'dark'` — never to user-supplied content; no injection risk.
  - No new API endpoints, user inputs, or authentication surfaces introduced.
  - ✅ No new trust boundaries; existing security posture unchanged.

- **User Experience Consistency**:
  - The theme selector button (`.topbar__theme-btn`) uses the same size, shape, `border-radius`, focus ring, and hover state as `.topbar__toggle`, established in feature 001.
  - The dropdown menu (`.theme-menu`) uses the same surface colour, border, `border-radius`, and box-shadow as existing modal/card surfaces in the app.
  - All CSS custom property values in `[data-theme="dark"]` are chosen to meet WCAG AA contrast (4.5:1 for normal text) — verified against the dark background values in the data model.
  - `.sr-only` visually-hidden utility class is already present (added in feature 006) — no addition needed.
  - The topbar layout change (always visible, desktop `left: var(--sidebar-width)`) aligns the title and button with the content area width, matching the sidebar's visual rhythm. ✅

- **Performance**:
  - Theme application is synchronous CSS attribute mutation on `<html>` — no async work, no layout recalculations beyond the one repaint triggered by the cascade. For the 11 overridden custom properties, this is < 5 ms on any modern browser. ✅
  - FOUC prevention: the inline script in `<head>` runs synchronously before the first paint, setting `data-theme` before any CSS is applied. No flash is possible. ✅
  - Real-time OS tracking: the `matchMedia` `'change'` event fires on the browser's event loop — the handler calls `applyTheme()` only when preference is `'system'`; no polling, no timers. ✅
  - No new network requests or API calls introduced. ✅

## Project Structure

### Documentation (this feature)

```text
specs/007-dark-mode-toggle/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── ui-contract.md   # Theme button and menu HTML/CSS/ARIA contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── index.html                          # MODIFIED: flash-prevention script in <head>,
    │                                       #           theme button in .topbar,
    │                                       #           topbar always-visible layout fix
    ├── css/
    │   └── styles.css                      # MODIFIED: [data-theme="dark"] overrides,
    │                                       #           .topbar__theme-btn styles,
    │                                       #           .theme-menu / .theme-menu__item styles,
    │                                       #           topbar always-visible layout
    └── ts/
        ├── main.ts                         # MODIFIED: import and call initTheme()
        ├── theme.ts                        # NEW: theme preference module
        └── __tests__/
            └── theme.test.ts              # NEW: Vitest unit tests for theme.ts

src/WorkoutTracker.Infrastructure/          # UNCHANGED
src/WorkoutTracker.Api/                     # UNCHANGED
src/WorkoutTracker.Tests/                   # UNCHANGED (backend tests)
```

**Structure Decision**: The existing .NET Aspire solution structure is preserved. No new projects. The `theme.ts` module follows the established `sidebar.ts` pattern — a standalone module with a single exported `init*()` function, imported and called from `main.ts`. The Vitest test file follows the existing `__tests__/router.test.ts` pattern.

## Complexity Tracking

> No constitution violations — no entries required.

## Implementation Phases

### Phase 0: Research & Clarification

**Output**: `research.md` ✅ Complete

**Key findings**:
1. **No backend changes needed** — pure frontend CSS + TypeScript change.
2. **CSS custom properties** are already used consistently across the app — dark mode is implemented by overriding the 8 colour properties under `[data-theme="dark"]` on `<html>`.
3. **Inline script in `<head>`** is the only reliable FOUC prevention strategy — reads `localStorage` and sets `data-theme` synchronously before first paint.
4. **Topbar made always-visible** — offset by `left: var(--sidebar-width)` on desktop so it doesn't overlap the sidebar; content area gains matching `padding-top` on all breakpoints.
5. **`matchMedia('(prefers-color-scheme: dark)').addEventListener('change', ...)`** provides real-time OS tracking without polling.
6. **Allowlist validation** of `localStorage` value prevents any unexpected attribute value on `<html>`.

### Phase 1: Design & Contracts

**Output**: All complete ✅

- `research.md` — all unknowns resolved
- `data-model.md` — no schema changes; documents theme state model and localStorage contract
- `contracts/ui-contract.md` — theme button and dropdown HTML/CSS/ARIA specification
- `quickstart.md` — user-facing walkthrough for selecting and switching themes

**Constitution Check (post-design)**: All Phase 1 outputs confirm no backend changes, no migrations, no new API surfaces. Layout change (topbar always visible) is additive and consistent with existing BEM structure. WCAG AA contrast verified for all dark palette values. No constitution violations.

### Phase 2: Implementation (Dependency Graph)

**Prerequisites**: Phase 1 complete ✅

**Workstream A: Flash-prevention inline script + topbar layout (`index.html`)**

1. Add inline `<script>` as last child of `<head>` (before `</head>`):
   - Reads `localStorage.getItem('workout-tracker-theme')`
   - Validates against `['light', 'dark', 'system']`; defaults to `'system'` if invalid/missing
   - Resolves effective theme: `'dark'` → `'dark'`; `'light'` → `'light'`; `'system'` → query `prefers-color-scheme`
   - Sets `document.documentElement.setAttribute('data-theme', resolvedTheme)` synchronously
2. Add theme selector button markup inside `.topbar`, right-aligned (after `.topbar__title`):
   - `<button id="theme-btn" class="topbar__theme-btn" type="button" aria-label="Theme: System" aria-haspopup="true" aria-expanded="false">`
   - Contains: moon/sun inline SVG icon + `<span class="topbar__theme-indicator" aria-hidden="true">` (system dot indicator)
3. Add dropdown menu markup immediately after the button (outside `.topbar`, inside `<header>`):
   - `<div id="theme-menu" class="theme-menu" role="menu" aria-labelledby="theme-btn" hidden>`
   - Three `<button type="button" role="menuitem" class="theme-menu__item" data-theme-value="[light|dark|system]">` items, each with icon SVG + label text

**Workstream B: CSS (`styles.css`)**

1. **Dark theme overrides** — add `[data-theme="dark"]` block after `:root`:
   ```
   --color-primary-light: #1e3a5f
   --color-error:         #f87171
   --color-error-bg:      #450a0a
   --color-text:          #f9fafb
   --color-text-light:    #9ca3af
   --color-bg:            #111827
   --color-white:         #1f2937
   --color-border:        #374151
   ```
2. **Topbar always-visible layout** — change `.topbar { display: none }` to `display: flex`; remove the `@media (max-width: 767px) .topbar { display: flex }` rule; add `@media (min-width: 768px) .topbar { left: var(--sidebar-width) }` so topbar appears to the right of the sidebar on desktop; move `padding-top: calc(var(--topbar-height) + var(--spacing-lg))` on `.content` out of the mobile media query to apply at all breakpoints.
3. **`.topbar__theme-btn`** — same dimensions, background, border, cursor, hover, and focus-visible styles as `.topbar__toggle`; `margin-left: auto` to push it to the right edge.
4. **`.topbar__theme-indicator`** — `6px` dot absolutely positioned at top-right of the button icon; `background: var(--color-primary)`; hidden by default via `display: none`; shown when `[data-theme-pref="system"]` attribute is present on `<html>` (set by `applyTheme()`).
5. **`.theme-menu`** — `position: absolute`; `top: calc(var(--topbar-height) + 0.25rem)`; `right: var(--spacing-md)`; `z-index: 200`; `background: var(--color-white)`; `border: 1px solid var(--color-border)`; `border-radius: var(--radius)`; `box-shadow: 0 4px 12px rgba(0,0,0,0.12)`; `min-width: 9rem`; `padding: var(--spacing-xs) 0`.
6. **`.theme-menu__item`** — `display: flex`; `align-items: center`; `gap: var(--spacing-sm)`; `width: 100%`; `padding: var(--spacing-xs) var(--spacing-md)`; `background: none`; `border: none`; `cursor: pointer`; `color: var(--color-text)`; `font-size: var(--font-size-base)`; hover: `background: var(--color-bg)`. Active item: `font-weight: 600`; checkmark icon shown via `[data-active="true"]` attribute.

**Workstream C: `theme.ts` (new module)**

```typescript
export type ThemePreference = 'light' | 'dark' | 'system';
export type ResolvedTheme   = 'light' | 'dark';

const STORAGE_KEY = 'workout-tracker-theme';
const VALID: ThemePreference[] = ['light', 'dark', 'system'];

export function getStoredPreference(): ThemePreference
  // reads localStorage; validates; returns 'system' if missing/invalid

export function getSystemPreference(): ResolvedTheme
  // queries matchMedia('(prefers-color-scheme: dark)'); returns 'dark' | 'light'

export function resolveTheme(pref: ThemePreference): ResolvedTheme
  // 'light'|'dark' → return as-is; 'system' → getSystemPreference()

export function applyTheme(pref: ThemePreference): void
  // 1. resolved = resolveTheme(pref)
  // 2. document.documentElement.setAttribute('data-theme', resolved)
  // 3. document.documentElement.setAttribute('data-theme-pref', pref)
  //    (drives system indicator dot visibility via CSS attribute selector)
  // 4. Update #theme-btn aria-label: "Theme: Light|Dark|System"
  // 5. Update #theme-btn icon: moon SVG when resolved=light, sun SVG when resolved=dark
  // 6. Update each .theme-menu__item: set data-active="true" on matching item, "false" on others

export function initTheme(): void
  // 1. pref = getStoredPreference()
  // 2. applyTheme(pref)
  // 3. Register matchMedia 'change' listener:
  //    if getStoredPreference() === 'system' → applyTheme('system')
  // 4. Register #theme-btn click → toggle menu (toggle [hidden] on #theme-menu,
  //    toggle aria-expanded on button, move focus to first menu item when opening)
  // 5. Register click on each .theme-menu__item:
  //    pref = item.dataset.themeValue; localStorage.setItem(STORAGE_KEY, pref);
  //    applyTheme(pref); close menu
  // 6. Register document 'click' outside #theme-btn + #theme-menu → close menu
  // 7. Register document 'keydown' Escape → close menu, return focus to #theme-btn
  // 8. Register menu item 'keydown' ArrowDown/ArrowUp → move focus between items
```

**Workstream D: `main.ts`**

1. Add `import { initTheme } from './theme.js';`
2. Add `initTheme();` call inside `initializeApp()`, after `initSidebar()` and before `init()`

**Workstream E: `__tests__/theme.test.ts`**

Unit tests covering:
- `getStoredPreference()`: returns `'system'` for missing key; returns `'system'` for invalid value; returns stored `'light'`; returns stored `'dark'`; returns stored `'system'`
- `resolveTheme()`: `'light'` → `'light'`; `'dark'` → `'dark'`; `'system'` + OS dark → `'dark'`; `'system'` + OS light → `'light'`
- `applyTheme()`: sets `data-theme` attribute correctly for all three inputs; sets `data-theme-pref` attribute; updates `aria-label` on button; updates `data-active` on menu items

**Dependencies**:
- Workstream A (HTML) and Workstream B (CSS) are independent — can start in parallel
- Workstream C (`theme.ts`) depends on the DOM structure finalised in A
- Workstream D (`main.ts`) depends on C being complete
- Workstream E (tests) depends on C being complete
- All workstreams must complete before final Vitest + browser smoke test run
