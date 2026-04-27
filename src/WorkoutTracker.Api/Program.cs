using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Infrastructure.Data;
using WorkoutTracker.Infrastructure.Data.Models;

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
    var workout = await db.PlannedWorkouts
        .FirstOrDefaultAsync(pw => pw.PlannedWorkoutId == workoutId);

    if (workout is null)
    {
        return Results.Json(new { error = "Workout not found." }, statusCode: 404);
    }

    var body = await context.Request.ReadFromJsonAsync<SessionCreateRequest>();
    var loggedExercises = body?.LoggedExercises ?? [];

    foreach (var item in loggedExercises)
    {
        if (item.LoggedWeight is { Length: > 100 })
            return Results.Json(new { error = "Logged weight must not exceed 100 characters." }, statusCode: 400);

        if (item.Effort is not null && (item.Effort < 1 || item.Effort > 10))
            return Results.Json(new { error = "Effort must be between 1 and 10." }, statusCode: 400);
    }

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
            return Results.Json(new { error = "One or more logged exercises are not part of this workout." }, statusCode: 400);
        }
    }

    var session = new WorkoutSession
    {
        WorkoutSessionId = Guid.NewGuid(),
        PlannedWorkoutId = workoutId,
        WorkoutName = workout.Name,
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
        });
    }

    db.WorkoutSessions.Add(session);
    await db.SaveChangesAsync();

    return Results.Json(new
    {
        session.WorkoutSessionId,
        session.PlannedWorkoutId,
        session.WorkoutName,
        LoggedExercises = session.LoggedExercises.Select(le => new
        {
            le.LoggedExerciseId,
            le.ExerciseId,
            le.LoggedWeight,
            le.Notes,
            le.Effort,
        }).ToList(),
    }, statusCode: 201);
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
    public SessionLoggedExerciseItem[] LoggedExercises { get; set; } = [];
}

internal sealed class SessionLoggedExerciseItem
{
    public Guid ExerciseId { get; set; }
    public string? LoggedWeight { get; set; }
    public string? Notes { get; set; }
    public int? Effort { get; set; }
}

internal sealed class ExerciseCreateRequest
{
    public string? Name { get; set; }
    public Guid[] MuscleIds { get; set; } = [];
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
