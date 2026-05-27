# Data Model: Delete Session

## Affected Entities

### WorkoutSession

The entity being deleted. No structural changes to the entity or its table.

| Field | Type | Notes |
|-------|------|-------|
| WorkoutSessionId | Guid (PK) | Used to identify the session to delete |
| PlannedWorkoutId | Guid? (FK, nullable) | Set to null on delete of PlannedWorkout (SetNull); no impact here |
| WorkoutName | string | Read for display; not involved in delete logic |
| OverallEffort | int? | Not involved |
| CompletedAt | DateTime (shadow) | Not involved |

**Delete behaviour**: `db.WorkoutSessions.Remove(session)` followed by `SaveChangesAsync()`. EF Core issues `DELETE FROM workout_session WHERE workout_session_id = @p0`.

---

### LoggedExercise

Child entity. Automatically removed by the database cascade on `WorkoutSession` deletion.

| Field | Type | Notes |
|-------|------|-------|
| LoggedExerciseId | Guid (PK) | Removed automatically |
| WorkoutSessionId | Guid (FK) | Cascade: `ON DELETE CASCADE` |
| ExerciseId | Guid? (FK, nullable) | SetNull on Exercise delete; no impact here |
| LoggedWeight | string? | Removed with parent |
| Effort | int? | Removed with parent |

**Cascade**: Configured via `OnDelete(DeleteBehavior.Cascade)` in `WorkoutTrackerDbContext.cs:157`. No application-level removal code needed.

---

## No Schema Changes

No migrations are required. The cascade constraint and all entity definitions are already in place.

## Delete Flow Summary

```
DELETE /api/sessions/{sessionId}
  → EF: SELECT WorkoutSession WHERE WorkoutSessionId = @id
  → If null → 404
  → db.WorkoutSessions.Remove(session)
  → SaveChangesAsync()
      └── DELETE FROM logged_exercise WHERE workout_session_id = @id  [cascade]
      └── DELETE FROM workout_session WHERE workout_session_id = @id
  → 204 No Content
```
