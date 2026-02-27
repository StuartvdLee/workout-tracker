namespace Api.Contracts;

public sealed record CreateSessionRequest(DateTimeOffset StartedAt, string? Notes);
public sealed record WorkoutSessionResponse(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string? Notes);
