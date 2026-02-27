using Api.Application.Exercises;
using Api.Contracts;
using Api.Infrastructure.Repositories;

namespace Api.Application.Sessions;

public sealed class SessionCommandService(
    IWorkoutSessionRepository workoutSessionRepository,
    IExerciseEntryRepository exerciseEntryRepository)
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<WorkoutSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var entity = await workoutSessionRepository.CreateAsync(DefaultUserId, request.StartedAt, request.Notes, cancellationToken);
        return new WorkoutSessionResponse(entity.Id, entity.StartedAt, entity.EndedAt, entity.Notes);
    }

    public async Task<ExerciseEntryResponse> AddEntryAsync(Guid sessionId, CreateExerciseEntryRequest request, CancellationToken cancellationToken)
    {
        Validate(request.Sets, request.Reps, request.Weight);
        var entity = await exerciseEntryRepository.CreateAsync(
            sessionId,
            DefaultUserId,
            request.ExerciseName,
            ExerciseNameNormalizer.Normalize(request.ExerciseName),
            request.Sets,
            request.Reps,
            request.Weight,
            request.WeightUnit,
            request.PerformedAt,
            cancellationToken);
        return ToResponse(entity);
    }

    public async Task<IReadOnlyList<ExerciseEntryResponse>> ListEntriesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var entries = await exerciseEntryRepository.ListBySessionAsync(sessionId, cancellationToken);
        return entries.Select(ToResponse).ToList();
    }

    public async Task<ExerciseEntryResponse?> UpdateEntryAsync(Guid entryId, UpdateExerciseEntryRequest request, CancellationToken cancellationToken)
    {
        var updated = await exerciseEntryRepository.UpdateAsync(entryId, request.Sets, request.Reps, request.Weight, request.PerformedAt, cancellationToken);
        return updated is null ? null : ToResponse(updated);
    }

    public Task<bool> DeleteEntryAsync(Guid entryId, CancellationToken cancellationToken) =>
        exerciseEntryRepository.DeleteAsync(entryId, cancellationToken);

    private static ExerciseEntryResponse ToResponse(ExerciseEntryRecord entity) =>
        new(entity.Id, entity.SessionId, entity.ExerciseName, entity.NormalizedExerciseName, entity.Sets, entity.Reps, entity.Weight, entity.WeightUnit, entity.PerformedAt);

    private static void Validate(int sets, int reps, decimal weight)
    {
        if (sets is < 1 or > 100 || reps is < 1 or > 500 || weight is < 0 or > 5000)
        {
            throw new ArgumentOutOfRangeException(nameof(sets), "Invalid sets/reps/weight values.");
        }
    }
}
