namespace WorkoutTracker.Infrastructure.Data.Models;

public class Muscle
{
    public Guid MuscleId { get; set; }

    public required string Name { get; set; }

    public ICollection<ExerciseMuscle> ExerciseMuscles { get; set; } = [];
}
