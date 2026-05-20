# Research: Exercises Muscles Link

## SPA Navigation for In-Page Links

**Decision**: Use `import { navigate } from "../router.js"` in `exercises.ts` and attach a `click` event listener to the anchor that calls `navigate('/muscles')` and `event.preventDefault()`.

**Rationale**: The router already exposes `navigate()` as the canonical SPA navigation function. The sidebar uses the same pattern. Using a standard `<a href="/muscles">` without a handler would cause a full page reload; `navigate()` preserves SPA state and avoids flicker.

**Alternatives considered**:
- `window.location.href = '/muscles'` — full reload, inconsistent with the app's SPA pattern. Rejected.
- Programmatic button with `type="button"` — loses the browser's default link behaviour (e.g., right-click → open in new tab). Rejected; `<a>` is semantically correct.
- Global click delegation on `document` — over-engineering for a single targeted link. Rejected.

## CSS for Inline Form Link

**Decision**: Add `.exercise-form__manage-link` with minimal styling (colour inheriting from the brand blue, no underline by default, underline on hover/focus). Placed on the same line as the label using `display: inline` (the default for `<a>`).

**Rationale**: No global `a {}` rule exists in `styles.css`. A scoped BEM modifier keeps the style explicit and avoids unintended side-effects on other anchors that may be added in future.

**Alternatives considered**:
- Using a global `a {}` rule — risky, could affect other future links. Rejected.
- Using an existing button class — semantically wrong for navigation. Rejected.

## Link Placement

**Decision**: The anchor is placed **inside the `<label>` element**, after a dash separator — `Targeted muscles (optional) – <a class="exercise-form__manage-link" href="/muscles">Manage</a>`. Only the word "Manage" is wrapped in the anchor, not the full label text.

**Rationale**: Placing the link inside the label keeps the label text and link on the same visual line without needing extra CSS flex/layout rules. Because only "Manage" is the anchor target (not the whole label text), clicking the non-link part of the label still behaves normally. The dash provides a clear visual separation that reads naturally: "Targeted muscles (optional) – Manage". A sibling element after the label would require additional layout work to appear inline with it.

**Alternatives considered**:
- Sibling element after `<label>` — requires explicit CSS to appear on the same line; slightly more markup for no benefit. Rejected in favour of inline placement.
- After the muscle toggles container — too far from the label, breaks the visual association. Rejected.
