# Implementation Plan: Accessible Exercise Ordering While Scrolling

**Branch**: `stuartvdlee-accessible-exercise-ordering` | **Date**: 2026-11-26 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/034-accessible-exercise-ordering/spec.md`

## Summary

Keep the current workout's order action available while vertically scrolling and replace its text with an accessible reorder icon. Preserve the existing `#session-edit-order` button, its event listener and all reorder state. On desktop, place it in a dedicated full-height side rail with sticky positioning. On mobile, float a prominent fixed button below the top bar so the workout keeps its full width. Add focused real-browser coverage for scroll geometry, accessible operation, modal layering, and the unchanged order-editing flow.

## Technical Context

**Language/Version**: Vanilla TypeScript ~7.0.2 and CSS; C# / .NET 10 for browser tests; no backend changes
**Primary Dependencies**: Existing ASP.NET Core web host, browser native sticky and fixed positioning, Playwright and xUnit
**Storage**: Existing in-memory active workout state and session persistence unchanged
**Testing**: Playwright E2E in `WorkoutTracker.E2ETests`, frontend Vitest and TypeScript build
**Target Platform**: Desktop and mobile browsers; the minimum feature viewport, zoom, theme and input matrix is defined in [spec.md](./spec.md)
**Project Type**: Web application
**Performance Goals**: With 50 exercises, at least 19 of 20 scroll samples render within 100 ms and at least 19 of 20 entries and exits each complete within 1 second; no added network requests or scroll listeners
**Constraints**: Retain current reorder logic, button ID, visibility rules, Done/Cancel behavior and data associations; no new dependencies, APIs or storage; mobile workout controls remain reachable by scrolling, while navigation and dialogs remain above the button
**Scale/Scope**: One active-workout UI, associated styles, focused E2E tests, and a concise README update

## Constitution Check

*Pre-research gate: PASS; re-evaluated after design below.*

- **Code quality**: Reuse the existing button and handler, order state, sortable helper, CSS tokens and BEM naming. Use a desktop rail and mobile overlay, not a duplicate action or scroll-driven JavaScript.
- **Tests**: Add a regression that fails before the change and proves actual viewport visibility and activation without automatic scrolling at top, middle and bottom. Cover accessible name/keyboard, desktop clearance, mobile full width and reachable controls, modal stacking, and existing Done/Cancel behavior in Playwright. Run existing reorder regressions and frontend checks.
- **Security**: Presentation-only change; no new input, data transfer, permissions or dependencies. Existing data rendering and session API remain unchanged.
- **UX**: Reuse theme tokens, native button semantics, a recognizable list-and-arrows icon, visible focus, and the existing 44px touch-target token. Retain all loading, empty, single-item, editing, and dialog behavior.
- **Performance**: Native positioning avoids per-scroll JavaScript and network traffic. Verify the 50-exercise, 19-of-20, 100 ms scroll budget and one-second entry and exit budget as described below.

## Project Structure

### Documentation

```text
specs/034-accessible-exercise-ordering/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code

```text
src/WorkoutTracker.Web/wwwroot/
├── ts/pages/active-session.ts       # Restructure layout; icon-only accessible existing button
└── css/styles.css                   # Desktop rail, mobile floating control, responsive/theme styling
src/WorkoutTracker.E2ETests/E2E/
└── WorkoutReorderTests.cs           # Extend current-workout reorder browser coverage
README.md                            # Update user-facing workout logging feature description
```

**Structure Decision**: Follow feature 030's active-session and reorder-test ownership and feature 033's native CSS positioning and real-browser geometry approach. No new runtime module or API contract is required.

## Phase 0: Research Outcomes

See [research.md](./research.md). Feature 030 established the current reorder state, collapsed view, sortable interactions, and data-preservation requirements; feature 033 demonstrated that native positioning must be validated in a real browser. The current mobile top bar is fixed at `z-index: 80` and `--topbar-height`; modal backdrops use `z-index: 200`. The previous visible-text assertion now checks the accessible button name and icon.

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Existing entities, UI states, transitions, and invariants.
- [contracts/ui-contract.md](./contracts/ui-contract.md): Scroll, semantic, layout, modal, and interaction guarantees.
- [quickstart.md](./quickstart.md): Setup and runnable browser and manual validation.

## Implementation Approach

1. In `active-session.ts`, wrap the existing header, exercise list, errors and footer actions in a content column; place the same `#session-edit-order` button in a sibling rail that stretches for the content column's height. Keep modal backdrops outside this layout. Replace visible text with a decorative, inline list-and-up/down-arrows SVG, and label the button `Edit order` with `aria-label`. Preserve listener attachment and `updateEditOrderButton()`; do not make a second control.
2. In `styles.css`, use a two-column desktop layout with a flexible `min-width: 0` content column and a rail of at least `--min-touch-target`, reserving the rail when hidden. Make the desktop button sticky. Below 768 CSS pixels, restore full-width content and fix a prominent blue button with a white icon, border and shadow below `--topbar-height`. Maintain focus and touch-target conventions; keep it below navigation and modal backdrops.
3. Keep the existing collapsed editor, sortable helper, Done/Cancel and exercise-ID keyed values unchanged. On entry, focus the first reorder handle, or Done when there is no handle.
4. Update the prior visible-text assertion, extend the existing E2E suite, and document the new entry affordance in the README.

## Test Design and Performance Budgets

| Evidence | Target and method |
|---|---|
| Scroll and activation | Seed a 50-exercise workout using the existing API fixture; at 0%, 50%, and 100% of document scroll range, assert the button's bounding rectangle is within the unobscured viewport and hit-test its center; verify desktop controls are not overlapped and mobile controls remain reachable by scrolling. Activate at each position without Playwright auto-scrolling (e.g. `mouse.click` on the previously measured center), assert editor opens, then restore the normal view and repeat. |
| Accessibility and layout | Assert button role and accessible name `Edit order`, decorative SVG, no visible text, keyboard focus indicator and Enter/Space activation. Check the viewport/zoom matrix in [spec.md](./spec.md), both themes, short and long workouts and long names; confirm 44px target and no horizontal clipping. Manual screen-reader and touch checks supplement browser assertions. |
| State and regressions | Test hidden action during loading, blocking workout-load failure, empty workout and editing; test available action for one or more exercises after previous-performance and session-save errors. Verify modal focus and layering, Done preserving order and weight/effort, unchanged Cancel, and changed Cancel with confirm/continue. Retain drag/keyboard reorder tests and verify no session POST on entry/exit. |
| Scroll performance and stability | On a normal local run in a 1024×768 viewport at 100% zoom with 50 exercises, sample 20 evenly spaced offsets across the full scroll range. For each, record elapsed time from setting the scroll position until the second animation frame; treat it as a render proxy, not a direct paint measurement. At least 19 of 20 samples must be ≤100 ms; at every offset assert the action is visible within the unobscured viewport, has nonzero opacity, and its center hit-tests to the button or its descendant. Separately observe continuous manual scrolling for visible flicker; record browser, machine conditions and results. |
| Latency | In a 50-exercise fixture, run 20 enter/exit cycles; measure each from programmatic button activation through the next two animation frames and check that the expected view is present. At least 19 entries and 19 exits must each complete within 1 second under normal local conditions. This is a render proxy, not a direct measurement of visible paint. Record results; do not treat test retries as extra samples. |
| Discoverability | Conduct the spec's usability check: at least 90% of participants independently find and open the action within 10 seconds from a scrolled position. Record participant count, method, and outcome; automated accessibility checks are not a substitute. |
| Overhead | Verify through request observation and source review that opening/scrolling adds zero requests and zero scroll listeners. |

The browser fixture asserts API seed responses for the long workout. Viewport assertions precede coordinate activation so Playwright's ordinary `ClickAsync` cannot mask the scroll defect. The geometry regression was run against the pre-change implementation and then against the implementation; see [tasks.md](./tasks.md) for current verification and remaining checks.

## Post-Design Constitution Check

- **Code quality**: PASS — one semantic button and native layout, no duplicated reorder state or unnecessary abstraction.
- **Tests**: PARTIAL — browser regression, simulated touch and keyboard, data integrity, states, layout and existing flow are covered; actual zoom, screen-reader and human usability checks remain open.
- **Security**: PASS — no changed trust boundary, data handling, or dependencies.
- **UX**: PARTIAL — desktop rail, mobile overlay and modal stacking are covered by browser checks; manual visual and usability checks remain open.
- **Performance**: PARTIAL — automated 50-exercise scroll and editor-transition proxy budgets passed, with zero added scroll handlers or requests; manual continuous-scroll flicker verification remains open.

## Complexity Tracking

No constitution exceptions identified.
