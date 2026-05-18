# Implementation Plan: Add Targeted Muscles In-App

**Branch**: `015-manage-targeted-muscles` | **Date**: 2026-05-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/015-manage-targeted-muscles/spec.md`

## Summary

Users currently cannot add muscles from within the app — the list is entirely seeded via database migrations. This feature introduces a `POST /api/muscles` endpoint and an inline "Add muscle" mini-form below the targeted muscles toggles on the exercises page. When a valid, unique name is submitted, the new muscle is immediately inserted into the client-side `muscles[]` array in alphabetical order, both toggle containers are re-rendered, and the new muscle appears unselected (the user may toggle it as desired) — all without a page reload. The change touches one API file, one web proxy file, one TypeScript page module, one CSS file, and adds unit + E2E tests. No schema migrations are required.

## Technical Context

**Language/Version**: C# on .NET 10.0 (backend), TypeScript ~6.0.3 (frontend)
**Primary Dependencies**: ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
**Storage**: PostgreSQL via EF Core — no schema changes; `Muscle` table already exists with seeded rows
**Testing**: xUnit 3.2.2 + WebApplicationFactory integration tests (real PostgreSQL via `TEST_DB_CONNECTION`); Vitest frontend unit tests; Playwright E2E tests
**Target Platform**: Web browser (mobile-first, responsive)
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: New muscle appears in the sorted toggle list within 1 second of a successful save under normal network conditions
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing tests must continue to pass; BEM CSS naming; strict TypeScript (`strict: true`, `noUnusedLocals`, `noUnusedParameters`)
**Scale/Scope**: Changes touch 4 files (`Program.cs` API, `Program.cs` Web, `exercises.ts`, `styles.css`) + `MusclesApiTests.cs` + `ExercisesPageTests.cs`; no migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced — no `any`, all new state variables explicitly typed. CSS follows BEM: new elements use `.muscle-add__*` block. C# follows existing inline anonymous-type pattern and `EF.Functions.ILike` + `ExerciseQueryHelper.EscapeLike` for case-insensitive duplicate checks — no new helper classes. No speculative abstractions or dead code. ✅

- **Testing**:
  - **Backend integration tests** (`MusclesApiTests.cs`): New tests for `POST /api/muscles` — (a) 201 with valid name; (b) created muscle appears in subsequent `GET /api/muscles` in alphabetical order; (c) 400 empty name; (d) 400 whitespace-only name; (e) 400 name exceeds 100 characters; (f) 400 exact-case duplicate; (g) 400 case-insensitive duplicate (e.g., "biceps" when "Biceps" exists); (h) name is trimmed before persistence.
  - **E2E (Playwright / `ExercisesPageTests.cs`)**: New tests — `AddMuscle_NewMuscleAppearsInCreateFormImmediately`, `AddMuscle_IsSortedAlphabetically`, `AddMuscle_DuplicateNameShowsError`, `AddMuscle_EmptyNameShowsError`, `AddMuscle_CanBeSelectedAndSavedWithExercise`, `AddMuscle_InEditModal_AppearsImmediately`.
  - **Regression**: `MuscleToggles_AllTwelveDisplayed` asserts count 12 — passes because test database is reset between tests via `ResetDataAsync()`; custom muscles added in one test do not survive into others.
  - Tests treated as mandatory. ✅

- **Security**: `POST /api/muscles` uses `ReadFromJsonAsync` + explicit null-coalescing + `Trim()` — consistent with `POST /api/exercises`. Duplicate check uses `EF.Functions.ILike` with `ExerciseQueryHelper.EscapeLike` — prevents injection via LIKE pattern characters. No user authentication in this single-user app (SR-002 exception documented identically to all prior features). No secrets or sensitive data involved. ✅

- **User Experience Consistency**: The add-muscle mini-form is placed inline below the muscle toggles in both the create form and the edit modal, using `exercise-form__input`, `exercise-form__submit`, and `exercise-form__api-error` CSS classes alongside new `.muscle-add__*` wrappers — matching the visual language of the surrounding form. Error states use `role="alert"` + `aria-live="polite"` consistent with existing `#exercise-error` and `#exercise-api-error` elements. Loading state: "Add" button shows "Adding..." with `aria-disabled="true"` — same pattern as "Saving..." on the exercise submit button. ✅

- **Performance**: Adding a muscle is a point mutation (one INSERT) on a small table with no FK cascade. The duplicate check currently scans the small `muscles` table (`ILike`), and the endpoint serializes concurrent insert attempts via transaction-scoped advisory lock to keep behavior consistent. Client-side insert + alphabetical sort of a list of ~20 items is O(n log n) and imperceptible. ✅

## Project Structure

### Documentation (this feature)

```text
specs/015-manage-targeted-muscles/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── api-contract.md  # POST /api/muscles endpoint
│   └── ui-contract.md   # Add-muscle inline form HTML/CSS/ARIA contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Api/
└── Program.cs                              # MODIFIED: add POST /api/muscles + MuscleCreateRequest record

src/WorkoutTracker.Web/
└── Program.cs                              # MODIFIED: add POST /api/muscles proxy route
└── wwwroot/
    ├── css/
    │   └── styles.css                      # MODIFIED: add .muscle-add__* styles
    └── ts/
        └── pages/
            └── exercises.ts                # MODIFIED: add-muscle state, inline form, handleAddMuscle,
                                            #           insertMuscleAlphabetically, update both
                                            #           renderMuscleToggles + renderEditMuscleToggles

src/WorkoutTracker.UnitTests/
└── Api/
    └── MusclesApiTests.cs                  # MODIFIED: add POST /api/muscles tests

src/WorkoutTracker.E2ETests/
└── E2E/
    └── ExercisesPageTests.cs               # MODIFIED: add add-muscle E2E tests
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects, no new files beyond spec artifacts. The add-muscle form is inlined directly in `exercises.ts` — consistent with how all other exercise-page interactions (edit modal, delete modal) are co-located in that single page module.

## Complexity Tracking

> No constitution violations. No complexity justification required.

## Post-Design Constitution Re-check

*Re-evaluated after Phase 1 design artifacts are complete.*

- **Code Quality** ✅ — Four existing files surgically modified. No new abstractions, no `any`, BEM naming, `ExerciseQueryHelper.EscapeLike` reused as-is.
- **Testing** ✅ — Backend: nine new integration tests. E2E: six new Playwright tests. Existing `MuscleToggles_AllTwelveDisplayed` continues to pass due to per-test fixture reset.
- **Security** ✅ — Input trimming, ILike with EscapeLike, single-user auth exception documented consistently.
- **User Experience Consistency** ✅ — BEM naming, existing CSS tokens, `role="alert"` + `aria-live="polite"`, loading state matches existing pattern.
- **Performance** ✅ — Single INSERT, O(n log n) client-side sort on a tiny list, no N+1.

No violations. Plan is ready for `/speckit.tasks`.
