# workout-tracker

A fitness application for tracking workouts and exercise progress. Built with C# / .NET 10, vanilla TypeScript, and .NET Aspire.

## Features

- **Exercises** — Create and manage exercises with muscle group tagging
- **Planned Workouts** — Build reusable workout templates from your exercise library (create, edit, delete)
- **Workout Logging** — Select 3 or 5 sets before starting a planned workout, log weight and effort, and change exercise order from anywhere while scrolling
- **Workout Recovery** — Progress is saved in the same browser profile and restored after a refresh. Use Resume workout to continue, or explicitly discard it; retry if storage or completion fails.
- **Previous Performance** — See weight, effort, and sets from each exercise's latest comparable session while training
- **Workout History** — View completed workouts with current and previous weight, sets, and effort, and edit the session-wide sets value
- **Workout Stats Graph** — Chart a workout's history by overall session effort or by individual exercise, with each session's sets shown as background bars behind the weight and effort lines

Sessions created before sets tracking was introduced remain supported. Their sets values are shown with the standard no-data marker in history, omitted from active-workout previous-performance summaries, and left without a bar in the stats graph.

Workout drafts stay on the device and require browser storage and Web Locks. A completion interrupted by a lost response must be retried with the saved draft; it is not duplicated. Completion receipts and rejection fences remain in the database even if the corresponding history entry is deleted.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for TypeScript compilation)
- [Docker](https://www.docker.com/) (required for PostgreSQL via .NET Aspire)

## Getting Started

### Local secrets (first-time setup)

The AppHost requires a persistent PostgreSQL password stored in user secrets. Run this once from the `src/WorkoutTracker.AppHost` directory:

```bash
dotnet user-secrets set "Parameters:postgresPassword" "<your-dev-password>"
```

The password is stored locally and never committed to the repo.

Completion requests are limited to 1 MiB, 1,000 exercises and 1,000 characters per note. Retained receipts and rejection fences grow over time, so monitor database storage. At most 100 distinct rejection fences are recorded per operation and 10,000 in total; once a limit is reached, new invalid attempts remain locked until capacity is increased, while valid corrections can still complete. Do not delete fences to recover capacity.

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

The workout-completion fingerprint transition is forward-only: rolling it back could bypass retained rejection fences.

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