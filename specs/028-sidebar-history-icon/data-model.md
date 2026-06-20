# Data Model: Sidebar History Icon Change

## Entity: SidebarIcon

- **Description**: A navigation icon displayed alongside a sidebar label; purely presentational.
- **Fields**:
  - `navItem` (enum, required): `home` | `workouts` | `exercises` | `muscles` | `history`
  - `lucideIconName` (string): canonical Lucide icon identifier (e.g., `history`)
  - `svgPaths` (string[]): one or more `<path>` elements defining the icon shape
  - `ariaHidden` (boolean, always `true`): icon is decorative; label provides accessible text
- **Validation Rules**:
  - `width` and `height` MUST be `"20"` on all sidebar icons
  - `viewBox` MUST be `"0 0 24 24"` on all sidebar icons
  - `stroke` MUST be `"currentColor"` (no hardcoded colour values)
  - `stroke-width` MUST be `"2"`, `stroke-linecap` MUST be `"round"`, `stroke-linejoin` MUST be `"round"`

## In-Scope Changes

| Nav Item | Previous Icon (description)         | New Icon (Lucide) |
|----------|-------------------------------------|-------------------|
| History  | Clock face (circle + clock hands)   | `history`         |

## Out-of-Scope

| Nav Item  | Icon        | Status    |
|-----------|-------------|-----------|
| Let's go! | Flame       | Unchanged |
| Workouts  | Dumbbell    | Unchanged |
| Exercises | Sport shoe  | Unchanged |
| Muscles   | Body/muscle | Unchanged |
