# UI Contract: Sidebar Navigation Layout

**Feature**: 002-sidebar-navigation
**Date**: 2026-03-27

## Page Routes

The application exposes the following client-side routes. All routes resolve to the same `index.html` shell via server-side fallback.

| Route        | Page        | Default | Description                           |
| ------------ | ----------- | ------- | ------------------------------------- |
| `/`          | Home        | Yes     | Workout selection form                 |
| `/workouts`  | Workouts    | No      | Placeholder — coming soon              |
| `/exercises` | Exercises   | No      | Placeholder — coming soon              |

Any unrecognised route redirects to `/` (Home).

## HTML Structure Contract

The `index.html` shell defines the following semantic structure that page modules and tests depend on:

```
body
├── aside.sidebar                          # Sidebar navigation panel
│   ├── div.sidebar__header                # App title / branding area
│   ├── nav.sidebar__nav                   # Navigation container
│   │   └── a.sidebar__link[data-page]     # Menu items (×3), each with:
│   │       ├── svg.sidebar__icon          # Inline SVG icon
│   │       └── span.sidebar__label        # Text label
│   └── (future: sidebar footer area)
├── div.sidebar__backdrop                  # Mobile overlay backdrop (hidden on desktop)
├── header.topbar                          # Mobile-only top bar
│   └── button.topbar__toggle              # Hamburger menu button
└── main.content                           # Page content area
    └── [page-specific content]            # Rendered by page modules
```

## CSS Class Contract

### Sidebar

| Class                     | Element   | Purpose                                |
| ------------------------- | --------- | -------------------------------------- |
| `.sidebar`                | `aside`   | Sidebar container                       |
| `.sidebar--open`          | `aside`   | Mobile: sidebar visible (modifier)      |
| `.sidebar__header`        | `div`     | App title area at top of sidebar        |
| `.sidebar__nav`           | `nav`     | Navigation list container               |
| `.sidebar__link`          | `a`       | Individual menu item                    |
| `.sidebar__link--active`  | `a`       | Currently active page (modifier)        |
| `.sidebar__icon`          | `svg`     | Menu item icon                          |
| `.sidebar__label`         | `span`    | Menu item text                          |
| `.sidebar__backdrop`      | `div`     | Semi-transparent mobile overlay         |
| `.sidebar__backdrop--visible` | `div` | Backdrop shown (modifier)               |

### Top Bar (mobile)

| Class                | Element   | Purpose                              |
| -------------------- | --------- | ------------------------------------ |
| `.topbar`            | `header`  | Mobile top bar container              |
| `.topbar__toggle`    | `button`  | Hamburger menu toggle                 |

### Content Area

| Class       | Element | Purpose                                  |
| ----------- | ------- | ---------------------------------------- |
| `.content`  | `main`  | Main content area beside sidebar          |

### Page-Specific

| Class                    | Element | Purpose                             |
| ------------------------ | ------- | ----------------------------------- |
| `.page-placeholder`      | `div`   | Wrapper for placeholder pages        |
| `.page-placeholder__title` | `h1` | Page heading                         |
| `.page-placeholder__text`  | `p`  | Coming soon description              |

## Data Attributes

| Attribute        | On Element       | Values                          | Purpose                        |
| ---------------- | ---------------- | ------------------------------- | ------------------------------ |
| `data-page`      | `.sidebar__link` | `home`, `workouts`, `exercises` | Identifies which page the link navigates to |

## ARIA Contract

| Element              | Attribute              | Value                      |
| -------------------- | ---------------------- | -------------------------- |
| `.sidebar__nav`      | `aria-label`           | `"Main navigation"`        |
| `.sidebar__link--active` | `aria-current`     | `"page"`                   |
| `.topbar__toggle`    | `aria-label`           | `"Toggle navigation"`      |
| `.topbar__toggle`    | `aria-expanded`        | `"true"` / `"false"`       |
| `.topbar__toggle`    | `aria-controls`        | ID of sidebar element       |

## API Dependencies (unchanged)

This feature does not introduce or modify any API endpoints. The existing endpoint consumed by the Home page remains:

| Method | Path                | Response                       | Used By   |
| ------ | ------------------- | ------------------------------ | --------- |
| GET    | `/api/workout-types` | `[{workoutTypeId, name}, ...]` | Home page |
