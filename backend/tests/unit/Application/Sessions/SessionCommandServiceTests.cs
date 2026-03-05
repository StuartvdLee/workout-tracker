using Api.Application.Sessions;
using Xunit;

namespace Backend.Tests.Unit.Application.Sessions;

public sealed class SessionCommandServiceTests
{
    [Theory]
    [InlineData("Push", true)]
    [InlineData("Pull", true)]
    [InlineData("Legs", true)]
    [InlineData("", false)]
    [InlineData("Arms", false)]
    public void Validates_WorkoutType_Values(string workoutType, bool expected)
    {
        var result = SessionCommandService.IsValidWorkoutType(workoutType);
        Assert.Equal(expected, result);
    }
}
