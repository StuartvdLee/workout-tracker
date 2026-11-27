# Code Review: Resume Active Workout Security Recheck

**Ready for Production**: No  
**Critical Issues**: 0

## Review plan

Recheck the migration/write race and key-verifier guard, then test request and durable-fence limits against alternate request paths.

## Priority 1 (Must Fix) ⛔

### Medium — Legacy requests bypass exercise and notes limits

`src/WorkoutTracker.Api/Program.cs:811-819` applies both limits only when `fingerprint` is non-null. A caller can omit `completionOperationId`, submit substantially more than 1,000 repetitions of a valid exercise within the 1 MiB request cap, and have each inserted (`Program.cs:830-865`). Legacy notes can likewise approach the full request limit. This bypasses the intended per-request resource bounds; the endpoint intentionally permits legacy requests (`Program.cs:908-909`).

Apply appropriate limits to both request paths, and reject duplicate exercise IDs where compatible. For example, validate `loggedExercises.Length` regardless of `fingerprint`, returning an ordinary unmarked error for legacy requests and a committed rejection for protected requests.

### Medium — Per-operation fence cap does not bound total fence growth

`src/WorkoutTracker.Api/Program.cs:786-795` counts only the caller-supplied operation ID, so a caller can send invalid requests with fresh UUIDs and cause an unlimited number of permanent rejection rows. The 100-fence cap never applies across those operations. This remains a database-exhaustion path for a caller with access to the endpoint.

Add a deployment-wide admission/rate and storage-capacity policy, with monitoring; return an unmarked error when no fence can be retained. Preserve previously committed fences and replay behavior. Test repeated fresh operation IDs as well as a single operation.

## Migration recheck

For a database applying the revised migration, the table locks precede the empty-table check and trigger installation (`src/WorkoutTracker.Infrastructure/Data/Migrations/20261127100437_PinCompletionProtectionKey.cs:13-55`); the current API sets the transaction-local marker only after comparing the pinned verifier (`src/WorkoutTracker.Api/Program.cs:911-927`). No remaining instance-key race was identified on that path. If the earlier version of this same migration has already been applied anywhere, EF will not rerun it: deploy the trigger through a new migration or verify and remediate those databases separately.

No tests were run in this review.
