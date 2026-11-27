# Research: Resume an In-Progress Workout After a Refresh

## Draft persistence

- **Decision**: Use a versioned, bounded `localStorage` draft scoped to the current origin and browser profile, with explicit storage error handling.
- **Rationale**: The requirement is refresh recovery in the same browser context. Before this feature, `active-session.ts` kept workout progress only in module memory, so refresh reset it. Browser storage requires no new request per input.
- **Alternatives considered**: `sessionStorage` restricts recovery to a tab and may be copied to an opener-created tab; a server-side draft requires a new persistence API. Neither establishes user identity by itself.

## Single-user access boundary

- **Decision**: Use the confirmed single-user deployment model and do not add application-level identity or multi-user draft isolation. Azure SSO is configured separately by the user; this feature does not configure it. The live Azure configuration was not independently inspected.
- **Rationale**: The user reports signing in through Azure SSO and being the only user. `WorkoutTracker.Web/Program.cs` and `WorkoutTracker.Api/Program.cs` do not themselves establish an application user identity. Browser storage is accessible to scripts and anyone using the same origin/profile.
- **Alternatives considered**: Adding an application identity system is unnecessary for the stated single-user use case. Tab or profile isolation does not protect against another person using the same profile.

## Safe completion

- **Decision**: Hold an exclusive Web Lock for the active-draft lifecycle, and use the persisted draft UUID as the immutable completion operation UUID; freeze the payload before sending it. Add an optional idempotency ID to session creation and a durable server receipt with a unique constraint, written in the same transaction as the completed session. Replay returns the original session ID, not an original response snapshot.
- **Rationale**: Before this feature, every `POST /api/workouts/{workoutId}/sessions` created a fresh GUID. The client cannot tell whether a dropped response committed, and a client-only guard disappears on refresh. Keep receipts after session deletion to stop a delayed replay from recreating history.
- **Alternatives considered**: Client-only `isSaving`, independently generated completion IDs or querying history are race-prone across tabs. A unique field on the session is insufficient after deletion unless an independently retained tombstone exists. Changing every historical route to represent pending sessions would disturb existing history. A local-storage lease can expire while a tab is paused and cannot provide reliable exclusive ownership.

## Reconciliation and cleanup

- **Decision**: Persist the immutable pending operation before sending. On uncertain outcome, retry the same operation before allowing edits, discard or a new operation; on known success remove the draft, reporting cleanup failure and retrying removal. A durable rejection fence permits editing or discarding only after a response marked `safeToEdit`. Do not overwrite another draft.
- **Rationale**: A new key after an ambiguous commit could create two sessions; a changed payload must instead conflict under the same draft key. A failed removal may otherwise resurrect a completed or discarded draft. Idempotent replay makes repeated reconciliation safe.
- **Alternatives considered**: Treating an error as a failed completion or deleting an uncertain draft silently would risk duplicates or loss of progress.

## Fingerprints and compatibility

- **Decision**: Use a versioned unkeyed SHA-256 fingerprint for receipts and rejection fences; no completion secret or key provisioning is required. Retain incompatible older fences, reject new older-version writes, and refuse rollback of the fingerprint-transition migration.
- **Rationale**: The user chose to remove the operational burden of a separately managed fingerprint key. This preserves retry matching, but database readers may be able to guess workout values from fingerprints.
- **Alternatives considered**: A keyed HMAC would better protect against guessing after database disclosure, but requires a stable external secret. An incompatible older fence cannot safely be matched without its original scheme, so affected operations remain locked instead of silently bypassing it.

## Compatibility, validation and performance

- **Decision**: Continue accepting existing create requests without an operation ID and leave historical data/queries unchanged. Validate stored drafts and create requests with bounded sizes, strict shapes and permitted values; the new request-size, exercise-count and notes limits apply to legacy requests too. Show recoverable errors rather than silently resetting.
- **Rationale**: Features 016, 031 and 032 establish the nullable legacy history model, current create response and 3/5 sets contract. `localStorage` is untrusted and may throw or contain stale/corrupt data. Save actual input changes with bounded latency rather than adding network calls.
- **Alternatives considered**: Modifying legacy history rows or treating malformed data as an empty draft would respectively create migration risk or silent data loss.
