# Code Review: Completion fingerprint rollback recheck

**Ready for Production**: Yes, for the reviewed rollback scope  
**Critical Issues**: 0

## Review plan

Check downgrade behavior, retained rejection fences, and older fingerprint writes.

## Priority 1 (Must Fix) ⛔

None found.

## Recommended Changes

None for the reviewed rollback scope. The transition migration throws before generating downgrade operations (`src/WorkoutTracker.Infrastructure/Data/Migrations/20261127102509_UseUnkeyedCompletionFingerprints.cs:28-30`). The PostgreSQL integration test checks the failed downgrade, rejected replay, and blocked older fingerprint write (`src/WorkoutTracker.UnitTests/Api/SessionApiTests.cs:255-272`).

The test was inspected but not run during this review.
