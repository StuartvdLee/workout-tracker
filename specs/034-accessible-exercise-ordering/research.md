# Research: Accessible Exercise Ordering While Scrolling

## Persistent placement

**Decision**: Use a dedicated sticky side rail on desktop and a fixed floating button over full-width workout content on mobile, using native CSS.

**Rationale**: The current action is in a header that scrolls out of view. A full-height desktop rail provides a sticky containing boundary without covering controls. On mobile, reserving rail space shifts and narrows the workout, so the user requested a prominent floating control instead; workout controls must remain reachable by scrolling. The fixed top bar requires a responsive top offset, and navigation and modal stacking must remain above the action.

**Alternatives considered**: A mobile rail narrows content; a sticky full-width header can cover scrolling content; JavaScript scroll listeners add unnecessary state and work. A sticky button inside the existing short header would stop sticking at the header boundary.

## Icon and accessible operation

**Decision**: Use one native button with `aria-label="Edit order"` and a decorative list-and-up/down-arrows inline SVG; retain the existing button ID and focus styling.

**Rationale**: The app already uses inline decorative SVGs in accessible icon buttons, and the list-plus-arrows metaphor communicates ordering better than the existing drag-handle dots alone. Native semantics provide keyboard activation; the 44px token provides a usable target.

**Alternatives considered**: Visible text contradicts the request; an unlabeled icon is inaccessible; a duplicate floating button introduces competing focus targets and state synchronization.

## Existing workflow and data

**Decision**: Reuse `toggleOrderEditing`, `updateEditOrderButton`, the shared sortable helper, and existing Done/Cancel and modal flows without changing session persistence.

**Rationale**: Feature 030 and current code already implement the required order editor and associate logged values with exercise IDs. Changing only placement and presentation minimizes risk.

**Alternatives considered**: A new reorder mode or endpoint would duplicate working behavior and expand scope.

## Verification

**Decision**: Extend `WorkoutReorderTests.cs` using real browser geometry, hit testing, accessible semantics, and existing flow tests; supplement with manual screen-reader, touch, themes, and usability checks.

**Rationale**: Feature 033 established real-browser testing for sticky layout; jsdom cannot prove layout or occlusion. Normal Playwright clicks may auto-scroll, so the scroll test measures the viewport and clicks its coordinates before the browser can mask a regression. The prior visible-text assertion was replaced with accessible-name and icon checks. Manual verification remains tracked in [tasks.md](./tasks.md).

**Alternatives considered**: A CSS-only assertion or simulated DOM test cannot prove actual scrolling or an unobstructed hit target. Manual-only testing does not satisfy the constitution.
