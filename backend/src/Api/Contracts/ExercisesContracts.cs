namespace Api.Contracts;

public sealed record CreateExerciseEntryRequest(
    string ExerciseName,
    int Sets,
    int Reps,
    decimal Weight,
    string WeightUnit,
    DateTimeOffset PerformedAt);

public sealed record UpdateExerciseEntryRequest(
    int? Sets,
    int? Reps,
    decimal? Weight,
    DateTimeOffset? PerformedAt);

public sealed record ExerciseEntryResponse(
    Guid Id,
    Guid SessionId,
    string ExerciseName,
    string NormalizedExerciseName,
    int Sets,
    int Reps,
    decimal Weight,
    string WeightUnit,
    DateTimeOffset PerformedAt);
