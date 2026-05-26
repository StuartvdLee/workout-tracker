# Implementation Plan: Change Effort Slider Colours

**Branch**: `024-change-effort-slider-colours` | **Date**: 2026-05-26 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/024-change-effort-slider-colours/spec.md`

## Summary

Update effort sliders to use the finalized 10-step colour palette (`#22C55E` to `#DC2626`) while preserving existing slider behavior, fallback state handling, and consistency across all in-scope workout tracking flows. The implementation follows established patterns from earlier slider-colour and modal-change specs, reusing existing TypeScript event handling and CSS transition patterns without backend or API changes.

## Technical Context

**Language/Version**: TypeScript ~6.0.3 (frontend), C# / .NET 10 (backend unaffected)  
**Primary Dependencies**: Vanilla TypeScript, existing app CSS architecture, DOM range input support (`accent-color`)  
**Storage**: N/A (no schema or persistence format changes)  
**Testing**: Frontend TypeScript build (`npm run build`) + Vitest (`npm test`) + Playwright E2E via `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`  
**Target Platform**: Modern web browsers used by Workout Tracker (desktop + mobile)  
**Project Type**: Web application (ASP.NET Core + vanilla TypeScript frontend)  
**Performance Goals**: Colour changes appear immediate during slider interaction; no added network calls  
**Constraints**: Preserve existing slider UX semantics, strict TypeScript compile settings, no new frameworks  
**Scale/Scope**: Frontend-only change touching slider color mapping behavior and related tests/docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Plan keeps changes localized to existing slider and utility modules, avoids duplicated mapping logic, and follows existing naming/patterns from previous specs 020 and 023. ✅
- **Testing**: Plan requires frontend build + Vitest regression coverage for palette mapping and Playwright E2E coverage for changed user journeys. ✅
- **Security**: No new data ingress/egress or trust boundary change; static in-app colour mapping only. ✅
- **User Experience Consistency**: Reuses existing slider states (`not touched`, `touched`, restored value) and interaction patterns, changing only colour outputs. ✅
- **Performance**: Uses constant-time mapping and existing event handlers; no new async paths or heavy DOM operations. ✅

## Project Structure

### Documentation (this feature)

```text
specs/024-change-effort-slider-colours/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-contract.md
└── tasks.md              # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
├── wwwroot/
│   ├── ts/
│   │   ├── pages/
│   │   │   └── active-session.ts         # primary slider interaction logic
│   │   ├── utils.ts                       # effort value metadata mapping helpers
│   │   └── __tests__/
│   │       └── utils.test.ts              # utility-level colour mapping tests
│   └── css/
│       └── styles.css                     # slider visual treatment
```

**Structure Decision**: Use the existing web frontend structure and extend only the in-place slider utility/event/style files; no new architectural layers required.

## Phase 0: Research Outcomes

Research captured in [research.md](./research.md) resolves implementation choices by reusing proven approaches from `specs/020-effort-slider-colours/plan.md` and `specs/023-unsaved-changes-warning/plan.md`:
- Keep colour derivation centralized in a single mapping source
- Keep state transitions aligned with existing touched/reset behavior
- Preserve lightweight visual updates using existing handlers

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Defines effort-level and slider visual state entities and constraints
- [contracts/ui-contract.md](./contracts/ui-contract.md): Defines user-visible slider colour behavior contract with finalized 10-step palette
- [quickstart.md](./quickstart.md): Implementation and verification flow for this feature

## Post-Design Constitution Check

- **Code Quality**: Design keeps one source of truth for colour mapping and no speculative abstractions. ✅
- **Testing**: Design includes deterministic mapping checks and regression validation in existing frontend test pipeline. ✅
- **Security**: Design remains static/local-only and does not alter auth or sensitive data paths. ✅
- **User Experience Consistency**: Design explicitly preserves existing loading/empty/success/error and fallback patterns while updating colours. ✅
- **Performance**: Design remains O(1) per input update and reuses current rendering flow. ✅

## Complexity Tracking

> No constitution violations or exceptions identified.
