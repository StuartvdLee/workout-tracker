using Microsoft.EntityFrameworkCore;

namespace WorkoutTracker.Infrastructure.Data;

public class WorkoutTrackerDbContext(DbContextOptions<WorkoutTrackerDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workout_tracker");
    }
}
