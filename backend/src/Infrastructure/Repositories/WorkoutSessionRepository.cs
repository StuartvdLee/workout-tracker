using Api.Infrastructure.Persistence;

namespace Api.Infrastructure.Repositories;

public interface IWorkoutSessionRepository
{
    Task<WorkoutSessionEntity> CreateAsync(Guid userId, DateTimeOffset startedAt, string? notes, CancellationToken cancellationToken);
}

public sealed class WorkoutSessionRepository(WorkoutTrackerDbContext dbContext) : IWorkoutSessionRepository
{
    public async Task<WorkoutSessionEntity> CreateAsync(Guid userId, DateTimeOffset startedAt, string? notes, CancellationToken cancellationToken)
    {
        var entity = new WorkoutSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartedAt = startedAt,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.WorkoutSessions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
