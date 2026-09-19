using Xunit;

namespace WorkoutTracker.UnitTests.Api;

public class PreviousExerciseDataSelectorTests
{
    [Fact]
    public void SelectLatestUsablePerExercise_CarriesSetsFromSelectedSession()
    {
        var exerciseId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow;

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [Session(completedAt, 5, Exercise(exerciseId, "80", 7))],
            new HashSet<Guid> { exerciseId });

        var comparison = Assert.Single(result).Value;
        Assert.Equal(5, comparison.Sets);
        Assert.Equal(completedAt, comparison.CompletedAt);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_UsesSetsFromOlderFallbackSession()
    {
        var exerciseId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [
                Session(now, 5, Exercise(exerciseId, null, null)),
                Session(now.AddDays(-1), 3, Exercise(exerciseId, "75", null)),
            ],
            new HashSet<Guid> { exerciseId });

        var comparison = Assert.Single(result).Value;
        Assert.Equal("75", comparison.LoggedWeight);
        Assert.Equal(3, comparison.Sets);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_SetsOnlyRowDoesNotSuppressOlderData()
    {
        var exerciseId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [
                Session(now, 5, Exercise(exerciseId, null, null)),
                Session(now.AddDays(-1), 3, Exercise(exerciseId, "70", 6)),
            ],
            new HashSet<Guid> { exerciseId });

        var comparison = Assert.Single(result).Value;
        Assert.Equal("70", comparison.LoggedWeight);
        Assert.Equal(6, comparison.Effort);
        Assert.Equal(3, comparison.Sets);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_AllowsLegacyNullSets()
    {
        var exerciseId = Guid.NewGuid();

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [Session(DateTime.UtcNow, null, Exercise(exerciseId, "50", null))],
            new HashSet<Guid> { exerciseId });

        Assert.Null(Assert.Single(result).Value.Sets);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_SelectsEachExerciseIndependently()
    {
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [
                Session(now, 5, Exercise(firstId, "80", 8), Exercise(secondId, null, null)),
                Session(now.AddDays(-1), 3, Exercise(secondId, "60", 6)),
            ],
            new HashSet<Guid> { firstId, secondId });

        Assert.Equal(5, result[firstId].Sets);
        Assert.Equal(3, result[secondId].Sets);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_IgnoresExercisesOutsideTargetSet()
    {
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [Session(DateTime.UtcNow, 3, Exercise(targetId, "50", null), Exercise(otherId, "90", null))],
            new HashSet<Guid> { targetId });

        Assert.True(result.ContainsKey(targetId));
        Assert.False(result.ContainsKey(otherId));
    }

    [Fact]
    public void SelectLatestUsablePerExercise_PreservesCallerOrderForTies()
    {
        var exerciseId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow;

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [
                Session(completedAt, 5, Exercise(exerciseId, "80", null)),
                Session(completedAt, 3, Exercise(exerciseId, "70", null)),
            ],
            new HashSet<Guid> { exerciseId });

        Assert.Equal(5, Assert.Single(result).Value.Sets);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_StopsAtMaximumSessionCount()
    {
        var exerciseId = Guid.NewGuid();
        var sessions = Enumerable.Range(0, PreviousExerciseDataSelector.MaxSessionsToScan)
            .Select(index => Session(DateTime.UtcNow.AddMinutes(-index), 5, Exercise(exerciseId, null, null)))
            .Append(Session(DateTime.UtcNow.AddYears(-1), 3, Exercise(exerciseId, "60", null)));

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            sessions,
            new HashSet<Guid> { exerciseId });

        Assert.Empty(result);
    }

    [Fact]
    public void SelectLatestUsablePerExercise_ReturnsEmptyForNoUsableHistory()
    {
        var exerciseId = Guid.NewGuid();

        var result = PreviousExerciseDataSelector.SelectLatestUsablePerExercise(
            [Session(DateTime.UtcNow, 3, Exercise(exerciseId, null, null))],
            new HashSet<Guid> { exerciseId });

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("80", null, true)]
    [InlineData(null, 7, true)]
    [InlineData("80", 7, true)]
    [InlineData("   ", null, false)]
    [InlineData(null, null, false)]
    public void HasUsableComparisonData_UsesOnlyWeightOrEffort(string? weight, int? effort, bool expected)
    {
        Assert.Equal(expected, PreviousExerciseDataSelector.HasUsableComparisonData(weight, effort));
    }

    private static HistoricalSessionData Session(
        DateTime completedAt,
        int? sets,
        params HistoricalExerciseData[] exercises) =>
        new(Guid.NewGuid(), completedAt, null, sets, [.. exercises]);

    private static HistoricalExerciseData Exercise(Guid exerciseId, string? weight, int? effort) =>
        new(exerciseId, weight, effort, null);
}
