namespace Api.Application.Exercises;

public static class ExerciseNameNormalizer
{
    public static string Normalize(string exerciseName)
    {
        var trimmed = exerciseName.Trim();
        var collapsed = string.Join(' ', trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return collapsed.ToLowerInvariant();
    }
}
