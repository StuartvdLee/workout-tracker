# Code Review: Resume Active Workout — Final Security Review

**Ready for Production**: No  
**Critical Issues**: 0

## Review plan

Review transactional idempotency and rejection fences; key pinning and migration; untrusted request bounds; browser recovery and locking; deployment trust and secret configuration.

## Priority 1 (Must Fix) ⛔

### High — Migration can race an unpinned API revision

`src/WorkoutTracker.Infrastructure/Data/Migrations/20261127100437_PinCompletionProtectionKey.cs:13-21` checks for existing unpinned records only at migration time. The deployment applies migrations before updating the API image (`.github/workflows/ci.yml:175-179,196-209`). If an older revision implementing unpinned fingerprints is still serving requests, it can create a rejection after that check. A new differently keyed instance can then pin its own verifier on first use (`src/WorkoutTracker.Api/Program.cs:904-910`) and miss the old rejection fingerprint (`Program.cs:769-774`), allowing a previously rejected payload to be accepted after the underlying validation condition changes.

Quiesce all unpinned writers before migration and keep them stopped until the original key is verified and pinned, or implement an atomic database-enforced transition that prevents old writers from creating records after the check. Test a request racing migration and first pin using different keys.

## Recommended Changes

### Medium — Completion request and durable rejection storage are insufficiently bounded

The API deserializes and fingerprints the complete request before applying limits (`src/WorkoutTracker.Api/Program.cs:731-745`). It has no application-level limit on exercise count or notes, accepts repeated exercise IDs (`Program.cs:805-828,841-852`), and writes one permanent fence per distinct rejected fingerprint (`Program.cs:777-788`). An authenticated or otherwise network-authorized caller can submit large batches, oversized notes, or many distinct invalid payloads to consume API memory, database storage and processing capacity. The browser-only limits (`src/WorkoutTracker.Web/wwwroot/ts/active-workout-draft.ts:28-30`) do not protect the API.

Apply a request-body limit at both proxy and API, cap exercise count and notes, reject null and duplicate entries, and place operational limits and monitoring on retained fences without expiring them unsafely. For example, reject `loggedExercises.Length > 1000` and duplicate exercise IDs before constructing entities. Ensure any response marked `safeToEdit` is still backed by a committed fence.

## Deployment verification

The web ingress is public (`bicep/main.bicep:39-46`); Azure SSO policy, the effective API network boundary, and any gateway CSRF protections cannot be verified from this repository. Verify these in the deployed environment. The workflow wires the key (`.github/workflows/ci.yml:152-159,196-209`), but actual secret presence, randomness, backup, stability, and consistency across revisions cannot be established from code.

Legacy requests intentionally omit the operation ID and therefore do not receive idempotency guarantees (`src/WorkoutTracker.Api/Program.cs:895-896`); do not rely on these guarantees for legacy clients.

No tests were run in this review.
