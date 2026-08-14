using Xunit;

namespace ResQ.BuildingBlocks.Testing.Integration;

/// <summary>
/// xUnit collection definition that shares a single <see cref="PostgresContainerFixture"/> across
/// every test in the <c>"database"</c> collection, so one container serves the whole collection.
/// </summary>
/// <remarks>
/// Annotate a test class with <c>[Collection("database")]</c> to join this collection.
/// </remarks>
[CollectionDefinition("database")]
public class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>
{
}
