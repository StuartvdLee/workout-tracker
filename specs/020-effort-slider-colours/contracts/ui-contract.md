# UI Contract: Effort Slider Colour Feedback

## Colour Palette

The effort colour lookup is the single source of truth for all effort slider colouring across the application.

| Value | Label    | Hex       |
|-------|----------|-----------|
| 1     | Easy     | `#267252` |
| 2     | Easy     | `#127368` |
| 3     | Easy     | `#0E6577` |
| 4     | Moderate | `#356089` |
| 5     | Moderate | `#2E3C80` |
| 6     | Moderate | `#4C3D8A` |
| 7     | Hard     | `#68448C` |
| 8     | Hard     | `#71398B` |
| 9     | All Out  | `#8A417D` |
| 10    | All Out  | `#8A3666` |

## Slider Instances

Two slider instances exist in the application. Both MUST implement identical colour behaviour.

### 1. Per-exercise effort slider (active workout)

- **Element**: `<input class="active-session__effort-slider" type="range" id="effort-{exerciseId}">`
- **Value display**: `<span class="active-session__effort-value" id="effort-value-{exerciseId}">`
- **Band display**: `<span class="active-session__effort-band" id="effort-band-{exerciseId}">`

### 2. Overall effort modal slider

- **Element**: `<input class="effort-modal__slider" type="range" id="overall-effort-slider">`
- **Value display**: `<span class="effort-modal__value" id="overall-effort-value">`
- **Band display**: `<span class="effort-modal__band" id="overall-effort-band">`

## Colour State Machine

```
State: NOT_TOUCHED
  - data-touched = "false"
  - slider accent-color: (unset — browser default)
  - value display color: (unset — var(--color-text))
  - band display color: (unset — var(--color-text-secondary))
  - trigger → user interacts with slider → State: TOUCHED

State: TOUCHED
  - data-touched = "true"
  - slider accent-color: EFFORT_COLOUR[value]
  - value display color: EFFORT_COLOUR[value]
  - band display color: EFFORT_COLOUR[value]
  - trigger (overall modal only) → openEffortModal() → State: NOT_TOUCHED
  - trigger (per-exercise only) → renderExerciseInputs() rebuilds DOM → State: NOT_TOUCHED
    (unless previousLogEntries has a saved value, in which case → State: TOUCHED at restored value)
```

## Behaviour Contract

| Event                                    | Slider `accent-color`         | Value display `color`         | Band display `color`          |
|------------------------------------------|-------------------------------|-------------------------------|-------------------------------|
| Re-render with pre-existing effort value | `EFFORT_COLOUR[value]`        | `EFFORT_COLOUR[value]`        | `EFFORT_COLOUR[value]`        |
| User drags slider (input)                | `EFFORT_COLOUR[current]`      | `EFFORT_COLOUR[current]`      | `EFFORT_COLOUR[current]`      |
| User releases slider                     | No additional change required | No additional change required | No additional change required |
| Modal reset (not touched)                | `""` (cleared)                | `""` (cleared)                | `""` (cleared)                |
| Value not yet selected                   | `""` (unset)                  | `""` (unset)                  | `""` (unset)                  |

> **Per-exercise slider reset**: Per-exercise sliders are torn down and rebuilt by `renderExerciseInputs()` on each call. No explicit colour-clearing is needed — the DOM element is recreated from scratch with default (unset) inline styles, then optionally restored from `previousLogEntries` if a prior value exists. Only the overall effort modal slider uses an explicit `style.accentColor = ""` clear in `openEffortModal()`.

## CSS Transition

Both slider classes MUST include `transition: accent-color 0.15s ease`. Band and value display elements MUST include `transition: color 0.15s ease`. This matches the existing `background-color 0.15s ease` transition on interactive elements.

## `getEffortColour` Function Signature

```typescript
// Added to utils.ts, exported alongside getEffortLabel
export function getEffortColour(value: number): string
// Returns the hex colour string for values 1–10, empty string for any other value.
```

Example:
```typescript
getEffortColour(1)   // "#267252"
getEffortColour(5)   // "#2E3C80"
getEffortColour(10)  // "#8A3666"
getEffortColour(0)   // ""
getEffortColour(11)  // ""
```
