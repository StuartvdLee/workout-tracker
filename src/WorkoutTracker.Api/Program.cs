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

    var duplicate = await db.Exercises
        .AnyAsync(e => EF.Functions.ILike(e.Name, name));

    if (duplicate)
    {
        return Results.Json(new { error = "An exercise with this name already exists." }, statusCode: 400);
    }

    var muscleIds = body?.MuscleIds ?? [];
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
        .Include(e => e.ExerciseMuscles)
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

    var duplicate = await db.Exercises
        .AnyAsync(e => e.ExerciseId != exerciseId && EF.Functions.ILike(e.Name, name));

    if (duplicate)
    {
        return Results.Json(new { error = "An exercise with this name already exists." }, statusCode: 400);
    }

    var muscleIds = body?.MuscleIds ?? [];
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
    db.ExerciseMuscles.RemoveRange(exercise.ExerciseMuscles);

    foreach (var muscleId in muscleIds)
    {
        exercise.ExerciseMuscles.Add(new ExerciseMuscle
        {
            ExerciseMuscleId = Guid.NewGuid(),
            ExerciseId = exercise.ExerciseId,
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

app.Run();

internal sealed class ExerciseCreateRequest
{
    public string? Name { get; set; }
    public Guid[] MuscleIds { get; set; } = [];
}
