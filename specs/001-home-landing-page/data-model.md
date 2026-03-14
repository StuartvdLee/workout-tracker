# Data Model: Home Landing Page

**Date**: 2026-03-14
**Feature**: 001-home-landing-page

## Overview

This feature has no persistent data. The workout types are a fixed,
hardcoded set used only for display in a dropdown. No database tables,
migrations, or storage operations are introduced.

## Entities

### Workout Type (client-side only)

A predefined category of workout used for dropdown selection.

| Attribute | Description                          |
|-----------|--------------------------------------|
| value     | Internal identifier (e.g., "push")   |
| label     | Display text (e.g., "Push")          |

**Fixed values**: Push, Pull, Legs

**Storage**: None — defined as constants in the frontend TypeScript code.

**Relationships**: None for this feature. In future features, a selected
workout type will be associated with a Workout session entity.

## State Transitions

### Dropdown Selection State

```text
[Unselected] --(user selects option)--> [Selected: Push|Pull|Legs]
[Selected]   --(user selects placeholder)--> [Unselected]
```

### Validation State

```text
[No Error] --(button pressed + unselected)--> [Error: "Please select a workout"]
[Error]    --(button pressed + selected)----> [No Error]
[Error]    --(button pressed + unselected)--> [Error] (no change, no duplication)
```

## Future Considerations

When the workout logging feature is implemented, the Workout Type will
become a foreign key reference in a `workouts` table stored in
PostgreSQL. The frontend constants should be aligned with the database
values at that point.
