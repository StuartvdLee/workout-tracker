using Xunit;

namespace WorkoutTracker.UnitTests.Infrastructure;

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
}
