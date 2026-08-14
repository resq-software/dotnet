using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Testing.Integration;
using Widgets.Api;
using Widgets.Infrastructure;
using Xunit;

namespace Widgets.IntegrationTests;

/// <summary>
/// Binds <c>[Collection("database")]</c> in THIS assembly to the shared
/// <see cref="PostgresContainerFixture"/> — xUnit requires the collection definition to live in the same
/// assembly as the tests that join it, so it cannot be inherited from the building-block package.
/// </summary>
[CollectionDefinition("database")]
public sealed class DatabaseCollection : ICollectionFixture<PostgresContainerFixture>;

/// <summary>
/// End-to-end tests over the real host: a Testcontainers PostgreSQL database behind
/// <see cref="ResqWebApplicationFactory{TEntryPoint}"/>. Category-tagged so the default CI test run
/// (which lacks Docker) filters them out; a Docker-enabled job runs <c>--filter Category=Integration</c>.
/// </summary>
[Trait("Category", "Integration")]
[Collection("database")]
public sealed class WidgetsApiTests(PostgresContainerFixture fixture) : IAsyncLifetime, IDisposable
{
    private readonly WidgetsApiFactory _factory = new(fixture.ConnectionString);

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WidgetsDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Post_then_get_returns_the_created_widget()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act — create
        var created = await client.PostAsJsonAsync("/api/widgets", new { Name = "Gadget", Quantity = 7 });

        // Assert — create
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await created.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();

        // Act — read back
        var fetched = await client.GetAsync($"/api/widgets/{id}");

        // Assert — read back
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var widget = await fetched.Content.ReadFromJsonAsync<WidgetResponse>();
        widget.Should().NotBeNull();
        widget!.Id.Should().Be(id);
        widget.Name.Should().Be("Gadget");
        widget.Quantity.Should().Be(7);
    }

    [Fact]
    public async Task Post_with_invalid_body_returns_problem_details_400()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/widgets", new { Name = "", Quantity = -3 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    /// <summary>Points the host's <c>Widgets</c> connection string at the throwaway container.</summary>
    private sealed class WidgetsApiFactory(string connectionString) : ResqWebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Widgets"] = connectionString,
                }));

            base.ConfigureWebHost(builder);
        }
    }
}
