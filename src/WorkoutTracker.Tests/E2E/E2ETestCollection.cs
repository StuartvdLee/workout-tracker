using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[CollectionDefinition("E2E")]
public class E2ETests : ICollectionFixture<WebAppFixture>, ICollectionFixture<PlaywrightFixture>
{
}
