using System.ComponentModel.DataAnnotations;

namespace WorkoutTracker.Infrastructure.Data.Models;

public class PlannedWorkout
{
    public Guid PlannedWorkoutId { get; set; }

    [MaxLength(150)]
    public required string Name { get; set; }

    public ICollection<PlannedWorkoutExercise> Exercises { get; set; } = [];

    public ICollection<WorkoutSession> Sessions { get; set; } = [];
}
