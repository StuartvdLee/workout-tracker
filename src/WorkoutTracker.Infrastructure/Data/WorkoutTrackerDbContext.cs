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
    public DbSet<Muscle> Muscles => Set<Muscle>();
    public DbSet<ExerciseMuscle> ExerciseMuscles => Set<ExerciseMuscle>();
    public DbSet<PlannedWorkout> PlannedWorkouts => Set<PlannedWorkout>();
    public DbSet<PlannedWorkoutExercise> PlannedWorkoutExercises => Set<PlannedWorkoutExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<LoggedExercise> LoggedExercises => Set<LoggedExercise>();

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
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Name)
                .IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("ck_exercises_name_length", "length(name) <= 150"));
        });

        modelBuilder.Entity<Muscle>(entity =>
        {
            entity.HasKey(e => e.MuscleId);
            entity.Property(e => e.Name).IsRequired();

            entity.HasData(
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-00000000000c"), Name = "Adductors" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "Back" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "Biceps" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000003"), Name = "Calves" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000004"), Name = "Chest" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000005"), Name = "Core" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000006"), Name = "Forearms" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000007"), Name = "Glutes" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000008"), Name = "Hamstrings" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-000000000009"), Name = "Quads" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-00000000000a"), Name = "Shoulders" },
                new Muscle { MuscleId = Guid.Parse("a1000000-0000-0000-0000-00000000000b"), Name = "Triceps" }
            );
        });

        modelBuilder.Entity<ExerciseMuscle>(entity =>
        {
            entity.HasKey(e => e.ExerciseMuscleId);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.ExerciseMuscles)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Muscle)
                .WithMany(m => m.ExerciseMuscles)
                .HasForeignKey(e => e.MuscleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ExerciseId, e.MuscleId })
                .IsUnique();
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

        modelBuilder.Entity<PlannedWorkout>(entity =>
        {
            entity.HasKey(e => e.PlannedWorkoutId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("ck_planned_workouts_name_length", "length(name) <= 150"));
            entity.Property<DateTime>("CreatedAt").HasDefaultValueSql("now()");
            entity.Property<DateTime>("UpdatedAt").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<PlannedWorkoutExercise>(entity =>
        {
            entity.HasKey(e => e.PlannedWorkoutExerciseId);

            entity.HasOne(e => e.PlannedWorkout)
                .WithMany(pw => pw.Exercises)
                .HasForeignKey(e => e.PlannedWorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.PlannedWorkoutExercises)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.PlannedWorkoutId, e.ExerciseId }).IsUnique();
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.HasKey(e => e.WorkoutSessionId);

            entity.Property(e => e.WorkoutName).HasMaxLength(150);

            entity.HasOne(e => e.PlannedWorkout)
                .WithMany(pw => pw.Sessions)
                .HasForeignKey(e => e.PlannedWorkoutId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property<DateTime>("CompletedAt").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<LoggedExercise>(entity =>
        {
            entity.HasKey(e => e.LoggedExerciseId);

            entity.HasOne(e => e.WorkoutSession)
                .WithMany(ws => ws.LoggedExercises)
                .HasForeignKey(e => e.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.LoggedExercises)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint(
                "ck_logged_exercise_effort_range",
                "effort IS NULL OR (effort >= 1 AND effort <= 10)"));
        });
    }
}
