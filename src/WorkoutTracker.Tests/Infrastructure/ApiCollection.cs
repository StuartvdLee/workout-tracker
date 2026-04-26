using Xunit;

namespace WorkoutTracker.Tests.Infrastructure;

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
}
