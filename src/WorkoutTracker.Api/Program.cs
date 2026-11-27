using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkoutTracker.Infrastructure.Data;
using WorkoutTracker.Infrastructure.Data.Models;

// Stable advisory lock key used to serialize muscle name duplicate checks + inserts in POST /api/muscles.
const long MuscleNameAdvisoryLockId = 610871121330421911;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<WorkoutTrackerDbContext>("workout-tracker-db",
    configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

app.MapGet("/api/workout-types", async (WorkoutTrackerDbContext db) =>
{
    var workoutTypes = await db.WorkoutTypes
        .OrderBy(wt => wt.Name)
        .Select(wt => new { wt.WorkoutTypeId, wt.Name })
        .ToListAsync();

    return Results.Ok(workoutTypes);
});

app.MapGet("/api/muscles", async (WorkoutTrackerDbContext db) =>
{
    var muscles = await db.Muscles
        .OrderBy(m => m.Name)
        .Select(m => new { m.MuscleId, m.Name })
        .ToListAsync();

    return Results.Ok(muscles);
});

app.MapPost("/api/muscles", async (HttpContext context, WorkoutTrackerDbContext db) =>
{
    var body = await context.Request.ReadFromJsonAsync<MuscleCreateRequest>();
    var name = body?.Name?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
        return Results.Json(new { error = "Muscle name is required." }, statusCode: 400);

    if (name.Length > 100)
        return Results.Json(new { error = "Muscle name must be 100 characters or fewer." }, statusCode: 400);

    var normalizedName = ExerciseQueryHelper.EscapeLike(name);
    var muscleId = Guid.NewGuid();
    Muscle? muscle = null;

    var strategy = db.Database.CreateExecutionStrategy();
    var duplicate = false;

    await strategy.ExecuteInTransactionAsync(
        async () =>
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0});", MuscleNameAdvisoryLockId);

            duplicate = await db.Muscles
                .AnyAsync(m => EF.Functions.ILike(m.Name, normalizedName, "\\"));

            if (!duplicate)
            {
                muscle = new Muscle { MuscleId = muscleId, Name = name };
                db.Muscles.Add(muscle);
                await db.SaveChangesAsync();
            }
        },
        async () =>
        {
            muscle = await db.Muscles.SingleOrDefaultAsync(m => m.MuscleId == muscleId);
            if (muscle is not null)
            {
                duplicate = false;
                return true;
            }

            if (duplicate)
                return true;

            return false;
        });

    if (duplicate)
        return Results.Json(new { error = "A muscle with this name already exists." }, statusCode: 400);

    var createdMuscle = muscle!;
    return Results.Json(new { createdMuscle.MuscleId, createdMuscle.Name }, statusCode: 201);
});

app.MapPatch("/api/muscles/{muscleId:guid}", async (Guid muscleId, HttpContext context, WorkoutTrackerDbContext db) =>
{
    var body = await context.Request.ReadFromJsonAsync<MuscleUpdateRequest>();
    var name = body?.Name?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
        return Results.Json(new { error = "Muscle name is required." }, statusCode: 400);

    if (name.Length > 100)
        return Results.Json(new { error = "Muscle name must be 100 characters or fewer." }, statusCode: 400);

    var normalizedName = ExerciseQueryHelper.EscapeLike(name);
    var strategy = db.Database.CreateExecutionStrategy();
    var duplicate = false;
    var notFound = false;

    await strategy.ExecuteInTransactionAsync(
        async () =>
        {
            db.ChangeTracker.Clear();
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0});", MuscleNameAdvisoryLockId);

            var muscle = await db.Muscles.FirstOrDefaultAsync(m => m.MuscleId == muscleId);
            if (muscle is null)
            {
                notFound = true;
                return;
            }

            notFound = false;
            duplicate = await db.Muscles
                .AnyAsync(m => m.MuscleId != muscleId && EF.Functions.ILike(m.Name, normalizedName, "\\"));

            if (!duplicate)
            {
                muscle.Name = name;
                await db.SaveChangesAsync();
            }
        },
        async () =>
        {
            var current = await db.Muscles.AsNoTracking().SingleOrDefaultAsync(m => m.MuscleId == muscleId);
            if (current is null)
                return notFound;

            if (string.Equals(current.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                duplicate = false;
                notFound = false;
                return true;
            }

            if (duplicate)
                return true;

            return false;
        });

    if (notFound)
        return Results.Json(new { error = "Muscle not found." }, statusCode: 404);

    if (duplicate)
        return Results.Json(new { error = "A muscle with this name already exists." }, statusCode: 400);

    return Results.Ok(new { MuscleId = muscleId, Name = name });
});

app.MapDelete("/api/muscles/{muscleId:guid}", async (Guid muscleId, WorkoutTrackerDbContext db) =>
{
    var muscle = await db.Muscles.FirstOrDefaultAsync(m => m.MuscleId == muscleId);
    if (muscle is null)
        return Results.Json(new { error = "Muscle not found." }, statusCode: 404);

    db.Muscles.Remove(muscle);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/exercises", async (WorkoutTrackerDbContext db) =>
{
    var exercises = await db.Exercises
        .Include(e => e.ExerciseMuscles)
        .ThenInclude(em => em.Muscle)
        .OrderBy(e => e.Name)
        .Select(e => new
        {
            e.ExerciseId,
            e.Name,
            Muscles = e.ExerciseMuscles
                .Select(em => new { em.Muscle.MuscleId, em.Muscle.Name })
                .OrderBy(m => m.Name)
                .ToList(),
        })
        .ToListAsync();

    return Results.Ok(exercises);
});

app.MapPost("/api/exercises", async (HttpContext context, WorkoutTrackerDbContext db) =>
{
    var body = await context.Request.ReadFromJsonAsync<ExerciseCreateRequest>();
    var name = body?.Name?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.Json(new { error = "Exercise name is required." }, statusCode: 400);
    }

    if (name.Length > 150)
    {
        return Results.Json(new { error = "Exercise name must be 150 characters or fewer." }, statusCode: 400);
    }

    var normalizedName = ExerciseQueryHelper.EscapeLike(name);
    var duplicate = await db.Exercises
        .AnyAsync(e => EF.Functions.ILike(e.Name, normalizedName, "\\"));

    if (duplicate)
    {
        return Results.Json(new { error = "An exercise with this name already exists." }, statusCode: 400);
    }

    var muscleIds = (body?.MuscleIds ?? []).Distinct().ToArray();
    if (muscleIds.Length > 0)
    {
        var validMuscleCount = await db.Muscles
            .CountAsync(m => muscleIds.Contains(m.MuscleId));

        if (validMuscleCount != muscleIds.Length)
        {
            return Results.Json(new { error = "One or more selected muscles are invalid." }, statusCode: 400);
        }
    }

    var exercise = new Exercise
    {
        ExerciseId = Guid.NewGuid(),
        Name = name,
    };

    foreach (var muscleId in muscleIds)
    {
        exercise.ExerciseMuscles.Add(new ExerciseMuscle
        {
            ExerciseMuscleId = Guid.NewGuid(),
            ExerciseId = exercise.ExerciseId,
            MuscleId = muscleId,
        });
    }

    db.Exercises.Add(exercise);
    await db.SaveChangesAsync();

    var muscles = await db.ExerciseMuscles
        .Where(em => em.ExerciseId == exercise.ExerciseId)
        .Include(em => em.Muscle)
        .Select(em => new { em.Muscle.MuscleId, em.Muscle.Name })
        .ToListAsync();

    return Results.Json(new { exercise.ExerciseId, exercise.Name, Muscles = muscles }, statusCode: 201);
});

app.MapPut("/api/exercises/{exerciseId:guid}", async (Guid exerciseId, HttpContext context, WorkoutTrackerDbContext db) =>
{
    var exercise = await db.Exercises
        .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);

    if (exercise is null)
    {
        return Results.Json(new { error = "Exercise not found." }, statusCode: 404);
    }

    var body = await context.Request.ReadFromJsonAsync<ExerciseCreateRequest>();
    var name = body?.Name?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.Json(new { error = "Exercise name is required." }, statusCode: 400);
    }

    if (name.Length > 150)
    {
        return Results.Json(new { error = "Exercise name must be 150 characters or fewer." }, statusCode: 400);
    }

    var normalizedName = ExerciseQueryHelper.EscapeLike(name);
    var duplicate = await db.Exercises
        .AnyAsync(e => e.ExerciseId != exerciseId && EF.Functions.ILike(e.Name, normalizedName, "\\"));

    if (duplicate)
    {
        return Results.Json(new { error = "An exercise with this name already exists." }, statusCode: 400);
    }

    var muscleIds = (body?.MuscleIds ?? []).Distinct().ToArray();
    if (muscleIds.Length > 0)
    {
        var validMuscleCount = await db.Muscles
            .CountAsync(m => muscleIds.Contains(m.MuscleId));

        if (validMuscleCount != muscleIds.Length)
        {
            return Results.Json(new { error = "One or more selected muscles are invalid." }, statusCode: 400);
        }
    }

    exercise.Name = name;

    // Delete existing muscles via bulk operation (bypasses change tracker)
    await db.ExerciseMuscles
        .Where(em => em.ExerciseId == exerciseId)
        .ExecuteDeleteAsync();

    foreach (var muscleId in muscleIds)
    {
        db.ExerciseMuscles.Add(new ExerciseMuscle
        {
            ExerciseMuscleId = Guid.NewGuid(),
            ExerciseId = exerciseId,
            MuscleId = muscleId,
        });
    }

    await db.SaveChangesAsync();

    var muscles = await db.ExerciseMuscles
        .Where(em => em.ExerciseId == exercise.ExerciseId)
        .Include(em => em.Muscle)
        .Select(em => new { em.Muscle.MuscleId, em.Muscle.Name })
        .ToListAsync();

    return Results.Ok(new { exercise.ExerciseId, exercise.Name, Muscles = muscles });
});

app.MapDelete("/api/exercises/{exerciseId:guid}", async (Guid exerciseId, WorkoutTrackerDbContext db) =>
{
    var exercise = await db.Exercises
        .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);

    if (exercise is null)
    {
        return Results.Json(new { error = "Exercise not found." }, statusCode: 404);
    }

    db.Exercises.Remove(exercise);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/api/workouts", async (WorkoutTrackerDbContext db) =>
{
    var workouts = await db.PlannedWorkouts
        .Include(pw => pw.Exercises)
        .ThenInclude(e => e.Exercise)
        .OrderBy(pw => pw.Name)
        .Select(pw => new
        {
            pw.PlannedWorkoutId,
            pw.Name,
            ExerciseCount = pw.Exercises.Count,
            Exercises = pw.Exercises
                .OrderBy(e => e.Sequence)
                .Select(e => new { e.ExerciseId, Name = e.Exercise.Name, e.TargetReps, e.TargetWeight })
                .ToList(),
        })
        .ToListAsync();

    return Results.Ok(workouts);
});

app.MapGet("/api/workouts/{workoutId:guid}", async (Guid workoutId, WorkoutTrackerDbContext db) =>
{
    var workout = await db.PlannedWorkouts
        .Include(pw => pw.Exercises)
        .ThenInclude(e => e.Exercise)
        .FirstOrDefaultAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (workout is null)
    {
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);
    }

    return Results.Ok(new
    {
        workout.PlannedWorkoutId,
        workout.Name,
        ExerciseCount = workout.Exercises.Count,
        Exercises = workout.Exercises
            .OrderBy(e => e.Sequence)
            .Select(e => new { e.ExerciseId, Name = e.Exercise.Name, e.TargetReps, e.TargetWeight })
            .ToList(),
    });
});

app.MapGet("/api/workouts/{workoutId:guid}/previous-performance", async (Guid workoutId, WorkoutTrackerDbContext db) =>
{
    var workout = await db.PlannedWorkouts
        .Where(pw => pw.PlannedWorkoutId == workoutId)
        .Select(pw => new
        {
            ExerciseIds = pw.Exercises.Select(pwe => pwe.ExerciseId).ToList(),
        })
        .FirstOrDefaultAsync();

    if (workout is null)
    {
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);
    }

    var targetExerciseIds = workout.ExerciseIds.ToHashSet();
    var sessions = await db.WorkoutSessions
        .Where(ws => ws.PlannedWorkoutId == workoutId)
        .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
        .ThenByDescending(ws => ws.WorkoutSessionId)
        .Take(PreviousExerciseDataSelector.MaxSessionsToScan)
        .Select(ws => new
        {
            ws.WorkoutSessionId,
            CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
            ws.Sets,
            LoggedExercises = ws.LoggedExercises
                .Where(le => workout.ExerciseIds.Contains(le.ExerciseId))
                .Select(le => new HistoricalExerciseData(
                    le.ExerciseId,
                    le.LoggedWeight,
                    le.Effort,
                    le.Sequence))
                .ToList(),
        })
        .ToListAsync();

    var latestExercises = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
        sessions.Select(s => new HistoricalSessionData(s.WorkoutSessionId, s.CompletedAt, null, s.Sets, s.LoggedExercises)),
        targetExerciseIds);

    if (latestExercises.Count == 0)
    {
        return Results.Ok(new
        {
            hasPreviousSession = false,
            completedAt = (DateTime?)null,
            exercises = Array.Empty<object>(),
        });
    }

    return Results.Ok(new
    {
        hasPreviousSession = true,
        completedAt = (DateTime?)latestExercises.Values.Max(e => e.CompletedAt),
        exercises = latestExercises.Values
            .OrderBy(e => e.ExerciseId)
            .Select(e => new
            {
                e.ExerciseId,
                e.LoggedWeight,
                e.Effort,
                e.Sequence,
                e.Sets,
                e.CompletedAt,
            })
            .ToList(),
    });
});

app.MapGet("/api/workouts/{workoutId:guid}/session-trends", async (Guid workoutId, WorkoutTrackerDbContext db) =>
{
    var workoutExists = await db.PlannedWorkouts
        .AnyAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (!workoutExists)
    {
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);
    }

    var sessions = await db.WorkoutSessions
        .Where(ws => ws.PlannedWorkoutId == workoutId)
        .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
        .ThenByDescending(ws => ws.WorkoutSessionId)
        .Take(50)
        .Select(ws => new
        {
            CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
            ws.OverallEffort,
            ws.Sets,
            Exercises = ws.LoggedExercises
                .OrderBy(le => le.Sequence)
                .Select(le => new
                {
                    le.ExerciseId,
                    le.Exercise!.Name,
                    le.LoggedWeight,
                    le.Effort,
                }).ToList(),
        })
        .ToListAsync();

    // Reverse to chronological order
    sessions.Reverse();

    return Results.Ok(new
    {
        dataPoints = sessions.Select(s => new
        {
            completedAt = s.CompletedAt,
            overallEffort = s.OverallEffort,
            sets = s.Sets,
            exercises = s.Exercises.Select(e => new
            {
                exerciseId = e.ExerciseId,
                exerciseName = e.Name,
                loggedWeight = e.LoggedWeight,
                effort = e.Effort,
            }),
        }),
    });
});

app.MapPost("/api/workouts", async (HttpContext context, WorkoutTrackerDbContext db) =>
{
    var body = await context.Request.ReadFromJsonAsync<WorkoutCreateRequest>();
    var name = body?.Name?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.Json(new { error = "Workout name is required." }, statusCode: 400);
    }

    if (name.Length > 150)
    {
        return Results.Json(new { error = "Workout name must be 150 characters or fewer." }, statusCode: 400);
    }

    var normalizedName = ExerciseQueryHelper.EscapeLike(name);
    var duplicate = await db.PlannedWorkouts
        .AnyAsync(pw => EF.Functions.ILike(pw.Name, normalizedName, "\\"));

    if (duplicate)
    {
        return Results.Json(new { error = "A workout with this name already exists." }, statusCode: 400);
    }

    var exercises = body?.Exercises ?? [];
    if (exercises.Length == 0)
    {
        return Results.Json(new { error = "At least one exercise is required." }, statusCode: 400);
    }

    var exerciseIds = exercises.Select(e => e.ExerciseId).ToArray();
    var distinctExerciseIds = exerciseIds.Distinct().ToArray();
    if (distinctExerciseIds.Length != exerciseIds.Length)
    {
        return Results.Json(new { error = "Each exercise may only be included once in a workout." }, statusCode: 400);
    }

    var validExerciseCount = await db.Exercises.CountAsync(e => distinctExerciseIds.Contains(e.ExerciseId));
    if (validExerciseCount != distinctExerciseIds.Length)
    {
        return Results.Json(new { error = "One or more selected exercises are invalid." }, statusCode: 400);
    }

    var plannedWorkout = new PlannedWorkout
    {
        PlannedWorkoutId = Guid.NewGuid(),
        Name = name,
    };

    for (var i = 0; i < exercises.Length; i++)
    {
        plannedWorkout.Exercises.Add(new PlannedWorkoutExercise
        {
            PlannedWorkoutExerciseId = Guid.NewGuid(),
            PlannedWorkoutId = plannedWorkout.PlannedWorkoutId,
            ExerciseId = exercises[i].ExerciseId,
            Sequence = i + 1,
            TargetReps = exercises[i].TargetReps,
            TargetWeight = exercises[i].TargetWeight,
        });
    }

    db.PlannedWorkouts.Add(plannedWorkout);
    await db.SaveChangesAsync();

    var created = await db.PlannedWorkouts
        .Include(pw => pw.Exercises)
        .ThenInclude(e => e.Exercise)
        .FirstAsync(pw => pw.PlannedWorkoutId == plannedWorkout.PlannedWorkoutId);

    return Results.Json(new
    {
        created.PlannedWorkoutId,
        created.Name,
        ExerciseCount = created.Exercises.Count,
        Exercises = created.Exercises
            .OrderBy(e => e.Sequence)
            .Select(e => new { e.ExerciseId, Name = e.Exercise.Name, e.TargetReps, e.TargetWeight })
            .ToList(),
    }, statusCode: 201);
});

app.MapPut("/api/workouts/{workoutId:guid}", async (Guid workoutId, HttpContext context, WorkoutTrackerDbContext db) =>
{
    var workout = await db.PlannedWorkouts
        .FirstOrDefaultAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (workout is null)
    {
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);
    }

    var body = await context.Request.ReadFromJsonAsync<WorkoutCreateRequest>();
    var name = body?.Name?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.Json(new { error = "Workout name is required." }, statusCode: 400);
    }

    if (name.Length > 150)
    {
        return Results.Json(new { error = "Workout name must be 150 characters or fewer." }, statusCode: 400);
    }

    var normalizedName = ExerciseQueryHelper.EscapeLike(name);
    var duplicate = await db.PlannedWorkouts
        .AnyAsync(pw => pw.PlannedWorkoutId != workoutId && EF.Functions.ILike(pw.Name, normalizedName, "\\"));

    if (duplicate)
    {
        return Results.Json(new { error = "A workout with this name already exists." }, statusCode: 400);
    }

    var exercises = body?.Exercises ?? [];
    if (exercises.Length == 0)
    {
        return Results.Json(new { error = "At least one exercise is required." }, statusCode: 400);
    }

    var exerciseIds = exercises.Select(e => e.ExerciseId).ToArray();
    var distinctExerciseIds = exerciseIds.Distinct().ToArray();
    if (distinctExerciseIds.Length != exerciseIds.Length)
    {
        return Results.Json(new { error = "Each exercise may only be included once in a workout." }, statusCode: 400);
    }

    var validExerciseCount = await db.Exercises.CountAsync(e => distinctExerciseIds.Contains(e.ExerciseId));
    if (validExerciseCount != distinctExerciseIds.Length)
    {
        return Results.Json(new { error = "One or more selected exercises are invalid." }, statusCode: 400);
    }

    workout.Name = name;

    await db.PlannedWorkoutExercises
        .Where(pwe => pwe.PlannedWorkoutId == workoutId)
        .ExecuteDeleteAsync();

    for (var i = 0; i < exercises.Length; i++)
    {
        db.PlannedWorkoutExercises.Add(new PlannedWorkoutExercise
        {
            PlannedWorkoutExerciseId = Guid.NewGuid(),
            PlannedWorkoutId = workoutId,
            ExerciseId = exercises[i].ExerciseId,
            Sequence = i + 1,
            TargetReps = exercises[i].TargetReps,
            TargetWeight = exercises[i].TargetWeight,
        });
    }

    await db.SaveChangesAsync();

    var updated = await db.PlannedWorkouts
        .Include(pw => pw.Exercises)
        .ThenInclude(e => e.Exercise)
        .FirstAsync(pw => pw.PlannedWorkoutId == workoutId);

    return Results.Ok(new
    {
        updated.PlannedWorkoutId,
        updated.Name,
        ExerciseCount = updated.Exercises.Count,
        Exercises = updated.Exercises
            .OrderBy(e => e.Sequence)
            .Select(e => new { e.ExerciseId, Name = e.Exercise.Name, e.TargetReps, e.TargetWeight })
            .ToList(),
    });
});

app.MapDelete("/api/workouts/{workoutId:guid}", async (Guid workoutId, WorkoutTrackerDbContext db) =>
{
    var workout = await db.PlannedWorkouts
        .FirstOrDefaultAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (workout is null)
    {
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);
    }

    db.PlannedWorkouts.Remove(workout);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPost("/api/workouts/{workoutId:guid}/sessions", async (Guid workoutId, HttpContext context, WorkoutTrackerDbContext db) =>
{
    const long maxRequestBytes = 1_048_576;
    if (context.Request.ContentLength > maxRequestBytes)
        return Results.Json(new { error = "Workout completion request is too large." }, statusCode: 413);
    var sizeFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
    if (sizeFeature is { IsReadOnly: false }) sizeFeature.MaxRequestBodySize = maxRequestBytes;
    var body = await context.Request.ReadFromJsonAsync<SessionCreateRequest>();
    if (body?.CompletionOperationId == Guid.Empty)
        return Results.Json(new { error = "Completion operation ID must not be empty." }, statusCode: 400);

    var fingerprint = body?.CompletionOperationId is not null
        ? "s1:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            WorkoutId = workoutId,
            body.Sets,
            body.OverallEffort,
            LoggedExercises = body.LoggedExercises ?? [],
        }))))
        : null;

    async Task<IResult?> FindReceiptAsync()
    {
        var receipt = await db.WorkoutCompletionReceipts.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OperationId == body!.CompletionOperationId);
        if (receipt is null) return null;

        var exists = await db.WorkoutSessions.AnyAsync(s => s.WorkoutSessionId == receipt.WorkoutSessionId);
        if (receipt.WorkoutId != workoutId || receipt.RequestFingerprint != fingerprint)
            return receipt.WorkoutId == workoutId && exists
                ? Results.Json(new { error = "Completion operation conflicts with an existing request.", receipt.WorkoutSessionId }, statusCode: 409)
                : Results.Json(new { error = "Completion operation conflicts with an existing request." }, statusCode: 409);
        return exists
            ? Results.Ok(new { receipt.WorkoutSessionId })
            : Results.Json(new { error = "The completed workout has been deleted." }, statusCode: 410);
    }

    async Task<IResult> CreateSessionAsync()
    {
    if (fingerprint is not null && await FindReceiptAsync() is { } replay)
        return replay;

    if (fingerprint is not null)
    {
        if (await db.WorkoutCompletionRejections.AsNoTracking().AnyAsync(
            r => r.OperationId == body!.CompletionOperationId && !r.RequestFingerprint.StartsWith("s1:")))
            return Results.Json(new { error = "This completion has a rejection recorded by an incompatible version and cannot be retried safely." }, statusCode: 409);

        var rejection = await db.WorkoutCompletionRejections.AsNoTracking().FirstOrDefaultAsync(
            r => r.OperationId == body!.CompletionOperationId && r.RequestFingerprint == fingerprint);
        if (rejection is not null)
            return Results.Json(new { error = rejection.Error, safeToEdit = true }, statusCode: rejection.StatusCode);
    }

    async Task<IResult> RejectAsync(string error, int statusCode = 400)
    {
        if (fingerprint is null)
            return Results.Json(new { error }, statusCode: statusCode);
        if (await db.WorkoutCompletionRejections.CountAsync(r => r.OperationId == body!.CompletionOperationId) >= 100)
            return Results.Json(new { error = "Too many rejected attempts for this completion. The saved completion remains locked." }, statusCode: 429);
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtextextended('workout-tracker-rejection-cap', 0))");
        if (await db.WorkoutCompletionRejections.CountAsync() >= 10_000)
            return Results.Json(new { error = "Workout completion rejection storage is full. The saved completion remains locked." }, statusCode: 429);
        db.WorkoutCompletionRejections.Add(new WorkoutCompletionRejection
        {
            OperationId = body!.CompletionOperationId!.Value,
            RequestFingerprint = fingerprint,
            Error = error,
            StatusCode = statusCode,
        });
        await db.SaveChangesAsync();
        return Results.Json(new { error, safeToEdit = true }, statusCode: statusCode);
    }

    var workout = await db.PlannedWorkouts
        .FirstOrDefaultAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (workout is null)
    {
        return await RejectAsync("Workout not found.", 404);
    }

    var loggedExercises = body?.LoggedExercises ?? [];

    if (body?.Sets is not 3 and not 5)
        return await RejectAsync("Sets must be 3 or 5.");
    if (loggedExercises.Length > 1_000)
        return await RejectAsync("A completion must contain at most 1,000 exercises.");

    foreach (var item in loggedExercises)
    {
        if (item is null)
            return await RejectAsync("Logged exercises must not contain null entries.");
        if (item.Notes is { Length: > 1_000 })
            return await RejectAsync("Completion notes must not exceed 1,000 characters.");
        if (item.LoggedWeight is { Length: > 100 })
            return await RejectAsync("Logged weight must not exceed 100 characters.");

        if (item.Effort is not null && (item.Effort < 1 || item.Effort > 10))
            return await RejectAsync("Effort must be between 1 and 10.");
    }

    if (body?.OverallEffort is not null && (body.OverallEffort < 1 || body.OverallEffort > 10))
        return await RejectAsync("Overall effort must be between 1 and 10.");

    if (loggedExercises.Length > 0)
    {
        var loggedExerciseIds = loggedExercises.Select(le => le.ExerciseId).Distinct().ToArray();
        var validPlannedExerciseIds = await db.PlannedWorkoutExercises
            .Where(pwe => pwe.PlannedWorkoutId == workoutId)
            .Select(pwe => pwe.ExerciseId)
            .ToListAsync();

        var invalidIds = loggedExerciseIds.Except(validPlannedExerciseIds).ToArray();
        if (invalidIds.Length > 0)
        {
            return await RejectAsync("One or more logged exercises are not part of this workout.");
        }
    }

    var session = new WorkoutSession
    {
        WorkoutSessionId = Guid.NewGuid(),
        PlannedWorkoutId = workoutId,
        WorkoutName = workout.Name,
        OverallEffort = body?.OverallEffort,
        Sets = body!.Sets,
    };

    foreach (var item in loggedExercises)
    {
        session.LoggedExercises.Add(new LoggedExercise
        {
            LoggedExerciseId = Guid.NewGuid(),
            WorkoutSessionId = session.WorkoutSessionId,
            ExerciseId = item.ExerciseId,
            LoggedWeight = item.LoggedWeight,
            Notes = item.Notes,
            Effort = item.Effort,
            Sequence = item.Sequence,
        });
    }

    db.WorkoutSessions.Add(session);
    if (fingerprint is not null)
        db.WorkoutCompletionReceipts.Add(new WorkoutCompletionReceipt
        {
            OperationId = body!.CompletionOperationId!.Value,
            WorkoutId = workoutId,
            WorkoutSessionId = session.WorkoutSessionId,
            RequestFingerprint = fingerprint,
        });
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "pk_workout_completion_receipts" })
    {
        db.ChangeTracker.Clear();
        return await FindReceiptAsync()
            ?? throw new InvalidOperationException("The completion receipt was not found after a unique-key conflict.", ex);
    }

    return Results.Json(new
    {
        session.WorkoutSessionId,
        session.PlannedWorkoutId,
        session.WorkoutName,
        session.OverallEffort,
        session.Sets,
        LoggedExercises = session.LoggedExercises.Select(le => new
        {
            le.LoggedExerciseId,
            le.ExerciseId,
            le.LoggedWeight,
            le.Notes,
            le.Effort,
            le.Sequence,
        }).ToList(),
    }, statusCode: 201);
    }

    if (fingerprint is null)
        return await CreateSessionAsync();

    return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({body!.CompletionOperationId!.Value.ToString()}, 0))");
        var result = await CreateSessionAsync();
        await transaction.CommitAsync();
        return result;
    });
});

app.MapGet("/api/sessions", async (WorkoutTrackerDbContext db) =>
{
    var sessions = await db.WorkoutSessions
        .Include(ws => ws.PlannedWorkout)
        .Include(ws => ws.LoggedExercises)
        .ThenInclude(le => le.Exercise)
        .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
        .Select(ws => new
        {
            ws.WorkoutSessionId,
            ws.PlannedWorkoutId,
            WorkoutName = ws.WorkoutName ?? (ws.PlannedWorkout != null ? ws.PlannedWorkout.Name : null),
            CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
            OverallEffort = ws.OverallEffort,
            Sets = ws.Sets,
            LoggedExercises = ws.LoggedExercises.Select(le => new
            {
                le.LoggedExerciseId,
                le.ExerciseId,
                ExerciseName = le.Exercise.Name,
                le.LoggedWeight,
                le.Notes,
                le.Effort,
            }).ToList(),
        })
        .ToListAsync();

    return Results.Ok(sessions);
});

app.MapGet("/api/sessions/latest", async (WorkoutTrackerDbContext db) =>
{
    var latest = await db.WorkoutSessions
        .Include(ws => ws.PlannedWorkout)
        .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
        .ThenByDescending(ws => ws.WorkoutSessionId)
        .Select(ws => new
        {
            WorkoutName = ws.WorkoutName ?? (ws.PlannedWorkout != null ? ws.PlannedWorkout.Name : null),
            CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
        })
        .FirstOrDefaultAsync();

    if (latest is null)
    {
        return Results.Ok(new { hasSession = false });
    }

    return Results.Ok(new
    {
        hasSession = true,
        workoutName = latest.WorkoutName,
        completedAt = (DateTime?)latest.CompletedAt,
    });
});

app.MapGet("/api/sessions/{sessionId:guid}", async (Guid sessionId, WorkoutTrackerDbContext db) =>
    await SessionDetailResponseBuilder.BuildAsync(sessionId, db));

app.MapPut("/api/sessions/{sessionId:guid}", async (Guid sessionId, HttpContext context, WorkoutTrackerDbContext db) =>
{
    var session = await db.WorkoutSessions
        .Include(ws => ws.LoggedExercises)
        .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId);

    if (session is null)
    {
        return Results.Json(new { error = "Session not found." }, statusCode: 404);
    }

    SessionUpdateRequest? body;
    bool setsSpecified;
    try
    {
        using var document = await JsonDocument.ParseAsync(context.Request.Body);
        setsSpecified = document.RootElement.EnumerateObject()
            .Any(property => property.Name.Equals("sets", StringComparison.OrdinalIgnoreCase));
        body = document.RootElement.Deserialize<SessionUpdateRequest>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    catch (JsonException)
    {
        return Results.Json(new { error = "A JSON request body is required." }, statusCode: 400);
    }
    catch (BadHttpRequestException)
    {
        return Results.Json(new { error = "A JSON request body is required." }, statusCode: 400);
    }
    catch (InvalidOperationException)
    {
        return Results.Json(new { error = "A JSON request body is required." }, statusCode: 400);
    }

    if (body is null)
    {
        return Results.Json(new { error = "A JSON request body is required." }, statusCode: 400);
    }

    var loggedExercises = body.LoggedExercises ?? [];

    if (setsSpecified && body.Sets is not null and not 3 and not 5)
        return Results.Json(new { error = "Sets must be 3 or 5." }, statusCode: 400);

    if (body.OverallEffort is not null && (body.OverallEffort < 1 || body.OverallEffort > 10))
        return Results.Json(new { error = "Overall effort must be between 1 and 10." }, statusCode: 400);

    foreach (var item in loggedExercises)
    {
        if (item.LoggedWeight is { Length: > 100 })
            return Results.Json(new { error = "Logged weight must not exceed 100 characters." }, statusCode: 400);

        if (item.Effort is not null && (item.Effort < 1 || item.Effort > 10))
            return Results.Json(new { error = "Effort must be between 1 and 10." }, statusCode: 400);
    }

    var requestedLoggedExerciseIds = loggedExercises.Select(le => le.LoggedExerciseId).ToArray();
    if (requestedLoggedExerciseIds.Distinct().Count() != requestedLoggedExerciseIds.Length)
    {
        return Results.Json(new { error = "Each logged exercise may only be included once." }, statusCode: 400);
    }

    var sessionLoggedExercises = session.LoggedExercises.ToDictionary(le => le.LoggedExerciseId);
    if (requestedLoggedExerciseIds.Any(id => !sessionLoggedExercises.ContainsKey(id)))
    {
        return Results.Json(new { error = "One or more logged exercises are not part of this session." }, statusCode: 400);
    }

    session.OverallEffort = body.OverallEffort;
    if (setsSpecified)
    {
        session.Sets = body.Sets;
    }
    foreach (var item in loggedExercises)
    {
        var loggedExercise = sessionLoggedExercises[item.LoggedExerciseId];
        loggedExercise.LoggedWeight = string.IsNullOrWhiteSpace(item.LoggedWeight) ? null : item.LoggedWeight;
        loggedExercise.Effort = item.Effort;
    }

    await db.SaveChangesAsync();

    return await SessionDetailResponseBuilder.BuildAsync(sessionId, db);
});

app.MapDelete("/api/sessions/{sessionId:guid}", async (Guid sessionId, WorkoutTrackerDbContext db) =>
{
    var session = await db.WorkoutSessions
        .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId);

    if (session is null)
    {
        return Results.Json(new { error = "Session not found." }, statusCode: 404);
    }

    db.WorkoutSessions.Remove(session);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();

// Expose Program class for WebApplicationFactory in tests
public partial class Program { }

internal sealed class WorkoutCreateRequest
{
    public string? Name { get; set; }
    public WorkoutExerciseItem[] Exercises { get; set; } = [];
}

internal sealed class WorkoutExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? TargetReps { get; set; }
    public string? TargetWeight { get; set; }
}

internal sealed class SessionCreateRequest
{
    public Guid? CompletionOperationId { get; set; }
    public int? Sets { get; set; }
    public int? OverallEffort { get; set; }
    public SessionLoggedExerciseItem[] LoggedExercises { get; set; } = [];
}

internal sealed class SessionLoggedExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public string? Notes { get; set; }
    public int? Effort { get; set; }
    public int? Sequence { get; set; }
}

internal sealed class SessionUpdateRequest
{
    public int? Sets { get; set; }
    public int? OverallEffort { get; set; }
    public SessionUpdateLoggedExerciseItem[] LoggedExercises { get; set; } = [];
}

internal sealed class SessionUpdateLoggedExerciseItem
{
    public Guid LoggedExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public int? Effort { get; set; }
}

internal sealed class ExerciseCreateRequest
{
    public string? Name { get; set; }
    public Guid[] MuscleIds { get; set; } = [];
}

internal sealed class MuscleCreateRequest
{
    public string? Name { get; set; }
}

internal sealed class MuscleUpdateRequest
{
    public string? Name { get; set; }
}

internal sealed record HistoricalExerciseData(Guid ExerciseId, string? LoggedWeight, int? Effort, int? Sequence);

internal sealed record HistoricalSessionData(
    Guid WorkoutSessionId,
    DateTime CompletedAt,
    int? OverallEffort,
    int? Sets,
    List<HistoricalExerciseData> LoggedExercises);

internal sealed record LatestExerciseComparison(
    Guid ExerciseId,
    string? LoggedWeight,
    int? Effort,
    int? Sequence,
    int? Sets,
    DateTime CompletedAt);

internal static class PreviousExerciseDataSelector
{
    internal const int MaxSessionsToScan = 200;

    internal static Dictionary<Guid, LatestExerciseComparison> SelectLatestUsablePerExercise(
        IEnumerable<HistoricalSessionData> sessions,
        ISet<Guid> targetExerciseIds)
    {
        var selected = new Dictionary<Guid, LatestExerciseComparison>();

        foreach (var session in sessions.Take(MaxSessionsToScan))
        {
            foreach (var loggedExercise in session.LoggedExercises)
            {
                if (!targetExerciseIds.Contains(loggedExercise.ExerciseId) ||
                    selected.ContainsKey(loggedExercise.ExerciseId) ||
                    !HasUsableComparisonData(loggedExercise.LoggedWeight, loggedExercise.Effort))
                {
                    continue;
                }

                selected[loggedExercise.ExerciseId] = new LatestExerciseComparison(
                    loggedExercise.ExerciseId,
                    loggedExercise.LoggedWeight,
                    loggedExercise.Effort,
                    loggedExercise.Sequence,
                    session.Sets,
                    session.CompletedAt);
            }

            if (selected.Count == targetExerciseIds.Count)
            {
                break;
            }
        }

        return selected;
    }

    internal static bool HasUsableComparisonData(string? loggedWeight, int? effort) =>
        !string.IsNullOrWhiteSpace(loggedWeight) || effort is not null;
}

internal static class SessionDetailResponseBuilder
{
    internal static async Task<IResult> BuildAsync(Guid sessionId, WorkoutTrackerDbContext db)
    {
        var session = await db.WorkoutSessions
            .Where(ws => ws.WorkoutSessionId == sessionId)
            .Select(ws => new
            {
                ws.WorkoutSessionId,
                ws.PlannedWorkoutId,
                WorkoutName = ws.WorkoutName ?? (ws.PlannedWorkout != null ? ws.PlannedWorkout.Name : null),
                CompletedAt = EF.Property<DateTime>(ws, "CompletedAt"),
                ws.OverallEffort,
                ws.Sets,
                Exercises = ws.LoggedExercises
                    .OrderBy(le => le.Sequence)
                    .Select(le => new
                    {
                        le.LoggedExerciseId,
                        le.ExerciseId,
                        ExerciseName = le.Exercise.Name,
                        le.LoggedWeight,
                        le.Effort,
                    }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (session is null)
        {
            return Results.Json(new { error = "Session not found." }, statusCode: 404);
        }

        var sessionExerciseIds = session.Exercises.Select(e => e.ExerciseId).ToHashSet();
        var priorSessions = session.PlannedWorkoutId is null
            ? new List<HistoricalSessionData>()
            : await db.WorkoutSessions
                .Where(ws =>
                    ws.PlannedWorkoutId == session.PlannedWorkoutId &&
                    (
                        EF.Property<DateTime>(ws, "CompletedAt") < session.CompletedAt ||
                        (EF.Property<DateTime>(ws, "CompletedAt") == session.CompletedAt &&
                         ws.WorkoutSessionId.CompareTo(sessionId) < 0)
                    ))
                .OrderByDescending(ws => EF.Property<DateTime>(ws, "CompletedAt"))
                .ThenByDescending(ws => ws.WorkoutSessionId)
                .Take(PreviousExerciseDataSelector.MaxSessionsToScan)
                .Select(ws => new HistoricalSessionData(
                    ws.WorkoutSessionId,
                    EF.Property<DateTime>(ws, "CompletedAt"),
                    ws.OverallEffort,
                    ws.Sets,
                    ws.LoggedExercises
                        .Where(le => sessionExerciseIds.Contains(le.ExerciseId))
                        .Select(le => new HistoricalExerciseData(
                            le.ExerciseId,
                            le.LoggedWeight,
                            le.Effort,
                            le.Sequence))
                        .ToList()))
                .ToListAsync();

        var priorSession = priorSessions.FirstOrDefault();
        var previousExercises = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(priorSessions, sessionExerciseIds);

        return Results.Ok(new
        {
            session.WorkoutSessionId,
            session.PlannedWorkoutId,
            session.WorkoutName,
            session.CompletedAt,
            session.OverallEffort,
            session.Sets,
            PreviousOverallEffort = priorSession?.OverallEffort,
            Exercises = session.Exercises.Select(le =>
            {
                // Single-user app: no cross-user access control needed (consistent with features 008 and 013)
                previousExercises.TryGetValue(le.ExerciseId, out var prior);
                return new
                {
                    le.LoggedExerciseId,
                    le.ExerciseId,
                    le.ExerciseName,
                    le.LoggedWeight,
                    le.Effort,
                    PreviousWeight = prior?.LoggedWeight,
                    PreviousEffort = prior?.Effort,
                    PreviousSets = prior?.Sets,
                };
            }).ToList(),
        });
    }
}

internal static class ExerciseQueryHelper
{
    /// <summary>
    /// Escapes LIKE pattern special characters so the value is matched literally by ILike.
    /// </summary>
    internal static string EscapeLike(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
