# UI Contract: Sidebar History Icon Change

## Purpose

Define the canonical SVG markup for the History sidebar icon being replaced, including all required wrapper attributes.

## Icon Contract

### History — Lucide `history`

```html
<svg class="sidebar__icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
  <path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
  <path d="M3 3v5h5" />
  <path d="M12 7v5l4 2" />
</svg>
```

Source: Lucide `history` — https://lucide.dev/icons/history

---

## Attribute Consistency Contract

All sidebar icons (including the one above) MUST use the following wrapper attributes:

| Attribute        | Required Value  |
|------------------|-----------------|
| `class`          | `sidebar__icon` |
| `width`          | `20`            |
| `height`         | `20`            |
| `viewBox`        | `0 0 24 24`     |
| `fill`           | `none`          |
| `stroke`         | `currentColor`  |
| `stroke-width`   | `2`             |
| `stroke-linecap` | `round`         |
| `stroke-linejoin`| `round`         |
| `aria-hidden`    | `true`          |

## Unchanged Icons

The following sidebar icons are explicitly out of scope and MUST NOT be modified:

| Nav Item  | Current Icon |
|-----------|--------------|
| Let's go! | Flame        |
| Workouts  | Dumbbell     |
| Exercises | Sport shoe   |
| Muscles   | Body/muscle  |
