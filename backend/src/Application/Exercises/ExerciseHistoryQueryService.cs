using Api.Infrastructure.Repositories;

namespace Api.Application.Exercises;

public sealed class ExerciseHistoryQueryService(IExerciseEntryRepository exerciseEntryRepository)
{
    public Task<PagedExerciseHistory> GetHistoryAsync(Guid userId, string exerciseName, int page, int pageSize, CancellationToken cancellationToken)
    {
        var normalized = ExerciseNameNormalizer.Normalize(exerciseName);
        return exerciseEntryRepository.GetExerciseHistoryAsync(userId, exerciseName, normalized, page, pageSize, cancellationToken);
    }
}

public sealed record PagedExerciseHistory(
    string ExerciseName,
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<ExerciseEntryRecord> Entries);
