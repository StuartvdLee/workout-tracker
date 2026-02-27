using Api.Application.Exercises;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Repositories;

public sealed record ExerciseEntryRecord(
    Guid Id,
    Guid SessionId,
    Guid UserId,
    string ExerciseName,
    string NormalizedExerciseName,
    int Sets,
    int Reps,
    decimal Weight,
    string WeightUnit,
    DateTimeOffset PerformedAt);

public interface IExerciseEntryRepository
{
    Task<ExerciseEntryRecord> CreateAsync(
        Guid sessionId,
        Guid userId,
        string exerciseName,
        string normalizedExerciseName,
        int sets,
        int reps,
        decimal weight,
        string weightUnit,
        DateTimeOffset performedAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExerciseEntryRecord>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<ExerciseEntryRecord?> GetByIdAsync(Guid entryId, CancellationToken cancellationToken);
    Task<PagedExerciseHistory> GetExerciseHistoryAsync(Guid userId, string exerciseName, string normalizedExerciseName, int page, int pageSize, CancellationToken cancellationToken);
    Task<ExerciseEntryRecord?> GetPreviousEntryAsync(Guid userId, string normalizedExerciseName, DateTimeOffset performedAt, CancellationToken cancellationToken);
    Task<ExerciseEntryRecord?> GetBestEntryAsync(Guid userId, string normalizedExerciseName, CancellationToken cancellationToken);
    Task<ExerciseEntryRecord?> UpdateAsync(Guid entryId, int? sets, int? reps, decimal? weight, DateTimeOffset? performedAt, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid entryId, CancellationToken cancellationToken);
}

public sealed class ExerciseEntryRepository(WorkoutTrackerDbContext dbContext) : IExerciseEntryRepository
{
    public async Task<ExerciseEntryRecord> CreateAsync(
        Guid sessionId,
        Guid userId,
        string exerciseName,
        string normalizedExerciseName,
        int sets,
        int reps,
        decimal weight,
        string weightUnit,
        DateTimeOffset performedAt,
        CancellationToken cancellationToken)
    {
        var entity = new ExerciseEntryEntity
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            ExerciseName = exerciseName,
            NormalizedExerciseName = normalizedExerciseName,
            Sets = sets,
            Reps = reps,
            Weight = weight,
            WeightUnit = weightUnit,
            PerformedAt = performedAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ExerciseEntries.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToRecord(entity);
    }

    public async Task<IReadOnlyList<ExerciseEntryRecord>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await dbContext.ExerciseEntries
            .AsNoTracking()
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.PerformedAt)
            .Select(ToRecordExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<ExerciseEntryRecord?> GetByIdAsync(Guid entryId, CancellationToken cancellationToken)
    {
        return await dbContext.ExerciseEntries
            .AsNoTracking()
            .Where(x => x.Id == entryId)
            .Select(ToRecordExpression())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedExerciseHistory> GetExerciseHistoryAsync(Guid userId, string exerciseName, string normalizedExerciseName, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.ExerciseEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.NormalizedExerciseName == normalizedExerciseName);

        var total = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(x => x.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToRecordExpression())
            .ToListAsync(cancellationToken);

        return new PagedExerciseHistory(exerciseName, page, pageSize, total, entries);
    }

    public async Task<ExerciseEntryRecord?> GetPreviousEntryAsync(Guid userId, string normalizedExerciseName, DateTimeOffset performedAt, CancellationToken cancellationToken)
    {
        return await dbContext.ExerciseEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.NormalizedExerciseName == normalizedExerciseName && x.PerformedAt < performedAt)
            .OrderByDescending(x => x.PerformedAt)
            .Select(ToRecordExpression())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExerciseEntryRecord?> GetBestEntryAsync(Guid userId, string normalizedExerciseName, CancellationToken cancellationToken)
    {
        return await dbContext.ExerciseEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.NormalizedExerciseName == normalizedExerciseName)
            .OrderByDescending(x => x.Sets * x.Reps * x.Weight)
            .Select(ToRecordExpression())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ExerciseEntryRecord?> UpdateAsync(Guid entryId, int? sets, int? reps, decimal? weight, DateTimeOffset? performedAt, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExerciseEntries.FirstOrDefaultAsync(x => x.Id == entryId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (sets.HasValue) entity.Sets = sets.Value;
        if (reps.HasValue) entity.Reps = reps.Value;
        if (weight.HasValue) entity.Weight = weight.Value;
        if (performedAt.HasValue) entity.PerformedAt = performedAt.Value;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    public async Task<bool> DeleteAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExerciseEntries.FirstOrDefaultAsync(x => x.Id == entryId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.ExerciseEntries.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ExerciseEntryRecord ToRecord(ExerciseEntryEntity entity) =>
        new(entity.Id, entity.SessionId, entity.UserId, entity.ExerciseName, entity.NormalizedExerciseName, entity.Sets, entity.Reps, entity.Weight, entity.WeightUnit, entity.PerformedAt);

    private static System.Linq.Expressions.Expression<Func<ExerciseEntryEntity, ExerciseEntryRecord>> ToRecordExpression() =>
        entity => new ExerciseEntryRecord(
            entity.Id,
            entity.SessionId,
            entity.UserId,
            entity.ExerciseName,
            entity.NormalizedExerciseName,
            entity.Sets,
            entity.Reps,
            entity.Weight,
            entity.WeightUnit,
            entity.PerformedAt);
}
