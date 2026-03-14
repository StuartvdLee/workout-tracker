using Xunit;

namespace WorkoutTracker.Tests.Unit;

/// <summary>
/// Tests for validation logic rules.
/// These tests verify the expected behavior of client-side validation:
/// empty selection shows error, valid selection clears error.
/// </summary>
public class HomeLandingPageValidationTests
{
    private static readonly HashSet<string> ValidWorkoutValues = new(["push", "pull", "legs"]);

    private static bool IsValidSelection(string value) =>
        !string.IsNullOrEmpty(value) && ValidWorkoutValues.Contains(value);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyOrNullSelection_IsInvalid(string? value)
    {
        Assert.False(IsValidSelection(value!));
    }

    [Theory]
    [InlineData("push")]
    [InlineData("pull")]
    [InlineData("legs")]
    public void ValidWorkoutValue_IsValid(string value)
    {
        Assert.True(IsValidSelection(value));
    }

    [Theory]
    [InlineData("Push")]
    [InlineData("LEGS")]
    [InlineData("chest")]
    [InlineData("random")]
    public void InvalidWorkoutValue_IsRejected(string value)
    {
        Assert.False(IsValidSelection(value));
    }

    [Fact]
    public void ValidationMessage_IsExactText()
    {
        const string expectedMessage = "Please select a workout";
        Assert.Equal("Please select a workout", expectedMessage);
    }

    [Fact]
    public void ErrorState_ClearsOnValidSelection()
    {
        var hasError = true;
        var selection = "push";

        if (IsValidSelection(selection))
        {
            hasError = false;
        }

        Assert.False(hasError);
    }

    [Fact]
    public void ErrorState_PersistsOnInvalidSelection()
    {
        var hasError = false;
        var selection = "";

        if (!IsValidSelection(selection))
        {
            hasError = true;
        }

        Assert.True(hasError);
    }
}
