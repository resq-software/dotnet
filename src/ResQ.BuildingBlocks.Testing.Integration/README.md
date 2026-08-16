<!--
  Copyright 2026 ResQ Systems, Inc.

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
-->

# ResQ.BuildingBlocks.Testing.Integration

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Testing.Integration?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing.Integration)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> Docker-backed integration-test helpers: a Testcontainers PostgreSQL fixture, an xUnit database collection, and a `WebApplicationFactory` over `Microsoft.AspNetCore.Mvc.Testing`.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Testing.Integration
```

Depends on `ResQ.BuildingBlocks.Testing`, `ResQ.BuildingBlocks.Application`, and `ResQ.BuildingBlocks.Domain`, plus `Testcontainers`, `Testcontainers.PostgreSql`, `xunit`, and `Microsoft.AspNetCore.Mvc.Testing` (carries a `FrameworkReference` on `Microsoft.AspNetCore.App`). Kept separate from [`ResQ.BuildingBlocks.Testing`](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing) so unit-only consumers never drag in Docker or the ASP.NET Core test host. Requires a running **Docker** daemon.

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Testing.Integration;
using System.Net.Http.Json;
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

// Point the app under test at the throwaway container database.
internal sealed class ApiFactory(string connectionString) : ResqWebApplicationFactory<Program>
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // e.g. replace the production DbContext options with `connectionString`
    }
}
```

## API Reference

### `PostgresContainerFixture`

An xUnit `IAsyncLifetime` fixture that starts a throwaway PostgreSQL container (`postgres:16-alpine`) for the lifetime of a test collection and tears it down afterwards. `InitializeAsync` throws when Docker is unavailable.

| Member | Type | Description |
|--------|------|-------------|
| `Container` | `Testcontainers.PostgreSql.PostgreSqlContainer` | The underlying Testcontainers PostgreSQL container. |
| `ConnectionString` | `string` | The ADO.NET connection string for the running container. Only valid after `InitializeAsync` has completed. |
| `InitializeAsync()` | `Task` | Starts the container. Throws if Docker is not available. |
| `DisposeAsync()` | `Task` | Stops and removes the container. |

### `DatabaseCollection`

xUnit collection definition (`[CollectionDefinition("database")]` over `ICollectionFixture<PostgresContainerFixture>`) that shares a single `PostgresContainerFixture` across every test in the `"database"` collection, so one container serves the whole collection. Annotate a test class with `[Collection("database")]` to join it and receive the fixture through the constructor.

### `ResqWebApplicationFactory<TEntryPoint>`

A `WebApplicationFactory<TEntryPoint>` that bootstraps the application under test and exposes a single override point for swapping real adapters (the `DbContext`, integration-event publishers, and so on) with container-backed or recording test doubles.

| Member | Signature | Description |
|--------|-----------|-------------|
| `ConfigureWebHost` | `protected override void ConfigureWebHost(IWebHostBuilder builder)` | Wires `ConfigureTestServices` into the test host's service configuration via `builder.ConfigureTestServices(...)`. |
| `ConfigureTestServices` | `protected virtual void ConfigureTestServices(IServiceCollection services)` | Override to replace registrations for the test run — for example, point the `DbContext` at the Testcontainers database or swap publishers for recording doubles. The base implementation does nothing. |

`TEntryPoint` is the application's entry-point type (usually its `Program`).

## Gating Integration Tests

`PostgresContainerFixture.InitializeAsync()` throws when the Docker daemon is unavailable, so keep these tests off Docker-less jobs. Tag them with a trait — for example `[Trait("Category", "Integration")]` — and run them only on a Docker-enabled job:

```bash
dotnet test --filter Category=Integration
```

## Prerequisites

- **Target framework**: `net9.0` only. `Microsoft.AspNetCore.Mvc.Testing 9.0.0` ships net9-only assets, overriding the repository's inherited `net8.0;net9.0`.
- **ASP.NET Core**: a `FrameworkReference` on `Microsoft.AspNetCore.App` (the shared framework must be installed on the test host).
- **Docker**: a running Docker daemon for the Testcontainers PostgreSQL fixture.
- **xUnit**: the fixtures are xUnit-specific (`IAsyncLifetime`, `[CollectionDefinition]`, `ICollectionFixture<T>`).

## Part of ResQ.BuildingBlocks

A Clean/Hexagonal (Ports & Adapters) reference architecture as reusable NuGet packages — the frame, not the domain.

| Package | Purpose |
|---------|---------|
| [ResQ.BuildingBlocks.Domain](https://www.nuget.org/packages/ResQ.BuildingBlocks.Domain) | DDD primitives — Entity, AggregateRoot, ValueObject, Result/Error, guards (zero-dependency core) |
| [ResQ.BuildingBlocks.Application](https://www.nuget.org/packages/ResQ.BuildingBlocks.Application) | CQRS contracts, driven ports, and pipeline behaviors (own zero-dependency mediator) |
| [ResQ.BuildingBlocks.Adapters.Persistence](https://www.nuget.org/packages/ResQ.BuildingBlocks.Adapters.Persistence) | EF Core adapter — unit of work, repository/specification, outbox/inbox, audit |
| [ResQ.BuildingBlocks.Adapters.Messaging](https://www.nuget.org/packages/ResQ.BuildingBlocks.Adapters.Messaging) | Broker-agnostic integration-event transport (in-memory Channels) |
| [ResQ.BuildingBlocks.Adapters.Web](https://www.nuget.org/packages/ResQ.BuildingBlocks.Adapters.Web) | Minimal-API adapter — Result→ProblemDetails, versioning, OpenAPI |
| [ResQ.BuildingBlocks.ServiceDefaults](https://www.nuget.org/packages/ResQ.BuildingBlocks.ServiceDefaults) | Aspire-style OpenTelemetry / health / resilience + CQRS pipeline wiring |
| [ResQ.BuildingBlocks.Testing](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing) | Dependency-free test doubles (clock, UoW fakes, recorders, data builder) |
| [ResQ.BuildingBlocks.Testing.Integration](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing.Integration) | Integration-testing helpers (WebApplicationFactory host, Testcontainers) |

## License

Apache-2.0
