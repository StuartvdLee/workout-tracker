# Data Model: Resume an In-Progress Workout

The delivered model follows the single-user deployment assumptions in [plan.md](./plan.md). Azure SSO is managed separately and no application identity is required.

## Active Workout Draft

One versioned record per origin/browser profile, stored under the `workout-tracker-active-draft` local-storage key:

| Field | Type | Invariant |
|---|---|---|
| version | integer | Version `1` is supported; unknown versions are not overwritten automatically. |
| draftId | UUID | Non-empty UUID generated once and durably saved before use; stable through the workout and reused as its completion operation ID. |
| workoutId | UUID | Identifies the selected planned workout. |
| sets | 3 or 5 | The selected session-wide value. |
| exerciseOrder | UUID array | At most 1,000 unique exercise IDs; committed order only. |
| entries | exercise ID to `Active Workout Draft Entry` | At most 1,000 entries; IDs are unique and match `exerciseOrder` and the loaded workout. |
| completion | null or frozen operation | Present before a completion request is sent; see below. |

### Active Workout Draft Entry

| Field | Type | Invariant |
|---|---|---|
| loggedWeight | string | Empty or at most 100 UTF-16 code units, matching the API's 100-character limit. |
| loggedEffort | integer or null | Null or an integer from 1 through 10. |

The entire serialized draft, including a frozen completion payload, is limited to 1,048,576 UTF-8 bytes on both read and write. Check the raw storage value's size before parsing and the serialized value before writing. Completion's logged-exercise collection has the same 1,000-item limit and must match the draft's exercise IDs. An oversized draft or workout produces an actionable error, preserves the existing saved draft and current page input, and is never silently truncated or overwritten. A storage quota or access error remains a separate save/restore failure.

Validate all fields, bounds and relationships before restoring; do not execute or insert stored strings as HTML. Missing/deleted workouts, changed exercise sets or malformed/incompatible drafts must surface a recoverable conflict; never silently drop entries or overwrite the draft. A new workout cannot overwrite a different existing draft without explicit resolution. Persist accepted order after the user selects Done; cancelling order edit preserves the pre-edit order.

## Cross-Tab Ownership

Use one exclusive, origin-scoped Web Lock for the active-draft lifecycle. Acquire it before reading, creating, editing, completing, discarding or clearing a draft; re-read storage after acquisition and hold the lock until the active flow is exited or the document closes. A second tab must not mutate the draft or submit completion while another holds the lock: explain that it is open elsewhere and offer retry after that tab exits. If Web Locks is unavailable or acquisition fails, show an actionable error and do not proceed with uncoordinated draft writes or completion. Refresh reacquires the lock and re-reads the draft; the browser releases it when the owning document closes. Do not steal a lock or use a timer-based local-storage lease.

## Frozen Completion Operation

The draft's `completion` object contains the `draftId` as `completionOperationId` and the exact validated create payload, including sets, ordered logged exercises and overall effort; the workout ID remains on the parent draft. Persist the operation successfully before the POST. Keep the payload immutable across ambiguous outcomes; repeated requests use the same UUID and payload. A committed rejection fence and `safeToEdit: true` allow correction while retaining the same operation UUID; an uncertain outcome must be reconciled first. Never generate a replacement operation UUID for the same draft. A different payload after a receipt exists conflicts and must not trigger a new operation.

## Completion Receipt

An additive PostgreSQL table contains a unique operation UUID, canonical request fingerprint, workout ID and resulting session ID. Session creation and receipt insertion commit in one transaction. The `s1:`-prefixed SHA-256 fingerprint covers workout ID and the typed payload using deterministic serialization, including array order and explicit nulls. It requires no configured secret; database readers may be able to guess workout values from fingerprints. A transaction-scoped PostgreSQL advisory lock serializes requests for an operation, and the unique receipt key provides an additional constraint.

Retrying a failed transaction clears tracked entities before the next attempt. Receipts from an incompatible fingerprint version cannot be matched and result in a conflict.

Receipt records remain after session deletion; no cascading foreign key to the session. A same-key/same-payload replay returns only the original session ID when it exists; it does not promise a snapshot of the original response. If the session was deleted, report the deletion instead of recreating it. A different payload/workout with the same key conflicts. Requests without an operation UUID use the legacy non-idempotent path, subject to the limits in the API contract. Receipts do not expire automatically.

## Rejection Fence

A retained record associates an operation UUID and request fingerprint with its validation error and response status. Before returning a response marked safe for editing, the API commits this fence while holding the operation's transaction-scoped lock. A delayed request with that same payload cannot later create a session, even if the workout template changes. A corrected payload retains the operation UUID and can proceed; after it succeeds, the completion receipt prevents all conflicting payloads from creating another session. Fences are retained independently of history deletion, like receipts.

The database rejects new writes using incompatible fingerprint versions. If an operation has an older rejection fence, it remains locked instead of allowing an unmatchable payload through. The fingerprint transition migration cannot be rolled back, because allowing an older writer could bypass retained fences. Each operation can retain at most 100 distinct rejection fences, and admission is serialized against a global limit of 10,000 fences. Further invalid attempts return an unmarked `429`, keeping the client frozen, while a valid correction can still succeed. Monitor database growth and increase capacity before exhaustion; never delete retained fences.

## State Transitions

`none → active → completion-pending → completed → cleared`; `active → discarded → cleared`. Save failure leaves the live `active` state and last known durable draft intact with an actionable warning. An uncertain completion remains `completion-pending` until idempotent reconciliation; a removal failure remains visible and blocks a new draft until cleanup succeeds. Discard requires explicit confirmation and successful removal; it cannot erase completed history. Corrupt or conflicting data stays untouched until the user explicitly clears it.
