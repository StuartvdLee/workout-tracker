---
description: "Tasks for accessible exercise ordering while scrolling"
---

# Tasks: Accessible Exercise Ordering While Scrolling

**Input**: Design documents in `specs/034-accessible-exercise-ordering/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [UI contract](./contracts/ui-contract.md), [quickstart.md](./quickstart.md)

**Tests**: Browser tests are required by the plan and project constitution. The scroll regression was observed failing before the original placement change and passing afterward.

**Format**: `[ID] [P?] [Story?] Description with file path`; `[P]` marks work that can run concurrently with other ready tasks in different files.

## Phase 1: Setup

**Purpose**: Establish the existing feature and browser-test context without adding dependencies.

- [X] T001 Inspect the active-session layout, existing reorder fixture, and validation prerequisites in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`, `src/WorkoutTracker.Web/wwwroot/css/styles.css`, and `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs` using `specs/034-accessible-exercise-ordering/quickstart.md`

## Phase 2: Foundational

**Purpose**: Prepare the shared fixture required for long-workout verification.

- [X] T002 Extend the existing API fixture in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs` to create and open a workout with 50 uniquely named exercises, asserting successful seed responses

**Checkpoint**: The long-workout fixture is ready for story tests; no production API or schema changes are needed.

## Phase 3: User Story 1 - Enter order editing from anywhere in a workout (Priority: P1)

**Goal**: Keep the existing order action available while scrolling, with a desktop rail and a mobile overlay that leaves workout controls reachable.

**Independent Test**: Open a 50-exercise workout, scroll to the top, middle and bottom, and activate the visible control at each position without a return to the top.

### Tests

- [X] T003 [P] [US1] Add a real-browser regression in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs` that checks the control's unobscured viewport bounds, center hit-test and desktop clearance from visible inputs/actions at 0%, 50%, and 100% scroll, then clicks measured coordinates without auto-scroll and verifies the editor opens; run it before implementation and record its expected failure

### Implementation

- [X] T004 [P] [US1] Update the workout logging feature description in `README.md` to explain that exercise ordering is accessible while scrolling
- [X] T005 [US1] In `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`, move the existing `#session-edit-order` button into a full-height sibling rail beside the workout header, entries, errors and actions, keeping modal backdrops outside the layout and preserving the button listener and availability logic
- [X] T006 [US1] In `src/WorkoutTracker.Web/wwwroot/css/styles.css`, reserve a desktop rail beside a flexible content column and make the button sticky; on mobile float it below the top bar over full-width content, preserving workout control access and modal/navigation stacking

**Checkpoint**: The scroll regression passes, including at the page bottom and on short workouts, without modifying reorder behavior.

## Phase 4: User Story 2 - Recognize and access the order control (Priority: P1)

**Goal**: Replace the label with a recognizable accessible icon usable across supported inputs and themes.

**Independent Test**: Identify and activate the control using pointer, touch, keyboard and screen reader, including narrow and wide layouts.

### Tests

- [ ] T007 [US2] Update the existing visible-text assertion and add browser checks for the native button role, accessible name `Edit order`, decorative SVG, no visible label, focus indication, Enter/Space activation, 44px target and viewport/theme clearance using the matrix in `specs/034-accessible-exercise-ordering/spec.md`, in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`

### Implementation

- [X] T008 [US2] Replace the button's visible text with an `aria-hidden` list-and-up/down-arrows SVG and set its accessible name to `Edit order` while retaining the existing button and ID in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`
- [X] T009 [US2] Style the icon button, 44px minimum target, theme-aware colors, hover and visible focus using existing tokens in `src/WorkoutTracker.Web/wwwroot/css/styles.css`; keep the control usable at narrow widths, supported zoom and with long names
- [X] T010 [US2] Verify keyboard focus remains usable after the now-hidden button opens the editor and, only if needed, move focus to an appropriate existing editor control in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`

**Checkpoint**: The icon action is identifiable and operable, with workout controls reachable at supported mobile and desktop sizes.

## Phase 5: User Story 3 - Preserve the existing editing flow (Priority: P1)

**Goal**: Preserve reorder, Done, Cancel, modal and exercise-value behavior when the action is reached from a scrolled position.

**Independent Test**: From a scrolled position, reorder and select Done; separately cancel unchanged and changed orders, both confirming and declining the discard prompt.

### Tests and integration

- [X] T011 [US3] Extend `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs` to enter from a scrolled position, assert name-only editing, reorder with existing drag/keyboard interactions, select Done, and verify sequence plus weight/effort association with no session POST on entry or exit
- [X] T012 [US3] Extend `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs` to cover unchanged Cancel, changed Cancel with both continue and discard, action hidden during loading, blocking workout-load failure, empty and editing states, action available for one exercise and after non-blocking previous-performance or session-save failures, and modal focus/layering over the control
- [X] T013 [US3] Preserve or correct any regressions revealed by these tests in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` and `src/WorkoutTracker.Web/wwwroot/css/styles.css` without replacing the existing sortable helper, persistence behavior or data association

**Checkpoint**: Existing ordering and cancellation behavior works after entering from a scrolled position, with workout values intact.

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T014 [P] Review `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts` and `src/WorkoutTracker.Web/wwwroot/css/styles.css` for unchanged permissions, data handling, safe rendering, zero new network requests and zero scroll listeners; record any change-caused security or integrity regression for resolution before release
- [ ] T015 [P] Run and document the viewport/zoom matrix, light/dark, touch, keyboard, screen-reader, long-name and modal checks described in `specs/034-accessible-exercise-ordering/quickstart.md`; report any unavailable environments rather than claiming they passed
- [ ] T016 Verify performance for a 50-exercise workout using the method in `specs/034-accessible-exercise-ordering/plan.md`: record 20 scroll samples at evenly spaced offsets with the order control visible and hit-testable at each and at least 19 samples within 100 ms, manually inspect continuous scrolling for flicker, and record 20 entry and 20 exit samples with at least 19 of each within one second
- [ ] T017 Conduct the discoverability check in `specs/034-accessible-exercise-ordering/plan.md`, recording participant count, method and results against the 90% within 10 seconds target; explicitly report if participants are unavailable
- [X] T018 Run the focused `WorkoutReorderTests` suite and frontend `npm test` and `npm run build` using `specs/034-accessible-exercise-ordering/quickstart.md`; address change-caused failures and report any blocked verification
- [X] T019 Verify the revised mobile overlay in `src/WorkoutTracker.E2ETests/E2E/WorkoutReorderTests.cs`: full content width and button contrast in both themes at 375×667, plus hit-testing representative workout controls after scrolling at 320×568

**Verification status**: The focused browser suite passed 30 cases after the mobile change; frontend tests passed 96 cases and the TypeScript build passed. Scroll geometry was checked in both themes at 320×568, 375×667, 768×768 and 1024×768; 640×480 was checked in light theme only and is an approximation, not an actual 200% zoom test. Keyboard activation and focus were checked at the default desktop viewport, and touch was simulated at the bottom of a mobile workout. Mobile full width and button contrast were checked at 375×667 in both themes; representative input, slider and footer control centers were hit-tested after scrolling at 320×568.

T007 and T015 remain open for the full verification matrix, including actual 200% zoom, dark theme at the zoom-equivalent viewport, real screen-reader and touch-device checks, and manual visual checks. T016's automated 20-position and entry/exit proxy budgets passed at 1024×768; the session artifact `ordering-performance.json` records a sample run before the mobile-only styling revision, and the focused suite passed again afterward. The manual continuous-scroll flicker check remains. T017 requires human participants and has not been conducted.

## Dependencies & Execution Order

### Phase and story dependencies

- Setup T001 precedes foundational fixture T002.
- US1 T003 precedes T005 and T006; T005 establishes markup for T006. T004 is independent of T003–T006 after the foundation.
- US2 follows US1's rail and existing test-file changes: T007 precedes T008 and T009; T010 follows T008.
- US3 follows the US1 and US2 integration because it validates the resulting scrolled icon entry point. T011 and T012 share a test file and should be sequential; T013 follows their results.
- Polish follows the relevant story work; T014 and T015 are independent after all story work, and T016–T018 require the integrated implementation.

**Story graph**: Setup → Foundation → US1 → US2 → US3 → Polish. All three are P1; the sequence reflects shared-file and integration dependencies, not a lower business priority. Each story has its own independent test and checkpoint.

### Parallel opportunities

- **US1**: After T002, T004 (`README.md`) can run alongside T003 (`WorkoutReorderTests.cs`); T005 and T006 are sequential because the rail styles depend on the markup.
- **US2**: No safe intra-story parallel edit is identified: T007 establishes the tests before T008/T009, and T008/T010 both modify the same TypeScript file.
- **US3**: No safe intra-story parallel edit is identified: T011 and T012 modify the same test file, and T013 responds to their results.
- **Polish**: T014 source review and T015 manual UX checks can run concurrently after the integrated stories; avoid modifying the same files concurrently if T014 discovers a defect.

## Implementation Strategy

### MVP first

Complete setup, the fixture and US1, then verify the scroll regression independently. This is the smallest demonstrable scroll-access increment; the complete issue also requires US2's icon/accessibility work and US3's regression guarantees before release.

### Incremental delivery

1. Establish the fixture and prove the scroll defect with a failing browser regression.
2. Deliver the desktop sticky rail and mobile floating button and verify US1.
3. Add the named icon and input/accessibility coverage for US2.
4. Verify the unchanged editing and cancellation journeys for US3.
5. Complete security, UX, measurable performance, discoverability and command-line validation before release.
