# workout-tracker

A fitness application for tracking workouts and exercise progress. Built with C# / .NET 10, vanilla TypeScript, and .NET Aspire.

## Features

- **Exercises** — Create and manage exercises with muscle group tagging
- **Planned Workouts** — Build reusable workout templates from your exercise library (create, edit, delete)
- **Workout Logging** — Start a planned workout and log reps, weight, and notes for each exercise
- **Workout History** — View completed workouts grouped by date with expandable session details

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for TypeScript compilation)
- [Docker](https://www.docker.com/) (required for PostgreSQL via .NET Aspire)

## Getting Started

```bash
cd src
dotnet run --project WorkoutTracker.AppHost
```

The Aspire AppHost will start a PostgreSQL container and apply migrations automatically in development mode.

## Database Migrations

The project uses [Entity Framework Core](https://learn.microsoft.com/ef/core/) with PostgreSQL for data persistence. Migrations are managed via the `dotnet ef` CLI tool.

### Setup

Install the EF Core tools (one-time):

```bash
dotnet tool install --global dotnet-ef
```

### Creating a New Migration

From the `src/` directory:

```bash
dotnet ef migrations add <MigrationName> \
  --project WorkoutTracker.Infrastructure \
  --startup-project WorkoutTracker.Api \
  --output-dir Data/Migrations
```

### Applying Migrations

**In development:** Migrations are applied automatically on startup when running via the Aspire AppHost.

**Manually (against a running database):**

```bash
dotnet ef database update \
  --project WorkoutTracker.Infrastructure \
  --startup-project WorkoutTracker.Api
```

### Reverting a Migration

To revert the last applied migration:

```bash
dotnet ef database update <PreviousMigrationName> \
  --project WorkoutTracker.Infrastructure \
  --startup-project WorkoutTracker.Api
```

To remove the last migration (if not yet applied):

```bash
dotnet ef migrations remove \
  --project WorkoutTracker.Infrastructure \
  --startup-project WorkoutTracker.Api
```

### Database Schema

All tables are created under the `workout_tracker` schema in the PostgreSQL database.