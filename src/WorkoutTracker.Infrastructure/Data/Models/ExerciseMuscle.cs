namespace WorkoutTracker.Infrastructure.Data.Models;

public class ExerciseMuscle
{
    public Guid ExerciseMuscleId { get; set; }

    public Guid ExerciseId { get; set; }

    public Guid MuscleId { get; set; }

    public Exercise Exercise { get; set; } = null!;

    public Muscle Muscle { get; set; } = null!;
}
