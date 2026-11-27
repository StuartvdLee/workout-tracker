namespace WorkoutTracker.Infrastructure.Data.Models;

public class WorkoutCompletionRejection
{
    public Guid OperationId { get; set; }
    public string RequestFingerprint { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}
