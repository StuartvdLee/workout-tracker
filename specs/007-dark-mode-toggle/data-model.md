# Data Model: Dark Mode Toggle

**Feature**: `007-dark-mode-toggle`  
**Date**: 2026-05-05

## Summary of Changes

**No database schema changes are required for this feature.**

Theme preference is a per-device, client-side setting stored in `localStorage`. It has no relationship to workout data, user accounts, or server state.

---

## Application-Level State Model

### Theme Preference

The user's explicit theme selection is stored in the browser's `localStorage`.

| Property       | Value                          |
|----------------|--------------------------------|
| Storage key    | `workout-tracker-theme`        |
| Allowed values | `'light'` \| `'dark'` \| `'system'` |
| Default        | `'system'` (when key is absent or value is unrecognised) |
| Persistence    | Per device / per browser; cleared with browser storage |

### Resolved Theme

The *effective* theme applied to the UI — computed from the stored preference and OS state.

| ThemePreference | OS preference   | ResolvedTheme |
|-----------------|-----------------|---------------|
| `'light'`       | any             | `'light'`     |
| `'dark'`        | any             | `'dark'`      |
| `'system'`      | dark            | `'dark'`      |
| `'system'`      | light           | `'light'`     |
| `'system'`      | undetectable    | `'light'`     |

`ResolvedTheme` is applied as the `data-theme` attribute on `<html>`. It is always either `'light'` or `'dark'` — never `'system'`.

### Button Icon

A separate `data-theme-pref` attribute is also set on `<html>` (value: `'light'`, `'dark'`, or `'system'`). CSS uses this to show the icon that represents the active preference:

```css
html[data-theme-pref="light"]  .topbar__theme-icon--sun    { display: block; }
html[data-theme-pref="dark"]   .topbar__theme-icon--moon   { display: block; }
html[data-theme-pref="system"] .topbar__theme-icon--system { display: block; }
```

---

## TypeScript Types

```typescript
type ThemePreference = 'light' | 'dark' | 'system';
type ResolvedTheme   = 'light' | 'dark';
```

---

## State Transitions

```
         ┌──────────────────────────────────────────────────────┐
         │                   User selects menu item             │
         ▼                                                      │
  ┌─────────────┐   select Light   ┌──────────────┐            │
  │   current   │ ──────────────── │   light      │ ───────────┤
  │ preference  │   select Dark    │   dark       │            │
  │             │ ──────────────── │   system     │            │
  └─────────────┘   select System  └──────────────┘            │
                                          │                     │
                         OS pref changes  │  (only when system) │
                                          ▼                     │
                                   ┌────────────┐              │
                                   │ re-resolve │──────────────┘
                                   │ & apply    │
                                   └────────────┘
```

### Transitions

| Trigger | Action |
|---|---|
| User selects "Light" | Store `'light'`; set `data-theme="light"`; update button icon to sun |
| User selects "Dark" | Store `'dark'`; set `data-theme="dark"`; update button icon to moon |
| User selects "System" | Store `'system'`; resolve from OS; set `data-theme`; show monitor icon |
| OS preference changes (while `'system'` is stored) | Re-resolve; update `data-theme`; update icon |
| Page load (preference exists) | Read stored value; resolve; set `data-theme` (via inline script + `initTheme()`) |
| Page load (no stored preference) | Default to `'system'`; resolve from OS; set `data-theme` |
| Stored value is invalid/corrupt | Treat as `'system'`; write `'system'` back to localStorage on next selection |

---

## No Other Entities Affected

All backend entities (`PlannedWorkout`, `WorkoutSession`, `LoggedExercise`, `Exercise`, `Muscle`, `ExerciseMuscle`) are entirely unaffected. The theme setting is not transmitted to or stored on the server.
