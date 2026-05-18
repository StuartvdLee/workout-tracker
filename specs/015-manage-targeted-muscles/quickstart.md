# Quickstart: Add Targeted Muscles In-App

**Feature**: `015-manage-targeted-muscles`

---

## Running the App

```bash
cd src/WorkoutTracker.AppHost
dotnet run
```

The Aspire dashboard will print URLs for the API and Web projects.

---

## Manual Verification Checklist

1. Open the **Exercises** page.
2. In the "Targeted muscles" section of the **Add Exercise** form, type a new muscle name (e.g., "Hip Flexors") in the "New muscle name" input.
3. Click **Add**.
4. Confirm:
   - [ ] "Hip Flexors" appears immediately as a toggle button in the muscle list.
   - [ ] It is alphabetically positioned between "Hamstrings" and "Quads".
   - [ ] It is **not** selected (toggle is in the default unselected state).
   - [ ] No page reload occurred.
5. Click the "Hip Flexors" toggle to select it, fill in an exercise name, then click **Add Exercise**.
6. Confirm the saved exercise shows "Hip Flexors" as a muscle chip.
7. Reload the page.
8. Confirm "Hip Flexors" still appears in the muscle list.
9. Try to add "hip flexors" (lowercase) again — confirm the error "A muscle with this name already exists." appears.
10. Try to add an empty name — confirm the error "Muscle name is required." appears.
11. Open the **Edit** modal for any exercise.
12. Confirm "Hip Flexors" appears in the edit modal's muscle toggle list at the correct alphabetical position.
13. Add a new muscle from the edit modal (e.g., "Neck") — confirm it appears immediately, unselected, at the correct alphabetical position.

---

## Running Tests

**Backend integration tests**:
```bash
dotnet test src/WorkoutTracker.UnitTests/WorkoutTracker.UnitTests.csproj
```
Requires a running PostgreSQL instance. Set `TEST_DB_CONNECTION` if not using the default `localhost:5432/workout_tracker_test`.

**Frontend unit tests**:
```bash
cd src/WorkoutTracker.Web && npm test
```

**E2E tests** (requires the app to be running):
```bash
dotnet test src/WorkoutTracker.E2ETests/WorkoutTracker.E2ETests.csproj
```

---

## Key Files

| File | Change |
|------|--------|
| `src/WorkoutTracker.Api/Program.cs` | Add `POST /api/muscles` endpoint + `MuscleCreateRequest` record |
| `src/WorkoutTracker.Web/Program.cs` | Add `POST /api/muscles` proxy route |
| `src/WorkoutTracker.Web/wwwroot/ts/pages/exercises.ts` | Add-muscle state, HTML, handlers, insert + re-render logic |
| `src/WorkoutTracker.Web/wwwroot/css/styles.css` | Add `.muscle-add__*` styles |
| `src/WorkoutTracker.UnitTests/Api/MusclesApiTests.cs` | New `POST /api/muscles` tests |
| `src/WorkoutTracker.E2ETests/E2E/ExercisesPageTests.cs` | New add-muscle E2E tests |
