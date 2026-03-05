# Quickstart: Simplified Homepage Session Start

## Prerequisites
- .NET 10 SDK
- Node.js 22+ and npm
- PostgreSQL 16+

## 1) Start backend API
```bash
cd backend
dotnet restore src/Api/Api.csproj
dotnet ef database update --project src/Api/Api.csproj --startup-project src/Api/Api.csproj
dotnet run --project src/Api/Api.csproj
```

## 2) Start frontend app
```bash
cd frontend
npm install
npm run dev
```

## 3) Validate P1: Minimal homepage session start
1. Open homepage.
2. Confirm visible elements are only:
   - Title: `Workout Tracker`
   - Dropdown label: `Workout Type`
   - Options: `Push`, `Pull`, `Legs`
   - Button: `Start Session`
3. Select `Push` and press `Start Session`.
4. Confirm a session is created and includes workout type + started timestamp.

## 4) Validate P2: Required workout type
1. Open homepage and do not select a workout type.
2. Press `Start Session`.
3. Confirm a clear error prompts selection of workout type.
4. Select `Legs` and confirm error clears.

## 5) Validate P3: Legacy content removal
1. Open homepage.
2. Confirm links `Session`, `History`, and `Progression` are not shown.
3. Confirm `Add Exercise Entry` section and all content under it are not shown.

## 6) Run automated tests

### Backend
```bash
cd backend
dotnet test
```

### Frontend
```bash
cd frontend
npm test
```

### End-to-end
```bash
cd e2e
npm install
npx playwright install chromium
npm test
```

## Evidence required for PR review
- Backend integration test output for session creation validation/timestamp behavior.
- Frontend integration test output for required dropdown validation and simplified homepage rendering.
- E2E output proving end-to-end start-session flow works from homepage.
- Screenshots of homepage default state and missing-workout-type error state.
- Timing evidence for homepage render and successful start-session latency.
