# Data Model: Change Effort Slider Colours

## Entity: EffortLevel

- **Description**: Discrete user-selected intensity value represented by slider position.
- **Fields**:
  - `value` (integer, required): allowed range 1-10
  - `band` (enum, derived): Easy (1-3), Moderate (4-6), Hard (7-8), All Out (9-10)
- **Validation Rules**:
  - Value MUST be an integer in [1, 10] for mapped colour rendering
  - Out-of-range values MUST use neutral fallback state

## Entity: EffortColourMapping

- **Description**: Canonical mapping from EffortLevel value to display colour.
- **Fields**:
  - `1` → `#22C55E`
  - `2` → `#4ADE80`
  - `3` → `#84CC16`
  - `4` → `#A3E635`
  - `5` → `#EAB308`
  - `6` → `#F59E0B`
  - `7` → `#F97316`
  - `8` → `#EA580C`
  - `9` → `#EF4444`
  - `10` → `#DC2626`
- **Validation Rules**:
  - Every valid EffortLevel value MUST map to exactly one hex colour
  - Mapping MUST be consistent across all in-scope effort sliders

## Entity: SliderVisualState

- **Description**: Current UI representation of an effort slider.
- **Fields**:
  - `isTouched` (boolean): whether user has interacted with slider
  - `selectedValue` (integer | null): current effort value when present
  - `appliedColour` (hex string | null): derived from selected value mapping or null for neutral fallback
- **State Transitions**:
  - `Untouched` → `Touched`: user sets/selects value; mapped colour applied
  - `Touched` → `Untouched`: reset flow clears selected value; neutral fallback shown
  - `Touched` → `Touched` (restore): previously saved value restored; matching colour re-applied
  - `Any` → `Fallback`: invalid/unset value encountered; neutral appearance enforced

## Relationships

- `EffortLevel.value` determines `EffortColourMapping` lookup
- `SliderVisualState.selectedValue` references `EffortLevel.value`
- `SliderVisualState.appliedColour` is derived from `EffortColourMapping` when value is valid
