using WorkoutTracker.E2ETests.Infrastructure;
using Xunit;

namespace WorkoutTracker.E2ETests.E2E;

[CollectionDefinition("E2E", DisableParallelization = false)]
public class E2ETests : ICollectionFixture<WebAppFixture>, ICollectionFixture<PlaywrightFixture>
{
}
