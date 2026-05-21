# Implementation Plan: Fix Effort Modal Outside-Click Behaviour

**Branch**: `022-fix-modal-outside-click` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/022-fix-modal-outside-click/spec.md`

## Summary

Clicking outside the Overall Workout Effort modal (on its backdrop) incorrectly calls `handleEffortSkip()`, which saves the active session with no effort rating. The fix changes the backdrop `click` handler in `active-session.ts` to call `closeEffortModal()` instead — identical to the X button behaviour added in feature 021. A new E2E regression test confirms the corrected behaviour and that the session is not saved after a backdrop dismiss.

## Technical Context

**Language/Version**: TypeScript ~6.0.3 (frontend only); C# / .NET 10 (backend — unaffected)  
**Primary Dependencies**: Vanilla TypeScript — no JS frameworks. No new dependencies.  
**Storage**: N/A — no data model changes  
**Testing**: Playwright E2E (`WorkoutHistoryTests.cs`); existing E2E suite must continue to pass  
**Target Platform**: Web browser (mobile-first, responsive; Chrome 93+, Firefox 92+, Safari 15.4+, Edge 93+)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: No measurable change — a one-line event handler change  
**Constraints**: Strict TypeScript (noUnusedLocals, noUnusedParameters, noImplicitReturns); BEM CSS; existing tests must pass; no JS frameworks  
**Scale/Scope**: 1 TypeScript file modified (`active-session.ts`) + 1 E2E test file (`WorkoutHistoryTests.cs`); no backend, no CSS, no API changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: The fix is a single-line change in `initEffortModal()` — replacing `handleEffortSkip()` with `closeEffortModal()` in the backdrop `click` handler. Both functions already exist and are used in the same file. No new abstractions, no dead code, no `any`. Strict TypeScript constraints are unaffected. ✅

- **Testing**: One new E2E regression test is mandatory (Constitution II): `SaveWorkout_EffortModal_BackdropClick_DismissesWithoutSaving` in `WorkoutHistoryTests.cs`. The test must fail before the fix and pass after it. Pattern mirrors the existing `WorkoutsPageTests.cs` backdrop-click test and the existing `SaveWorkout_EffortModal_CloseButton_DismissesWithoutSaving` test (feature 021). ✅

- **Security**: No new inputs, no API calls, no trust boundaries. The change is a purely client-side DOM dismiss action — no data is transmitted. ✅

- **User Experience Consistency**: The corrected behaviour (backdrop click → close only, no save) matches the X button behaviour introduced in feature 021. This is consistent with all other backdrop-dismiss patterns in the app (discard modal, edit modals). ✅

- **Performance**: No network call is made on backdrop dismiss (was: one POST to save session). The change reduces work on this code path. ✅

## Project Structure

### Documentation (this feature)

```text
specs/022-fix-modal-outside-click/
├── plan.md              # This file
├── research.md          # Phase 0 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

*No `data-model.md`, `quickstart.md`, or `contracts/` — pure one-line frontend bug fix with no API surface or data model changes.*

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    └── ts/
        └── pages/
            └── active-session.ts                  # MODIFIED: in initEffortModal(), change
                                                   #   backdrop click handler from
                                                   #   handleEffortSkip() → closeEffortModal()

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs                     # MODIFIED: add
                                                   #   SaveWorkout_EffortModal_BackdropClick_DismissesWithoutSaving
                                                   #   (click backdrop edge, assert modal hidden,
                                                   #    assert session NOT saved, assert re-open works)
```

**Structure Decision**: Web application pattern (ASP.NET Core + Aspire + vanilla TypeScript SPA). Consistent with all prior features.

## Complexity Tracking

> No constitution violations.
