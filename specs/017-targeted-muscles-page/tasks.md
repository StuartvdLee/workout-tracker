# Tasks: Dedicated Muscles Page

**Input**: Design documents from `/specs/017-targeted-muscles-page/`
**Branch**: `targeted-muscles-page`
**Feature**: Move full CRUD management of muscles to its own dedicated page; remove inline add-muscle form from Exercises page.

**Tests**: Automated tests are REQUIRED for every user story. Backend integration tests cover all new `PATCH` and `DELETE` endpoint behaviours. E2E tests cover the new Muscles page, edit dismissal via Escape, the delete confirmation modal, and exercise-page regression. No new Vitest tests are needed — the page follows an established DOM-rendering pattern with no pure logic to unit-test.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. Foundation phase must complete before user story work begins. US4 (View Grid) and US1 (Add Muscle) are both P1 and are implemented together as Phase 3.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in same phase)
- **[Story]**: Which user story this task belongs to ([US1], [US2], [US3], [US4], [US5])
- Exact file paths included in all descriptions

---

## Phase 2: Foundation (Blocking Prerequisites)

**Purpose**: Wire up the new route, sidebar link, and page skeleton so all user story phases can build on it. No user story work can begin until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T001 Register `/muscles` route in `src/WorkoutTracker.Web/wwwroot/ts/main.ts`: add `import { render as renderMuscles } from "./pages/muscles.js";` alongside the existing page imports and `registerRoute("/muscles", renderMuscles)` after the existing route registrations
- [x] T002 [P] Add "Muscles" sidebar link to `src/WorkoutTracker.Web/wwwroot/index.html`: insert `<a class="sidebar__link" href="/muscles" data-page="muscles">` with the biceps-flexed SVG icon and `<span class="sidebar__label">Muscles</span>` — position after the "Exercises" entry; follow exact HTML pattern of existing sidebar links
- [x] T003 Create `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts` with: `Muscle` interface (`readonly muscleId: string; readonly name: string;`), module-level state (`let muscles: Muscle[] = []`, `let isSubmitting = false`, edit modal state, delete confirmation state), and exported `async function render(container: HTMLElement): Promise<void>` that sets `container.innerHTML` with the full page HTML scaffold (title, add-form, muscle-list section, empty-state div, edit-modal-backdrop, delete-confirm-backdrop) and calls `loadMuscles()` then `initEventListeners()`

**Checkpoint**: `/muscles` route resolves in the browser; sidebar shows "Muscles" link; page renders with empty state message.

---

## Phase 3: User Story 4 + User Story 1 — View Grid & Add a Muscle (Priority: P1) 🎯 MVP

**Goal (US4)**: Navigating to `/muscles` shows the full alphabetically-sorted muscle grid using the chip-style layout from the exercises page. An empty state message is shown when no muscles exist.

**Goal (US1)**: The user types a muscle name in the text input, clicks "Add Muscle", and the new muscle immediately appears in the grid in alphabetical order without a page reload. The form clears on success. Duplicate and empty-name attempts show error messages.

**Independent Test**: Navigate to `/muscles`, verify grid with all 12 seeded muscles. Type "Hip Flexors", click "Add Muscle", verify it appears between "Hamstrings" and "Quads" without reload. Type "chest" and attempt to add — verify error "A muscle with this name already exists." is shown.

### Tests for US4 + US1 ⚠️

> **Write these tests FIRST in `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` (new file) — they MUST fail before implementation starts.**

- [x] T004 Create `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` with `[Collection("E2E")]` attribute, `WebAppFixture` + `PlaywrightFixture` injection, and a `CreatePageAsync()` helper that calls `WebAppFixture.ResetMuscles()`, opens a new Playwright page at `{_webApp.BaseUrl}/muscles`, and awaits `LoadState.NetworkIdle`
- [x] T005 [P] [US4] Add E2E test `NavigateToMusclesPage_ShowsMuscleGrid` in `MusclesPageTests.cs`: navigate to `/muscles`, assert `#muscle-grid` is visible and contains at least 12 muscle cards (the seeded muscles)
- [x] T006 [P] [US1] Add E2E test `AddMuscle_AppearsInGridImmediately` in `MusclesPageTests.cs`: fill `#muscle-name` with "Hip Flexors", click the `#muscle-form .muscle-form__submit` button, assert a card with text "Hip Flexors" appears in `#muscle-grid` without a page reload
- [x] T007 [P] [US1] Add E2E test `AddMuscle_IsSortedAlphabetically` in `MusclesPageTests.cs`: add "Hip Flexors", assert the card appears between the "Hamstrings" and "Quads" cards in DOM order within `#muscle-grid`
- [x] T008 [P] [US1] Add E2E test `AddMuscle_EmptyNameShowsError` in `MusclesPageTests.cs`: click submit with empty input, assert `#muscle-error` contains "Muscle name is required." and no new card is added to the grid
- [x] T009 [P] [US1] Add E2E test `AddMuscle_DuplicateNameShowsError` in `MusclesPageTests.cs`: type "chest" and click submit, assert `#muscle-api-error` (or `#muscle-error`) shows "A muscle with this name already exists." and the grid card count does not increase

### Implementation for US4 + US1

- [x] T010 [US4] Add full HTML scaffold inside `render()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts`:
  - `<h1 class="muscles-page__title">Muscles</h1>`
  - `<form class="muscle-form" id="muscle-form" novalidate>` containing: label + `<input class="muscle-form__input" id="muscle-name" name="muscle-name" maxlength="100" autocomplete="off" aria-describedby="muscle-error" />`, `<div class="muscle-form__error" id="muscle-error" role="alert" aria-live="polite"></div>`, `<div class="muscle-form__actions"><button class="muscle-form__submit" type="submit">Add Muscle</button></div>`, `<div class="muscle-form__api-error" id="muscle-api-error" role="alert" aria-live="polite"></div>`
  - `<section class="muscle-list"><h2 class="muscle-list__heading">Your Muscles</h2><div class="muscle-list__loading" id="muscle-loading">Loading...</div><div class="muscle-list__empty" id="muscle-empty" style="display:none;">No muscles yet. Add your first muscle above!</div><div class="muscle-list__grid" id="muscle-grid"></div></section>`
  - Edit modal backdrop + modal and delete confirmation modal backdrop + modal (see T024/T032 for full markup)
- [x] T011 [P] [US4] Add `.muscles-page`, `.muscles-page__title`, `.muscle-form`, `.muscle-form__group`, `.muscle-form__label`, `.muscle-form__input`, `.muscle-form__error`, `.muscle-form__actions`, `.muscle-form__submit` (blue primary button, same as `.exercise-form__submit` and `.workout-form__submit`), `.muscle-form__api-error`, `.muscle-list`, `.muscle-list__heading`, `.muscle-list__grid` (`repeat(auto-fill, minmax(6rem, 1fr))`, matching `.exercise-form__muscles`), `.muscle-list__empty`, `.muscle-list__loading` (same loading indicator style as other pages), and `.muscle-card` (pill chip style matching `.muscle-toggle` shape/size/border-radius/font as a clickable button) CSS rules in `src/WorkoutTracker.Web/wwwroot/css/styles.css`
- [x] T012 [US4] Implement `async function loadMuscles(): Promise<void>` in `muscles.ts`: show `#muscle-loading`, fetch `GET /api/muscles`, assign to `muscles`, call `renderMuscleGrid()`, then hide `#muscle-loading` — hide it in both success and error paths (consistent with UX-004 / FR-013). Implement `function renderMuscleGrid(): void`: clear `#muscle-grid`, if empty show `#muscle-empty`, else hide it and render one clickable `.muscle-card[data-muscle-id]` button per muscle with `<span class="muscle-card__name">{name}</span>` and a click handler that opens the edit modal.
- [x] T013 [US1] Implement `async function handleAddMuscle(event: SubmitEvent): Promise<void>` in `muscles.ts`: prevent default, trim input, reject empty (show "Muscle name is required." in `#muscle-error`), set submit button to "Adding..." + `aria-disabled="true"`, call `POST /api/muscles` with `{ name }`, on 201 JSON parse response, push muscle into `muscles[]` maintaining sort, call `renderMuscleGrid()`, clear input and errors; on 400/error display `data.error` (or generic fallback) in `#muscle-api-error`; always restore button to "Add Muscle" + remove `aria-disabled`
- [x] T014 [US1] Implement `function initEventListeners(): void` in `muscles.ts` wiring `#muscle-form` `submit` event to `handleAddMuscle`; ensure existing `muscles[]` sort is maintained using `localeCompare`-based insertion (matching the alphabetical order from `GET /api/muscles`)

**Checkpoint**: US4 + US1 fully functional and independently testable. All T005–T009 E2E tests should pass.

---

## Phase 4: User Story 5 — Remove Muscle Management from Exercises Page (Priority: P2)

**Goal**: The Exercises page no longer shows an "add muscle" input or button. The muscle toggle selection UI is unchanged — users can still select existing muscles for exercises. All add-muscle E2E tests from `ExercisesPageTests.cs` are removed; a regression test confirms the form is absent.

**Independent Test**: Navigate to `/exercises`, open the create-exercise form — confirm there is no `#add-muscle-form` element or "Add" button below the muscle toggles. Confirm muscle toggles are still present and selectable.

- [x] T015 [US5] Remove the `.muscle-add` HTML block (containing `#add-muscle-form`, `#add-muscle-name`, `#add-muscle-btn`, `#add-muscle-error`) from the create-exercise form scaffold in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`
- [x] T016 [P] [US5] Remove the `.muscle-add` HTML block (containing `#edit-add-muscle-form`, `#edit-add-muscle-name`, `#edit-add-muscle-btn`, `#edit-add-muscle-error`) from the edit-exercise modal scaffold in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`
- [x] T017 [US5] Remove from `exercises.ts`: `let isAddingMuscle: boolean = false;`, `let isEditAddingMuscle: boolean = false;` state declarations; `handleAddMuscle()`, `handleEditAddMuscle()`, `insertMuscleAlphabetically()` functions; the click/keydown event bindings for `#add-muscle-btn` and `#edit-add-muscle-btn` — and any resulting `noUnusedLocals` TypeScript errors
- [x] T018 [P] [US5] In `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`: remove all `AddMuscle_*` test methods (tests for add-muscle form that no longer exists); add regression test `ExercisesPage_HasNoAddMuscleForm` asserting `page.Locator("#add-muscle-form")` has count 0 and `page.Locator("#edit-add-muscle-form")` has count 0 (after opening edit modal)

**Checkpoint**: US5 complete. Exercises page has no add-muscle form; muscle toggle selection still works; `ExercisesPage_HasNoAddMuscleForm` passes.

---

## Phase 5: User Story 2 — Edit a Muscle (Priority: P2)

**Goal**: Each muscle card on the Muscles page is a clickable button. Clicking anywhere on the card opens a modal pre-populated with the current name. Saving renames the muscle and the grid updates immediately. Escape or backdrop click discards changes; there is no Cancel button.

**Independent Test**: Navigate to `/muscles`, click the "Back" card, rename it to "Upper Back", click Save — verify "Upper Back" appears in the grid in the correct alphabetical position. Click the same card, change name to "Chest", click Save — verify error "A muscle with this name already exists." is shown.

### Tests for US2 ⚠️

> **Write these tests FIRST — they MUST fail before T021–T024 are complete.**

- [x] T019 [P] [US2] Add backend integration tests to `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs`: `PatchMuscle_Returns200_WithUpdatedName`, `PatchMuscle_Returns404_ForUnknownId`, `PatchMuscle_Returns400_ForEmptyName`, `PatchMuscle_Returns400_ForNameTooLong`, `PatchMuscle_DuplicateNameCaseInsensitive_Returns400`, `PatchMuscle_NameIsTrimmed`, `PatchMuscle_UpdatedNameAppearsInGetMuscles`
- [x] T020 [P] [US2] Add mock `PATCH /api/muscles/{muscleId}` endpoint to `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs` (following pattern of mock `PUT /api/exercises/{exerciseId}`): validate name, check duplicate (case-insensitive, excluding self), update `_muscles` in-place, return `{ muscleId, name }` on 200 or error JSON on 400/404
- [x] T021 [P] [US2] Add E2E tests to `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs`: `EditMuscle_RenameAppearsInGrid` (click card → modal opens pre-filled → change name → Save → card updated in grid), `EditMuscle_EscapeDiscardChanges` (click card → change name → press Escape → grid unchanged), `EditMuscle_DuplicateNameShowsError` (rename to existing muscle name → error shown, name unchanged in grid)

### Implementation for US2

- [x] T022 [US2] Add `MuscleUpdateRequest` record (`public string? Name { get; set; }`) at the bottom of `src/WorkoutTracker.Api/Program.cs` alongside `MuscleCreateRequest`; add `PATCH /api/muscles/{muscleId:guid}` endpoint: fetch muscle by id (404 if missing), read `MuscleUpdateRequest`, trim name, validate (400 for empty/too-long), wrap advisory-lock + `ILike`/`EscapeLike` duplicate check (excluding `m.MuscleId != muscleId`) + `SaveChangesAsync` inside `db.Database.CreateExecutionStrategy().ExecuteAsync()`, return `Results.Ok(new { muscle.MuscleId, muscle.Name })`
- [x] T023 [P] [US2] Add `PATCH /api/muscles/{muscleId}` proxy route in `src/WorkoutTracker.Web/Program.cs` following the pattern of `PUT /api/exercises/{exerciseId}` proxy
- [x] T024 [US2] Add edit modal HTML to the muscles.ts page scaffold (placeholder added in T010): `<div class="edit-modal-backdrop" id="edit-modal-backdrop" style="display:none;"><div class="edit-modal" id="edit-modal" role="dialog" aria-modal="true" aria-labelledby="edit-modal-title"><h2 class="edit-modal__title" id="edit-modal-title">Edit Muscle</h2><form class="edit-modal__form" id="edit-modal-form" novalidate>` with `#edit-muscle-name` input (maxlength 100), `#edit-muscle-error` error div, submit button "Save", delete button `#edit-modal-delete-btn`, and `#edit-modal-api-error` div
- [x] T025 [US2] Render each muscle as a clickable `<button class="muscle-card" aria-label="Edit {name}">` inside `renderMuscleGrid()` in `muscles.ts`; keep the visible label in `<span class="muscle-card__name">{name}</span>` and add hover/focus styles for the full-card interaction in `styles.css`
- [x] T026 [US2] Implement in `muscles.ts`: `let editingMuscleId: string | null = null;` state, `function openEditModal(muscle: Muscle): void` (pre-fill `#edit-muscle-name`, clear errors, set `editingMuscleId`, show backdrop, focus input, trap tab key using `trapModalTabKey` from `prestart-modal.ts`), `function closeEditModal(): void` (hide backdrop, clear `editingMuscleId`), `async function handleEditSave(event: SubmitEvent): Promise<void>` (prevent default, validate, call `PATCH /api/muscles/{editingMuscleId}`, on success update `muscles[]` in place + re-render, close modal; on error show in `#edit-modal-api-error`); wire card click, `#edit-modal-form` submit, `#edit-modal-delete-btn` click, `#edit-modal-backdrop` click (stop propagation on modal), and `Escape` keydown to open/close functions in `initEventListeners()`

**Checkpoint**: US2 fully functional. All T019–T021 tests pass.

---

## Phase 6: User Story 3 — Delete a Muscle (Priority: P3)

**Goal**: Each muscle card can be opened for editing by clicking it. Clicking Delete in the edit modal opens a separate delete confirmation modal. Confirming sends a DELETE request; the card is removed from the grid. Cancelling keeps the card unchanged.

**Independent Test**: Navigate to `/muscles`, click the "Adductors" card, click Delete in the edit modal, then click Delete in the confirmation modal — verify the "Adductors" card is removed. Open another card, click Delete, then click Cancel in the confirmation modal — verify the card remains.

### Tests for US3 ⚠️

> **Write these tests FIRST — they MUST fail before T028–T030 are complete.**

- [x] T027 [P] [US3] Add backend integration tests to `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs`: `DeleteMuscle_Returns204_ForExistingMuscle`, `DeleteMuscle_Returns404_ForUnknownMuscle`, `DeleteMuscle_RemovedFromSubsequentGet`, `DeleteMuscle_ExerciseMuscleAssociationsAreRemovedByCascade` (create exercise with muscle, delete muscle, verify exercise's muscle list is empty via `GET /api/exercises`)
- [x] T028 [P] [US3] Add mock `DELETE /api/muscles/{muscleId}` endpoint to `src/WorkoutTracker.E2ETests/Infrastructure/WebAppFixture.cs` (following pattern of mock `DELETE /api/exercises/{exerciseId}`): find muscle by id, return 404 if missing, remove from `_muscles`, return 204
- [x] T029 [P] [US3] Add E2E tests to `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs`: `DeleteMuscle_RemovedFromGrid` (click card → edit modal → delete confirm modal → confirm → card no longer in grid), `DeleteMuscle_CancelKeepsMuscle` (click card → edit modal → delete confirm modal → cancel → card still in grid)

### Implementation for US3

- [x] T030 [US3] Add `DELETE /api/muscles/{muscleId:guid}` endpoint in `src/WorkoutTracker.Api/Program.cs`: fetch muscle by id (404 if missing), `db.Muscles.Remove(muscle)`, `await db.SaveChangesAsync()` (EF cascade deletes `ExerciseMuscle` rows automatically), return `Results.NoContent()`
- [x] T031 [P] [US3] Add `DELETE /api/muscles/{muscleId}` proxy route in `src/WorkoutTracker.Web/Program.cs` following the pattern of `DELETE /api/exercises/{exerciseId}` proxy
- [x] T032 [US3] Implement delete from within the edit modal in `muscles.ts`: clicking `#edit-modal-delete-btn` closes the edit modal, opens `#delete-confirm-backdrop`, populates the confirmation message, and on confirm calls the DELETE endpoint; on 204 remove the muscle from `muscles[]` and call `renderMuscleGrid()`; on cancel or error keep the muscle unchanged
- [x] T033 [P] [US3] Reuse the existing `.delete-modal-backdrop`, `.delete-modal`, `.delete-modal__actions`, `.delete-modal__delete`, `.delete-modal__cancel`, and `.delete-modal__error` CSS in `src/WorkoutTracker.Web/wwwroot/css/styles.css` for the separate confirmation modal; no inline card confirmation styles are needed

**Checkpoint**: US3 fully functional. All T027–T029 tests pass. Full CRUD on Muscles page complete.

---

## Phase 7: Polish & Verification

**Purpose**: Confirm all code compiles with zero errors, all tests pass, and no regressions are introduced.

- [x] T034 Run `dotnet build src/WorkoutTracker.slnx` and confirm zero errors and zero warnings in all modified files
- [x] T035 [P] Run `cd src/WorkoutTracker.Web && npm run build` and confirm TypeScript compiles cleanly (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns` all pass) for both `muscles.ts` and the modified `exercises.ts`
- [x] T036 [P] Run `dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj` and confirm all backend integration tests pass, including: 7 new `PatchMuscle_*` tests, 4 new `DeleteMuscle_*` tests, and all pre-existing `GetMuscles_*` and `PostMuscle_*` tests
- [x] T037 [P] Run `cd src/WorkoutTracker.Web && npm test` and confirm all Vitest frontend tests pass with no regressions
- [x] T038 [P] Run `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj` and confirm all E2E tests pass, including: 10 new `MusclesPageTests` tests, 1 new `ExercisesPage_HasNoAddMuscleForm` regression test, and all pre-existing exercises/workouts/history tests
- [x] T039 [P] Manually verify UX consistency (SC-008): (a) confirm the clickable muscle cards on `/muscles` use the same chip-style layout as the muscle toggles on the Exercises page (same pill shape, border-radius, font size, gap, and `repeat(auto-fill, minmax(6rem, 1fr))` grid); (b) confirm the edit modal behaviour (focus trap, Escape key dismissal, backdrop click dismissal, Save + Delete actions with no Cancel button) matches the intended modal pattern; (c) confirm the separate delete confirmation modal matches the Exercises page delete modal; (d) confirm the loading indicator shown during the initial `GET /api/muscles` fetch is consistent with loading states on other pages (UX-004 / FR-013)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 2 (Foundation)       ──────────────────────────────────┐
                                                              ▼
Phase 3 (US4 + US1, P1)   ←── depends on Phase 2 complete ──┤
Phase 4 (US5, P2)          ←── depends on Phase 2 complete ──┤  (independent of Phase 3)
Phase 5 (US2, P2)          ←── depends on Phase 3 complete ──┤
Phase 6 (US3, P3)          ←── depends on Phase 5 complete ──┘
Phase 7 (Polish)           ←── depends on all phases complete
```

### Within Phase 3 (US4 + US1)

```
T003 (skeleton) ──→ T010 (full HTML) ──→ T012 (render fn) ──→ T013 (add handler) ──→ T014 (events)
T004 (test file) ──→ T005–T009 (tests, parallel)
T011 (CSS, parallel with above)
```

### Within Phase 5 (US2)

```
T022 (PATCH endpoint) ←─── T019 (backend tests, parallel)
T023 (proxy, parallel with T022)
T024 (modal HTML) ──→ T025 (edit button) ──→ T026 (modal logic)
T020 (mock endpoint) ←─── T021 (E2E tests, parallel after T020)
```

### Within Phase 6 (US3)

```
T030 (DELETE endpoint) ←─── T027 (backend tests, parallel)
T031 (proxy, parallel with T030)
T032 (delete button + handler) ←─── T029 (E2E tests, parallel after T028)
T028 (mock endpoint) ──→ T029 (E2E tests)
T033 (CSS, parallel)
```

### Parallel Opportunities Per Phase

| Phase | Parallel tasks |
|-------|---------------|
| Phase 2 | T002, T003 (separate files) |
| Phase 3 | T005–T009 (all E2E tests), T011 (CSS) with T010–T014 |
| Phase 4 | T015, T016 (same file, sequential); T018 (separate file, parallel) |
| Phase 5 | T019 (backend tests), T020 (mock), T023 (proxy) — all parallel with each other and with T024–T026 |
| Phase 6 | T027, T028, T031, T033 — all parallel with T030 and T032 |
| Phase 7 | T035–T039 all parallel |

---

## Implementation Strategy

**MVP** (minimum viable value delivery): Complete **Phase 3 only** — users can navigate to the Muscles page, view the grid, and add new muscles. This is independently useful even before edit/delete or exercises-page cleanup.

**Incremental delivery order**:
1. Phase 2 (Foundation) — unblocks all work
2. Phase 3 (US4 + US1) — delivers the new page with add capability ← **MVP**
3. Phase 4 (US5) — removes the now-redundant inline form from Exercises page
4. Phase 5 (US2) — adds rename capability
5. Phase 6 (US3) — adds delete capability
6. Phase 7 (Polish) — verifies everything

**Total tasks**: 39  
**New files**: `muscles.ts`, `MusclesPageTests.cs`  
**Modified files**: `main.ts`, `index.html`, `exercises.ts`, `styles.css`, `Program.cs` (API), `Program.cs` (Web), `MusclesApiTests.cs`, `ExercisesPageTests.cs`, `WebAppFixture.cs`
