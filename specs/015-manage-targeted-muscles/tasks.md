# Tasks: Add Targeted Muscles In-App

**Input**: Design documents from `/specs/015-manage-targeted-muscles/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Automated tests are REQUIRED for every user story. Backend integration tests are written first and MUST fail before implementation. E2E tests are written first and MUST fail before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. No new project initialization is required — the .NET Aspire solution and all tooling are fully set up.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: Add the `MuscleCreateRequest` record that the `POST /api/muscles` endpoint handler depends on. This unblocks both user story phases.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T001 Add `MuscleCreateRequest` record (`public string? Name { get; set; }`) at the bottom of `src/WorkoutTracker.Api/Program.cs` alongside `ExerciseCreateRequest`

**Checkpoint**: Foundation ready — user story phases can now proceed.

---

## Phase 3: User Story 1 — Add a New Muscle (Priority: P1) 🎯 MVP

**Goal**: A user can type a new muscle name inline on the exercises page (create form and edit modal), click Add, and see the muscle immediately appear as a selected toggle in the correct alphabetical position — without a page reload — and it is persisted across sessions.

**Independent Test**: Open the exercises page, type "Hip Flexors" in the add-muscle input, click Add. Confirm the toggle appears between "Hamstrings" and "Quads" without a reload, is auto-selected, and survives a page refresh.

### Tests for User Story 1 ⚠️

> **Write these tests FIRST — they MUST fail before implementation starts.**

- [X] T002 [P] [US1] Add backend integration tests `PostMuscle_Returns201_WithValidName`, `PostMuscle_CreatedMuscleAppearsInGetMusclesSortedAlphabetically`, and `PostMuscle_NameIsTrimmedBeforePersistence` to `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs`
- [X] T002a [P] [US1] Add backend integration tests `PostMuscle_EmptyName_Returns400`, `PostMuscle_WhitespaceName_Returns400`, and `PostMuscle_NameTooLong_Returns400` to `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs` — these validate input rejection logic implemented in T007 and MUST fail before T007 is complete
- [X] T003 [P] [US1] Add E2E test `AddMuscle_NewMuscleAppearsInCreateFormImmediately` (type name → click Add → assert toggle present without reload) in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`
- [X] T004 [P] [US1] Add E2E test `AddMuscle_IsSortedAlphabetically` (add "Hip Flexors" → assert it appears between "Hamstrings" and "Quads" toggles) in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`
- [X] T005 [P] [US1] Add E2E test `AddMuscle_CanBeSelectedAndSavedWithExercise` (add muscle → auto-selected → save exercise → verify muscle chip shown) in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`
- [X] T006 [P] [US1] Add E2E test `AddMuscle_InEditModal_AppearsImmediately` (open edit modal → add new muscle → assert toggle appears in modal immediately) in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`

### Implementation for User Story 1

- [X] T007 [US1] Implement `POST /api/muscles` endpoint in `src/WorkoutTracker.Api/Program.cs`: read `MuscleCreateRequest`, trim name, reject empty/whitespace (400 `"Muscle name is required."`), reject > 100 chars (400 `"Muscle name must be 100 characters or fewer."`), create `Muscle { MuscleId = Guid.NewGuid(), Name = name }`, save, return 201 `{ muscleId, name }`
- [X] T008 [P] [US1] Add `POST /api/muscles` proxy route in `src/WorkoutTracker.Web/Program.cs` following the exact `POST /api/exercises` proxy pattern (lines 55–72)
- [X] T009 [P] [US1] Add `.muscle-add`, `.muscle-add__input`, `.muscle-add__btn`, and `.muscle-add__error` CSS rules in `src/WorkoutTracker.Web/wwwroot/css/styles.css` using existing CSS custom properties (`--spacing-xs`, `--color-error`, `--font-size-sm`, etc.)
- [X] T010 [US1] Add add-muscle form HTML to the create form and edit modal scaffolds in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`: `#add-muscle-name` input + `#add-muscle-btn` button + `#add-muscle-error` div (with `role="alert"` and `aria-live="polite"`) below `#exercise-muscles`; same with `edit-` prefix IDs below `#edit-exercise-muscles`
- [X] T011 [US1] Implement `insertMuscleAlphabetically(muscle: Muscle): void` helper and `handleAddMuscle()` / `handleEditAddMuscle()` async functions in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`: call `POST /api/muscles`, show "Adding..." + `aria-disabled` during fetch, on 201 insert into `muscles[]` in sorted order, call `renderMuscleToggles()` and `renderEditMuscleToggles()`, add new muscle to active selection set (`selectedMuscleIds` or `selectedEditMuscleIds`), clear input, return focus to input
- [X] T012 [US1] Wire click event on `#add-muscle-btn` / `#edit-add-muscle-btn` and `keydown` Enter on the name inputs to call `handleAddMuscle()` / `handleEditAddMuscle()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`; add `isAddingMuscle` and `isEditAddingMuscle` guard flags

**Checkpoint**: User Story 1 happy-path is fully functional and independently testable. All T002, T002a, T003–T006 tests should now pass. Error handling for non-2xx API responses is completed in US2 (T018).

---

## Phase 4: User Story 2 — Prevent Duplicate Muscles (Priority: P2)

**Goal**: Attempts to add a muscle whose name already exists (case-insensitive) are rejected with a clear error message. The muscle list is not mutated; the user can correct the name and try again.

**Independent Test**: With "Biceps" in the list, type "biceps" in the add-muscle input and click Add. Confirm no new toggle appears and an error message "A muscle with this name already exists." is shown.

### Tests for User Story 2 ⚠️

> **Write these tests FIRST — they MUST fail before implementation starts.**

- [X] T013 [P] [US2] Add backend integration tests `PostMuscle_DuplicateName_Returns400` (exact case) and `PostMuscle_DuplicateNameDifferentCase_Returns400` (e.g. "biceps" when "Biceps" exists) to `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs` — these MUST fail before T016 is complete
- [X] T014 [P] [US2] Add E2E test `AddMuscle_DuplicateNameShowsError` (type "Chest" → click Add → assert error message shown, no new toggle created) in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`
- [X] T015 [P] [US2] Add E2E test `AddMuscle_EmptyNameShowsError` (click Add with empty input → assert client-side validation error shown, no API call made) in `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs`

### Implementation for User Story 2

- [X] T016 [US2] Add case-insensitive duplicate check to `POST /api/muscles` in `src/WorkoutTracker.Api/Program.cs`: `var normalizedName = ExerciseQueryHelper.EscapeLike(name); var duplicate = await db.Muscles.AnyAsync(m => EF.Functions.ILike(m.Name, normalizedName, "\\"));` — return 400 `"A muscle with this name already exists."` if true
- [X] T017 [US2] Add client-side pre-flight validation to `handleAddMuscle()` and `handleEditAddMuscle()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`: show `"Muscle name is required."` for empty/whitespace input; show `"Muscle name must be 100 characters or fewer."` for input > 100 chars — abort before calling API
- [X] T018 [US2] Handle API 400 response in `handleAddMuscle()` and `handleEditAddMuscle()` in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts`: display `data.error` (or generic fallback) in the add-muscle error container; retain the input value so the user can correct it without retyping

**Checkpoint**: User Stories 1 and 2 are both fully functional and independently testable. All T013–T015 tests should now pass.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Accessibility verification, build validation, and full test suite confirmation.

- [X] T019 [P] Confirm all three add-muscle error containers in `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` carry `role="alert"` + `aria-live="polite"` and that both add-muscle inputs carry `aria-describedby` pointing to their error container — consistent with `#exercise-error` / `#exercise-api-error` pattern
- [X] T020 [P] Build TypeScript via `npm run build` in `src/WorkoutTracker.Web` and resolve any type errors — confirm `strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns` all pass
- [X] T021 [P] Run backend test suite (`dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj`) and confirm all tests pass, including pre-existing `GetMuscles_Returns200WithAllMuscles` (count 12) and `GetMuscles_ReturnsMusclesInAlphabeticalOrder`
- [X] T022 [P] Run E2E test suite (`dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`) and confirm all tests pass, including pre-existing `MuscleToggles_AllTwelveDisplayed`
- [ ] T023 Run the quickstart.md manual verification checklist end-to-end (add "Hip Flexors", verify sort position, persist across reload, attempt duplicate "hip flexors", attempt empty name, verify edit modal)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — start immediately
- **User Story 1 (Phase 3)**: Depends on T001 (MuscleCreateRequest record)
- **User Story 2 (Phase 4)**: Depends on Phase 3 completion (duplicate check extends the endpoint and handlers created in US1)
- **Polish (Phase 5)**: Depends on all user story phases being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends only on T001
- **User Story 2 (P2)**: Depends on Phase 3 (extends the same endpoint and the same handlers)

### Within Each User Story

- Tests MUST be written and fail before implementation
- T010 (HTML scaffold) before T011 (handlers) before T012 (event wiring)
- T008 (proxy) and T009 (CSS) are independent of T007–T012 and can run in parallel

### Parallel Opportunities

- T002, T002a, T003–T006 (all tests for US1) can be written in parallel
- T008 (proxy route) and T009 (CSS) can be worked on in parallel with T007 (endpoint)
- T010 → T011 → T012 are sequential (same file, dependent on each other)
- T013–T015 (all tests for US2) can be written in parallel
- T016 (backend duplicate check) and T017+T018 (frontend validation) can be worked on in parallel
- T019–T022 (polish tasks) can all run in parallel

---

## Parallel Example: User Story 1

```
# All US1 tests can be written simultaneously:
Task T002:  PostMuscle_Returns201 + trim test in MusclesApiTests.cs
Task T002a: PostMuscle_EmptyName + WhitespaceName + NameTooLong in MusclesApiTests.cs
Task T003:  AddMuscle_NewMuscleAppearsInCreateFormImmediately E2E
Task T004:  AddMuscle_IsSortedAlphabetically E2E
Task T005:  AddMuscle_CanBeSelectedAndSavedWithExercise E2E
Task T006:  AddMuscle_InEditModal_AppearsImmediately E2E

# Once T007 is done, T008 and T009 can run in parallel:
Task T008: Web proxy POST /api/muscles (WorkoutTracker.Web/Program.cs)
Task T009: .muscle-add__* CSS styles (styles.css)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: T001 (MuscleCreateRequest record)
2. Write failing tests: T002–T006
3. Implement: T007 → T008 + T009 (parallel) → T010 → T011 → T012
4. **STOP and VALIDATE**: All T002–T006 tests pass, US1 manually verified
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Foundational → T001 done
2. Add User Story 1 (T002–T012) → test independently → deploy (MVP!)
3. Add User Story 2 (T013–T018) → test independently → deploy
4. Polish (T019–T023) → final validation

---

## Notes

- `[P]` tasks operate on different files or have no intra-phase dependencies
- `[US1]` / `[US2]` labels map tasks to specific user stories for traceability
- `ExerciseQueryHelper.EscapeLike` is already in `Program.cs` (line 737) — reuse it, do not duplicate
- The `muscles[]` module-level array in `exercises.ts` is shared by both create and edit toggle renders — inserting into it once refreshes both
- `MuscleToggles_AllTwelveDisplayed` will continue to pass because `ResetDataAsync()` runs before each test
- Commit after each phase checkpoint (T001, after Phase 3 checkpoint, after Phase 4 checkpoint)
