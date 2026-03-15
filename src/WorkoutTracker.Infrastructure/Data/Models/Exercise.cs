namespace WorkoutTracker.Infrastructure.Data.Models;

public class Exercise
{
    public Guid ExerciseId { get; set; }

    public required string Name { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];
}
