namespace WorkoutTracker.Infrastructure.Data.Models;

public class PlannedWorkoutExercise
{
    public Guid PlannedWorkoutExerciseId { get; set; }

    public Guid PlannedWorkoutId { get; set; }

    public Guid ExerciseId { get; set; }

    public int Sequence { get; set; }

    public string? TargetReps { get; set; }

    public string? TargetWeight { get; set; }

    public PlannedWorkout PlannedWorkout { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
