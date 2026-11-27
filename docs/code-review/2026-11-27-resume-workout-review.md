# Code Review: Resume Workout
**Ready for Production**: No
**Critical Issues**: 0

## Priority 1 (Must Fix) ⛔
- **Production key is not provisioned**: `.github/workflows/ci.yml:186-191` only deploys the image, and `bicep/main.bicep:30-36` supplies no fingerprint secret. Unless provisioned out of band, the API returns 503 for every new UI completion (`src/WorkoutTracker.Api/Program.cs:735-736`). Provision a stable secret through the deployment and validate it before rollout.
- **Changing keys invalidates rejection fences**: `src/WorkoutTracker.Api/Program.cs:738-745,769-774` computes and matches only the current key. If different instances or a later deployment use different valid keys, a request previously fenced with `safeToEdit` can miss the fence and commit after the user edits or discards the draft. Existing receipts also become conflicts. Enforce one durable key across all instances and retain versioned keys for verification; fail closed when the stored key version is unavailable.

## Recommended Changes
- **Transaction retry retains tracked entities**: `src/WorkoutTracker.Api/Program.cs:898-904` retries using the same context after `SaveChangesAsync` has accepted entity state. If commit fails and rolls back, a retry can attempt to track a second receipt or rejection with the same key, failing instead of retrying. Clear the change tracker at the start of each transaction attempt or create a fresh context, and test a transient failure after save and before commit against PostgreSQL.
