# Data Model: Simplified Homepage Session Start

## Entity: WorkoutSession
- Fields:
  - `id` (UUID, primary key)
  - `workoutType` (enum: `Push` | `Pull` | `Legs`)
  - `startedAt` (timestamp, server-generated at creation)
  - `endedAt` (timestamp, nullable)
  - `createdAt` (timestamp)
  - `updatedAt` (timestamp)
- Relationships:
  - 1-to-many with `ExerciseEntry` (existing relationship remains)
- Validation rules:
  - `workoutType` is required for creation
  - `workoutType` must be one of `Push`, `Pull`, `Legs`
  - `startedAt` must be set automatically by the system during successful create

## Entity: WorkoutTypeSelection (UI submission model)
- Fields:
  - `workoutType` (nullable enum before submit; required on submit)
  - `validationError` (string, nullable)
- Relationships:
  - Maps to `WorkoutSession.workoutType` during session creation
- Validation rules:
  - If submit occurs with no selected value, set field-level error
  - Error clears when a valid option is chosen

## Existing Entity Impact: ExerciseEntry
- Impact:
  - No schema change required for this feature
  - Homepage no longer shows `Add Exercise Entry`, but existing entry lifecycle remains unchanged on other pages/flows

## State Transitions

### Homepage Session Start Form
- `idle` -> `validation-error` when submit without workout type
- `validation-error` -> `ready` when workout type selected
- `ready` -> `submitting` when user presses `Start Session`
- `submitting` -> `success` when API creates session
- `submitting` -> `validation-error` when API returns invalid workout type

### WorkoutSession
- `new` -> `active` when created with valid `workoutType` and server-generated `startedAt`
- `active` -> `completed` when `endedAt` is set (existing behavior)

## Data Integrity Rules
- Session creation must be atomic for `workoutType` and `startedAt` persistence.
- No session record is created when `workoutType` is missing/invalid.
- Persisted `workoutType` values remain normalized to the allowed enum set.
