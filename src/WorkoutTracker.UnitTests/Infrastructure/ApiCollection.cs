using Xunit;

namespace WorkoutTracker.UnitTests.Infrastructure;

[CollectionDefinition("Api", DisableParallelization = true)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
}
