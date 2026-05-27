# Data Model: Sidebar Icons for Workouts and Exercises

## Entity: SidebarIcon

- **Description**: A navigation icon displayed alongside a sidebar label; purely presentational.
- **Fields**:
  - `navItem` (enum, required): `workouts` | `exercises` | `home` | `muscles` | `history`
  - `lucideIconName` (string): canonical Lucide icon identifier (e.g., `dumbbell`, `sport-shoe`)
  - `svgPaths` (string[]): one or more `<path>` elements defining the icon shape
  - `ariaHidden` (boolean, always `true`): icon is decorative; label provides accessible text
- **Validation Rules**:
  - `width` and `height` MUST be `"20"` on all sidebar icons
  - `viewBox` MUST be `"0 0 24 24"` on all sidebar icons
  - `stroke` MUST be `"currentColor"` (no hardcoded colour values)
  - `stroke-width` MUST be `"2"`, `stroke-linecap` MUST be `"round"`, `stroke-linejoin` MUST be `"round"`

## In-Scope Changes

| Nav Item  | Previous Icon (description)    | New Icon (Lucide)  |
|-----------|--------------------------------|--------------------|
| Workouts  | Barbell / barbell-style paths  | `dumbbell`         |
| Exercises | Bulleted list paths            | `sport-shoe`       |

## Out-of-Scope

| Nav Item  | Icon         | Status      |
|-----------|--------------|-------------|
| Let's go! | Flame        | Unchanged   |
| Muscles   | Body/muscle  | Unchanged   |
| History   | Clock circle | Unchanged   |
