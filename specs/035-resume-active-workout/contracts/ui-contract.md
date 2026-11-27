# UI Contract: Resume an In-Progress Workout

The draft is scoped to the browser profile under the single-user model described in [plan.md](../plan.md).

## Start and Restore

- When starting an eligible workout, persist a versioned initial draft including workout, sets, order and empty inputs. Restore the same draft on refresh, including typed weight, effort's untouched state, selected sets and committed order.
- Acquire the exclusive draft Web Lock before any draft read or mutation and hold it while the active flow is open, as specified in [data-model.md](../data-model.md#cross-tab-ownership). Another tab shows an open-elsewhere message and a retry action rather than editing, clearing or submitting; unavailable locking is an explicit error, never a silent uncoordinated fallback.
- An existing different draft must not be overwritten. Offer resume or, if its completion is not pending, explicit discard; a cancelled choice leaves it intact. On missing/changed workout data or invalid schema, explain the problem and offer retry or explicit clearing without changing history. If storage is unavailable, explain the problem and offer retry; clearing is not offered when storage cannot be read.
- Validate untrusted storage before using it and render strings as text. Restore form controls with their accessible labels and slider values; previous-performance loading remains independent.

## Saving Changes

- Persist input and committed order changes automatically; provide an accessible status/error region. A failed write immediately warns that progress may not survive refresh and exposes Retry. Keep entered values visible; only indicate saved after a successful write.
- Ensure input bursts do not defer saving indefinitely and satisfy PF-002. Cancelling an in-progress order edit restores the last committed order; selecting Done persists the new order.

## Complete and Discard

- Before submitting completion, durably store a frozen operation with the draft's `draftId` as its operation UUID and its exact payload. If this write fails, do not send the POST; retain values and offer retry. Never assign a new operation ID to that draft.
- Disable duplicate submissions while pending. A network error or lost response leaves the frozen operation intact; after refresh retry/reconcile it before edits, discard or new submission. Use the same UUID and exact payload. Display uncertain status and a Retry action.
- After confirmed success, remove the draft before normal history navigation; on removal failure, show actionable cleanup feedback and prevent resuming it or starting a replacement until cleanup succeeds. A stale completion marker must be reconciled, not treated as a fresh draft.
- Explicit discard removes only the draft; report and retry a removal failure rather than saying discard succeeded. Once discarded, refresh must not restore it or create history.
- A known conflict or deleted completed session has a clear actionable message; preserve the draft and never silently create a new completed session. For a same-workout conflict with an existing session ID, offer to inspect that history entry. For a deleted completion, offer explicit confirmed cleanup.

## Accessibility

Use the existing alert/live-region and modal conventions, keyboard operability and focus management. Synchronous draft writes report saved or failed status; pending completion reports checking or uncertainty without relying on colour.
