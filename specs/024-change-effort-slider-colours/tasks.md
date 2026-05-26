---

description: "Task list for implementing effort slider colour changes"
---

# Tasks: Change Effort Slider Colours

**Input**: Design documents from `/specs/024-change-effort-slider-colours/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: Automated tests are required for every story: frontend unit/build checks plus Playwright E2E coverage for changed user journeys.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`)
- Every task includes an explicit file path

## Phase 1: Setup (Shared Context)

**Purpose**: Align implementation and validation scope before coding.

- [x] T001 Confirm finalized palette and acceptance criteria in `specs/024-change-effort-slider-colours/spec.md`.
- [x] T002 Confirm testing and quality gates in `specs/024-change-effort-slider-colours/plan.md`.
- [x] T003 Confirm slider behavior contract and fallback rules in `specs/024-change-effort-slider-colours/contracts/ui-contract.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared mapping, baseline tests, and common styling used by all stories.

**⚠️ CRITICAL**: Complete this phase before user-story implementation.

- [x] T004 Update canonical 1-10 effort colour mapping in `src/WorkoutTracker.Web/wwwroot/ts/utils.ts`.
- [x] T005 [P] Add/update deterministic palette mapping tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts`.
- [x] T006 [P] Ensure slider and effort label transition rules remain consistent in `src/WorkoutTracker.Web/wwwroot/css/styles.css`.
- [x] T007 Run frontend validation (`npm run build && npm test`) from `src/WorkoutTracker.Web/package.json`.

**Checkpoint**: Shared mapping and frontend baseline checks are complete.

---

## Phase 3: User Story 1 - Updated Visual Feedback While Setting Effort (Priority: P1) 🎯 MVP

**Goal**: Users see the new colour immediately while changing effort values.

**Independent Test**: In active workout flows, changing slider values across 1-10 immediately shows mapped colours, with color visibility within 100 ms for at least 95% of interactions.

- [x] T008 [P] [US1] Add Playwright E2E coverage for per-exercise slider colour updates in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
- [x] T009 [P] [US1] Add Playwright E2E coverage for overall-effort slider colour updates in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
- [x] T010 [US1] Apply updated mapping to per-exercise slider interaction handlers in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [x] T011 [US1] Apply updated mapping to overall-effort slider interaction handlers in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [x] T012 [US1] Keep slider value and effort-band colour updates synchronized with value changes in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [x] T013 [US1] Run focused US1 regression in `src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj` and frontend checks in `src/WorkoutTracker.Web/package.json`.

**Checkpoint**: User Story 1 is independently functional and E2E-validated.

---

## Phase 4: User Story 2 - Consistent Colours Across All Effort Sliders (Priority: P2)

**Goal**: Equal effort values render identical colours across all in-scope slider surfaces.

**Independent Test**: Setting the same effort value on per-exercise and overall sliders yields the same visible colour output.

- [x] T014 [P] [US2] Add Playwright E2E consistency test across slider surfaces in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
- [x] T015 [US2] Ensure all in-scope slider paths use the shared mapping utility in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [x] T016 [US2] Remove/avoid any divergent slider color overrides in `src/WorkoutTracker.Web/wwwroot/css/styles.css`.
- [x] T017 [US2] Add/update consistency-focused utility tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts`.
- [x] T018 [US2] Run focused US2 regression in `src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj` and frontend checks in `src/WorkoutTracker.Web/package.json`.

**Checkpoint**: User Stories 1 and 2 are independently functional and consistent.

---

## Phase 5: User Story 3 - Clear Fallback for Unset or Invalid Values (Priority: P3)

**Goal**: Unset/invalid effort values render neutral fallback style without breaking interaction flow.

**Independent Test**: Untouched/reset sliders render neutral state, and invalid/unset values do not show mapped effort colours.

- [x] T019 [P] [US3] Add Playwright E2E coverage for untouched/reset neutral slider state in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
- [x] T020 [US3] Ensure untouched/reset and restored-value fallback paths are correctly handled in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [x] T021 [US3] Add/update invalid/unset fallback tests in `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts`.
- [x] T022 [US3] Run focused US3 regression in `src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj` and frontend checks in `src/WorkoutTracker.Web/package.json`.

**Checkpoint**: All three user stories are independently functional and covered.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final constitution-required verification across stories.

- [x] T023 [P] Add a measurable SC-002 verification protocol (95% <= 100 ms) to `specs/024-change-effort-slider-colours/quickstart.md`.
- [x] T024 [P] Implement/extend timing-focused E2E assertion(s) for SC-002 in `src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs`.
- [x] T025 Run full frontend validation in `src/WorkoutTracker.Web/package.json` and full E2E validation in `src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`.
- [x] T026 Record verification outcomes and final notes in `specs/024-change-effort-slider-colours/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no dependencies.
- **Phase 2 (Foundational)**: depends on Phase 1; blocks all user stories.
- **Phase 3 (US1)**: depends on Phase 2; MVP.
- **Phase 4 (US2)**: depends on Phase 2 and US1 mapping behavior.
- **Phase 5 (US3)**: depends on Phase 2 and established slider state behavior.
- **Phase 6 (Polish)**: depends on completion of US1-US3.

### User Story Dependency Graph

- **US1 (P1)** → independent after Foundational
- **US2 (P2)** → depends on shared mapping and US1 behavior parity
- **US3 (P3)** → depends on shared mapping and slider state handling

### Within-Story Order

- Story-specific E2E tests first, then implementation, then story-level validation runs.

---

## Parallel Execution Examples

### User Story 1

```bash
Task T008 in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T010 in src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts
```

### User Story 2

```bash
Task T014 in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T017 in src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts
```

### User Story 3

```bash
Task T019 in src/WorkoutTracker.E2ETests/E2E/WorkoutHistoryTests.cs
Task T021 in src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Validate with US1-focused frontend + E2E coverage.

### Incremental Delivery

1. Deliver US1 (immediate feedback + timing coverage baseline).
2. Deliver US2 (cross-surface consistency).
3. Deliver US3 (fallback resilience).
4. Complete final performance/security/UX verification tasks.
