# UI Contract: Sidebar Icons for Workouts and Exercises

## Purpose

Define the canonical SVG markup for the two sidebar icons being replaced, including all required wrapper attributes.

## Icon Contract

### Workouts — Lucide `dumbbell`

```html
<svg class="sidebar__icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
  <path d="M17.596 12.768a2 2 0 1 0 2.829-2.829l-1.768-1.767a2 2 0 0 0 2.828-2.829l-2.828-2.828a2 2 0 0 0-2.829 2.828l-1.767-1.768a2 2 0 1 0-2.829 2.829z" />
  <path d="m2.5 21.5 1.4-1.4" />
  <path d="m20.1 3.9 1.4-1.4" />
  <path d="M5.343 21.485a2 2 0 1 0 2.829-2.828l1.767 1.768a2 2 0 1 0 2.829-2.829l-6.364-6.364a2 2 0 1 0-2.829 2.829l1.768 1.767a2 2 0 0 0-2.828 2.829z" />
  <path d="m9.6 14.4 4.8-4.8" />
</svg>
```

Source: Lucide 1.16.0 `dumbbell` — https://lucide.dev/icons/dumbbell

---

### Exercises — Lucide `sport-shoe`

```html
<svg class="sidebar__icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
  <path d="m15 10.42 4.8-5.07" />
  <path d="M19 18h3" />
  <path d="M9.5 22 21.414 9.415A2 2 0 0 0 21.2 6.4l-5.61-4.208A1 1 0 0 0 14 3v2a2 2 0 0 1-1.394 1.906L8.677 8.053A1 1 0 0 0 8 9c-.155 6.393-2.082 9-4 9a2 2 0 0 0 0 4h14" />
</svg>
```

Source: Lucide 1.16.0 `sport-shoe` — https://lucide.dev/icons/sport-shoe

## Attribute Consistency Contract

All sidebar icons (including the two above) MUST use the following wrapper attributes:

| Attribute          | Required Value    |
|--------------------|-------------------|
| `class`            | `sidebar__icon`   |
| `width`            | `20`              |
| `height`           | `20`              |
| `viewBox`          | `0 0 24 24`       |
| `fill`             | `none`            |
| `stroke`           | `currentColor`    |
| `stroke-width`     | `2`               |
| `stroke-linecap`   | `round`           |
| `stroke-linejoin`  | `round`           |
| `aria-hidden`      | `true`            |

## Unchanged Icons

The following sidebar icons are explicitly out of scope and MUST NOT be modified:

| Nav Item  | Current Icon |
|-----------|--------------|
| Let's go! | Flame        |
| Muscles   | Body/muscle  |
| History   | Clock circle |
