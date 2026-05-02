# stark-ray Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-05-02

## Active Technologies
- PostgreSQL via EF Core — adding one nullable integer column (`effort`) to the existing `logged_exercise` table (005-active-workout-effort)
- C# on .NET 10.0 (backend — no changes), TypeScript 5.9.3 (frontend — primary change) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks or libraries) (006-reorder-exercises)
- PostgreSQL via EF Core — no schema changes; `planned_workout_exercise.sequence` already exists (006-reorder-exercises)

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
- 006-reorder-exercises: Added C# on .NET 10.0 (backend — no changes), TypeScript 5.9.3 (frontend — primary change) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks or libraries)
- 006-reorder-exercises: Added [if applicable, e.g., PostgreSQL, CoreData, files or N/A]
- 005-active-workout-effort: Added C# on .NET 10.0 (backend), TypeScript 5.9.3 (frontend) + ASP.NET Core minimal API, .NET Aspire 13.1.2, Entity Framework Core with Npgsql, vanilla TypeScript (no JS frameworks)


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
