# Data Model: Accessible Exercise Ordering While Scrolling

No persistence model, endpoint, or data migration changes were made.

## Existing entities

- **Current workout**: `workout` holds the workout identity, name, and ordered `exercises` array.
- **Workout exercise**: `exerciseId`, name and targets; array position represents the current order.
- **Log entry**: `logEntries` associates entered weight and effort with `exerciseId`, not array position.
- **Order-edit state**: `isOrderEditing`, `orderBeforeEditing`, and `hasOrderChanges` track the active edit and original sequence.

## State and invariants

| State | Order action | Behavior |
|---|---|---|
| Loading, blocking workout-load failure, or no exercises | Hidden under existing availability rules | Existing loading, workout-load error and empty handling remains unchanged. |
| Normal view with one or more exercises | Sticky in the desktop rail; floating above full-width content on mobile | Opens the existing editor. |
| Non-blocking error after a workout with exercises has loaded, including previous-performance or session-save failure | Visible in normal view | Retain the existing error feedback and allow ordering; availability still follows the exercise count and editing state. |
| Order editing | Hidden | Existing name-only sortable list and Done/Cancel controls remain available. |
| Modal open | Existing availability retained behind modal | Modal must remain above the action and maintain its focus behavior. |

Entering editing snapshots the sequence and focuses the first reorder handle, or Done for a single exercise. Done applies the in-memory sequence; Cancel without changes exits directly, and Cancel with changes asks for confirmation before restoration. Declining the confirmation continues editing. Exercise IDs and their associated logged values remain unchanged throughout. Saving the workout uses the existing session workflow.
