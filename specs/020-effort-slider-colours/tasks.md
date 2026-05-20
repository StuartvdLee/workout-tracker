---

description: "Task list template for feature implementation"
---

# Tasks: Effort Slider Colour Feedback

**Input**: Design documents from `/specs/020-effort-slider-colours/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ contracts/ui-contract.md ✅

**Tests**: Vitest unit tests for `getEffortColour` are REQUIRED. No new E2E tests needed — colour is a visual enhancement verified at unit level.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths included in all descriptions

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Shared utility and CSS infrastructure that ALL user story phases depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Add `EFFORT_COLOURS: Record<number, string>` lookup map and `export function getEffortColour(value: number): string` to `src/WorkoutTracker.Web/wwwroot/ts/utils.ts`, returning the hex colour for values 1–10 and `""` for any other value. Mirror the existing `EFFORT_LABELS` / `getEffortLabel` pattern exactly.
- [x] T002 Add `describe('getEffortColour', ...)` block to `src/WorkoutTracker.Web/wwwroot/ts/__tests__/utils.test.ts` with 11 test cases: one per value 1–10 asserting the exact hex string, plus one out-of-range value (e.g. `0`) asserting `""`. Run `cd src/WorkoutTracker.Web && npm test` and confirm all new tests pass.
- [x] T003 Add `transition: accent-color 0.15s ease` to the `.active-session__effort-slider` rule in `src/WorkoutTracker.Web/wwwroot/css/styles.css`.
- [x] T004 Add `transition: accent-color 0.15s ease` to the `.effort-modal__slider` rule in `src/WorkoutTracker.Web/wwwroot/css/styles.css`.
- [x] T005 Add `transition: color 0.15s ease` to `.active-session__effort-value` and `.active-session__effort-band` rules in `src/WorkoutTracker.Web/wwwroot/css/styles.css`.
- [x] T006 Add `transition: color 0.15s ease` to `.effort-modal__value` and `.effort-modal__band` rules in `src/WorkoutTracker.Web/wwwroot/css/styles.css`.

**Checkpoint**: `getEffortColour` exists, all 11 unit tests pass, CSS transitions are in place.

---

## Phase 2: User Stories 1 & 2 — Real-Time Colour While Sliding + Colour on Release (Priority: P1 + P2) 🎯 MVP

**Goal**: Both effort sliders (per-exercise and overall effort modal) update colour in real time as the user drags, and show the correct colour when they release. The `input` event fires on every value change including release, so one handler update delivers both stories.

**Independent Test**: Open an active workout session. Drag a per-exercise effort slider from 1 to 10 — confirm the slider thumb and band/value text cycle through the palette colours. Open the overall effort modal and drag its slider — confirm the same behaviour. Colour must appear at each integer step without delay.

- [x] T007 Add `import { getEffortLabel, getEffortColour, applyOrder } from "../utils.js";` (add `getEffortColour` to the existing import) in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`.
- [x] T008 [US1] [US2] In the per-exercise slider `input` event handler inside `buildExerciseList()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`, after setting `effortBandEl.textContent = label`, add: `effortSlider.style.accentColor = getEffortColour(value); effortValueEl.style.color = getEffortColour(value); effortBandEl.style.color = getEffortColour(value);`
- [x] T009 [US1] [US2] In `handleEffortSliderInput()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`, after setting `bandEl.textContent = label`, resolve `valueEl` and `bandEl` and apply the same three colour assignments: `slider.style.accentColor = getEffortColour(value);` and colour the value/band elements using `getEffortColour(value)`.
- [x] T010 [US1] [US2] Run `cd src/WorkoutTracker.Web && npm run build` and confirm zero TypeScript errors. Fix any type errors before proceeding.

**Checkpoint**: Both sliders update colour in real time. The per-exercise and overall effort modal sliders both reflect the correct palette colour on every drag step. Build passes.

---

## Phase 3: User Story 3 — Colour on Page Load / State Restoration (Priority: P3)

**Goal**: When an effort value is already set (e.g. the exercise list is rebuilt mid-session with an existing effort in `logEntries`, or the slider has a prior value), the correct colour is applied immediately without requiring user interaction.

**Independent Test**: Set an effort value on a per-exercise slider, then trigger a page reload or re-render of the exercise list. Confirm the slider and its band/value text render with the correct palette colour without needing to drag the slider again.

- [x] T011 [US3] In `buildExerciseList()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`: after setting `effortSlider.value = "1"` and the initial `data-touched = "false"` state, add a conditional block — if `entry.loggedEffort !== null && entry.loggedEffort !== undefined`, set `effortSlider.value = String(entry.loggedEffort)`, set `data-touched = "true"`, set `aria-valuenow`, set `aria-valuetext`, set `effortValueEl.textContent`, set `effortBandEl.textContent`, and apply `accentColor` + text `color` from `getEffortColour(entry.loggedEffort)` — matching the full state that a user interaction would have set.
- [x] T012 [US3] In `openEffortModal()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/active-session.ts`: in the reset block (after `slider.setAttribute("data-touched", "false")`), clear the slider colour with `slider.style.accentColor = ""`. Also clear the value and band text colours: `if (valueEl) valueEl.style.color = ""; if (bandEl) bandEl.style.color = "";`
- [x] T013 [US3] Run `cd src/WorkoutTracker.Web && npm run build` and confirm zero TypeScript errors.

**Checkpoint**: All three user stories are functional. Sliders colour correctly on interaction AND on state restore. Modal resets cleanly to neutral state on each open.

---

## Phase 4: Polish & Verification

**Purpose**: Cross-cutting checks to confirm nothing is broken and the feature is release-ready.

- [x] T014 [P] Run full frontend test suite: `cd src/WorkoutTracker.Web && npm test`. Confirm all tests pass including the new `getEffortColour` tests and all pre-existing tests.
- [x] T015 [P] Run TypeScript build one final time: `cd src/WorkoutTracker.Web && npm run build`. Confirm zero errors and zero unused variable/parameter warnings.
- [x] T016 Run the .NET solution build to confirm no backend regressions: `dotnet build src/WorkoutTracker.slnx`.
- [ ] T017 Manual smoke test — verify the following in a browser: (1) drag per-exercise slider 1→10 and observe colour change at each step; (2) open overall effort modal, drag slider 1→10, observe colour change, click Save; (3) open effort modal again, confirm slider resets to neutral (no colour); (4) re-drag to confirm colour returns immediately on first touch.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: No dependencies — start immediately
- **US1+US2 (Phase 2)**: Depends on T001 (`getEffortColour` exists) — can start after T001 + T002 pass
- **US3 (Phase 3)**: Depends on T007 (import already added in Phase 2) — start after Phase 2 complete
- **Polish (Phase 4)**: Depends on all implementation phases complete

### Within Each Phase

- T001 must complete before T008, T009, T011 (function must exist before it's called)
- T002 must complete before T010 (tests must pass before build verification)
- T003–T006 (CSS) are parallel to T001–T002 (different file)
- T007 must complete before T008 and T009 (import before use)
- T008 and T009 are independent of each other (different handler functions) — can run in parallel

### Parallel Opportunities

- T003, T004, T005, T006 (CSS transitions) all target `styles.css` — they MUST run sequentially to avoid write conflicts
- T003–T006 can run in parallel with T001 and T002 (different files entirely)
- T008 and T009 can run in parallel after T007 (different functions in the same file)
- T014 and T015 (final checks) can run in parallel

---

## Parallel Example: Phase 1

```
Parallel batch A (different files, no deps):
  Task T001: Add getEffortColour to utils.ts

Sequential (all target styles.css — must not run in parallel):
  Task T003: Add accent-color transition to .active-session__effort-slider in styles.css
  Task T004: Add accent-color transition to .effort-modal__slider in styles.css
  Task T005: Add color transition to .active-session__effort-value/.band in styles.css
  Task T006: Add color transition to .effort-modal__value/.band in styles.css

Sequential after T001 passes:
  Task T002: Write and run Vitest tests for getEffortColour
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Foundational (T001–T006)
2. Complete Phase 2: US1+US2 (T007–T010)
3. **STOP and VALIDATE**: Drag sliders in both locations — colour updates in real time ✅
4. Ship or continue to US3

### Incremental Delivery

1. Phase 1 complete → utility and CSS ready
2. Phase 2 complete → real-time colour on both sliders (MVP delivered)
3. Phase 3 complete → colour also correct on page load/state restore
4. Phase 4 complete → all tests green, build clean

---

## Notes

- [P] tasks = different files or non-overlapping code sections, no dependencies on each other
- `getEffortColour` is the single source of truth — both slider handlers MUST call it, no inline hex values
- The "not touched" state must remain neutral (no `accent-color` set) — do NOT apply value-1 colour on initial render
- `style.accentColor = ""` clears the inline style and falls back to the browser default (or any CSS rule)
- Verify tests fail first only applies to T002 — the Vitest test for `getEffortColour` must fail before T001 is written if doing strict TDD; otherwise write both together
- Commit after each checkpoint
