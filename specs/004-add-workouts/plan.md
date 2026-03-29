# Implementation Plan: Add Workouts

**Branch**: `004-add-workouts` | **Date**: 2026-03-29 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-add-workouts/spec.md`

## Summary

Replace the Workouts placeholder page with a fully functional workout management system. Users can create, view, edit, and delete planned workout templates—reusable collections of exercises with optional target rep/weight ranges. Users can also log completed workout sessions from these templates, capturing actual performance data, and view their workout history. The backend provides REST API endpoints for CRUD operations on planned workouts, workout sessions, and exercise logging. The frontend implements three main views: Workouts page (create/manage templates), Active Session view (logging interface), and History page (past workouts). All data persists in PostgreSQL via Entity Framework Core through new PlannedWorkout, WorkoutSession, WorkoutExercise, and LoggedExercise entities with proper relationships and constraints.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend)  
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)  
**Storage**: PostgreSQL via Entity Framework Core — extending existing schema with PlannedWorkout, WorkoutSession, WorkoutExercise, and LoggedExercise entities  
**Testing**: xUnit 3.2.2 + Microsoft Playwright 1.58.0 for E2E tests; mock API endpoints in WebAppFixture  
**Target Platform**: Web browser (mobile-first, responsive to desktop)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: Workouts page loads and displays list within 3 seconds on slow 3G (PR-001); History page loads within 3 seconds even with 100+ sessions (PR-003); visual feedback within 200ms of form submission (PR-002)  
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing CSS custom properties must be extended, not replaced; existing tests must continue to pass  
**Scale/Scope**: Single-user personal tracker; 3 pages reworked (Workouts, History, and Active Session), 15+ new API endpoints for workouts/sessions, 2 new EF migrations, ~250+ E2E test cases

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced via `tsconfig.json` (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`). CSS follows BEM naming convention established in `styles.css`. C# uses .NET nullable reference types (`<Nullable>enable</Nullable>`) and snake_case DB naming convention via `UseSnakeCaseNamingConvention()`. All new code must follow these established patterns. ✅ No deviations required.

- **Testing**: Playwright E2E tests are mandatory and must cover all seven user stories (P1-P4):
  - **P1**: Create planned workout with name; add exercises to planned workout; validation for required name, max length, duplicates, at least one exercise
  - **P2**: View planned workouts list with empty state
  - **P3**: Edit planned workout via modal with validation; Log completed workout
  - **P4**: Delete planned workout with confirmation; View workout history with empty state
  - Tests must cover form states (default, loading, success, error, edit), modal interactions (open, close, Escape, backdrop), validations, empty states, ARIA attributes, mobile responsiveness, and performance timing assertions. The existing `WebAppFixture` mock API pattern is extended with workout, session, and exercise logging endpoints. ✅ Tests are mandatory, not optional.

- **Security**: 
  - Planned workout names and exercise targets validated on both client (TypeScript) and server (API): trimmed, checked for emptiness, capped at 150 characters, checked for case-insensitive uniqueness
  - Exercise and workout IDs validated against known sets (whitelist) before use
  - Logged reps/weight values validated as numeric or empty (prevent injection attacks, SQLi, XSS)
  - No authentication required for single-user app, but architecture must support future multi-user with per-user data filtering
  - No secrets introduced; no third-party dependencies added
  - Delete operations properly cascade: deleting PlannedWorkout does NOT delete linked WorkoutSessions (history preserved)
  - ✅ Input validation on both tiers; data integrity maintained; no new trust boundaries

- **User Experience Consistency**: 
  - Workouts and History pages follow same layout patterns as Exercises page: sidebar navigation, content area, mobile-responsive design
  - Creation form matches Exercise form patterns: BEM CSS classes, inline error messages with `role="alert"` and `aria-live="polite"`, `novalidate` with custom validation
  - Edit uses modal dialog (same pattern as Exercise edit) with ARIA (role="dialog", aria-modal="true", focus trapping)
  - Delete uses confirmation dialog with role="alertdialog", red Delete button, blue/white Cancel button
  - Empty states follow placeholder pattern
  - Touch targets meet `--min-touch-target` (44px)
  - Active workout session view displays exercise targets and input fields for logging, with clear loading/error states
  - History view displays workouts in reverse chronological order with date indicators ("Today", "Yesterday", "N days ago")
  - ✅ All existing patterns followed; new History view pattern introduced with date grouping

- **Performance**: 
  - Workouts page load budget: 3 seconds on slow 3G (consistent with Home/Exercises pages) — verified via Playwright timing assertions
  - History page load budget: 3 seconds on slow 3G even with 100+ sessions — pagination or lazy loading may be needed
  - Form submission feedback: loading state visible within 200ms — verified via Playwright
  - API calls parallelised on page load where possible (get workouts + exercises in parallel)
  - Active session view renders synchronously after data fetch (no pagination needed initially)
  - ✅ Budgets defined; verification via Playwright timing assertions; pagination strategy deferred to Phase 2 if needed

## Project Structure

### Documentation (this feature)

```text
specs/004-add-workouts/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── api-contract.md  # REST API endpoints
│   └── ui-contract.md   # HTML/CSS/ARIA contracts
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/WorkoutTracker.Infrastructure/
├── Data/
│   ├── Models/
│   │   ├── PlannedWorkout.cs          # NEW: workout template entity
│   │   ├── WorkoutSession.cs          # NEW: completed workout session
│   │   ├── WorkoutExercise.cs         # NEW: junction table (Exercise ↔ PlannedWorkout)
│   │   ├── LoggedExercise.cs          # NEW: junction table (Exercise ↔ WorkoutSession)
│   │   └── Exercise.cs                # MODIFIED: add navigation to WorkoutExercise
│   ├── Migrations/
│   │   ├── [timestamp]_AddWorkoutTemplateAndSessionEntities.cs  # NEW: migration 1
│   │   └── [timestamp]_AddWorkoutSessionLoggingTables.cs        # NEW: migration 2
│   └── WorkoutTrackerDbContext.cs     # MODIFIED: add DbSets, entity config, relationships

src/WorkoutTracker.Api/
└── Program.cs                         # MODIFIED: add 15+ workout/session/logging API endpoints

src/WorkoutTracker.Web/
├── wwwroot/
│   ├── css/
│   │   └── styles.css                 # MODIFIED: add workout form, list, modal, session, history styles
│   └── ts/
│       ├── pages/
│       │   ├── workouts.ts            # MODIFIED: replace placeholder with full workout management
│       │   ├── active-session.ts      # NEW: active workout logging interface
│       │   └── history.ts             # MODIFIED: replace placeholder with full history view
│       └── api/
│           └── workout-client.ts      # NEW: typed API client for workout endpoints

src/WorkoutTracker.Tests/
├── E2E/
│   ├── WorkoutsPageTests.cs           # MODIFIED: replace placeholder tests with full coverage (P1-P2)
│   ├── WorkoutsEditDeleteTests.cs     # NEW: edit and delete tests (P3-P4)
│   ├── WorkoutSessionTests.cs         # NEW: active session logging and save (P3)
│   ├── WorkoutHistoryTests.cs         # NEW: history view and empty states (P4)
│   └── WorkoutValidationTests.cs      # NEW: comprehensive validation and edge cases
└── Infrastructure/
    └── WebAppFixture.cs               # MODIFIED: add mock workout, session, and logging endpoints
```

**Structure Decision**: The existing .NET Aspire solution structure is preserved. New entities are added to the Infrastructure project's Models directory following the established pattern. The API endpoints are added to the existing `Program.cs` minimal API file. The workouts, history, and active-session pages are implemented as new modules in the TypeScript pages directory. Frontend code follows the vanilla TypeScript pattern with no bundler — ES2022 modules output by `tsconfig.json`. No new projects or structural changes needed beyond the scope of 003-add-exercises.

## Implementation Phases

### Phase 0: Research & Clarification

**Output**: `research.md` with all design decisions documented

1. **Research Tasks**:
   - EF Core relationship patterns for junction tables with optional fields (target reps, logged reps) — best practices for one-to-many-to-many with metadata
   - TypeScript patterns for complex form state (multi-step, exercise selection, validation with dynamic fields)
   - History pagination strategies for large datasets (100+ sessions) — trade-offs between infinite scroll, cursor-based pagination, time-based grouping
   - Modal dialog accessibility patterns in vanilla JavaScript — focus trapping, keyboard events, backdrop interactions

2. **Design Decisions to Document**:
   - How to represent "target reps" range — store as string "8-12" or separate min/max integers? (Choose based on validation simplicity and query patterns)
   - History view grouping strategy — by day, week, or flat list? (Spec suggests date indicators: "Today", "Yesterday", "3 days ago")
   - Workflow for exercise removal from library while still linked to workouts — preserve as "unavailable exercise" or remove from workout UI?
   - Active session state management — client-side state with unsaved changes detection or persist-as-you-go?

### Phase 1: Design & Data Model

**Prerequisites**: `research.md` complete

1. **Database Layer** (`data-model.md`):
   - Define four entities: PlannedWorkout, WorkoutSession, WorkoutExercise (junction), LoggedExercise (junction)
   - Document relationships: PlannedWorkout → WorkoutExercise → Exercise; WorkoutSession → LoggedExercise → Exercise
   - Optional fields: target_reps, target_weight, logged_reps, logged_weight
   - Soft-delete consideration: PlannedWorkout deletion shouldn't cascade to WorkoutSession
   - Audit fields: created_at, updated_at, deleted_at (if soft-delete used)
   - Ordering: sequence field on WorkoutExercise to maintain exercise order within workout

2. **API Contract** (`contracts/api-contract.md`):
   - **Planned Workouts**: POST, GET (list), GET (by ID), PUT, DELETE
   - **Workout Sessions**: POST (start session), GET (history list), GET (by ID), PUT (update logged data during session)
   - **Logged Exercise Data**: POST (log exercise during session), GET, DELETE (remove logged exercise)
   - Error responses: validation errors (400), not found (404), conflict on duplicate name (409)
   - Request/response DTOs with validation rules

3. **UI Contract** (`contracts/ui-contract.md`):
   - Workouts page layout: create form + workout list + edit modal + delete confirmation
   - Active session view: exercise list with input fields for reps/weight + save/cancel buttons
   - History page: session list with reverse chronological order + date grouping + expand/collapse detail view
   - Form states: default, loading, success, error, edit mode
   - Modal states: open, closing, focus management
   - Empty states for all three pages

4. **Quickstart Guide** (`quickstart.md`):
   - Step-by-step walkthrough for creating a planned workout, adding exercises, starting a session, logging performance, saving session, and viewing history

5. **Agent Context Update**:
   - Run `.specify/scripts/bash/update-agent-context.sh copilot`
   - Add new technologies to agent-specific context: PlannedWorkout/WorkoutSession entity patterns, JWT token management if multi-user support added in future

**Output**: data-model.md, api-contract.md, ui-contract.md, quickstart.md, updated agent context

### Phase 2: Implementation (Dependency Graph)

**Prerequisites**: Phase 1 complete; all design decisions documented

**Parallel Workstreams** (can proceed independently):

**Workstream A: Database Layer**
- Create EF Core entities (PlannedWorkout, WorkoutSession, WorkoutExercise, LoggedExercise)
- Configure relationships, constraints, shadow properties for audit timestamps
- Create two migrations: (1) workout template entities, (2) session logging entities
- Seed data: none required; relies on user-created exercises from 003-add-exercises

**Workstream B: API Endpoints**
- Depends on: Workstream A entities created
- Planned Workouts CRUD: CreatePlannedWorkout, ListPlannedWorkouts, GetPlannedWorkout, UpdatePlannedWorkout, DeletePlannedWorkout
- Workout Sessions: StartWorkoutSession, GetWorkoutSession, ListWorkoutHistory (with pagination), SaveWorkoutSession
- Logged Exercises: LogExercisePerformance, GetLoggedExercise, UpdateLoggedExercise
- Validation: name uniqueness, exercise duplication, numeric validation for reps/weight
- Mock endpoints in WebAppFixture for E2E tests

**Workstream C: Frontend - Workouts Page**
- Depends on: Workstream B endpoints available
- Create form: workout name input + exercise selection UI + submit button
- Workout list: display all planned workouts with exercise counts, edit/delete/start buttons
- Edit modal: pre-populated with workout data, exercise management
- Delete confirmation: modal with red Delete / blue Cancel buttons
- Validation messaging: inline error display for name, exercises, duplicates
- Styles: extend styles.css with form, list, modal, button styles following BEM

**Workstream D: Frontend - Active Session & History**
- Depends on: Workstream B endpoints available; Workstream C completion not required but good for context
- Active session view: exercise list from template, input fields for actual reps/weight, save/cancel buttons
- History page: display completed sessions in reverse chronological order with date grouping
- Detail view: expand session to see full logged data
- Empty states for both pages

**Workstream E: E2E Testing**
- Depends on: All frontend views complete (Workstreams C & D)
- Test suite structure:
  - WorkoutsPageTests.cs: P1 (create, add exercises) + P2 (view list)
  - WorkoutsEditDeleteTests.cs: P3 (edit) + P4 (delete)
  - WorkoutSessionTests.cs: P3 (log, save)
  - WorkoutHistoryTests.cs: P4 (view history)
  - WorkoutValidationTests.cs: validation edge cases (empty name, duplicates, max length, no exercises, numeric validation)
- Test coverage: form states, modal interactions, validations, empty states, ARIA attributes, mobile responsiveness, performance timing
- WebAppFixture mock endpoints extended to support all workout operations

**Dependency Ordering**:
1. Phase 1 design complete (sequential)
2. Workstreams A & B can proceed in parallel (after Phase 1)
3. Workstreams C & D can proceed in parallel (after B)
4. Workstream E begins after C & D (needs UI to test)

### Phase 3: Testing & Release

**Prerequisites**: All Phase 2 workstreams complete

1. **E2E Test Execution**: All 250+ test cases passing, performance budgets verified
2. **Regression Testing**: Existing tests from 001, 002, 003 still pass with new feature
3. **Manual Exploratory Testing**: UX consistency check, mobile responsiveness, empty/error states
4. **Performance Validation**: 3-second load budget verified on slow 3G for Workouts and History pages
5. **Accessibility Audit**: ARIA attributes, keyboard navigation, focus management, screen reader compatibility
6. **Security Review**: Input validation, XSS prevention, CSRF handling (if applicable), no secrets in code
7. **Documentation Review**: README updated if needed; new entities and endpoints documented

## Complexity Tracking

> No constitution violations. The feature uses existing patterns and technologies throughout. The History page introduces a new date-grouping UI pattern (date indicators like "Today", "Yesterday"), which is a UX enhancement but does not violate any constitution principles.

**Notes**:
- Multi-step form complexity (exercise selection, target entry) is higher than Exercises CRUD, but within scope of single-page feature
- Modal-based editing (borrowed from Exercises feature) handles both create and edit workflows clearly
- Pagination strategy for History page deferred to Phase 2 if 100+ sessions cause performance issues; spec allows lazy loading as mitigation
- Future multi-user support will require authorization layer on API endpoints; initial implementation assumes single-user (no authz checks)
