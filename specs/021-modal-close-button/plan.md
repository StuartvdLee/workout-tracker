# Implementation Plan: Modal Close Button

**Branch**: `021-modal-close-button` | **Date**: 2026-05-21 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/021-modal-close-button/spec.md`

## Summary

Add an X close button to the top-right corner of the three edit-style modals (Edit Muscle, Edit Exercise, Edit Workout). The Edit Muscle modal is the only modal in the application with no dismiss-without-action path — it only has Save and Delete. The other two modals already have Cancel buttons; the X button is an additive consistency improvement. The X button is placed last in the modal's DOM order, positioned absolutely in the top-right corner via CSS, and wired to the existing `closeEditModal()` function in each page module. It is disabled while a save request is in progress. No backend, API, or data model changes are required.

## Technical Context

**Language/Version**: TypeScript ~6.0.3 (frontend only); C# / .NET 10 (backend — unaffected)  
**Primary Dependencies**: Vanilla TypeScript — no JS frameworks. CSS `position: absolute` for X button placement.  
**Storage**: N/A — no data model changes  
**Testing**: Playwright E2E (`MusclesPageTests.cs`); existing E2E suite must continue to pass  
**Target Platform**: Web browser (mobile-first, responsive; Chrome 93+, Firefox 92+, Safari 15.4+, Edge 93+)  
**Project Type**: Web application (SPA with Aspire orchestration)  
**Performance Goals**: No measurable change — X button is a static DOM element with one event listener  
**Constraints**: Strict TypeScript (noUnusedLocals, noUnusedParameters, noImplicitReturns); BEM CSS; existing tests must pass; no JS frameworks  
**Scale/Scope**: 4 files modified (muscles.ts, exercises.ts, workouts.ts, styles.css) + 1 E2E test file; no backend changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: All three modal files follow the same `.edit-modal` pattern — one shared CSS rule covers all three. The X button class `.edit-modal__close` follows BEM. The button is wired using the already-imported `closeEditModal()` function; no new abstractions. `position: relative` added to existing `.edit-modal` rule. No `any`, no dead code, no speculative abstraction. ✅

- **Testing**: One new E2E test in `MusclesPageTests.cs`: `EditMuscle_CloseButton_ClosesModalWithoutSaving` — opens the Edit Muscle modal, clicks X, asserts modal is hidden and muscle data unchanged. This is mandatory (P1 fix). Edit Exercise and Edit Workout X buttons call the same `closeEditModal()` function already covered by the existing Cancel E2E tests; adding a duplicate test would be redundant. Tests treated as mandatory. ✅

- **Security**: No new inputs, no API calls, no trust boundaries. The X button is a purely client-side DOM dismiss action. ✅

- **User Experience Consistency**: The X button appears in the same relative position (top-right) and uses the same colour tokens (`var(--color-text)`, `var(--color-bg)`) across all three modals. Dark mode is automatically handled by the existing `[data-theme="dark"]` custom property overrides. The `disabled` attribute during in-flight requests matches the existing Cancel button pattern in delete-confirm modals. The button is keyboard-accessible (Tab-focusable, Enter/Space-activatable) and has `aria-label="Close"`. ✅

- **Performance**: One static button per modal, one `addEventListener('click')`. No network calls, no layout-triggering operations. ✅

## Project Structure

### Documentation (this feature)

```text
specs/021-modal-close-button/
├── plan.md              # This file
├── research.md          # Phase 0 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

*No `data-model.md`, `quickstart.md`, or `contracts/` — pure frontend UI addition with no API surface or data model changes.*

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                              # MODIFIED: add `position: relative` to
    │                                               #   `.edit-modal`; add `.edit-modal__close`
    │                                               #   rule (absolute top-right, ghost icon
    │                                               #   button, hover/focus states, disabled state)
    └── ts/
        └── pages/
            ├── muscles.ts                          # MODIFIED: add X button HTML after </form>
            │                                       #   inside .edit-modal; wire click handler
            │                                       #   → closeEditModal(); disable during
            │                                       #   isEditSubmitting; add to initEventListeners
            ├── exercises.ts                        # MODIFIED: add X button HTML after </form>
            │                                       #   inside .edit-modal; wire click handler
            │                                       #   → closeEditModal(); disable during
            │                                       #   isEditSubmitting
            └── workouts.ts                         # MODIFIED: add X button HTML after </form>
                                                    #   inside .edit-modal; wire click handler
                                                    #   → closeEditModal(); disable during
                                                    #   isEditSubmitting

src/WorkoutTracker.E2ETests/
└── E2E/
    └── MusclesPageTests.cs                         # MODIFIED: add
                                                    #   EditMuscle_CloseButton_ClosesModalWithoutSaving

```

**Structure Decision**: Web application pattern (ASP.NET Core + Aspire + vanilla TypeScript SPA). Consistent with all prior features.

## Complexity Tracking

> No constitution violations.
