# Tasks: Home Landing Page

**Input**: Design documents from `/specs/001-home-landing-page/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Automated tests are REQUIRED for every user story and every bug fix. Include the appropriate unit, integration, or end-to-end coverage needed to prove behavior before implementation is complete.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story, with explicit work for security, user experience consistency, and performance verification where applicable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g. `[US1]`, `[US2]`, `[US3]`)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Aspire solution layout, frontend toolchain, and test project skeleton under `src/`.

- [x] T001 Create the solution file and `src/` project folders in `src/WorkoutTracker.slnx`, `src/WorkoutTracker.AppHost/`, `src/WorkoutTracker.ServiceDefaults/`, `src/WorkoutTracker.Api/`, `src/WorkoutTracker.Web/`, and `src/WorkoutTracker.Tests/`
- [x] T002 Create Aspire project definitions in `src/WorkoutTracker.AppHost/WorkoutTracker.AppHost.csproj`, `src/WorkoutTracker.ServiceDefaults/WorkoutTracker.ServiceDefaults.csproj`, and `src/WorkoutTracker.Api/WorkoutTracker.Api.csproj`
- [x] T003 [P] Create the web host project and static asset folders in `src/WorkoutTracker.Web/WorkoutTracker.Web.csproj`, `src/WorkoutTracker.Web/Program.cs`, and `src/WorkoutTracker.Web/wwwroot/`
- [x] T004 [P] Configure TypeScript compilation in `src/WorkoutTracker.Web/package.json` and `src/WorkoutTracker.Web/tsconfig.json`
- [x] T005 [P] Configure shared .NET code style and build settings in `.editorconfig` and `src/Directory.Build.props`
- [x] T006 [P] Create the test project and Playwright package references in `src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared hosting, frontend bootstrapping, and test infrastructure that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T007 Wire Aspire orchestration for the web and API projects in `src/WorkoutTracker.AppHost/Program.cs`
- [x] T008 [P] Add shared ASP.NET service defaults in `src/WorkoutTracker.ServiceDefaults/Extensions.cs`
- [x] T009 [P] Configure static file hosting and `/` fallback routing in `src/WorkoutTracker.Web/Program.cs`
- [x] T010 [P] Add baseline page shell and script/style references in `src/WorkoutTracker.Web/wwwroot/index.html`
- [x] T011 [P] Add shared CSS tokens, layout primitives, and error-state classes in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T012 [P] Add frontend bootstrap and fixed workout-type constants in `src/WorkoutTracker.Web/wwwroot/ts/main.ts`
- [x] T013 [P] Create Playwright web app fixture and browser lifecycle helpers in `src/WorkoutTracker.Tests/Infrastructure/WebAppFixture.cs` and `src/WorkoutTracker.Tests/Infrastructure/PlaywrightFixture.cs`
- [x] T014 Configure test host settings and base URL resolution in `src/WorkoutTracker.Tests/appsettings.json` and `src/WorkoutTracker.Tests/Infrastructure/TestSettings.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - Select Workout and Start (Priority: P1) 🎯 MVP

**Goal**: Let the user see the application title, choose Push/Pull/Legs, and press `Start Workout` without an error when a valid option is selected.

**Independent Test**: Load the home page, select each workout option in turn, press `Start Workout`, and confirm the selection is accepted without showing the validation error.

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T015 [P] [US1] Add Playwright happy-path tests for Push, Pull, and Legs selection in `src/WorkoutTracker.Tests/E2E/HomeLandingPageSelectionTests.cs`
- [x] T016 [P] [US1] Add unit tests for workout option configuration and selection state in `src/WorkoutTracker.Tests/Unit/HomeLandingPageSelectionStateTests.cs`

### Implementation for User Story 1

- [x] T017 [US1] Add the `Workout Tracker` heading, workout dropdown, placeholder option, and `Start Workout` button in `src/WorkoutTracker.Web/wwwroot/index.html`
- [x] T018 [US1] Implement workout option rendering and selected-workout state management in `src/WorkoutTracker.Web/wwwroot/ts/main.ts`
- [x] T019 [US1] Load the compiled frontend script and preserve no-op valid submit behavior in `src/WorkoutTracker.Web/wwwroot/index.html` and `src/WorkoutTracker.Web/package.json`
- [x] T020 [US1] Keep the selection UI accessible with labels and keyboard support in `src/WorkoutTracker.Web/wwwroot/index.html` and `src/WorkoutTracker.Web/wwwroot/css/styles.css`

**Checkpoint**: User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Validation When No Workout Selected (Priority: P1)

**Goal**: Show the exact message `Please select a workout` when the user presses `Start Workout` without making a selection, and clear the error after a valid retry.

**Independent Test**: Load the home page, press `Start Workout` with the placeholder still selected, confirm the exact error appears once, then select a workout and confirm the error clears on the next click.

### Tests for User Story 2 ⚠️

- [x] T021 [P] [US2] Add Playwright validation-flow coverage for missing selection and error clearing in `src/WorkoutTracker.Tests/E2E/HomeLandingPageValidationTests.cs`
- [x] T022 [P] [US2] Add unit tests for empty-selection validation and error reset logic in `src/WorkoutTracker.Tests/Unit/HomeLandingPageValidationTests.cs`

### Implementation for User Story 2

- [x] T023 [US2] Add an inline validation message region near the workout controls in `src/WorkoutTracker.Web/wwwroot/index.html`
- [x] T024 [US2] Implement empty-selection validation, single-error rendering, and error clearing in `src/WorkoutTracker.Web/wwwroot/ts/main.ts`
- [x] T025 [US2] Style the validation state for visibility and consistency in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T026 [US2] Guard the client-side handler against invalid or unexpected workout values in `src/WorkoutTracker.Web/wwwroot/ts/main.ts`

**Checkpoint**: User Stories 1 and 2 should both work independently on the same page.

---

## Phase 5: User Story 3 - Responsive Layout (Priority: P2)

**Goal**: Make the landing page mobile-first while keeping it readable and usable on desktop viewports.

**Independent Test**: Load the home page at 375 px and 1024 px widths, confirm there is no horizontal overflow, controls remain visible, and touch targets are at least 44 × 44 points.

### Tests for User Story 3 ⚠️

- [x] T027 [P] [US3] Add Playwright viewport tests for mobile and desktop layout behavior in `src/WorkoutTracker.Tests/E2E/HomeLandingPageResponsiveTests.cs`
- [x] T028 [P] [US3] Add automated checks for touch-target sizing and overflow behavior in `src/WorkoutTracker.Tests/E2E/HomeLandingPageAccessibilityTests.cs`

### Implementation for User Story 3

- [x] T029 [US3] Implement the mobile-first stacked layout and desktop width constraints in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T030 [US3] Tune spacing, typography, and control sizing for touch interaction in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T031 [US3] Ensure the HTML structure supports responsive behavior without layout shifts in `src/WorkoutTracker.Web/wwwroot/index.html`
- [x] T032 [US3] Add a lightweight performance audit script for the home page in `src/WorkoutTracker.Web/package.json` and `src/WorkoutTracker.Tests/E2E/HomeLandingPagePerformanceTests.cs`

**Checkpoint**: All user stories should now be independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, documentation alignment, and cross-story quality gates.

- [x] T033 [P] Update the feature runbook and `src/` paths in `specs/001-home-landing-page/quickstart.md`
- [x] T034 [P] Add regression coverage for repeated invalid clicks and placeholder reselection in `src/WorkoutTracker.Tests/E2E/HomeLandingPageRegressionTests.cs`
- [x] T035 Run the quickstart validation steps and document any fixes in `specs/001-home-landing-page/quickstart.md`
- [x] T036 Verify formatting, test, and performance commands in `src/WorkoutTracker.Web/package.json`, `src/WorkoutTracker.Tests/WorkoutTracker.Tests.csproj`, and `src/Directory.Build.props`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories.
- **User Stories (Phase 3+)**: Depend on Foundational completion.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts immediately after Foundational and delivers the MVP.
- **User Story 2 (P1)**: Builds on the same page shell as US1 and should follow once US1 markup and handlers exist.
- **User Story 3 (P2)**: Builds on the shared page markup from US1/US2 and can start once the core controls and validation UI are in place.

### Within Each User Story

- Tests MUST be written and fail before implementation.
- Markup and state wiring come before polish on styles.
- Core interaction logic comes before regression and performance verification.
- Each story must pass its independent test before moving on.

### Parallel Opportunities

- T003-T006 can run in parallel after T001-T002 create the solution skeleton.
- T008-T013 can run in parallel once the project files exist.
- T015 and T016 can run in parallel for US1.
- T021 and T022 can run in parallel for US2.
- T027 and T028 can run in parallel for US3.
- T033 and T034 can run in parallel during Polish.

---

## Parallel Example: User Story 1

```bash
# Launch the US1 tests together:
Task: "Add Playwright happy-path tests in src/WorkoutTracker.Tests/E2E/HomeLandingPageSelectionTests.cs"
Task: "Add unit tests for selection state in src/WorkoutTracker.Tests/Unit/HomeLandingPageSelectionStateTests.cs"

# Then implement the UI and behavior:
Task: "Add heading, dropdown, and button in src/WorkoutTracker.Web/wwwroot/index.html"
Task: "Implement workout option state in src/WorkoutTracker.Web/wwwroot/ts/main.ts"
```

## Parallel Example: User Story 2

```bash
# Launch the US2 tests together:
Task: "Add validation-flow Playwright tests in src/WorkoutTracker.Tests/E2E/HomeLandingPageValidationTests.cs"
Task: "Add validation unit tests in src/WorkoutTracker.Tests/Unit/HomeLandingPageValidationTests.cs"

# Then implement the validation UI and logic:
Task: "Add validation message region in src/WorkoutTracker.Web/wwwroot/index.html"
Task: "Implement validation and error clearing in src/WorkoutTracker.Web/wwwroot/ts/main.ts"
```

## Parallel Example: User Story 3

```bash
# Launch the US3 verification tasks together:
Task: "Add responsive layout tests in src/WorkoutTracker.Tests/E2E/HomeLandingPageResponsiveTests.cs"
Task: "Add touch-target and overflow checks in src/WorkoutTracker.Tests/E2E/HomeLandingPageAccessibilityTests.cs"

# Then implement responsive behavior:
Task: "Implement mobile-first layout rules in src/WorkoutTracker.Web/wwwroot/css/styles.css"
Task: "Tune responsive HTML structure in src/WorkoutTracker.Web/wwwroot/index.html"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Run the US1 independent test before proceeding.

### Incremental Delivery

1. Finish Setup + Foundational to establish the Aspire, static web, and test baseline.
2. Deliver US1 as the first usable landing page.
3. Add US2 to enforce validation and user feedback.
4. Add US3 to make the page responsive and performance-aware.
5. Finish Polish tasks to validate docs, regressions, and quality gates.

### Parallel Team Strategy

1. One engineer completes Setup and Foundational.
2. Then one engineer focuses on US2 validation while another prepares US3 test coverage, coordinating on shared files.
3. Use Polish to reconcile shared-file changes and run the final verification pass.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks.
- [Story] labels map every story task back to the originating user story.
- Every task includes an exact file path so it is executable without extra context.
- Tests are required by the project constitution and implementation plan.
- MVP scope is Phase 3 / User Story 1 only.
