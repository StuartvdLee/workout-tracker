namespace WorkoutTracker.Infrastructure.Data.Models;

public class LoggedExercise
{
    public Guid LoggedExerciseId { get; set; }

    public Guid WorkoutSessionId { get; set; }

    public Guid ExerciseId { get; set; }

    public int? LoggedReps { get; set; }

    public string? LoggedWeight { get; set; }

    public string? Notes { get; set; }

    public WorkoutSession WorkoutSession { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
