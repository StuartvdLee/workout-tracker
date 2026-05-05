# UI Contract: Dark Mode Toggle

**Feature**: `007-dark-mode-toggle`  
**Date**: 2026-05-05

---

## Topbar Layout (Responsive)

The `.topbar` element is hidden by default and rendered differently per breakpoint.

### Desktop (≥ 768 px)

The topbar is `display: flex` but has `background: transparent`, `border-bottom: none`, `left: auto`, and `width: auto`. The hamburger toggle and app title are hidden via `display: none`. Only the theme button is visible, floating transparently in the top-right corner of the viewport. No topbar height offset is added to `.content` on desktop.

```
                                                          [theme-btn ☾/☀]
                                          (transparent, top-right corner)
```

### Mobile (< 768 px)

The topbar is `display: flex` as a full-width bar. Content has `padding-top: calc(var(--topbar-height) + var(--spacing-lg))` to clear it.

```
┌──────────────────────────────────────────────────────────────────────────┐
│ [☰]      Workout Tracker                             [theme-btn ☾/☀] │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## HTML Structure

### Theme Button + Dropdown (inside `.topbar`)

```html
<div class="topbar__theme">
  <button
    class="topbar__theme-btn"
    id="theme-btn"
    aria-label="Theme: System"
    aria-haspopup="menu"
    aria-expanded="false"
    aria-controls="theme-menu"
    type="button"
  >
    <!-- Moon icon (shown in light mode) -->
    <svg
      class="topbar__theme-icon topbar__theme-icon--moon"
      aria-hidden="true"
      focusable="false"
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
    </svg>
    <!-- Sun icon (shown in dark mode) -->
    <svg
      class="topbar__theme-icon topbar__theme-icon--sun"
      aria-hidden="true"
      focusable="false"
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      <circle cx="12" cy="12" r="5"/>
      <line x1="12" y1="1"  x2="12" y2="3"/>
      <line x1="12" y1="21" x2="12" y2="23"/>
      <line x1="4.22" y1="4.22"  x2="5.64" y2="5.64"/>
      <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
      <line x1="1"  y1="12" x2="3"  y2="12"/>
      <line x1="21" y1="12" x2="23" y2="12"/>
      <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
      <line x1="18.36" y1="5.64"  x2="19.78" y2="4.22"/>
    </svg>
    <!-- System indicator dot (visible only when data-theme-pref="system") -->
    <span class="topbar__theme-indicator" aria-hidden="true"></span>
    <span class="sr-only">Theme</span>
  </button>

  <ul
    id="theme-menu"
    class="theme-menu"
    role="menu"
    aria-labelledby="theme-btn"
    hidden
  >
    <li role="none">
      <button class="theme-menu__item" role="menuitem" data-theme-value="light" type="button">
        <!-- Moon icon (small, inline) -->
        <svg class="theme-menu__icon" aria-hidden="true" focusable="false"
             xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
             fill="none" stroke="currentColor" stroke-width="2"
             stroke-linecap="round" stroke-linejoin="round">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
        </svg>
        Light
        <svg class="theme-menu__check" aria-hidden="true" focusable="false"
             xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
             fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round">
          <polyline points="20 6 9 17 4 12"/>
        </svg>
      </button>
    </li>
    <li role="none">
      <button class="theme-menu__item" role="menuitem" data-theme-value="dark" type="button">
        <!-- Sun icon (small, inline) -->
        <svg class="theme-menu__icon" aria-hidden="true" focusable="false"
             xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
             fill="none" stroke="currentColor" stroke-width="2"
             stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="5"/>
          <line x1="12" y1="1"  x2="12" y2="3"/>
          <line x1="12" y1="21" x2="12" y2="23"/>
          <line x1="4.22" y1="4.22"   x2="5.64" y2="5.64"/>
          <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
          <line x1="1"  y1="12" x2="3"  y2="12"/>
          <line x1="21" y1="12" x2="23" y2="12"/>
          <line x1="4.22" y1="19.78"  x2="5.64" y2="18.36"/>
          <line x1="18.36" y1="5.64"  x2="19.78" y2="4.22"/>
        </svg>
        Dark
        <svg class="theme-menu__check" aria-hidden="true" focusable="false"
             xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
             fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round">
          <polyline points="20 6 9 17 4 12"/>
        </svg>
      </button>
    </li>
    <li role="none">
      <button class="theme-menu__item" role="menuitem" data-theme-value="system" type="button">
        <!-- Monitor / auto icon -->
        <svg class="theme-menu__icon" aria-hidden="true" focusable="false"
             xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
             fill="none" stroke="currentColor" stroke-width="2"
             stroke-linecap="round" stroke-linejoin="round">
          <rect x="2" y="3" width="20" height="14" rx="2" ry="2"/>
          <line x1="8" y1="21" x2="16" y2="21"/>
          <line x1="12" y1="17" x2="12" y2="21"/>
        </svg>
        System
        <svg class="theme-menu__check" aria-hidden="true" focusable="false"
             xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"
             fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" stroke-linejoin="round">
          <polyline points="20 6 9 17 4 12"/>
        </svg>
      </button>
    </li>
  </ul>
</div>
```

---

## Attribute / State Matrix

| html attribute         | Value          | Meaning                                    |
|------------------------|----------------|--------------------------------------------|
| `data-theme`           | `"light"`      | Light theme active                         |
| `data-theme`           | `"dark"`       | Dark theme active                          |
| `data-theme-pref`      | `"light"`      | User explicitly selected light             |
| `data-theme-pref`      | `"dark"`       | User explicitly selected dark              |
| `data-theme-pref`      | `"system"`     | Following OS; indicator dot visible        |
| `aria-expanded`        | `"true"`       | Dropdown menu is open                      |
| `aria-expanded`        | `"false"`      | Dropdown menu is closed                    |
| `.theme-menu[hidden]`  | present        | Menu hidden (CSS `display: none`)          |
| `.theme-menu[hidden]`  | absent         | Menu visible                               |

---

## CSS Classes

### Icon Visibility

| Class | Condition when visible |
|---|---|
| `.topbar__theme-icon--moon` | `html[data-theme="light"] .topbar__theme-btn` |
| `.topbar__theme-icon--sun`  | `html[data-theme="dark"] .topbar__theme-btn`  |
| `.topbar__theme-indicator`  | `html[data-theme-pref="system"] .topbar__theme-btn` |

Both icon SVGs are rendered in the DOM at all times; CSS hides the inactive one:
```css
.topbar__theme-icon--moon,
.topbar__theme-icon--sun { display: none; }

html[data-theme="light"] .topbar__theme-icon--moon { display: block; }
html[data-theme="dark"]  .topbar__theme-icon--sun  { display: block; }
```

### Active Menu Item (checkmark)

The `.theme-menu__check` SVG is hidden by default. JavaScript adds `.theme-menu__item--active` to the currently selected item, which shows the checkmark:
```css
.theme-menu__check { display: none; }
.theme-menu__item--active .theme-menu__check { display: block; }
```

---

## Keyboard Interaction Contract

| Key | Focused element | Action |
|-----|-----------------|--------|
| Enter / Space | `.topbar__theme-btn` (closed) | Open menu; focus first item |
| Enter / Space | `.topbar__theme-btn` (open) | Close menu |
| Enter / Space | `.theme-menu__item` | Select item; close menu; return focus to button |
| ArrowDown | `.theme-menu__item` | Move focus to next item (wraps to first) |
| ArrowUp | `.theme-menu__item` | Move focus to previous item (wraps to last) |
| Escape | any within `.topbar__theme` | Close menu; return focus to button |
| Tab | any within `.topbar__theme` | Close menu; move focus to next focusable element |

---

## ARIA Contract

| Element | Role | Required ARIA attributes |
|---------|------|--------------------------|
| `.topbar__theme-btn` | `button` (implicit) | `aria-haspopup="menu"`, `aria-expanded`, `aria-controls="theme-menu"`, `aria-label="Theme: <current>"` (dynamic; initialised to `"Theme: System"`, updated by `applyTheme()` to `"Theme: Light"`, `"Theme: Dark"`, or `"Theme: System"`) |
| `#theme-menu` | `menu` | `aria-labelledby="theme-btn"` |
| `<li>` wrappers | `none` | — |
| `.theme-menu__item` | `menuitem` | — |
| `.topbar__theme-icon--*` SVGs | decorative | `aria-hidden="true"`, `focusable="false"` |
| `.topbar__theme-indicator` | decorative | `aria-hidden="true"` |
| `<span class="sr-only">Theme</span>` | — | Provides accessible label text for screen readers |

`aria-expanded` is toggled by JavaScript (`"false"` ↔ `"true"`) alongside the `hidden` attribute on `#theme-menu`.

---

## Flash-Prevention Script (inline `<head>`)

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

Placement: last element inside `<head>`, before `</head>`.

---

## CSS Custom Property Overrides

```css
[data-theme="dark"] {
  --color-primary-light: #1e3a5f;
  --color-error:         #f87171;
  --color-error-bg:      #450a0a;
  --color-text:          #f9fafb;
  --color-text-light:    #9ca3af;
  --color-bg:            #111827;
  --color-white:         #1f2937;
  --color-border:        #374151;
}
```
