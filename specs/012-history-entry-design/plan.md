# Implementation Plan: History Page Entry Redesign

**Branch**: `012-history-entry-design` | **Date**: 2026-05-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/012-history-entry-design/spec.md`

## Summary

Remove the relative date group headers ("Today", "Yesterday", "X days ago") from the History page and replace them with a flat list of sessions. Each entry displays the workout name as bold primary text and the full date (with time) as a muted secondary line directly beneath it. This is a **pure frontend change**: no backend, API, or database modifications are required. The expand/collapse behaviour and exercise details view are preserved unchanged.

## Technical Context

**Language/Version**: TypeScript 6.0.3 (frontend — all changes); C# on .NET 10.0 (backend — no changes)
**Primary Dependencies**: Vanilla TypeScript; `Intl.DateTimeFormat` Web API for locale-aware date formatting
**Storage**: N/A — no database or localStorage changes
**Testing**: Vitest frontend unit tests; xUnit + Playwright E2E tests (`WorkoutHistoryTests.cs`)
**Target Platform**: Web browser — all modern browsers supporting CSS custom properties and `Intl.DateTimeFormat`
**Project Type**: Web application (SPA with Aspire orchestration)
**Performance Goals**: Rendering is synchronous DOM manipulation — no measurable budget change from current implementation
**Constraints**: No external JS/CSS frameworks; vanilla TypeScript only; existing E2E tests must pass (one existing test asserting the old group header must be replaced)
**Scale/Scope**: Source changes touch 3 files (`history.ts`, `styles.css`, `WorkoutHistoryTests.cs`); additional specification files are added under `specs/012-history-entry-design/`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality**: TypeScript strict mode enforced via `tsconfig.json` (`strict: true`, `noUnusedLocals`, `noUnusedParameters`, `noImplicitReturns`). CSS follows BEM naming — the new `.history-session__date` class follows the established `.history-session__*` block pattern. `getDateLabel()` and the grouping logic in `renderSessions()` are removed entirely; `renderSession()` is updated to include a `.history-session__date` element inside a new `.history-session__info` wrapper. No speculative abstractions or dead code introduced. ✅

- **Testing**:
  - **E2E (Playwright / xUnit)**: The existing `HistoryPage_DateGrouping_ShowsToday` test asserts the old `.history-group__date-label` class — this MUST be replaced by two new tests: (a) verifying no `.history-group__date-label` exists, and (b) verifying each `.history-session` contains a visible `.history-session__date` element with non-empty text. All other existing E2E tests in `WorkoutHistoryTests.cs` use selectors unaffected by this change and MUST continue to pass.
  - **Frontend Vitest**: No new Vitest unit tests required — the `formatDate()` helper is a thin `Intl.DateTimeFormat` wrapper, consistent with the page-rendering convention from features 001–009. The existing `router.test.ts` tests must continue to pass.
  - Tests treated as mandatory, not optional. ✅

- **Security**: Workout names and dates rendered to the DOM MUST continue to use the existing `escapeHtml()` utility. No new user inputs, API endpoints, or trust boundaries introduced. ✅

- **User Experience Consistency**: `.history-session__date` uses `var(--color-text-light)` and `var(--font-size-sm)` — the same tokens used for muted secondary labels throughout the app. Date format: `Intl.DateTimeFormat` with `{ day: 'numeric', month: 'long', year: 'numeric' }` (e.g., "10 May 2026") combined with the existing `formatTime()` output via " · " separator. Loading, empty, and error states are unchanged. ✅

- **Performance**: Removing the grouping loop simplifies rendering. `Intl.DateTimeFormat` is a browser-native API with negligible overhead. No new network requests introduced. ✅

## Project Structure

### Documentation (this feature)

```text
specs/012-history-entry-design/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── ui-contract.md   # History entry HTML/CSS/ARIA contract
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/WorkoutTracker.Web/
└── wwwroot/
    ├── css/
    │   └── styles.css                          # MODIFIED: remove .history-group / .history-group__date-label;
    │                                           #           add .history-session__info, .history-session__date;
    │                                           #           remove .history-session__time
    └── ts/
        └── pages/
            └── history.ts                      # MODIFIED: remove getDateLabel() + grouping logic;
                                                #           add formatDate(); update renderSessions() + renderSession()

src/WorkoutTracker.E2ETests/
└── E2E/
    └── WorkoutHistoryTests.cs                  # MODIFIED: replace HistoryPage_DateGrouping_ShowsToday;
                                                #           add HistoryPage_NoGroupHeaders_FlatList
                                                #           and HistoryPage_EntryShowsDateBelowName

src/WorkoutTracker.Api/                         # UNCHANGED
src/WorkoutTracker.Infrastructure/              # UNCHANGED
src/WorkoutTracker.UnitTests/                   # UNCHANGED
```

**Structure Decision**: Existing .NET Aspire solution structure preserved. No new projects. Source code changes are surgical modifications to three existing files, with feature documentation added under `specs/012-history-entry-design/`.

## Complexity Tracking

> No constitution violations — no entries required.

## Implementation Phases

### Phase 0: Research & Clarification

**Output**: `research.md` ✅ Complete

**Key findings**:
1. **No backend changes needed** — `workoutName` and `completedAt` are already present in the `WorkoutSession` objects returned by `GET /api/sessions`.
2. **`Intl.DateTimeFormat`** is the correct browser-native API; consistent with the existing `formatTime()` usage in `history.ts`.
3. **Date format**: `{ day: 'numeric', month: 'long', year: 'numeric' }` → "10 May 2026". Combined with `formatTime()` via " · " → "10 May 2026 · 2:30 PM".
4. **E2E test**: `HistoryPage_DateGrouping_ShowsToday` must be replaced — the `.history-group__date-label` CSS class is deleted, making the existing assertion permanently invalid.
5. **CSS delta**: Remove `.history-group`, `.history-group__date-label`, `.history-session__time`. Add `.history-session__info`, `.history-session__date`.

### Phase 1: Design & Contracts

**Output**: All complete ✅

- `research.md` — all unknowns resolved
- `data-model.md` — no schema changes; documents visual state model for a history entry
- `contracts/ui-contract.md` — history session entry HTML/CSS/ARIA specification
- `quickstart.md` — user-facing walkthrough of the redesigned History page

**Constitution Check (post-design)**: No backend changes, no migrations, no new API surfaces. ARIA attributes preserved. No constitution violations.

### Phase 2: Implementation (Dependency Graph)

**Prerequisites**: Phase 1 complete ✅

**Workstream A: `history.ts`**

1. **Remove** `getDateLabel()` function entirely.
2. **Add** `formatDate()`:
   ```typescript
   function formatDate(isoDate: string): string {
     const datePart = new Intl.DateTimeFormat("en-GB", {
       day: "numeric",
       month: "long",
       year: "numeric",
     }).format(new Date(isoDate));
     return `${datePart} · ${formatTime(isoDate)}`;
   }
   ```
3. **Replace** `renderSessions()` grouping logic with a flat map:
   ```typescript
   function renderSessions(sessions: WorkoutSession[], container: HTMLElement): void {
     container.innerHTML = sessions.map((s) => renderSession(s)).join("");
     container.querySelectorAll<HTMLButtonElement>(".history-session__header").forEach((btn) => {
       btn.addEventListener("click", () => { toggleSession(btn); });
     });
   }
   ```
4. **Update** `renderSession()`:
   - Wrap workout name and new date span in `<div class="history-session__info">`:
     ```html
     <div class="history-session__info">
       <span class="history-session__workout-name">{workoutName}</span>
       <span class="history-session__date">{formatDate(completedAt)}</span>
     </div>
     ```
   - Remove `<span class="history-session__time">`.
   - Keep `<span class="history-session__exercise-count">` and `<span class="history-session__toggle">` unchanged.

**Workstream B: `styles.css`**

1. **Remove** `.history-group { … }` and `.history-group__date-label { … }`.
2. **Remove** `.history-session__time { … }`.
3. **Reset** `.history-session__header` to behave as a transparent, full-width button:
   ```css
   .history-session__header {
     /* … existing layout properties … */
     background-color: transparent;
     border: none;
     width: 100%;
     text-align: left;
   }
   ```
4. **Add** after `.history-session__workout-name`:
   ```css
   .history-session__info {
     flex: 1;
     min-width: 0;
     display: flex;
     flex-direction: column;
     gap: 2px;
   }

   .history-session__date {
     font-size: var(--font-size-sm);
     color: var(--color-text-light);
   }
   ```
5. **Update** `.history-session__workout-name`: remove `flex: 1; min-width: 0` (now owned by `.history-session__info`); add explicit `font-size: var(--font-size-base)` to ensure the name is visually larger than the muted date.

**Workstream C: `WorkoutHistoryTests.cs`**

1. **Remove** `HistoryPage_DateGrouping_ShowsToday` test.
2. **Add** `HistoryPage_NoGroupHeaders_FlatList`:
   ```csharp
   [Fact]
   public async Task HistoryPage_NoGroupHeaders_FlatList()
   {
       var page = await CreatePageAsync();
       try
       {
           await CreateWorkoutAndSessionViaApiAsync(page);
           await NavigateToHistoryAsync(page);
           await Expect(page.Locator(".history-session")).ToBeVisibleAsync();
           await Expect(page.Locator(".history-group__date-label")).ToHaveCountAsync(0);
       }
       finally { await page.CloseAsync(); }
   }
   ```
3. **Add** `HistoryPage_EntryShowsDateBelowName`:
   ```csharp
   [Fact]
   public async Task HistoryPage_EntryShowsDateBelowName()
   {
       var page = await CreatePageAsync();
       try
       {
           await CreateWorkoutAndSessionViaApiAsync(page);
           await NavigateToHistoryAsync(page);
           var dateEl = page.Locator(".history-session__date").First;
           await Expect(dateEl).ToBeVisibleAsync();
           await Expect(dateEl).ToContainTextAsync(new Regex(".+"));
       }
       finally { await page.CloseAsync(); }
   }
   ```
