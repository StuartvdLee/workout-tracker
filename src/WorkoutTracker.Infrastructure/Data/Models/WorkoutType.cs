namespace WorkoutTracker.Infrastructure.Data.Models;

public class WorkoutType
{
    public Guid WorkoutTypeId { get; set; }

    public required string Name { get; set; }

    public ICollection<Workout> Workouts { get; set; } = [];
}
