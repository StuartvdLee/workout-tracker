# Implementation Plan: Sidebar History Icon Change

**Branch**: `028-sidebar-history-icon` | **Date**: 2026-05-27 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/028-sidebar-history-icon/spec.md`

## Summary

Replace the SVG paths for the "History" sidebar icon in `index.html` with the Lucide `history` icon (circular back-arrow with clock hands), preserving all existing SVG attributes, CSS classes, and accessibility markup. No TypeScript, CSS, backend, or data changes are required.

## Technical Context

**Language/Version**: HTML (static template); TypeScript ~6.0.3 frontend unaffected  
**Primary Dependencies**: Inline SVG from Lucide (paths inlined; no library added)  
**Storage**: N/A  
**Testing**: `cd src/WorkoutTracker.Web && npm run build && npm test`; Playwright E2E via `dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj`  
**Target Platform**: Modern web browsers (desktop + mobile)  
**Project Type**: Web application (ASP.NET Core + vanilla TypeScript frontend)  
**Performance Goals**: Static SVG swap; zero additional network requests or render cost  
**Constraints**: Preserve `sidebar__icon` class, `width="20"`, `height="20"`, `viewBox="0 0 24 24"`, `stroke="currentColor"`, `stroke-width="2"`, `stroke-linecap="round"`, `stroke-linejoin="round"`, and `aria-hidden="true"` on the icon  
**Scale/Scope**: Single-file HTML change; one SVG block replaced

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: Change is surgical (one SVG block replacement in one file); no duplication, no speculative additions, no dead code. ✅
- **Testing**: No behavioral logic changes; existing automated test suite validates nothing is broken. Manual visual smoke check covers the visual outcome. ✅
- **Security**: Purely presentational static markup; no inputs, no auth/authz changes, no trust boundary impact. ✅
- **User Experience Consistency**: Icon reuses the same SVG attribute conventions as all other sidebar icons (stroke, currentColor, viewBox, aria-hidden). ✅
- **Performance**: Inline SVG replaces inline SVG; no new network requests, no measurable render difference. ✅

## Project Structure

### Documentation (this feature)

```text
specs/028-sidebar-history-icon/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── ui-contract.md   # Phase 1 output
└── tasks.md             # Created later by /speckit.tasks
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    └── index.html       # Only file changed — one SVG block replaced
```

**Structure Decision**: Single-file change within the existing web frontend; no new files, modules, or architectural layers required.

## Phase 0: Research Outcomes

Research captured in [research.md](./research.md).

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Defines the icon identity entity and its SVG contract
- [contracts/ui-contract.md](./contracts/ui-contract.md): Canonical SVG paths and attribute requirements for the icon
- [quickstart.md](./quickstart.md): Implementation steps and verification flow

## Post-Design Constitution Check

- **Code Quality**: Single-file replacement; SVG attribute conventions preserved identically across all sidebar icons. ✅
- **Testing**: No logic changes; existing frontend build + Vitest + E2E suite confirms nothing is regressed; manual smoke check confirms visual correctness. ✅
- **Security**: No security-relevant changes. ✅
- **User Experience Consistency**: New icon uses identical attribute set to all other sidebar icons; theme and accessibility behaviour unchanged. ✅
- **Performance**: SVG path complexity is comparable to existing icons; no measurable impact. ✅

## Complexity Tracking

> No constitution violations or exceptions identified.
