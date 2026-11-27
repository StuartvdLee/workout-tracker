# Implementation Plan: Resume an In-Progress Workout After a Refresh

**Branch**: `stuartvdlee-resume-workout-refresh` | **Date**: 2026-11-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/035-resume-active-workout/spec.md`
**Status**: Implemented locally; validation evidence and deployment observations are in [quickstart.md](./quickstart.md).

## Summary

The delivered feature persists a versioned active-workout draft in browser storage, restores its workout, sets, entered values and committed exercise order on refresh, and reports storage failures without discarding in-memory input. An exclusive browser lock coordinates tabs; the draft ID identifies a frozen completion, backed by a transactional receipt and durable rejection fences. The implementation reuses the existing active-session, proxy, session-create, history and test patterns.

**Trust model**: The user confirms this is a single-user application with manually configured Azure SSO. The feature neither manages SSO nor adds application identity or multi-user isolation. Browser storage is isolated by origin and browser profile, not by person. Live Azure configuration was not independently inspected; observed access responses are recorded in [quickstart.md](./quickstart.md).

## Technical Context

**Language/Version**: TypeScript ~7.0.2, C# / .NET 10  
**Primary Dependencies**: Vanilla TypeScript, browser Web Locks, ASP.NET Core minimal APIs, EF Core/Npgsql, PostgreSQL, Vitest, xUnit, Playwright, .NET Aspire  
**Storage**: Versioned browser `localStorage` draft; PostgreSQL completion receipts and rejection fences. Receipts are inserted atomically with completed sessions; existing history is not rewritten.  
**Testing**: Vitest for storage validation and state transitions; PostgreSQL-backed API integration tests; Playwright for refresh, controls, errors and recovery.  
**Target Platform**: Existing modern desktop/mobile browsers, same origin and browser profile.  
**Project Type**: Web application with an ASP.NET Core proxy and API.  
**Performance Goals**: Meet PF-001/SC-002 and PF-002; exact 100-trial procedures and criteria below.  
**Constraints**: No new third-party dependencies; strict TypeScript; retain existing history and ordinary legacy session requests, 3/5 set validation, edit-order cancellation, the accessible reorder action, prior-performance display and existing errors. Draft version 1, 1,000 exercises, 100 UTF-16 code units per weight and 1,048,576 UTF-8 bytes per draft are explicit limits. Completion requests, including legacy requests, have additional size and notes limits described in the API contract. Fingerprints are unkeyed SHA-256; no completion secret is required.  
**Scale/Scope**: One active draft per browser origin/profile; existing active session page, start/navigation flow, session-create API, additive receipt and rejection-fence tables/migrations, and focused tests.

## Constitution Check

*GATE: evaluated before research and again after design.*

- **Code Quality — delivered**: Uses the existing page, API and database patterns with typed draft validation.
- **Testing — delivered**: Focused storage, PostgreSQL concurrency/retry and browser regressions were run; see [quickstart.md](./quickstart.md) for results and scope.
- **Security — reviewed**: Untrusted inputs are bounded and the data-handling changes received **SE: Security** review. SSO remains externally managed; browser-profile access and the privacy limits of unkeyed fingerprints are documented.
- **User Experience — delivered**: Existing controls and modal styles are reused for save, restore, conflict and cleanup feedback.
- **Performance — measured**: Browser writes add no per-input network requests; the recorded 100-trial results are in [quickstart.md](./quickstart.md).

## Project Structure

### Documentation

```text
specs/035-resume-active-workout/
├── spec.md
├── plan.md
├── tasks.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── api-contract.md
│   └── ui-contract.md
└── checklists/requirements.md
```

### Source Code

```text
src/WorkoutTracker.Web/
├── Program.cs                                  # Preserve/forward create response and request
└── wwwroot/
    ├── ts/pages/active-session.ts              # Draft lifecycle, inputs and recovery
    ├── ts/pages/home.ts                        # Resume entry point
    ├── ts/router.ts                            # Release lock when leaving active flow
    ├── ts/active-workout-draft.ts              # Typed draft and lock helper
    ├── ts/__tests__/                          # Draft and failure unit tests
    └── css/styles.css                         # Existing styles reused by recovery controls
src/WorkoutTracker.Api/Program.cs                # Idempotent session creation and response
src/WorkoutTracker.Infrastructure/Data/
├── Models/                                    # Completion receipt and rejection fence entities
├── WorkoutTrackerDbContext.cs                 # Receipt and fence mappings
└── Migrations/                                # Additive tables and fingerprint transition
src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs
src/WorkoutTracker.E2ETests/
├── Infrastructure/WebAppFixture.cs           # Idempotent mock and fault injection
└── E2E/
    ├── WorkoutHistoryTests.cs                 # Resume and completion journeys
    └── WorkoutReorderTests.cs                 # Existing accessible reorder regressions
README.md                                      # User-facing recovery and profile limitations
```

**Structure Decision**: Follow feature 032's active-session/sets and create-route conventions, feature 023's discard interaction, feature 031's history compatibility and failure handling, and feature 034's accessible scroll-available reorder action. The browser draft is separate from completed history; a database receipt is needed because an in-memory button guard cannot prevent a second POST after a lost response or refresh.

## Phase 0: Research Outcomes

See [research.md](./research.md). Browser storage supports refresh recovery in one browser profile; transactional receipts prevent repeat creation after a lost response.

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): draft, frozen completion operation, receipt, rejection fences, validation and lifecycle.
- [contracts/api-contract.md](./contracts/api-contract.md): create/replay semantics, request limits and concurrency.
- [contracts/ui-contract.md](./contracts/ui-contract.md): restore, save, discard, conflicting draft and recovery behavior.
- [quickstart.md](./quickstart.md): targeted automated commands, manual flows and measurable performance checks.

## Test and Verification Design

1. **Draft unit tests**: save initial state and updates; round-trip weight, untouched/touched effort, sets and committed order; test version 1 and unknown versions, exact field and byte boundaries, duplicate IDs and invalid JSON; report read/write/remove failures and retry without losing live state.
2. **API integration tests**: ordinary legacy requests remain accepted within the documented limits; a new operation creates one session and receipt; same-operation replay and simultaneous requests return the same ID with one history entry; replay after a history edit returns the specified ID-only response; a different payload/workout conflicts; a transient commit failure is retried without duplicating history; deletion does not allow recreation. Tests also cover rejection fences, fingerprint-version compatibility and the forward-only migration.
3. **Browser tests**: refresh after saved edits; second refresh after further edits; initial sets and order; save/discard cleanup; no existing draft; invalid/unavailable storage; write and cleanup failure; different workout conflict; completion committed but response lost followed by reload/retry; repeat clicks; two tabs contend for a single draft and cannot both complete it, with retry after lock release and explicit failure when locking is unavailable; no alteration to history. Exercise order editing cancellation must not persist a transient order. Run feature 034's reorder regressions to preserve the accessible action, focus, scroll visibility and modal behavior.
4. **Security**: input bounds, storage exposure and retry safety received **SE: Security** review. Application-level identity and deployment configuration remain outside the feature.

## Performance Budgets & Measurement

- **PB-01 / PF-002**: For a representative 25-exercise draft, instrument the input event to successful storage write or accessible failure message. Every one of 100 trials under normal local conditions must finish within 2 seconds. Include a sustained sequence of input events and a deliberately failing write; do not rely on an indefinitely postponed debounce.
- **PB-02 / PF-001 / SC-002**: Using a saved 25-exercise draft, run 100 refreshes; start the clock at the browser page-ready signal and stop when restored controls are interactive with values and sets verified. At least 95 must complete within 2 seconds. Record environment, browser, warm-up, timings and failures.
- **PB-03**: Autosaving adds zero network requests per input; session create adds no additional request except explicit retry after an uncertain result. Verify via Playwright request observation.
- The 100-trial functional suites in SC-001, SC-003 and SC-004 must verify actual values/history after each independent trial; no sample-only proxy.

## Post-Design Constitution Check

Code quality, testing, UX and performance results are recorded in [quickstart.md](./quickstart.md). The implementation follows the confirmed single-user trust model and adds no application-level identity.

## Complexity Tracking

| Issue | Why it matters | Disposition |
|---|---|---|
| Deployment access controls | SSO is managed separately; browser data is accessible to anyone using the same profile. | No application SSO changes; document profile limitations and observed deployment evidence. |
| Durable idempotency receipt | An accepted POST can outlive a lost response or deleted history row. | Unique receipt and session inserted atomically; retained receipt prevents delayed replay from creating a second session. |
| Rejection fences | An earlier rejected request might arrive after a correction. | Retain a fingerprint fence before marking a rejection safe to edit; preserve incompatible older fences. |
