using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Infrastructure.Data;
using WorkoutTracker.Infrastructure.Data.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<WorkoutTrackerDbContext>("workout-tracker-db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedWorkoutTypesAsync(dbContext);
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

app.Run();

static async Task SeedWorkoutTypesAsync(WorkoutTrackerDbContext db)
{
    if (await db.WorkoutTypes.AnyAsync())
    {
        return;
    }

    db.WorkoutTypes.AddRange(
        new WorkoutType { WorkoutTypeId = Guid.NewGuid(), Name = "Push" },
        new WorkoutType { WorkoutTypeId = Guid.NewGuid(), Name = "Pull" },
        new WorkoutType { WorkoutTypeId = Guid.NewGuid(), Name = "Legs" }
    );

    await db.SaveChangesAsync();
}
