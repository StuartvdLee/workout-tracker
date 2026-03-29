# Research: Add Exercises

**Feature**: 003-add-exercises
**Date**: 2026-07-15

## R1: Muscle Data Storage Strategy

**Decision**: Store muscles in a dedicated `muscles` database table with seed data, not as hardcoded frontend constants.

**Rationale**: The spec requires exercises to persist muscle associations across sessions (FR-006) and muscles to be displayed from a predefined list (FR-010). A database table provides referential integrity for the many-to-many relationship, enables the API to serve the muscle list consistently, and follows the existing Entity Framework Core pattern used by WorkoutType. The 11 predefined muscles are seeded via EF Core's `HasData()` method in `OnModelCreating`, ensuring they exist after migration without manual intervention.

**Alternatives considered**:
- **Hardcoded constants on frontend and backend**: Simpler but creates a synchronisation risk — the frontend and backend must agree on the muscle list. Storing in the database with a single API endpoint eliminates this duplication. Also, a junction table with foreign keys enforces that only valid muscles can be associated with exercises.
- **JSON array column on Exercise**: Stores muscle names directly on the exercise row as a JSON array. Avoids the junction table but loses referential integrity, makes querying exercises by muscle impossible, and diverges from the normalised relational patterns already established.

## R2: Exercise-Muscle Many-to-Many Relationship in EF Core

**Decision**: Use an explicit `ExerciseMuscle` junction entity with its own primary key (`ExerciseMuscleId`), following the same pattern as the existing `WorkoutExercise` junction table.

**Rationale**: The project already uses an explicit junction entity for `WorkoutExercise` rather than EF Core's implicit many-to-many (`Skip Navigation`). Consistency with the existing pattern avoids introducing a second relational convention. The explicit junction entity also provides a natural place to add future attributes (e.g., primary vs secondary muscle targeting) without a migration restructure.

**Alternatives considered**:
- **EF Core implicit many-to-many** (via `Skip Navigation`): EF Core can auto-create a join table when both entities have collection navigation properties. Simpler to configure but deviates from the established `WorkoutExercise` pattern, and the auto-generated table/column names may not follow the project's snake_case convention cleanly.

## R3: Case-Insensitive Unique Constraint on Exercise Name

**Decision**: Add a unique index on a lowered expression of `exercise.name` in PostgreSQL, configured via the EF Core migration. Validate case-insensitive uniqueness in the API before saving.

**Rationale**: PostgreSQL unique constraints are case-sensitive by default. The spec requires case-insensitive duplicate detection (FR-004). An expression index on `LOWER(name)` enforces this at the database level as a safety net. The API also performs a LINQ query with `ToLower()` comparison before insert/update to return a meaningful validation error rather than a raw database exception.

**Alternatives considered**:
- **`citext` extension**: PostgreSQL's case-insensitive text type. Requires enabling a database extension (`CREATE EXTENSION citext`) which adds infrastructure complexity and may not be available in all PostgreSQL configurations. Overkill for a single column.
- **Application-only validation** (no DB constraint): Relies on the API to always check before saving. A race condition could allow duplicates if two requests arrive simultaneously. The database index prevents this.

## R4: Multi-Select UI Pattern for Muscle Selection

**Decision**: Use a grid of toggle buttons (styled as chips/pills) for muscle selection. Each button represents a muscle and toggles between selected and unselected states on click.

**Rationale**: The predefined list has exactly 11 items — small enough to display all at once without scrolling or searching. Toggle buttons are more touch-friendly than checkboxes (larger tap targets meeting the 44px minimum), more visually scannable than a multi-select dropdown, and align with the app's existing button styling patterns. Each toggle button uses `role="checkbox"` and `aria-checked` for accessibility, wrapped in a `role="group"` with a descriptive label.

**Alternatives considered**:
- **HTML `<select multiple>`**: Poor UX on both mobile (tiny touch targets) and desktop (requires Ctrl+click). Users can't see all selections at a glance.
- **Standard checkboxes**: Functional but visually less compact. The checkbox + label pattern takes more vertical space for 11 items. Toggle buttons in a responsive grid are more space-efficient.
- **Dropdown with checkboxes**: Requires a custom dropdown component. Over-engineered for 11 static items that fit comfortably in the viewport.

## R5: Form Create/Edit Mode Pattern

**Decision**: Use a single form component that switches between "create" and "edit" mode via a TypeScript state variable. The form HTML is rendered once; mode changes update the submit button label, show/hide the cancel button, and pre-populate fields. An `editingExerciseId` variable (null for create mode, exercise GUID for edit mode) drives the behaviour.

**Rationale**: The spec explicitly requires reusing the creation form for editing (FR-012, UX-003a). A state-driven approach keeps the DOM structure stable (no re-rendering on mode switch), preserves event listeners, and is straightforward to implement in vanilla TypeScript. The pattern mirrors how the Home page manages form state (module-level variables + DOM manipulation).

**Alternatives considered**:
- **Separate create and edit forms**: Violates FR-012 ("reuse the creation form for editing") and duplicates HTML/CSS/validation logic.
- **Re-render entire form on mode switch**: Works but unnecessary DOM churn. The form structure is identical in both modes — only the button label, cancel visibility, and field values change.

## R6: E2E Test Strategy for API-Backed CRUD Operations

**Decision**: Extend the existing `WebAppFixture` with in-memory mock API endpoints for exercises and muscles, following the same pattern as the existing `/api/workout-types` mock. Tests interact with mock endpoints that maintain an in-memory list, enabling create, read, and update operations within a test run.

**Rationale**: The existing test infrastructure uses `WebAppFixture` with mock endpoints — no real database in tests. This keeps tests fast, deterministic, and free of external dependencies (no Docker/PostgreSQL needed for test runs). The mock endpoints maintain a `List<MockExercise>` that starts empty per fixture instance, allowing tests to verify the full create → list → edit flow. Muscle data is returned from a static predefined list.

**Alternatives considered**:
- **Real database with test containers**: Provides higher fidelity but adds Docker as a test dependency, slows test execution significantly, and diverges from the established test pattern. The E2E tests are primarily validating frontend behaviour and API contracts, not database query correctness.
- **Mocking at the TypeScript level**: Would require a different testing approach (e.g., service workers, fetch interception). The current Playwright + real HTTP server approach is more realistic and already working.

## R7: Exercise Name Max Length and Whitespace Handling

**Decision**: Enforce a 150-character maximum length via EF Core `HasMaxLength(150)` (maps to `varchar(150)` in PostgreSQL) and validate on both the frontend (input `maxlength` attribute + TypeScript check) and backend (API validates before saving). Whitespace is trimmed on the backend before validation and storage; the frontend trims on submit.

**Rationale**: Defence in depth — the HTML `maxlength` attribute prevents most users from exceeding the limit, the TypeScript validation catches programmatic manipulation, and the database column constraint is the final safety net. Trimming whitespace before validation ensures that "  " (only spaces) is treated as empty (FR-002) and that "  Bench Press  " is stored as "Bench Press" for consistent duplicate checking.

**Alternatives considered**:
- **Frontend-only validation**: Bypassable via browser dev tools or direct API calls. Unacceptable given the constitution's security principle requiring input validation.
- **Database-only constraint**: Would surface as a raw PostgreSQL error (string truncation), not a user-friendly message. Validation must happen at the API level to return structured error responses.
