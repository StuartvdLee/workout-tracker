# stark-ray Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-05-18

## Active Technologies
- PostgreSQL via EF Core — adding one nullable integer column (`effort`) to the existing `logged_exercise` table (005-active-workout-effort)
- C# on .NET 10.0 (backend — no changes), TypeScript 5.9.3 (frontend — primary change) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks or libraries) (006-reorder-exercises)
- PostgreSQL via EF Core — no schema changes; `planned_workout_exercise.sequence` already exists (006-reorder-exercises)
- TypeScript 5.9.3 (frontend — primary change); C# on .NET 10 (backend — no changes) + Vanilla TypeScript (no JS frameworks or libraries); `window.matchMedia` Web API for OS preference detection; `localStorage` for preference persistence (007-dark-mode-toggle)
- `localStorage` key `workout-tracker-theme` with values `'light' | 'dark' | 'system'` — no database changes (007-dark-mode-toggle)
- PostgreSQL via EF Core — no schema changes; all required data is in the existing `workout_session` and `logged_exercise` tables (008-workout-exercise-history)
- PostgreSQL via EF Core — no schema changes; all required data is in the existing `workout_session` table (009-last-workout-hint)
- PostgreSQL via EF Core — one new nullable `int` column on `logged_exercise`; migration required (010-randomize-exercise-order)
- No changes — no schema changes, no migration required (011-randomise-exercise-order)
- TypeScript 5.9.3 (frontend — all changes); C# on .NET 10.0 (backend — no changes) + Vanilla TypeScript; `Intl.DateTimeFormat` Web API for locale-aware date formatting (012-history-entry-design)
- N/A — no database or localStorage changes (012-history-entry-design)
- PostgreSQL via EF Core — no schema changes; `Sequence` already exists on `logged_exercise` (013-show-exercise-order)

- C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks) (003-add-exercises)

## Project Structure

```text
src/
tests/
```

## Commands

npm test && npm run lint

## Code Style

C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend): Follow standard conventions

## Recent Changes
- 013-show-exercise-order: Added C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)
- 012-history-entry-design: Added TypeScript 5.9.3 (frontend — all changes); C# on .NET 10.0 (backend — no changes) + Vanilla TypeScript; `Intl.DateTimeFormat` Web API for locale-aware date formatting
- 011-randomise-exercise-order: Added C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
