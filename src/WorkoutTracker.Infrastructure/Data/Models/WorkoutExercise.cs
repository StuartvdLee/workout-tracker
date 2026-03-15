namespace WorkoutTracker.Infrastructure.Data.Models;

public class WorkoutExercise
{
    public Guid WorkoutExerciseId { get; set; }

    public Guid WorkoutId { get; set; }

    public Guid ExerciseId { get; set; }

    public int? Sets { get; set; }

    public int? Reps { get; set; }

    public decimal? Weight { get; set; }

    public Workout Workout { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
