using System.ComponentModel.DataAnnotations;

namespace WorkoutTracker.Infrastructure.Data.Models;

public class Exercise
{
    public Guid ExerciseId { get; set; }

    [MaxLength(150)]
    public required string Name { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = [];

    public ICollection<ExerciseMuscle> ExerciseMuscles { get; set; } = [];
}
