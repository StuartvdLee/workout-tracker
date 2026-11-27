# Code Review: Completion fingerprint transition

**Ready for Production**: No  
**Critical Issues**: 0

## Review plan

Review idempotent retries, rejection fences, migration ordering, and mixed revisions.

## Priority 1 (Must Fix) ⛔

### High — Rolling back the migration removes the version fence

`src/WorkoutTracker.Infrastructure/Data/Migrations/20261127102509_UseUnkeyedCompletionFingerprints.cs:28-35` drops both `s1:` constraints without preventing older writers from running. After an `s1:` rejection, an older unpinned keyed revision can miss that rejection and commit the same request if its validation condition has changed. This breaks the `safeToEdit` promise. The documented rollback procedure permits this transition (`README.md:81-88`).

Make rollback fail closed once `s1:` records exist, or retain an equivalent database-enforced fence that blocks older writes. Test a rejected request across rollback and an older concurrent revision.

### Medium — Migration breaks completions on the still-serving old revision

The migration drops the verifier table and rejects new non-`s1:` writes (`20261127102509_UseUnkeyedCompletionFingerprints.cs:13-23`), while deployment runs migrations before updating the API image (`.github/workflows/ci.yml:165-191`). The prior keyed revision depends on that table, as documented in `docs/code-review/2026-11-27-resume-active-workout-security-recheck.md:24-26`; other keyed revisions attempting writes hit the new checks. Completions and retries can therefore fail during deployment or if deployment stops after migration.

Quiesce or drain old revisions during the transition, or use a staged rollout that keeps their read path working while preventing unsafe writes. Exercise the real deployment ordering with both revisions.

## Verification

No tests were run. The available tools did not provide a git diff, so this review examined the working-tree implementation and migration rather than independently identifying every uncommitted file.
