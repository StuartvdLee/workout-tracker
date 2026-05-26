# UI Contract: Change Effort Slider Colours

## Purpose

Define the user-visible contract for effort slider colour behavior using the finalized 10-level palette.

## Palette Contract (Canonical)

| Effort Value | Band | Colour |
|-------------|------|--------|
| 1 | Easy | `#22C55E` |
| 2 | Easy | `#4ADE80` |
| 3 | Easy | `#84CC16` |
| 4 | Moderate | `#A3E635` |
| 5 | Moderate | `#EAB308` |
| 6 | Moderate | `#F59E0B` |
| 7 | Hard | `#F97316` |
| 8 | Hard | `#EA580C` |
| 9 | All Out | `#EF4444` |
| 10 | All Out | `#DC2626` |

## In-Scope UI Surfaces

The same contract MUST be applied on every effort slider in workout tracking flows, including:
1. Per-exercise effort sliders in active session flows
2. Overall effort slider flows using the same 1-10 effort model

## Interaction Contract

1. **Value change**: when slider value changes to any valid integer 1-10, mapped colour is shown immediately.
2. **Release**: no additional mapping is applied on release; colour remains the latest valid mapped value.
3. **Restoration**: if a previously selected value is restored, mapped colour appears without additional user interaction.
4. **Unset/invalid**: slider shows neutral fallback style (no mapped effort colour).

## Consistency Contract

1. The same effort value MUST render the same colour everywhere this contract applies.
2. No in-scope slider may use an alternate palette for values 1-10.
3. Existing slider behavior (labels, value semantics, save/reset behavior) MUST remain unchanged except colour output.
