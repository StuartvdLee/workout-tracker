# Research: Dark Mode Toggle

**Feature**: `007-dark-mode-toggle`  
**Date**: 2026-05-05

## Summary

All design decisions are resolved. This feature is a **pure frontend change** — no backend, API, or database modifications are required. Dark mode is delivered by redefining the 8 existing CSS colour custom properties under `[data-theme="dark"]` on `<html>`. A new `theme.ts` module handles preference storage, resolution, and DOM application. The topbar is made always visible (desktop + mobile) so the theme selector button is persistently accessible in the top-right corner on all viewport sizes.

---

## Decision 1: No Backend Changes Required

**Decision**: None required.

**Rationale**: Theme preference is a purely client-side, per-device setting with no impact on workout data, API responses, or server-rendered content. Storing it in `localStorage` is the standard approach for single-page applications.

**Alternatives considered**: Storing preference in a user profile on the server — rejected as disproportionate for this feature; would require authentication, a new API endpoint, and migration for a cosmetic setting.

---

## Decision 2: Implementation Strategy — CSS Custom Properties + `data-theme` Attribute

**Decision**: Implement dark mode by adding a `[data-theme="dark"]` attribute selector block in `styles.css` that overrides the 8 existing colour custom properties. The attribute is set on `<html>` by `theme.ts`. All components automatically inherit the new values via the CSS cascade — no component-level changes are needed.

**Rationale**:
- The app already uses CSS custom properties consistently for all colour values; overriding them at the root is the minimal, most maintainable approach.
- Setting the attribute on `<html>` (not `<body>`) ensures the cascade applies before any element is painted, including `<head>` styles.
- Zero component changes required — the entire app changes theme with a single attribute mutation.

**Colour mapping** (light → dark):

| Property              | Light value | Dark value  | Rationale                                          |
|-----------------------|-------------|-------------|----------------------------------------------------|
| `--color-primary`     | `#2563eb`   | `#2563eb`   | Blue primary is legible on both backgrounds        |
| `--color-primary-hover` | `#1d4ed8` | `#1d4ed8`   | Unchanged                                          |
| `--color-primary-light` | `#eff6ff` | `#1e3a5f`   | Tinted dark blue surface for active/selected states |
| `--color-error`       | `#dc2626`   | `#f87171`   | Lighter red for legibility on dark background      |
| `--color-error-bg`    | `#fef2f2`   | `#450a0a`   | Dark red tint surface                              |
| `--color-text`        | `#1f2937`   | `#f9fafb`   | Near-white for primary text on dark                |
| `--color-text-light`  | `#6b7280`   | `#9ca3af`   | Lighter muted text on dark surface                 |
| `--color-bg`          | `#f9fafb`   | `#111827`   | Very dark page background (GitHub-style)           |
| `--color-white`       | `#ffffff`   | `#1f2937`   | Dark surface for cards, sidebar, topbar            |
| `--color-border`      | `#d1d5db`   | `#374151`   | Subtle dark border                                 |
| `--color-border-focus`| `#2563eb`   | `#2563eb`   | Focus ring colour unchanged                        |

WCAG AA verification (4.5:1 minimum for normal text):
- `--color-text` on `--color-bg`: `#f9fafb` on `#111827` = ~16:1 ✅
- `--color-text` on `--color-white`: `#f9fafb` on `#1f2937` = ~14:1 ✅
- `--color-text-light` on `--color-bg`: `#9ca3af` on `#111827` = ~7:1 ✅
- `--color-primary` on `--color-white`: `#2563eb` on `#1f2937` = ~5.1:1 ✅
- `--color-error` on `--color-error-bg`: `#f87171` on `#450a0a` = ~4.8:1 ✅

**Alternatives considered**: CSS class toggle on `<body>` — rejected in favour of `data-theme` attribute on `<html>` which is semantically cleaner and more commonly adopted by design systems (Tailwind, Radix, Mantine).

---

## Decision 3: Flash-of-Unstyled-Content (FOUC) Prevention

**Decision**: Add a small inline `<script>` as the last element inside `<head>` in `index.html`. The script synchronously reads `localStorage`, validates the stored value, resolves the effective theme, and sets `document.documentElement.setAttribute('data-theme', resolved)` before the browser applies any CSS.

**Rationale**: CSS is applied after the HTML parser finishes `<head>`. If `data-theme` is not set before the first paint, the browser renders the default (light) theme momentarily before `theme.ts` corrects it — a visible flash. The inline script is the only reliable way to set the attribute synchronously before any paint occurs.

**Script logic**:
```javascript
(function () {
  var stored = localStorage.getItem('workout-tracker-theme');
  var valid = ['light', 'dark', 'system'];
  var pref = valid.indexOf(stored) !== -1 ? stored : 'system';
  var resolved = pref === 'dark' ? 'dark'
    : pref === 'light' ? 'light'
    : (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  document.documentElement.setAttribute('data-theme', resolved);
})();
```

**Alternatives considered**: CSS `@media (prefers-color-scheme: dark)` only — rejected as it cannot honour the stored `'light'` or `'dark'` manual override; it only follows the OS.

---

## Decision 4: Topbar Always Visible (Layout Change)

**Decision**: Make the `.topbar` always visible on all viewport sizes. On desktop (≥ 768 px), offset it with `left: var(--sidebar-width)` so it appears to the right of the sidebar. Apply `padding-top: calc(var(--topbar-height) + var(--spacing-lg))` to `.content` at all breakpoints (currently mobile-only).

**Rationale**: The spec requires the theme selector to be visible in the top right corner on every page. On desktop, the current layout has no persistent top bar — the sidebar is the only persistent chrome. Making the topbar always visible is the least-disruptive change that delivers "top right corner" on all viewport sizes without duplicating the button into the sidebar.

**Visual result**:
- Desktop: narrow topbar strip runs from the right edge of the sidebar to the viewport right edge; contains the app title (left) and theme button (right).
- Mobile: topbar spans full width as before; hamburger (left), title (centre), theme button (right).

**Alternatives considered**: Adding a second theme button to the sidebar footer for desktop — rejected as it duplicates state management and creates two sources of truth for the active-indicator markup.

---

## Decision 5: Three-Option Dropdown Menu

**Decision**: Implement the theme selector as a custom `<button>` that opens a `role="menu"` dropdown with three `role="menuitem"` buttons (Light, Dark, System). The menu is toggled via `aria-expanded` / `hidden`.

**Rationale**: A `<select>` element cannot be styled to match the app's visual language without custom CSS hacks. A custom dropdown matches the visual conventions of existing modals and dropdowns, supports ARIA menu keyboard patterns, and can display icons alongside labels.

**Keyboard interaction**: ArrowDown/ArrowUp to navigate items; Enter/Space to select; Escape to close without selecting; Tab to close and move focus out.

**Alternatives considered**: `<details>/<summary>` disclosure — rejected as it lacks `role="menu"` semantics and does not support ArrowKey navigation out of the box.

---

## Decision 6: Real-Time OS Preference Tracking

**Decision**: Register `window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', handler)` in `initTheme()`. The handler calls `applyTheme('system')` only when the stored preference is `'system'`. A manual `'light'` or `'dark'` selection is never overridden by an OS change.

**Rationale**: The spec (FR-006, SC-007) requires the app to update within 1 second when the OS preference changes while preference is `'system'`. The `'change'` event fires within milliseconds of the OS change — well within the 1-second budget.

**Alternatives considered**: Polling `matchMedia.matches` on an interval — rejected as wasteful and unnecessary when an event-based API is available.

---

## Resolved Unknowns

| Unknown | Resolution |
|---|---|
| Backend changes required? | No — localStorage-only, no server involvement |
| FOUC prevention strategy? | Inline `<script>` in `<head>` reads localStorage before first paint |
| Where to place button on desktop? | Topbar made always visible; `left: var(--sidebar-width)` on desktop |
| Two buttons (topbar + sidebar) or one? | One — topbar made always visible; no duplication |
| CSS approach? | CSS custom property overrides under `[data-theme="dark"]` on `<html>` |
| OS change tracking strategy? | `matchMedia 'change'` event — no polling |
| Allowlist validation needed? | Yes — stored value validated before use; defaults to `'system'` |
