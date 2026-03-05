namespace Api.Contracts;

public sealed record CreateSessionRequest(string? WorkoutType, string? Notes);
public sealed record WorkoutSessionResponse(Guid Id, string WorkoutType, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes);
