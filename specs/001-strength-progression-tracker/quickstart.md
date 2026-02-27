# Quickstart: Strength Progression Tracker

## Prerequisites
- .NET 10 SDK
- Node.js 22+ and npm
- PostgreSQL 16+

## 1) Start PostgreSQL
- Create database: `workout_tracker`
- Configure connection string:
  - `DATABASE_URL=Host=localhost;Port=5432;Database=workout_tracker;Username=postgres;Password=postgres`

## 2) Run backend API
```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```
- Expected API base URL: `http://localhost:5000/api`

## 3) Run frontend app
```bash
cd frontend
npm install
npm run dev
```
- Expected app URL: `http://localhost:5173`

## 4) Validate core user stories

### P1: Log session + exercise entry
1. Create a session.
2. Add exercise entry (`Bench Press`, sets/reps/weight).
3. Refresh and verify saved data persists.

### P2: Review history
1. Open exercise history for `Bench Press`.
2. Confirm prior entries are listed with date + sets + reps + weight.

### P3: Progression comparison
1. Add a newer `Bench Press` entry.
2. Open comparison and verify latest-vs-previous and latest-vs-best statuses.

## 5) Run tests

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
- Test run output for backend + frontend + E2E critical journeys
- API contract validation against `specs/001-strength-progression-tracker/contracts/exercise-api.yaml`
- Performance evidence (history load and comparison response timings)
- Screenshots of validation errors and comparison states

## Final Validation Checklist
- `backend`: run `dotnet test`
- `frontend`: run `npm test`
- `e2e`: run `npm test`
- Verify performance workflows in:
  - `scripts/perf/save-entry-metrics.md`
  - `scripts/perf/history-load-metrics.md`
  - `scripts/perf/comparison-metrics.md`
