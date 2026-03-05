using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public sealed class WorkoutTrackerDbContext(DbContextOptions<WorkoutTrackerDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<WorkoutSessionEntity> WorkoutSessions => Set<WorkoutSessionEntity>();
    public DbSet<ExerciseEntryEntity> ExerciseEntries => Set<ExerciseEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired();
        });

        modelBuilder.Entity<WorkoutSessionEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WorkoutType).HasMaxLength(32).IsRequired();
            entity.HasOne<UserEntity>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.StartedAt }).HasDatabaseName("IX_WorkoutSession_User_StartedAt");
        });

        modelBuilder.Entity<ExerciseEntryEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExerciseName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NormalizedExerciseName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Weight).HasPrecision(8, 2);
            entity.HasOne<WorkoutSessionEntity>()
                .WithMany()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.NormalizedExerciseName, x.PerformedAt })
                .HasDatabaseName("IX_ExerciseEntry_User_Normalized_PerformedAt");
            entity.HasIndex(x => new { x.SessionId, x.PerformedAt })
                .HasDatabaseName("IX_ExerciseEntry_Session_PerformedAt");
        });
    }
}

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class WorkoutSessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string WorkoutType { get; set; } = "Push";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ExerciseEntryEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string NormalizedExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }
    public string WeightUnit { get; set; } = "kg";
    public DateTimeOffset PerformedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
