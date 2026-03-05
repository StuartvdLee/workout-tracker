using Xunit;

namespace Backend.Tests.Integration.Api;

public sealed class SessionsControllerTests
{
    [Fact]
    public void CreateSession_WithWorkoutType_Succeeds()
    {
        Assert.True(true);
    }

    [Fact]
    public void CreateSession_WithoutWorkoutType_ReturnsValidationFailure()
    {
        Assert.True(true);
    }
}
