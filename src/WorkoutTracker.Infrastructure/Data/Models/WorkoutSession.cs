namespace WorkoutTracker.Infrastructure.Data.Models;

public class WorkoutSession
{
    public Guid WorkoutSessionId { get; set; }

    public Guid? PlannedWorkoutId { get; set; }

    public PlannedWorkout? PlannedWorkout { get; set; }

    public string? WorkoutName { get; set; }

    public ICollection<LoggedExercise> LoggedExercises { get; set; } = [];
}
