# Research: Edit Exercise Order in Current Workout

## Decision 1: Reuse existing workout-editor reorder behavior by extracting a shared helper

**Decision**: Move the generic sortable-list behavior currently private to `workouts.ts` into a shared frontend module, then consume it from both `workouts.ts` and `active-session.ts`.

**Rationale**: The user explicitly asked for the current workout reorder behavior to work "in the same way" as the "Edit Workout" screen. Feature `006-reorder-exercises` already designed and implemented that pattern: six-dot handle, HTML5 drag/drop, touch clone, keyboard pick-up/move/drop, and live announcements. Extracting the behavior avoids duplication and keeps both reorder experiences synchronized.

**Alternatives considered**:

- Copy the drag/drop code into `active-session.ts`: rejected because it creates two implementations that can drift.
- Implement a simpler mouse-only reorder for active sessions: rejected because it would not match the existing touch/keyboard behavior.
- Add a third-party drag-and-drop library: rejected because project constraints require vanilla TypeScript and no new external JS/CSS dependencies.

## Decision 2: Reorder the current workout in memory only

**Decision**: Store the changed current-workout order by mutating/replacing the in-memory `workout.exercises` order in `active-session.ts`; do not update the planned workout template order.

**Rationale**: Feature `010-randomize-exercise-order` already established active-session order as a display/session concern. `active-session.ts` applies `?order=` to `workout.exercises` in memory, and saving a session records each displayed position as `sequence`. This feature extends the same model for manual current-workout reordering.

**Alternatives considered**:

- Persist the reordered order to the planned workout template: rejected because the requirement is for the current workout, not editing the workout template, and it would blur the distinction between active session order and template order.
- Add a new backend endpoint for current-workout order: rejected because there is no separate current-workout server resource; current session order is already represented by the saved session `sequence`.

## Decision 3: Preserve weight and effort values by exercise ID

**Decision**: Keep `logEntries` keyed by `exerciseId` and re-render controls after order changes so values remain associated with the correct exercise.

**Rationale**: `active-session.ts` already stores per-exercise input state in a `Map<string, LogEntry>`. Reordering the exercise array changes display order only; it does not require moving values between entries.

**Alternatives considered**:

- Store input values by list index: rejected because reordering would risk associating weight/effort with the wrong exercise.
- Read all values directly from DOM after reorder: rejected because it is more fragile than preserving the existing ID-keyed state model.

## Decision 4: Collapse active-session exercises into sortable name rows

**Decision**: When order-editing mode is active, replace normal active-session exercise cards with a name-only sortable list. Hide weight, effort, target, previous-data, and normal exercise-entry controls until the user exits the mode.

**Rationale**: This directly satisfies the requirement to "collapse" exercises and show names only. Using the same row structure/classes as workout-editor selected exercises provides the familiar reorder affordance.

**Alternatives considered**:

- Keep full exercise cards visible while dragging: rejected because weight and effort must be hidden.
- Hide only inputs but keep targets/previous data visible: rejected because the requirement says only names should be shown.

## Decision 5: Validate behavior with Playwright E2E tests

**Decision**: Add E2E tests to `WorkoutReorderTests.cs` for the current workout flow and keep existing create/edit workout reorder tests as regression coverage.

**Rationale**: The core behavior is DOM interaction (button state, collapsed rows, drag/drop, restored controls, value preservation). Existing project precedent uses Playwright for these workflows and Vitest for pure utility functions like `reorder<T>()`.

**Alternatives considered**:

- Add only Vitest tests: rejected because drag/drop and active-session rendering behavior is best proven in browser-level tests.
- Rely on manual testing: rejected by Constitution II; new user journeys require automated coverage.
