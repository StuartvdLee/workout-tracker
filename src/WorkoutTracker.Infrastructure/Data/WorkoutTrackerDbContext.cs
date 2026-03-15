using Microsoft.EntityFrameworkCore;
using WorkoutTracker.Infrastructure.Data.Models;

namespace WorkoutTracker.Infrastructure.Data;

public class WorkoutTrackerDbContext(DbContextOptions<WorkoutTrackerDbContext> options)
    : DbContext(options)
{
    public DbSet<WorkoutType> WorkoutTypes => Set<WorkoutType>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workout_tracker");

        modelBuilder.Entity<WorkoutType>(entity =>
        {
            entity.HasKey(e => e.WorkoutTypeId);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<Workout>(entity =>
        {
            entity.HasKey(e => e.WorkoutId);
            entity.Property(e => e.WorkoutDate).IsRequired();

            entity.HasOne(e => e.WorkoutType)
                .WithMany(wt => wt.Workouts)
                .HasForeignKey(e => e.WorkoutTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.ExerciseId);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<WorkoutExercise>(entity =>
        {
            entity.HasKey(e => e.WorkoutExerciseId);

            entity.HasOne(e => e.Workout)
                .WithMany(w => w.WorkoutExercises)
                .HasForeignKey(e => e.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.WorkoutExercises)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
