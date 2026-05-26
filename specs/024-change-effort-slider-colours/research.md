# Research: Change Effort Slider Colours

## Context

This feature intentionally reuses decisions and patterns already validated in prior planning artifacts:
- `specs/020-effort-slider-colours/plan.md`
- `specs/020-effort-slider-colours/research.md`
- `specs/023-unsaved-changes-warning/plan.md`

## Decision 1: Keep a single source of truth for colour mapping

**Decision**: Define and consume one canonical effort-value-to-colour map for values 1-10 using the newly clarified palette.

**Rationale**: Prior spec 020 established that a centralized mapping avoids drift between slider instances and keeps testing straightforward. This feature only changes the palette values, not the pattern.

**Alternatives considered**:
- **Inline colour literals per slider handler**: rejected due to duplication and mismatch risk.
- **Server-driven colour configuration**: rejected because scope is frontend-only and no runtime configurability is required.

## Decision 2: Preserve existing slider interaction model

**Decision**: Continue updating visual state from existing slider interaction paths (input/change/restoration) instead of introducing new state orchestration.

**Rationale**: Existing behavior already handles touched/not-touched states and restoration; this feature request is colour replacement, so preserving event flow minimizes risk and rework.

**Alternatives considered**:
- **Add new slider state abstraction layer**: rejected as unnecessary complexity for a palette swap.
- **Delayed colour updates on release only**: rejected because existing UX and spec success criteria target immediate feedback.

## Decision 3: Retain neutral fallback semantics

**Decision**: Keep neutral/default appearance for unset or invalid effort values.

**Rationale**: This aligns with current user expectations and was already captured in spec edge cases and requirements. It also prevents misleading colour signals for unselected values.

**Alternatives considered**:
- **Force value 1 colour on unset**: rejected because it incorrectly implies explicit user choice.
- **Use error colour for invalid values**: rejected because invalid values are treated as fallback-safe, not user-facing failure.

## Decision 4: Frontend-only implementation

**Decision**: No backend/API/data schema updates.

**Rationale**: Effort value persistence already exists and colours are derived presentation state. Prior specs confirm this path is sufficient and lower risk.

**Alternatives considered**:
- **Persist colour with saved effort entries**: rejected as redundant derived data.
- **Expose palette via API**: rejected as unnecessary for fixed feature scope.
