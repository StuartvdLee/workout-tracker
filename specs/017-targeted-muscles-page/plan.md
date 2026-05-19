# Implementation Plan: Dedicated Muscles Page

**Branch**: `targeted-muscles-page` | **Date**: 2026-05-19 | **Spec**: [spec.md](./spec.md)
**Status**: Implemented  
**Input**: Feature specification from `/specs/017-targeted-muscles-page/spec.md`

## Summary

Muscles are currently managed inline on the Exercises page via an add-muscle mini-form embedded in the exercise form. This feature relocates full CRUD management to a dedicated **Muscles** page that follows the same layout pattern as the Exercises and Workouts pages (text input → blue action button → content grid), using an "Add Muscle" button and a grid of clickable muscle cards. Two new API endpoints (`PATCH /api/muscles/{id}` and `DELETE /api/muscles/{id}`) are added alongside the existing `GET` and `POST`. The edit flow opens from the full card, provides Save and Delete actions, and uses a separate delete confirmation modal that matches the exercise delete pattern. The Exercises page loses its inline add-muscle form but retains the muscle-toggle selection UI unchanged. The change touches one API file, one web proxy file, one new TypeScript page module, one modified TypeScript page module, one entry-point module, the index HTML, one CSS file, one existing API test file, one new E2E test file, and one modified E2E test file. No database migrations are required — the `Muscle` table already exists with cascade-delete configured on `ExerciseMuscle`.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
**Storage**: PostgreSQL via EF Core — no schema changes; `Muscle` and `ExerciseMuscle` tables already exist. `ExerciseMuscle` has `OnDelete(DeleteBehavior.Cascade)` on both the Exercise and Muscle FK sides — deleting a muscle automatically removes its join rows.
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests; Playwright E2E tests (mock `WebAppFixture` with in-memory state)
**Target Platform**: Web browser (mobile-first, responsive)
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: Add, edit, and delete operations reflect in the muscle grid within 1 second under normal network conditions
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; BEM CSS naming; strict TypeScript (`strict: true`, `noUnusedLocals`, `noUnusedParameters`); existing tests must continue to pass; `ExerciseQueryHelper.EscapeLike` + `EF.Functions.ILike` for case-insensitive duplicate checks; advisory lock pattern for concurrent insert safety
**Scale/Scope**: New file `muscles.ts`; modifications to `exercises.ts`, `main.ts`, `index.html`, `styles.css`, `Program.cs` (API), `Program.cs` (Web); new `MusclesPageTests.cs`; updated `MusclesApiTests.cs` and `ExercisesPageTests.cs`; no migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced — no `any`, all state variables explicitly typed. CSS follows BEM: new page block is `.muscles-page__*`; the grid uses clickable `.muscle-card` buttons rather than nested `.muscle-card__edit-btn` / `.muscle-card__delete-btn` icon controls. The edit modal exposes Save and Delete actions (no Cancel button), and deletion is confirmed in a separate modal that reuses the exercises-page delete modal styling. `PATCH /api/muscles/{id}` follows the same validation + `ILike` + `EscapeLike` duplicate-check pattern as `PUT /api/exercises/{id}`. `DELETE /api/muscles/{id}` follows the same pattern as `DELETE /api/exercises/{id}` — EF cascade handles `ExerciseMuscle` cleanup automatically. No speculative abstractions or dead code. ✅

- **Testing**:
  - **Backend integration tests** (`MusclesApiTests.cs`): New tests for `PATCH /api/muscles/{id}` — (a) 200 with updated name; (b) 404 for unknown id; (c) 400 empty name; (d) 400 name too long; (e) 400 duplicate name case-insensitive; (f) name is trimmed; (g) updated name reflected in subsequent `GET /api/muscles`. New tests for `DELETE /api/muscles/{id}` — (h) 204 for existing muscle; (i) 404 for unknown id; (j) deleted muscle absent from subsequent `GET`; (k) associated `ExerciseMuscle` rows are removed (cascade).
  - **E2E (`MusclesPageTests.cs`)**: New file — `NavigateToMusclesPage_ShowsMuscleGrid`, `AddMuscle_AppearsInGridImmediately`, `AddMuscle_IsSortedAlphabetically`, `AddMuscle_DuplicateNameShowsError`, `AddMuscle_EmptyNameShowsError`, `EditMuscle_RenameAppearsInGrid`, `EditMuscle_EscapeDiscardChanges`, `EditMuscle_DuplicateNameShowsError`, `DeleteMuscle_RemovedFromGrid`, `DeleteMuscle_CancelKeepsMuscle`. Delete tests cover the separate confirmation modal opened from the edit modal.
  - **E2E regression (`ExercisesPageTests.cs`)**: Remove tests that reference the add-muscle form (`AddMuscle_*` tests); add regression test `ExercisesPage_HasNoAddMuscleForm` confirming the form is absent; verify existing muscle-toggle selection tests still pass.
  - Tests treated as mandatory. ✅

- **Security**: `PATCH /api/muscles/{id}` uses `ReadFromJsonAsync` + explicit null-coalescing + `Trim()` — identical pattern to `PUT /api/exercises/{id}`. Duplicate check uses `EF.Functions.ILike` with `ExerciseQueryHelper.EscapeLike` to prevent LIKE-injection. `DELETE /api/muscles/{id}` has no user input beyond the route GUID which is typed as `Guid` by the route constraint. Single-user app, no cross-user access control concern (SR-002 exception consistent with all prior features). No secrets or sensitive data. ✅

- **User Experience Consistency**: The Muscles page uses the same page layout pattern as Exercises and Workouts: `<h1>` title → form with text input + submit button → content section. The muscle management cards reuse the `.muscle-toggle` chip visual language as clickable `.muscle-card` buttons with no separate icon buttons. The edit modal uses Save and Delete actions (no Cancel button), supports Escape-key and backdrop dismissal, and deletion uses a separate confirmation modal matching the exercises-page delete modal. Loading, empty, error, and success states follow the established page conventions (`role="alert"` + `aria-live="polite"` for errors; `style="display:none"` toggle for empty state). ✅

- **Performance**: `PATCH` and `DELETE` are point operations on a small table with no unbounded work. The advisory-lock pattern is preserved for `PATCH` (rename) to prevent concurrent duplicate name races, consistent with `POST`. `DELETE` does not need the lock — the cascade handles join-table cleanup at the DB level. Client-side grid re-render after edit/delete is O(n) on a list of ~20 items — imperceptible. ✅

## Project Structure

### Documentation (this feature)

```text
specs/017-targeted-muscles-page/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # PATCH + DELETE /api/muscles/{id} endpoints
│   └── ui-contract.md   # Muscles page layout + muscle card HTML/CSS/ARIA contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                              # MODIFIED: add PATCH /api/muscles/{id} + DELETE /api/muscles/{id}
                                            #           + MuscleUpdateRequest record

src/WorkoutTracker.Web/
├── Program.cs                              # MODIFIED: add PATCH + DELETE proxy routes for /api/muscles/{id}
└── wwwroot/
    ├── index.html                          # MODIFIED: add "Muscles" sidebar nav link
    ├── css/
    │   └── styles.css                      # MODIFIED: add .muscles-page__* styles, clickable .muscle-card buttons,
    │                                       #           .edit-modal__delete-btn, and reuse delete-modal styles
    └── ts/
        ├── main.ts                         # MODIFIED: import muscles page, register /muscles route
        └── pages/
            ├── muscles.ts                  # NEW: Muscles page (add, edit, delete, clickable card grid)
            └── exercises.ts                # MODIFIED: remove add-muscle form + associated state/handlers;
                                            #           keep muscle-toggle selection UI unchanged

src/WorkoutTracker.UnitTests/
└── Api/
    └── MusclesApiTests.cs                  # MODIFIED: add PATCH + DELETE endpoint tests

src/WorkoutTracker.E2ETests/
└── E2E/
    ├── MusclesPageTests.cs                 # NEW: E2E tests for the Muscles page and delete confirm flow
    └── ExercisesPageTests.cs               # MODIFIED: remove AddMuscle_* tests; add regression assertion
```

**Structure Decision**: Web application pattern (ASP.NET Core API + Aspire + vanilla TypeScript SPA). Consistent with all prior features.
