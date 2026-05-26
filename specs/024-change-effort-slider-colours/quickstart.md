# Quickstart: Change Effort Slider Colours

## 1. Implement

1. Update the canonical effort colour mapping to the finalized palette:
   - 1 `#22C55E`, 2 `#4ADE80`, 3 `#84CC16`, 4 `#A3E635`, 5 `#EAB308`
   - 6 `#F59E0B`, 7 `#F97316`, 8 `#EA580C`, 9 `#EF4444`, 10 `#DC2626`
2. Ensure all in-scope slider flows consume the same mapping.
3. Preserve neutral fallback behavior for unset/invalid values.
4. Preserve existing interaction semantics and state handling; only colour output changes.

## 2. Verify Locally

From repository root:

```bash
cd src/WorkoutTracker.Web && npm run build && npm test
```

## 3. Manual Smoke Check

1. Open active workout flow and set effort values 1 through 10.
2. Confirm each value renders the expected colour from the contract table.
3. Confirm same value shows same colour across all in-scope sliders.
4. Confirm unset/invalid value conditions still show neutral fallback style.

## 4. SC-002 Verification Protocol (95% <= 100 ms)

1. Run the latency-focused E2E test in `WorkoutHistoryTests`:

```bash
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj --filter "FullyQualifiedName~ActiveSession_EffortColourUpdate_LatencyMeetsBudget"
```

2. Pass condition: at least 19 of 20 sampled interactions apply the mapped colour within 100 ms.
3. Record the run date and result in this section after execution.

### Verification Record

- Date: 2026-05-26
- Result: PASS (`dotnet test ... --filter "FullyQualifiedName~ActiveSession_EffortColourUpdate_LatencyMeetsBudget"`)
- Notes: Full E2E suite also passed (238/238).

## 5. Ready for Task Breakdown

After the checks pass, proceed with:

```text
/speckit.tasks
```
