# Research: Delete Session

## Decision: API Endpoint Design

**Decision**: `DELETE /api/sessions/{sessionId:guid}` returning `204 No Content` on success and `404` with `{ "error": "Session not found." }` on missing resource.

**Rationale**: This is the established pattern used identically by `DELETE /api/muscles/{muscleId:guid}`, `DELETE /api/exercises/{exerciseId:guid}`, and `DELETE /api/workouts/{workoutId:guid}`. Consistency is mandatory per Constitution I.

**Alternatives considered**: `POST /api/sessions/{sessionId}/delete` (non-RESTful, inconsistent with project), `DELETE /api/sessions` with a body (non-standard, ASP.NET minimal APIs don't route body-based DELETEs by convention).

---

## Decision: Child Record Deletion

**Decision**: No explicit `Include(ws => ws.LoggedExercises)` or manual removal loop needed. Deleting the `WorkoutSession` entity is sufficient.

**Rationale**: `WorkoutTrackerDbContext` already configures `LoggedExercise → WorkoutSession` with `OnDelete(DeleteBehavior.Cascade)` (line 157 of `WorkoutTrackerDbContext.cs`). PostgreSQL will cascade the delete at the database level within the same transaction as `SaveChangesAsync()`.

**Alternatives considered**: Manually loading and removing `LoggedExercise` records before removing the session — rejected because the cascade constraint already guarantees correctness, and adding explicit removal would be redundant and slower (extra query).

---

## Decision: Confirmation Mechanism

**Decision**: Reuse `.discard-modal-backdrop` / `.discard-modal` CSS classes and the `role="alertdialog"` / focus management pattern established in features 021–023 and `active-session.ts`.

**Rationale**: The same CSS classes, ARIA attributes, button labels ("destructive" red + "safe" outlined), and focus-trap logic are already present and tested. Introducing a new pattern would violate Constitution IV (UX consistency).

**Alternatives considered**: Browser-native `confirm()` dialog — rejected because it is not styleable, blocks the main thread, and is inconsistent with the existing in-page modal pattern.

---

## Decision: Success Feedback Mechanism

**Decision**: `navigate('/history?deleted=1')` then read `?deleted=1` in `history.ts` to render a `<p class="history-page__banner" role="status">Session deleted.</p>` banner, followed by `history.replaceState(null, '', '/history')` to clean the URL.

**Rationale**: This approach requires no shared module state between `session-detail.ts` and `history.ts`, is consistent with how `session-detail.ts` already reads `?id=` to load the correct session, and self-cleans on the next navigation. `role="status"` announces the message to screen readers non-intrusively.

**Alternatives considered**:
- Module-level singleton flag — rejected because it creates hidden cross-module coupling and is reset on page reload.
- `sessionStorage` — heavier than needed for a one-time success indicator.
- Auto-dismissing toast component — no such component exists in the project; introducing one would be out of scope.

---

## Decision: No DB Migration Required

**Decision**: Skip migration.

**Rationale**: The cascade relationship was established when `LoggedExercise` was first introduced. `WorkoutTrackerDbContextModelSnapshot.cs` already reflects `DeleteBehavior.Cascade` for `LoggedExercise.WorkoutSessionId`. No schema change is needed for this feature.
