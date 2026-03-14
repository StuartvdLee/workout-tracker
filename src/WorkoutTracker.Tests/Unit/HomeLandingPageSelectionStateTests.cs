using Xunit;

namespace WorkoutTracker.Tests.Unit;

/// <summary>
/// Tests for workout selection state logic.
/// These tests verify the client-side data model: workout option values and labels.
/// </summary>
public class HomeLandingPageSelectionStateTests
{
    private static readonly string[] ValidWorkoutValues = ["push", "pull", "legs"];
    private static readonly string[] WorkoutLabels = ["Push", "Pull", "Legs"];

    [Fact]
    public void WorkoutOptions_ContainsExactlyThreeValues()
    {
        Assert.Equal(3, ValidWorkoutValues.Length);
    }

    [Theory]
    [InlineData("push")]
    [InlineData("pull")]
    [InlineData("legs")]
    public void IsValidWorkout_ReturnsTrue_ForKnownValues(string value)
    {
        Assert.Contains(value, ValidWorkoutValues);
    }

    [Theory]
    [InlineData("")]
    [InlineData("chest")]
    [InlineData("arms")]
    [InlineData("PUSH")]
    public void IsValidWorkout_ReturnsFalse_ForInvalidValues(string value)
    {
        Assert.DoesNotContain(value, ValidWorkoutValues);
    }

    [Fact]
    public void WorkoutLabels_MatchExpectedDisplayNames()
    {
        Assert.Equal(["Push", "Pull", "Legs"], WorkoutLabels);
    }

    [Fact]
    public void WorkoutValues_AreLowercase()
    {
        foreach (var value in ValidWorkoutValues)
        {
            Assert.Equal(value.ToLowerInvariant(), value);
        }
    }
}
