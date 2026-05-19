# Quickstart: Targeted Muscles Page

**Feature**: `017-targeted-muscles-page`

---

## Prerequisites

- .NET 10 SDK
- Node.js (for TypeScript build and frontend tests)
- PostgreSQL (for integration tests via `TEST_DB_CONNECTION`)
- .NET Aspire workload (`dotnet workload install aspire`)

---

## Run the App

```bash
# Start the full Aspire stack (API + Web + Postgres)
cd src/WorkoutTracker.AppHost
dotnet run
```

Navigate to the Web URL shown in terminal output. Click "Targeted Muscles" in the sidebar.

---

## Build

```bash
# Full solution build (includes TypeScript compilation)
dotnet build src/WorkoutTracker.slnx

# TypeScript only
cd src/WorkoutTracker.Web
npm run build
```

---

## Run Tests

```bash
# Backend integration tests (requires PostgreSQL)
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj

# Frontend unit tests
cd src/WorkoutTracker.Web
npm test

# E2E tests (requires app to be running)
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```

---

## Key Files for This Feature

| File | Purpose |
|------|---------|
| `src/WorkoutTracker.Api/Program.cs` | PATCH + DELETE endpoints for muscles |
| `src/WorkoutTracker.Web/Program.cs` | Proxy routes for PATCH + DELETE |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/muscles.ts` | New Targeted Muscles page |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` | Remove add-muscle form |
| `src/WorkoutTracker.Web/wwwroot/ts/main.ts` | Register /muscles route |
| `src/WorkoutTracker.Web/wwwroot/index.html` | Add sidebar nav link |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | New muscles page styles |
| `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs` | API tests for PATCH + DELETE |
| `src/WorkoutTracker.E2ETests/E2E/MusclesPageTests.cs` | E2E tests for the new page |

---

## API Quick Reference

| Method   | Path                          | Description             |
|----------|-------------------------------|-------------------------|
| GET      | /api/muscles                  | List all muscles (alphabetical) |
| POST     | /api/muscles                  | Create a new muscle     |
| PATCH    | /api/muscles/{muscleId}       | Rename a muscle         |
| DELETE   | /api/muscles/{muscleId}       | Delete a muscle         |
