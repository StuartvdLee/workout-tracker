# Tasks: Exercises Muscles Link

**Input**: Design documents from `/specs/019-exercises-muscles-link/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅

**Tests**: Three new E2E tests are added to `ExercisesPageTests.cs` before implementation begins, verifying the link is visible in both the Add form and the Edit modal, and that clicking it navigates to `/muscles`.

**Organization**: Single user story (P1) — the entire feature is one additive UI change. No blocking foundational work is required; all infrastructure (SPA router, CSS, E2E fixtures) already exists.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to

---

## Phase 1: User Story 1 — Navigate to Muscles Page from Exercises (Priority: P1) 🎯 MVP

**Goal**: A "Manage" link appears inside the "Targeted muscles (optional)" label (dash-separated) in both the Add Exercise form and the Edit Exercise modal. Clicking it navigates to `/muscles` via the SPA router.

**Independent Test**: Open the Exercises page — confirm a "Manage" link is visible inline in the "Targeted muscles (optional)" label in the Add form. Click it and confirm the browser navigates to `/muscles`. Open the Edit modal for any exercise and confirm the same link is present there.

### Tests for User Story 1

- [x] T001 [US1] Add three E2E tests to `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` — `MusclesLink_IsVisibleInAddForm` (asserts `a.exercise-form__manage-link` with text "Manage" is visible in `#exercise-form`), `MusclesLink_IsVisibleInEditModal` (opens edit modal for an exercise, asserts the link is visible inside `#edit-modal-form`), and `MusclesLink_NavigatesToMusclesPage` (clicks the Add-form link, asserts URL ends with `/muscles`).

### Implementation for User Story 1

- [x] T002 [P] [US1] Add `.exercise-form__manage-link` rule to `src/WorkoutTracker.Web/wwwroot/css/styles.css` — inline anchor style (`color: var(--color-primary)`, `font-size: var(--font-size-sm)`, no underline by default, underline on hover/focus-visible). No margin needed — the dash in the label text provides separation.
- [x] T003 [P] [US1] Update `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` — (1) add `import { navigate } from "../router.js"` at the top; (2) place `<a class="exercise-form__manage-link" href="/muscles">Manage</a>` inside the `<label class="exercise-form__label">` with a dash separator (`Targeted muscles (optional) – Manage`) in both the Add Exercise form and the Edit Exercise modal template strings; (3) add `initMusclesLinks()` function (called from `render()`) that queries all `a.exercise-form__manage-link` elements and attaches a `click` listener calling `event.preventDefault()` and `navigate('/muscles')`.

### Verification for User Story 1

- [x] T004 [US1] Build TypeScript to confirm no type errors: `cd src/WorkoutTracker.Web && npm run build`
- [x] T005 [US1] Run frontend unit tests to confirm no regressions: `cd src/WorkoutTracker.Web && npm test`
- [x] T006 [US1] Run E2E test suite to confirm all three new tests pass and no regressions: `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`

**Checkpoint**: "Manage" link is visible inline in both the Add form and the Edit modal labels. Clicking navigates to `/muscles`. All E2E tests pass.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (US1)**: No prerequisites — can start immediately

### Within User Story 1

- T001 (E2E tests) must be written and confirmed **failing** before T003 (implementation)
- T002 (CSS) and T003 (TypeScript) are independent — different files, can run in parallel once T001 is done
- T004 → T005 → T006 run sequentially after T002 and T003 are complete

### Parallel Opportunities

```bash
# Step 1: Write failing tests
T001: Add 3 E2E tests to ExercisesPageTests.cs (confirm failure)

# Step 2: Implement in parallel (different files)
T002: styles.css — add .exercise-form__manage-link rule
T003: exercises.ts — add anchor + navigate handler

# Step 3: Verify sequentially
T004: npm run build
T005: npm test
T006: dotnet test WorkoutTracker.E2ETests
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Write T001 E2E tests — confirm they **fail**
2. Run T002 and T003 in parallel (different files, no conflicts)
3. Run T004 (`npm run build`) — confirm TypeScript is clean
4. Run T005 (`npm test`) — confirm frontend unit tests pass
5. Run T006 (`dotnet test`) — confirm all three new E2E tests pass with no regressions
6. **VALIDATE**: Open the app, confirm "Manage" link is present inline in both the Add form and Edit modal labels, and navigates to `/muscles`

---

## Notes

- `navigate()` is the canonical SPA navigation function — imported from `../router.js` in `exercises.ts`
- The anchor uses `href="/muscles"` as a fallback for non-JS environments, with `event.preventDefault()` + `navigate()` for SPA navigation
- The link is placed inside the `<label>` element with a dash separator, so no extra CSS layout is needed to appear inline
- Both the Add form and Edit modal template strings live in the same `render()` function in `exercises.ts` — T003 covers both in a single edit
- `initMusclesLinks()` queries `a.exercise-form__manage-link` (both instances) and attaches click handlers — called once from `render()` after `container.innerHTML` is set
