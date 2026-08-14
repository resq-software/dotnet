# ResQ.BuildingBlocks.Testing.Integration

Docker-backed **integration-test helpers** for the ResQ building blocks. Kept separate from
[`ResQ.BuildingBlocks.Testing`](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing) so that
unit-only consumers never drag in Docker or the ASP.NET Core test host.

Targets `net9.0` only (matches `Microsoft.AspNetCore.Mvc.Testing 9.0.0`, whose assets are net9-only).
Carries a `FrameworkReference` on `Microsoft.AspNetCore.App` and depends on
`ResQ.BuildingBlocks.Testing` + `Application` + `Domain`.

## What's in the box

| Type | Role |
|------|------|
| `PostgresContainerFixture` | Starts a throwaway PostgreSQL container (Testcontainers) for a collection |
| `DatabaseCollection` | xUnit `[CollectionDefinition("database")]` sharing one container |
| `ResqWebApplicationFactory<TEntryPoint>` | `WebApplicationFactory` with a `ConfigureTestServices` hook |

## Requirements

A running **Docker** daemon. `PostgresContainerFixture.InitializeAsync()` throws when Docker is
unavailable, so gate these tests in CI — for example tag them `[Trait("Category","Integration")]`
and run `dotnet test --filter Category=Integration` only on a Docker-enabled job.

## Quick start

```csharp
using ResQ.BuildingBlocks.Testing.Integration;
using Xunit;

[Collection("database")]
public sealed class WidgetApiTests
{
    private readonly PostgresContainerFixture _db;

    public WidgetApiTests(PostgresContainerFixture db) => _db = db;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Creates_a_widget()
    {
        await using var factory = new ApiFactory(_db.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/widgets", new { name = "gadget" });

        response.EnsureSuccessStatusCode();
    }
}

// Point the app under test at the container database.
internal sealed class ApiFactory(string connectionString) : ResqWebApplicationFactory<Program>
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // e.g. replace the production DbContext options with `connectionString`
    }
}
```

## License

Apache-2.0. See [LICENSE](https://github.com/resq-software/dotnet/blob/main/LICENSE).
