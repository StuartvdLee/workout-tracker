using Api.Infrastructure.Repositories;

namespace Api.Application.Progression;

public sealed class ProgressComparisonService(IExerciseEntryRepository exerciseEntryRepository)
{
    public async Task<ProgressComparisonResult?> BuildAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var current = await exerciseEntryRepository.GetByIdAsync(entryId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var previous = await exerciseEntryRepository.GetPreviousEntryAsync(current.UserId, current.NormalizedExerciseName, current.PerformedAt, cancellationToken);
        var best = await exerciseEntryRepository.GetBestEntryAsync(current.UserId, current.NormalizedExerciseName, cancellationToken);

        var currentVolume = ComputeVolume(current.Sets, current.Reps, current.Weight);
        var previousVolume = previous is null ? (decimal?)null : ComputeVolume(previous.Sets, previous.Reps, previous.Weight);
        var bestVolume = best is null ? (decimal?)null : ComputeVolume(best.Sets, best.Reps, best.Weight);

        return new ProgressComparisonResult(
            current.ExerciseName,
            current.Id,
            currentVolume,
            previous?.Id,
            previousVolume,
            best?.Id,
            bestVolume,
            previousVolume is null ? null : currentVolume - previousVolume.Value,
            bestVolume is null ? null : currentVolume - bestVolume.Value,
            Status(currentVolume, previousVolume),
            Status(currentVolume, bestVolume));
    }

    private static decimal ComputeVolume(int sets, int reps, decimal weight) => sets * reps * weight;

    private static string Status(decimal current, decimal? baseline)
    {
        if (baseline is null)
        {
            return "no-baseline";
        }

        if (current > baseline.Value) return "improved";
        if (current < baseline.Value) return "declined";
        return "unchanged";
    }
}

public sealed record ProgressComparisonResult(
    string ExerciseName,
    Guid CurrentEntryId,
    decimal CurrentVolume,
    Guid? PreviousEntryId,
    decimal? PreviousVolume,
    Guid? BestEntryId,
    decimal? BestVolume,
    decimal? DeltaFromPrevious,
    decimal? DeltaFromBest,
    string StatusVsPrevious,
    string StatusVsBest);
