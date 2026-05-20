# Implementation Plan: Effort Slider Colour Feedback

**Branch**: `020-effort-slider-colours` | **Date**: 2026-05-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/020-effort-slider-colours/spec.md`

## Summary

Effort sliders (both per-exercise during an active workout and the overall effort modal) must display a colour corresponding to the current effort value (1–10), updating in real time while the user drags. Colour is applied to the slider control and the effort band/value display text. The feature is purely a frontend visual enhancement: a new `getEffortColour` utility function is added to `utils.ts`, both slider `input` event handlers are updated to apply the colour, CSS transitions make changes smooth, and the colour is cleared on slider reset. No backend changes are required.

## Technical Context

**Language/Version**: TypeScript ~6.0.3 (frontend only); C# / .NET 10 (backend — unaffected)
**Primary Dependencies**: Vanilla TypeScript — no JS frameworks. CSS `accent-color` property for slider thumb/fill; CSS `color` property for band/value display; CSS custom properties for state.
**Storage**: N/A — no data model changes
**Testing**: Vitest (frontend unit tests); Playwright E2E (existing suite)
**Target Platform**: Web browser (mobile-first, responsive; Chrome 93+, Firefox 92+, Safari 15.4+, Edge 93+)
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: Colour change must appear instantaneous to the user during drag (no perceptible delay); no new network requests
**Constraints**: Vanilla TypeScript only (strict, noUnusedLocals, noUnusedParameters, noImplicitReturns); BEM CSS naming; existing tests must continue to pass; no JS frameworks; `accent-color` transition capped at 0.15 s to avoid visual lag
**Scale/Scope**: Changes touch 3 files (`utils.ts`, `active-session.ts`, `styles.css`) + 1 test file; no backend, no DB, no API changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: All new code follows the established strict TypeScript rules. `getEffortColour` mirrors the existing `getEffortLabel` pattern in `utils.ts` — same module, same lookup table idiom. CSS follows BEM: colour transitions sit alongside existing `.active-session__effort-slider` and `.effort-modal__slider` rules without new modifier classes. No speculative abstractions, no dead code, no `any`. ✅

- **Testing**:
  - **Vitest unit tests** (`utils.test.ts`): New `describe('getEffortColour')` block — one test per value (1–10) asserting the exact hex colour output; one test for an out-of-range value (asserting empty string). These are mandatory and modelled on the existing `reorder`/`shuffle`/`applyOrder` pattern.
  - **Playwright E2E**: No new E2E tests required — the colour change is a visual enhancement; covered by Vitest at the unit level. Existing E2E tests must continue to pass (regression).
  - **No backend tests needed** — zero backend changes.
  - Tests treated as mandatory. ✅

- **Security**: No new inputs, outputs, or trust boundaries. Colour values are statically defined constants — no user input influences colour selection, eliminating injection risk. ✅

- **User Experience Consistency**: Slider colour follows the same "update on `input` event" pattern used for band label and value text in both slider handlers. The "not touched" neutral state (no `accent-color` applied, default browser rendering) is preserved until first interaction — consistent with the existing `data-touched` pattern. Reset on modal open clears the colour, matching the existing reset of value/band text. The 0.15 s colour transition matches the existing `background-color 0.15s ease` pattern used on buttons. ✅

- **Performance**: `getEffortColour` is a pure lookup — O(1), no side effects. `style.accentColor` and `style.color` are synchronous DOM property sets with no layout reflow. No network calls introduced. Colour updates fire in the existing `input` event handler — no additional event listeners. ✅

## Project Structure

### Documentation (this feature)

```text
specs/020-effort-slider-colours/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── contracts/
│   └── ui-contract.md   # Slider colour states + band/value colour contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                              # MODIFIED: add accent-color transition on
    │                                               #   .active-session__effort-slider and
    │                                               #   .effort-modal__slider; add colour
    │                                               #   transition on .active-session__effort-band,
    │                                               #   .active-session__effort-value,
    │                                               #   .effort-modal__band, .effort-modal__value
    └── ts/
        ├── utils.ts                                # MODIFIED: add EFFORT_COLOURS lookup table
        │                                           #   and export getEffortColour(value) function
        └── pages/
            └── active-session.ts                  # MODIFIED: import getEffortColour; apply
                                                   #   slider accent-color + band/value colour
                                                   #   in per-exercise input handler and in
                                                   #   handleEffortSliderInput(); clear colours
                                                   #   in openEffortModal() reset block

src/WorkoutTracker.Web/wwwroot/ts/__tests__/
└── utils.test.ts                                  # MODIFIED: add describe('getEffortColour')
                                                   #   with 11 test cases (values 1–10 + out-of-range)
```
