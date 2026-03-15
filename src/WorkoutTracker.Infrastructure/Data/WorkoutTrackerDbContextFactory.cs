using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorkoutTracker.Infrastructure.Data;

public class WorkoutTrackerDbContextFactory : IDesignTimeDbContextFactory<WorkoutTrackerDbContext>
{
    public WorkoutTrackerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerDbContext>();
        optionsBuilder.UseNpgsql();
        optionsBuilder.UseSnakeCaseNamingConvention();

        return new WorkoutTrackerDbContext(optionsBuilder.Options);
    }
}
