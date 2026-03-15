namespace WorkoutTracker.Infrastructure.Data.Models;

public class Workout
{
    public Guid WorkoutId { get; set; }

    public Guid WorkoutTypeId { get; set; }

    public DateOnly WorkoutDate { get; set; }

    public WorkoutType WorkoutType { get; set; } = null!;

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
