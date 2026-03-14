# Quickstart: Home Landing Page

**Date**: 2026-03-14
**Feature**: 001-home-landing-page

## Prerequisites

- .NET 10 SDK installed
- Node.js (for TypeScript compiler) or `dotnet tool` equivalent
- A modern browser (Chrome, Firefox, Safari, or Edge)

## Project Structure

```
src/
├── WorkoutTracker.sln              # Solution file
├── WorkoutTracker.Web/             # ASP.NET web project (static frontend)
├── WorkoutTracker.AppHost/         # .NET Aspire orchestration
├── WorkoutTracker.Tests/           # Unit and E2E (Playwright) tests
├── WorkoutTracker.ServiceDefaults/ # Shared service configuration
├── WorkoutTracker.Api/             # API project
└── Directory.Build.props           # Shared build settings
```

## Setup

1. Clone the repository and check out the feature branch:
   ```bash
   git clone <repo-url>
   cd workout-tracker
   git checkout 001-home-landing-page
   ```

2. Restore .NET dependencies and build:
   ```bash
   cd src && dotnet build
   ```

3. Install frontend dependencies and compile TypeScript:
   ```bash
   cd src/WorkoutTracker.Web && npm install && npm run build
   ```

4. Install Playwright browsers (required for E2E tests):
   ```bash
   node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium
   ```

## Running the Application

Start the web project directly:
```bash
cd src && dotnet run --project WorkoutTracker.Web
```

Or start the Aspire AppHost, which orchestrates all projects:
```bash
cd src && dotnet run --project WorkoutTracker.AppHost
```

Open the Aspire dashboard (URL shown in terminal output) to find the
Web project endpoint. Navigate to it in your browser.

## Verifying the Feature

### Manual Verification

1. **Title visible**: Confirm "Workout Tracker" appears at the top of
   the page.

2. **Dropdown present**: Confirm a dropdown with placeholder text
   "Select a workout" is displayed. Open it and verify the options:
   Push, Pull, Legs.

3. **Validation error**: Without selecting a workout, click "Start
   Workout". Verify the error message "Please select a workout"
   appears.

4. **Valid selection**: Select "Push" and click "Start Workout". Verify
   no error is displayed.

5. **Error clearance**: After seeing the error, select a workout and
   click "Start Workout". Verify the error disappears.

6. **Mobile layout**: Resize the browser to 375 px wide (or use device
   emulation). Confirm all elements are stacked vertically, fully
   visible, and touch-target sized.

7. **Desktop layout**: Resize to 1024 px or wider. Confirm the layout
   adapts appropriately.

### Automated Verification

Run all tests (unit and E2E):
```bash
cd src && dotnet test
```

Run only unit tests:
```bash
cd src && dotnet test --filter "FullyQualifiedName~Unit"
```

Run only E2E tests (requires Playwright browsers installed):
```bash
cd src && dotnet test --filter "FullyQualifiedName~E2E"
```

## Troubleshooting

- **TypeScript compilation errors**: Run `cd src/WorkoutTracker.Web && npm run build`
  and ensure `tsconfig.json` has `"strict": true` and target is `"ES2022"`.
- **Page not loading**: Check the Aspire dashboard for the correct Web
  project URL and port.
- **Static files not served**: Confirm `app.UseStaticFiles()` is
  called in `src/WorkoutTracker.Web/Program.cs` and that compiled JS is
  in `wwwroot/js/`.
- **Playwright tests failing**: Ensure browsers are installed with
  `node ~/.nuget/packages/microsoft.playwright/1.58.0/.playwright/package/cli.js install chromium`.
