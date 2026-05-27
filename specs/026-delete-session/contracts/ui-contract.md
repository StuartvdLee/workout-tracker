# UI Contract: Delete Session

## 1. Delete Button

**Location**: Bottom of `#session-detail-content`, appended after the chart section (or after the overall-effort row for ad-hoc sessions without a chart).

**HTML structure**:
```html
<div class="session-detail__delete-section">
  <button class="session-detail__delete" id="session-detail-delete" type="button">
    Delete session
  </button>
  <div class="session-detail__delete-error" id="session-detail-delete-error"
       role="alert" aria-live="polite" style="display:none;"></div>
</div>
```

**States**:
| State | Visual |
|-------|--------|
| Default | Enabled destructive-action button |
| In-flight | `disabled` attribute set; button text unchanged or "Deleting…" |
| Error | Error div visible with message text; button re-enabled |

---

## 2. Confirmation Modal

Reuses `.discard-modal-backdrop` / `.discard-modal` CSS classes exactly.

**HTML structure** (injected into `#session-detail-content` render):
```html
<div class="discard-modal-backdrop" id="session-delete-confirm-backdrop" style="display:none;">
  <div class="discard-modal" role="alertdialog" aria-modal="true"
       aria-labelledby="session-delete-confirm-title"
       aria-describedby="session-delete-confirm-desc">
    <h2 class="discard-modal__title" id="session-delete-confirm-title">Delete session?</h2>
    <p class="discard-modal__desc" id="session-delete-confirm-desc">
      This will permanently remove this session and cannot be undone.
    </p>
    <div class="discard-modal__actions">
      <button class="discard-modal__discard" type="button"
              id="session-delete-confirm-ok">Delete</button>
      <button class="discard-modal__continue" type="button"
              id="session-delete-confirm-cancel">Keep session</button>
    </div>
  </div>
</div>
```

**Focus management** (matches features 021–023 / `active-session.ts`):
- On open: focus moves to `#session-delete-confirm-ok` (Delete button)
- Tab: cycles between Delete and Keep session
- Escape: closes modal (equivalent to Keep session)
- On close (cancel): focus returns to `#session-detail-delete`

---

## 3. Success Banner (History page)

Shown on the History page when navigated from a successful deletion (`?deleted=1` query param). URL is cleaned via `history.replaceState` immediately.

**HTML**:
```html
<p class="history-page__banner" role="status">Session deleted.</p>
```

**Placement**: Immediately after `<h1 class="history-page__title">`, before loading/empty/list elements.

**Lifetime**: Rendered once on this navigation. Disappears when the user navigates away (router replaces `innerHTML`).

---

## 4. Interaction Flow

```
[Session Detail Page]
  User clicks "Delete session"
    → #session-delete-confirm-backdrop shown, focus → Delete button

  User clicks "Keep session" (or presses Escape)
    → modal hidden, focus → "Delete session" button
    → no change

  User clicks "Delete"
    → modal hidden
    → "Delete session" button disabled
    → DELETE /api/sessions/{id}
      → 204 or 404 → navigate('/history?deleted=1')
      → error       → re-enable button, show error message

[History Page]
  render() reads ?deleted=1
    → history.replaceState(null, '', '/history')
    → renders "Session deleted." banner above list
    → banner disappears on next navigation
```
