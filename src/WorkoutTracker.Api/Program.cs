using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<WorkoutTrackerDbContext>("workout-tracker-db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

app.Run();
