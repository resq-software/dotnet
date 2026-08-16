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

# ResQ.BuildingBlocks.Testing

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Testing?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> Lightweight, in-memory test doubles for the ResQ building blocks — deterministic clock, unit-of-work fakes, recording dispatchers/publishers, a fluent test-data builder, capturing/xUnit logger providers, and hexagon dependency-rule data.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Testing
```

Depends on `ResQ.BuildingBlocks.Application` and `ResQ.BuildingBlocks.Domain` (whose driven ports these doubles implement), and brings `xunit`, `FluentAssertions`, and `NSubstitute` transitively so a consuming unit-test project needs nothing more. No Docker and no ASP.NET framework reference — safe to add to any unit-test project. The heavy Docker / `WebApplicationFactory` helpers live in [`ResQ.BuildingBlocks.Testing.Integration`](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing.Integration).

## Quick Start

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Testing;

var services = new ServiceCollection();
// ... register the system under test ...
services.AddTestDoubles();   // Replace()s IClock, IUnitOfWork, IDomainEventDispatcher,
                             // IIntegrationEventPublisher with singletons you can inspect.

var provider = services.BuildServiceProvider();

// Time is deterministic: the same singleton is injected into the SUT and resolved here.
var clock = (FakeClock)provider.GetRequiredService<IClock>();
clock.Advance(TimeSpan.FromHours(1));

var publisher = (RecordingIntegrationEventPublisher)provider.GetRequiredService<IIntegrationEventPublisher>();

// ... exercise the SUT ...

publisher.Published.Should().ContainSingle();
```

## API Reference

### Driven-port doubles

Hand-written stand-ins for the `ResQ.BuildingBlocks.Application` driven ports. Register them yourself, or let [`AddTestDoubles`](#dependency-injection) wire the common set.

#### `FakeClock` — `IClock`

Deterministic clock; the instant only moves when the test says so. Defaults to `DateTimeOffset.UnixEpoch` so runs are reproducible.

| Member | Signature | Description |
|--------|-----------|-------------|
| ctor | `FakeClock(DateTimeOffset? start = null)` | Fixes the initial instant (`UnixEpoch` when `null`) |
| `UtcNow` | `DateTimeOffset { get; }` | The instant currently reported |
| `Set` | `void Set(DateTimeOffset utc)` | Jump to an absolute instant |
| `Advance` | `void Advance(TimeSpan by)` | Move forward (or backward, if negative) |

#### Unit-of-work fakes — `IUnitOfWork`

Three `SaveChangesAsync` behaviors for the three cases a handler test cares about.

| Type | `SaveChangesAsync` behavior | Notable members |
|------|-----------------------------|-----------------|
| `NoopUnitOfWork` | Completes, returns `0` | — |
| `FakeUnitOfWork` | Completes, returns `AffectedRows` | `int AffectedRows { get; set; }`, `int SaveChangesCallCount { get; }` |
| `ThrowingUnitOfWork` | Always faults | ctor `ThrowingUnitOfWork(Exception? exception = null)` — defaults to `InvalidOperationException` |

`FakeUnitOfWork.SaveChangesCallCount` counts calls so a test can assert a handler committed exactly once; set `AffectedRows` to script the returned row count.

### Event recorders

Capture what the system under test emits, then assert on it. Both implement their respective `ResQ.BuildingBlocks.Application` port.

| Type | Port | Captured in | Method |
|------|------|-------------|--------|
| `RecordingDomainEventDispatcher` | `IDomainEventDispatcher` | `IReadOnlyList<IDomainEvent> Dispatched { get; }` | `DispatchAsync(IEnumerable<IDomainEvent>, CancellationToken = default)` |
| `RecordingIntegrationEventPublisher` | `IIntegrationEventPublisher` | `IReadOnlyList<IntegrationEvent> Published { get; }` | `PublishAsync(IntegrationEvent, CancellationToken = default)` |

```csharp
var dispatcher = (RecordingDomainEventDispatcher)provider.GetRequiredService<IDomainEventDispatcher>();
// ... exercise the SUT ...
dispatcher.Dispatched.Should().ContainItemsAssignableTo<OrderPlaced>();
```

### Logger providers

Two `ILoggerProvider` implementations for asserting on, or surfacing, what the SUT logs. Each formatted line is rendered as `"{LogLevel}: {categoryName}: {message}"`.

| Type | Purpose | Members |
|------|---------|---------|
| `CapturingLoggerProvider` | Buffers formatted log lines in memory for assertions | `IReadOnlyList<string> Entries { get; }`, `CreateLogger(string)`, `Dispose()` |
| `XUnitLoggerProvider` | Forwards log lines to an xUnit sink so they appear in test output | ctor `XUnitLoggerProvider(ITestOutputHelper output)`, `CreateLogger(string)`, `Dispose()` |

```csharp
var provider = new CapturingLoggerProvider();
using var loggerFactory = LoggerFactory.Create(b => b.AddProvider(provider));
// ... exercise code that logs ...
provider.Entries.Should().Contain(e => e.Contains("order placed"));
```

`XUnitLoggerProvider` tolerates writes issued after the test has finished (the output sink is gone) rather than throwing.

### Test-data `Builder<T>`

Abstract base for fluent test-data builders. Derive from it, expose `With…` mutators, and implement `Build`. The implicit conversion lets a builder be passed anywhere a `T` is expected.

| Member | Signature | Description |
|--------|-----------|-------------|
| `Build` | `abstract T Build()` | Materialize the configured instance |
| implicit operator | `static implicit operator T(Builder<T>)` | Build on assignment — a `Builder<T>` is usable as a `T` |

```csharp
public sealed class WidgetBuilder : Builder<Widget>
{
    private string _name = "default";
    public WidgetBuilder Named(string name) { _name = name; return this; }
    public override Widget Build() => new(_name);
}

Widget widget = new WidgetBuilder().Named("gadget"); // implicit Build()
```

### Dependency injection

#### `AddTestDoubles(this IServiceCollection) : IServiceCollection`

`Replace()`s the four driven-port registrations with the recording/fake doubles, each as a **singleton** so a test resolves the same instance the SUT was given.

| Port | Replacement |
|------|-------------|
| `IClock` | `FakeClock` |
| `IUnitOfWork` | `NoopUnitOfWork` |
| `IDomainEventDispatcher` | `RecordingDomainEventDispatcher` |
| `IIntegrationEventPublisher` | `RecordingIntegrationEventPublisher` |

Need `FakeUnitOfWork` or `ThrowingUnitOfWork` instead of the no-op? Register it yourself after calling `AddTestDoubles`, or `Replace` `IUnitOfWork` directly.

### Architecture rules

#### `HexagonRules.DependencyRule`

Machine-readable statement of the hexagonal dependency rule, meant to drive a consumer's architecture tests (for example, NetArchTest). No architecture-test framework is referenced here, so unit-only consumers pull nothing extra.

| Member | Type |
|--------|------|
| `DependencyRule` | `static IReadOnlyList<(string Inner, string[] MayNotBeReferencedBy)> { get; }` |

Each pair names a ring's prefix (`Inner`) and the outer-ring prefixes it must not depend on. Domain depends on nothing; Application may depend only on Domain; adapters may depend only on Application and Domain.

```csharp
foreach (var (inner, mustNotBeReferencedBy) in HexagonRules.DependencyRule)
{
    // feed `inner` / `mustNotBeReferencedBy` into NetArchTest in your own arch-test project
}
```

## Prerequisites

- **Target frameworks**: `net8.0`, `net9.0`
- **Test stack**: brings `xunit`, `FluentAssertions`, and `NSubstitute` transitively; `XUnitLoggerProvider` binds to `Xunit.Abstractions.ITestOutputHelper`

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
