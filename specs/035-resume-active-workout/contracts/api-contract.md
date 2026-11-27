# API Contract: Idempotent Workout Completion

The API retains the single-user deployment boundary described in [plan.md](../plan.md); no application-level user identity is introduced.

## POST `/api/workouts/{workoutId}/sessions`

- Existing JSON fields and requests without the optional `completionOperationId` retain the legacy non-idempotent response and history semantics, subject to the new request, exercise-count and notes limits below.
- A new client supplies its persisted draft's non-empty `draftId` as UUID `completionOperationId` in the JSON body, with existing `sets`, `overallEffort` and `loggedExercises`; it must never generate a different operation ID for that draft. The web proxy must forward the body and preserve response status and body.
- Validate the UUID and payload constraints. Canonicalize the typed payload and workout ID with deterministic field order, explicit nulls and preserved exercise order; calculate an unkeyed SHA-256 fingerprint with the `s1:` prefix and uppercase hexadecimal digest. Database readers may be able to guess workout values from fingerprints.
- First valid request atomically inserts the completed session and its unique receipt, returning `201` and the existing session-create response including `workoutSessionId`.
- Replay of the same key and same canonical payload returns `200` with `{ "workoutSessionId": "<original UUID>" }`. The client accepts this minimal shape as well as the existing `201` response; edited history does not change the replay result. Replay checks the receipt before rejecting a now-deleted workout or changed template.
- Reuse of a key for a different workout or payload returns `409` with a clear error; it never creates or modifies a session. For a conflict on the same workout whose completed session still exists, include `workoutSessionId` so the user can inspect the existing completion; do not return that ID for a different-workout conflict. The client must preserve its draft and never retry with a new key to evade the conflict.
- If the receipt exists but its completed session was subsequently deleted, return `410` with an explicit error and never recreate it. The receipt is retained independently of history deletion.
- Concurrent operation-ID requests are serialized by a transaction-scoped PostgreSQL advisory lock; a unique receipt constraint also protects the operation ID. Session and receipt commit atomically.
- A timeout, disconnected response or server/proxy error is an unknown outcome to the client: retry only the same frozen operation. The API must never claim a failed response proves no transaction committed.
- Before returning a validation `400` or missing-workout `404` marked `safeToEdit: true`, commit a durable rejection fence for the operation and exact fingerprint. A delayed identical request must remain rejected even if the workout changes. Only this marker permits correction using the same operation ID; unmarked errors remain uncertain.
- Existing rejection fences from an incompatible fingerprint version return `409` without `safeToEdit`; a migrated database retains them and rejects new writes in older formats. The fingerprint-transition migration is forward-only.
- All requests, including legacy requests, are limited to 1,048,576 bytes at the web proxy and API, 1,000 exercises and 1,000 UTF-16 code units per note. An operation is limited to 100 distinct rejection fences, and the database to 10,000 fences overall; additional invalid attempts return `429` without `safeToEdit`, while a valid correction can still complete.

Existing history endpoints and rows remain unchanged; drafts do not appear in history.
