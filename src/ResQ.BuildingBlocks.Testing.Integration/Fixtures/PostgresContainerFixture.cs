using Testcontainers.PostgreSql;
using Xunit;

namespace ResQ.BuildingBlocks.Testing.Integration;

/// <summary>
/// An xUnit fixture that starts a throwaway PostgreSQL container for the lifetime of a test
/// collection and tears it down afterwards.
/// </summary>
/// <remarks>
/// Requires a running Docker daemon: <see cref="InitializeAsync"/> throws if Docker is unavailable.
/// Tag the tests that use it so CI can gate them (for example <c>[Trait("Category","Integration")]</c>)
/// and run them only on a Docker-enabled job.
/// </remarks>
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    /// <summary>Gets the underlying Testcontainers PostgreSQL container.</summary>
    public PostgreSqlContainer Container => _container;

    /// <summary>Gets the ADO.NET connection string for the running container.</summary>
    /// <remarks>Only valid after <see cref="InitializeAsync"/> has completed.</remarks>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>Starts the container. Throws if Docker is not available.</summary>
    /// <returns>A task that completes when the container is ready.</returns>
    public Task InitializeAsync() => _container.StartAsync();

    /// <summary>Stops and removes the container.</summary>
    /// <returns>A task that completes when the container has been disposed.</returns>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
