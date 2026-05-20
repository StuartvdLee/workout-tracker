# Research: Effort Slider Colour Feedback

## Decision 1: Mechanism for applying colour to a range slider

**Decision**: Use the CSS `accent-color` property set via JavaScript inline style on the `<input type="range">` element.

**Rationale**: `accent-color` is the W3C standard way to apply a theme colour to form controls. On `input[type="range"]` it colours the thumb and the filled track in all major modern browsers (Chrome 93+, Firefox 92+, Safari 15.4+). Setting it via `element.style.accentColor = '#267252'` requires no extra DOM elements, no custom track markup, and no third-party library. The property is applied synchronously in the existing `input` event handler alongside the existing `aria-valuenow` and label update calls.

**Alternatives considered**:
- **CSS gradient on the track background** — Achieves a coloured fill using `background: linear-gradient(to right, colour X%, neutral X%)` with a CSS custom property for the percentage. More complex: requires calculating the percentage from the value, applying vendor-prefixed pseudo-element selectors (`::-webkit-slider-runnable-track`, `::-moz-range-track`), and additional CSS to suppress default track appearance. `accent-color` is simpler and covers the same browsers.
- **Third-party range slider library (e.g., `noUiSlider`)** — Full control over markup and styling. Rejected: the project uses vanilla TypeScript and the existing sliders are plain `<input type="range">` elements; introducing a library for a colour change alone is disproportionate. The user confirmed a library is acceptable if needed, but `accent-color` makes it unnecessary.
- **CSS custom property + `::before` overlay** — An overlay div positioned over the track to simulate a coloured fill. Rejected: fragile, depends on exact layout measurements, and accessibility tools may behave unexpectedly.

## Decision 2: Scope of colour application

**Decision**: Apply colour to both (a) the slider control itself (`accent-color`) and (b) the effort band/value display text (`color`).

**Rationale**: Colouring only the slider thumb leaves the text label in the default grey, which diminishes the visual impact. Applying the same effort colour to the band text (e.g., "Easy", "Hard") and the value number creates a cohesive colour coding system that reinforces the effort label. Both elements already update in the existing `input` handler, so the change is additive.

**Alternatives considered**:
- **Slider only** — Simpler, but the text label lacks colour cues.
- **Text only** — The slider stays default; the effort label gains colour. Less impactful than colouring both.

## Decision 3: Default / "not yet touched" state

**Decision**: When `data-touched="false"`, do not apply any `accent-color` or text `color` — leave the slider and labels at their default browser/CSS rendering.

**Rationale**: The existing `data-touched` flag already gate-keeps the "Not rated" label and absent `aria-valuenow`. Keeping the default appearance until first interaction avoids showing a colour (green for value 1) before the user has explicitly chosen anything, which would be misleading.

**Reset**: In `openEffortModal()`, the reset block already clears label text and removes `aria-valuenow`. The colour reset is added to the same block: `slider.style.accentColor = ''` and `valueEl.style.color = ''` / `bandEl.style.color = ''`.

## Decision 4: CSS transition

**Decision**: Add `transition: accent-color 0.15s ease` to both slider classes (`.active-session__effort-slider`, `.effort-modal__slider`) and `transition: color 0.15s ease` to the band/value display elements.

**Rationale**: 0.15 s matches the existing button `background-color` transition. Short enough to feel instant during drag; long enough to prevent harsh snapping. `accent-color` transitions are supported in Chrome 119+ and Firefox 119+. In older browsers the transition is ignored gracefully — the colour still applies, just without animation.

## Decision 5: No backend changes

**Decision**: Zero backend changes.

**Rationale**: The colour palette is a static, statically-defined lookup in the frontend. No new data is stored or transmitted. The effort value (1–10) is already persisted; the colour is derived from it entirely in the browser.

## Browser support summary

| Browser | `accent-color` | `accent-color` transition |
|---------|---------------|--------------------------|
| Chrome 93+ | ✅ thumb + fill | ✅ Chrome 119+ |
| Firefox 92+ | ✅ thumb + fill | ✅ Firefox 119+ |
| Safari 15.4+ | ✅ thumb (fill varies) | partial |
| Edge 93+ | ✅ thumb + fill | ✅ Edge 119+ |

In Safari, the filled track may not reflect `accent-color` as precisely as in Chromium/Firefox. The thumb colour always updates. This is an acceptable limitation; it does not affect any stated functional requirement, all of which describe colour visible on the slider, not specifically the track fill.
