using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Infrastructure.Data;

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

app.Run();
