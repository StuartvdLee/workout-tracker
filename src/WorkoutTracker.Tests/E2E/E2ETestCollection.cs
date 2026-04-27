using WorkoutTracker.Tests.Infrastructure;
using Xunit;

namespace WorkoutTracker.Tests.E2E;

[CollectionDefinition("E2E", DisableParallelization = false)]
public class E2ETests : ICollectionFixture<WebAppFixture>, ICollectionFixture<PlaywrightFixture>
{
}
