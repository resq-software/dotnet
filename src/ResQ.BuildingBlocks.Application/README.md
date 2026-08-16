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

# ResQ.BuildingBlocks.Application

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Application?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Application)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> Application-core abstractions — CQRS command/query contracts, the driven-port interfaces, and cross-cutting pipeline behaviors. Depends only on the domain core.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Application
```

Depends on [`ResQ.BuildingBlocks.Domain`](https://www.nuget.org/packages/ResQ.BuildingBlocks.Domain) plus `FluentValidation`, `Scrutor`, and the `Microsoft.Extensions.DependencyInjection`/`Logging` abstractions. It owns its mediator — there is no MediatR (or other pipeline-library) dependency, so no licensing surprises.

## Quick Start

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

// 1. A command, its validator, and its Result-returning handler (never throws for expected failures).
public sealed record RegisterVehicle(string Vin) : ICommand<Guid>;

public sealed class RegisterVehicleValidator : AbstractValidator<RegisterVehicle>
{
    public RegisterVehicleValidator() => RuleFor(c => c.Vin).NotEmpty().Length(17);
}

internal sealed class RegisterVehicleHandler(IUnitOfWork uow)
    : ICommandHandler<RegisterVehicle, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterVehicle command, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        // ... add the aggregate to its repository ...
        await uow.SaveChangesAsync(ct);
        return id; // implicit Guid -> Result<Guid>
    }
}

// 2. Wire the core + the cross-cutting pipeline. First registered = outermost.
var services = new ServiceCollection();
services.AddLogging();
services.AddResqApplication(typeof(RegisterVehicle).Assembly);   // ISender + scanned handlers
services.AddValidatorsFrom(typeof(RegisterVehicle).Assembly);    // FluentValidation validators
services.AddLoggingPipeline();      // outermost behavior
services.AddValidationPipeline();   // runs after logging, before the handler

// 3. Dispatch through the ordered behavior chain (Sender is scoped).
using var scope = services.BuildServiceProvider().CreateScope();
var sender = scope.ServiceProvider.GetRequiredService<ISender>();

Result<Guid> result = await sender.Send(new RegisterVehicle("1HGCM82633A004352"));
if (result.IsSuccess)
{
    Console.WriteLine($"Registered {result.Value}");
}
```

A blank VIN never reaches the handler: `ValidationBehavior` short-circuits with a failed `Result` carrying a single `ErrorType.Validation` error (the per-field failures folded into its message) rather than throwing.

## API Reference

### CQRS Contracts

Marker interfaces that split reads from writes; every handler returns a `Result` (or `Result<T>`) instead of throwing for expected failures.

| Type | Description |
|------|-------------|
| `IQuery<TResponse>` | A read that returns `TResponse` and never mutates state |
| `ICommand` | A command that mutates state and returns only success/failure |
| `ICommand<TResponse>` | A command that mutates state and returns `TResponse` on success |
| `IQueryHandler<TQuery, TResponse>` | `Handle(TQuery, CancellationToken) -> Task<Result<TResponse>>` |
| `ICommandHandler<TCommand>` | `Handle(TCommand, CancellationToken) -> Task<Result>` |
| `ICommandHandler<TCommand, TResponse>` | `Handle(TCommand, CancellationToken) -> Task<Result<TResponse>>` |

### Dispatch

`ISender` (default implementation `Sender`, registered scoped) routes each request to its single handler through the ordered behavior chain. A process-wide static cache maps each request CLR type to a reusable, stateless pipeline wrapper.

| Member | Signature | Description |
|--------|-----------|-------------|
| `ISender.Send` | `(ICommand, CancellationToken = default) -> Task<Result>` | Dispatch a value-less command |
| `ISender.Send<TResponse>` | `(ICommand<TResponse>, CancellationToken = default) -> Task<Result<TResponse>>` | Dispatch a value-returning command |
| `ISender.Send<TResponse>` | `(IQuery<TResponse>, CancellationToken = default) -> Task<Result<TResponse>>` | Dispatch a query |

`CqrsRequest` provides static classification helpers used by observability behaviors:

| Member | Signature | Description |
|--------|-----------|-------------|
| `CqrsRequest.IsCommand` | `(Type) -> bool` | True if the type closes `ICommand` or `ICommand<T>` |
| `CqrsRequest.IsQuery` | `(Type) -> bool` | True if the type closes `IQuery<T>` |

### Driven Ports

The Core owns these interfaces; Infrastructure adapters implement them.

| Type | Member | Description |
|------|--------|-------------|
| `IUnitOfWork` | `SaveChangesAsync(CancellationToken = default) -> Task<int>` | Commits the unit of work atomically; returns rows written |
| `IClock` | `UtcNow -> DateTimeOffset` | The current instant (UTC), so time is testable and deterministic |
| `IDomainEventDispatcher` | `DispatchAsync(IEnumerable<IDomainEvent>, CancellationToken = default) -> Task` | Dispatches domain events to their handlers after persistence |

### Domain Events

`IDomainEventHandler<TEvent>.Handle(TEvent, CancellationToken) -> Task` handles one domain-event type. `DomainEventDispatcher` (the default `IDomainEventDispatcher`, constructed from an `IServiceProvider`) fans each event out to its registered handlers by runtime type.

### Pipeline Behaviors

A behavior wraps handler execution (validation, logging, transactions, caching…). `RequestHandlerDelegate<TResponse>` invokes the next stage — the next behavior, or the handler.

| Type | Description |
|------|-------------|
| `IPipelineBehavior<TRequest, TResponse>` | `Handle(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken) -> Task<TResponse>` |
| `ValidationBehavior<TRequest, TResponse>` | Runs all registered FluentValidation validators (each on its own `ValidationContext`, awaited concurrently); on failure short-circuits with a failed `Result`/`Result<T>` carrying an `ErrorType.Validation` error instead of throwing |
| `LoggingBehavior<TRequest, TResponse>` | Logs the start, completion, and failure of each request via `ILogger<>` |

Behaviors run in DI registration order, **first-registered = outermost**. The canonical order is `Logging → Validation → Tracing → Metrics → Transaction`: register logging and validation here, the observability behaviors from `ServiceDefaults` next, and the persistence transaction behavior last (innermost).

### Persistence Ports

Storage-agnostic repository and specification contracts; adapters translate them onto their query engine (e.g. EF Core `IQueryable`).

`IRepository<TAggregate, TId>` — full read/write surface:

| Member | Signature | Description |
|--------|-----------|-------------|
| `GetByIdAsync` | `(TId, CancellationToken = default) -> Task<TAggregate?>` | Load by identity |
| `ListAsync` | `(ISpecification<TAggregate>, CancellationToken = default) -> Task<IReadOnlyList<TAggregate>>` | Materialize all matches |
| `FirstOrDefaultAsync` | `(ISpecification<TAggregate>, CancellationToken = default) -> Task<TAggregate?>` | First match or `null` |
| `AnyAsync` | `(ISpecification<TAggregate>, CancellationToken = default) -> Task<bool>` | Existence check |
| `CountAsync` | `(ISpecification<TAggregate>, CancellationToken = default) -> Task<int>` | Count matches |
| `AddAsync` | `(TAggregate, CancellationToken = default) -> Task` | Stage an insert |
| `Update` | `(TAggregate) -> void` | Stage an update |
| `Remove` | `(TAggregate) -> void` | Stage a delete |

`IReadRepository<TAggregate, TId>` exposes the read-only subset (`GetByIdAsync`, `ListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync`) for query-side handlers.

`ISpecification<T>` — the storage-agnostic query description (all read-only):

| Member | Type | Description |
|--------|------|-------------|
| `Criteria` | `Expression<Func<T, bool>>?` | Filter predicate; `null` matches everything |
| `Includes` | `IReadOnlyList<Expression<Func<T, object?>>>` | Eager-load member selectors |
| `IncludeStrings` | `IReadOnlyList<string>` | Eager-load provider-specific paths |
| `OrderBy` | `IReadOnlyList<(Expression<Func<T, object?>> KeySelector, bool Descending)>` | Ordering keys applied in order |
| `Skip` | `int?` | Rows to skip, or `null` |
| `Take` | `int?` | Max rows to take, or `null` |
| `AsNoTracking` | `bool` | Run without change tracking |
| `AsSplitQuery` | `bool` | Execute as a split query |
| `IgnoreQueryFilters` | `bool` | Ignore global query filters |

`Specification<T>` — the base implementation. Construct with `Specification()` or `Specification(Expression<Func<T, bool>> criteria)`, then compose it from a derived type via the protected builders:

| Method | Description |
|--------|-------------|
| `AddInclude(Expression<Func<T, object?>>)` | Add an eager-load member selector |
| `AddInclude(string)` | Add a provider-specific include path |
| `ApplyOrderBy(Expression<Func<T, object?>>)` | Append an ascending sort key |
| `ApplyOrderByDescending(Expression<Func<T, object?>>)` | Append a descending sort key |
| `ApplyPaging(int skip, int take)` | Set `Skip`/`Take` |
| `AsNoTrackingQuery()` | Set `AsNoTracking` |
| `AsSplitQueryable()` | Set `AsSplitQuery` |
| `IgnoreFilters()` | Set `IgnoreQueryFilters` |

### Pagination

Immutable page records returned by query handlers.

| Type | Members | Description |
|------|---------|-------------|
| `OffsetPage<T>` | `Items`, `Page`, `PageSize`, `TotalRows`, `TotalPages` (computed) | Offset/page pagination with a total-row count |
| `CursorPage<T>` | `Items`, `NextCursor`, `PrevCursor`, `HasMore` | Keyset (cursor) pagination |

### Integration Ports

Shared by the messaging and persistence (outbox) adapters, so neither references the other.

| Type | Member | Description |
|------|--------|-------------|
| `IntegrationEvent` | `Id`, `OccurredOnUtc`, `EventType` | Abstract base record for a fact published across service boundaries; `EventType` defaults to the CLR `FullName` for routing/type resolution |
| `IIntegrationEventPublisher` | `PublishAsync(IntegrationEvent, CancellationToken = default) -> Task` | Publish an integration event to the transport |
| `IMessageSerializer` | `ContentType`, `Serialize(object, Type) -> byte[]`, `Deserialize(ReadOnlySpan<byte>, Type) -> object?` | Wire serialization for events |
| `IIntegrationEventTypeRegistry` | `Register(Type)`, `TryResolve(string, out Type?) -> bool` | Maps `EventType` names back to CLR types on receive |
| `IIdempotencyStore` | `HasProcessedAsync(messageId, handler, CancellationToken) -> Task<bool>`, `MarkProcessedAsync(messageId, handler, CancellationToken) -> Task` | Deduplicate at-least-once delivery per `(messageId, handler)` |

### Composition-Root Helpers

`ApplicationServiceCollectionExtensions` wires the core into an `IServiceCollection`.

| Method | Description |
|--------|-------------|
| `AddResqApplication(params Assembly[])` | Registers `ISender` and `IDomainEventDispatcher` (scoped) and scans the assemblies for CQRS + domain-event handlers, registering them scoped |
| `AddValidatorsFrom(params Assembly[])` | Registers all FluentValidation validators found in the assemblies as scoped |
| `AddPipelineBehavior(Type openGenericBehaviorType)` | Registers an open-generic `IPipelineBehavior<,>` as transient, preserving order |
| `AddLoggingPipeline()` | Registers the shipped `LoggingBehavior<,>` (outermost of the canonical order) |
| `AddValidationPipeline()` | Registers the shipped `ValidationBehavior<,>` (after logging) |

## Prerequisites

- **Target frameworks**: `net8.0`, `net9.0`
- **FluentValidation** for the validation pipeline; validators are discovered via `AddValidatorsFrom`
- Handler and validator scanning use reflection (`Scrutor`) — preserve those types when trimming/AOT (the scanning entry points are annotated `RequiresUnreferencedCode`/`RequiresDynamicCode`)

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
