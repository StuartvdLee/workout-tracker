namespace WorkoutTracker.Infrastructure.Data.Models;

public class WorkoutCompletionReceipt
{
    public Guid OperationId { get; set; }
    public Guid WorkoutId { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public string RequestFingerprint { get; set; } = string.Empty;
}
