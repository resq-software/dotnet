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

# ResQ.BuildingBlocks.Adapters.Persistence

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Adapters.Persistence?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Adapters.Persistence)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> EF Core 9 persistence adapter — a scoped unit of work that dispatches domain events before saving, specification-driven repositories, an audit interceptor, and a transactional outbox plus inbox idempotency.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Adapters.Persistence
```

Depends on `ResQ.BuildingBlocks.Application` and `ResQ.BuildingBlocks.Domain`, plus `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`. It is **provider-agnostic** — it references only EF Core and Relational, so the consumer picks the provider (Npgsql, SQLite, …) in the `configure` callback. It never references the messaging adapter: the outbox/inbox use the messaging **ports** that live in the application core.

## Quick Start

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Adapters.Persistence;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

// 1. A DbContext that maps the outbox/inbox tables plus your own configurations.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder
            .ApplyResqInfrastructure()                       // outbox + inbox tables
            .ApplyConfigurationsFrom(GetType().Assembly);    // your IEntityTypeConfiguration<T>s
}

// 2. A concrete repository: derive from Repository<TAggregate, TId> and the aggregate's port.
public interface IOrderRepository : IRepository<Order, OrderId>;

public sealed class OrderRepository(AppDbContext db)
    : Repository<Order, OrderId>(db), IOrderRepository;

// 3. Wire it up.
services.AddResqPersistence<AppDbContext>(
    (sp, options) => options.UseNpgsql(connectionString),
    setup =>
    {
        setup.UseOutbox = true;              // register EfOutbox + OutboxRelay
        setup.UseTransactionBehavior = true; // wrap commands in a transaction (default)
        setup.EnableAudit = true;            // attach AuditInterceptor (default)
    });

services.AddResqRepositories(typeof(AppDbContext).Assembly);

// 4. In a handler: mutate through the repository, commit through the unit of work.
public sealed class PlaceOrderHandler(IOrderRepository orders, IUnitOfWork uow)
{
    public async Task Handle(Order order, CancellationToken ct)
    {
        await orders.AddAsync(order, ct);
        await uow.SaveChangesAsync(ct); // dispatches domain events, then persists — atomically
    }
}
```

## API Reference

### The keystone: `EfUnitOfWork`

`EfUnitOfWork` is the scoped `IUnitOfWork`. Its `SaveChangesAsync` **dispatches domain events before** the underlying `DbContext.SaveChanges`, in a drain loop over `ChangeTracker.Entries<IHasDomainEvents>()`:

1. Collect tracked aggregates with pending events; snapshot and clear them.
2. `await IDomainEventDispatcher.DispatchAsync(events)` — a handler may enqueue an outbox row or raise more events.
3. Repeat until no aggregate has pending events, then call the underlying `SaveChangesAsync`.

Because dispatch happens **inside the same save**, outbox rows a handler enqueues commit atomically with the aggregate write. There is deliberately **no** domain-event-dispatch interceptor: EF's internal service provider does not contain application services. **Contract:** commits must flow through `IUnitOfWork.SaveChangesAsync` for domain events to dispatch.

| Member | Signature | Description |
|--------|-----------|-------------|
| ctor | `EfUnitOfWork(DbContext, IDomainEventDispatcher)` | Resolved scoped; the `DbContext` is the relay registered by `AddResqPersistence`. |
| `SaveChangesAsync` | `(CancellationToken = default) -> Task<int>` | Drains and dispatches domain events, then saves. Returns the affected row count. |

### Repositories

`Repository<TAggregate, TId>` and `ReadRepository<TAggregate, TId>` are abstract, specification-driven aggregate repositories. Derive a concrete type from one of them **and** the aggregate's application-level port, then register both with `AddResqRepositories`. `ReadRepository` forces `AsNoTracking`; the read/write `Repository` tracks.

Type parameters: `where TAggregate : AggregateRoot<TId>`, `where TId : notnull`.

| Member | Signature | Description |
|--------|-----------|-------------|
| `GetByIdAsync` | `(TId id, CancellationToken = default) -> Task<TAggregate?>` | Find by identity. |
| `ListAsync` | `(ISpecification<TAggregate> spec, CancellationToken = default) -> Task<IReadOnlyList<TAggregate>>` | Materialize all matches of the spec. |
| `FirstOrDefaultAsync` | `(ISpecification<TAggregate> spec, CancellationToken = default) -> Task<TAggregate?>` | First match, or `null`. |
| `CountAsync` | `(ISpecification<TAggregate> spec, CancellationToken = default) -> Task<int>` | Count matches. |
| `AnyAsync` | `(ISpecification<TAggregate> spec, CancellationToken = default) -> Task<bool>` | Existence check. |
| `AddAsync` | `(TAggregate aggregate, CancellationToken = default) -> Task` | Track for insert. *(`Repository` only.)* |
| `Update` | `(TAggregate aggregate) -> void` | Track for update. *(`Repository` only.)* |
| `Remove` | `(TAggregate aggregate) -> void` | Track for delete. *(`Repository` only.)* |
| `Set` | `DbSet<TAggregate>` | Protected backing set, for advanced queries. |

### `SpecificationEvaluator`

Bridges the EF-free `ISpecification<T>` (criteria, includes, ordering, paging, tracking, split-query, ignore-filters) onto an `IQueryable<T>`. The repositories call it internally; call it directly to compose a spec over any queryable.

| Member | Signature | Description |
|--------|-----------|-------------|
| `GetQuery<T>` | `static (IQueryable<T> input, ISpecification<T> spec) -> IQueryable<T>` | Applies the specification to `input` and returns the composed query. |

### `TransactionBehavior<TRequest, TResponse>`

A CQRS pipeline behavior that wraps **commands** (detected through the CQRS marker interfaces, so `ICommand<T>` is caught too) in a transaction via EF's execution strategy. Queries pass through untouched. Registered last in the pipeline (the innermost behavior around the handler) when `UseTransactionBehavior` is on.

| Member | Signature | Description |
|--------|-----------|-------------|
| ctor | `TransactionBehavior(DbContext, ILogger<TransactionBehavior<TRequest, TResponse>>)` | Resolved as an open-generic `IPipelineBehavior<,>`. |
| `Handle` | `(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken) -> Task<TResponse>` | Runs `next` inside a transaction for commands; passes through for queries. |

### Auditing — `AuditInterceptor` + `IAuditable`

`AuditInterceptor` is a **singleton** save-changes interceptor that stamps created/modified timestamps from `IClock` onto tracked `IAuditable` entries — created + modified on `Added`, modified on `Modified`. It depends only on the singleton `IClock`, so it is safe to share across a pooled `DbContext`.

| Type | Member | Description |
|------|--------|-------------|
| `AuditInterceptor` | `AuditInterceptor(IClock)` | Singleton interceptor; attached when `EnableAudit` is on. |
| `AuditInterceptor` | `SavingChanges` / `SavingChangesAsync` | Overrides that stamp timestamps before the underlying save. |
| `IAuditable` | `SetCreated(DateTimeOffset utc)` | Called once, on insert. Set through a method (not a public setter). |
| `IAuditable` | `SetModified(DateTimeOffset utc)` | Called on insert and on every update. |

### Transactional outbox

Enqueue integration events in the **same transaction** as the aggregate write; a background relay publishes them at-least-once.

| Type | Member | Description |
|------|--------|-------------|
| `IOutbox` / `EfOutbox` | `Enqueue(IntegrationEvent event)` | Serializes and stages an event row for the current transaction. `EfOutbox(DbContext, IMessageSerializer, IClock)`. |
| `OutboxRelay` | `OutboxRelay(IServiceScopeFactory, IOptions<OutboxOptions>, ILogger<OutboxRelay>)` | Hosted service; the at-least-once background publisher. |
| `OutboxMessage` | `Id`, `Type`, `Content` (`byte[]`), `OccurredOnUtc`, `ProcessedOnUtc?`, `Attempts`, `Error?` | The persisted row. |
| `OutboxMessageConfiguration` | `Configure(EntityTypeBuilder<OutboxMessage>)` | Maps the outbox table (applied by `ApplyResqInfrastructure`). |

#### `OutboxOptions`

| Option | Type | Constraint | Description |
|--------|------|------------|-------------|
| `PollingInterval` | `TimeSpan` | `> TimeSpan.Zero` | How often the relay polls for unpublished rows. |
| `BatchSize` | `int` | `>= 1` | Rows drained per poll. |
| `MaxAttempts` | `int` | `>= 1` | Delivery attempts before a row is parked with its `Error`. |

Constraints are enforced at startup (`ValidateOnStart`).

### Inbox idempotency

Deduplicate inbound message handling by `(messageId, handler)`.

| Type | Member | Description |
|------|--------|-------------|
| `EfIdempotencyStore` | `HasProcessedAsync(string messageId, string handler, CancellationToken) -> Task<bool>` | Has this `(messageId, handler)` pair already run? `EfIdempotencyStore(DbContext, IClock)`. |
| `EfIdempotencyStore` | `MarkProcessedAsync(string messageId, string handler, CancellationToken) -> Task` | Record the pair as processed. |
| `InboxMessage` | `MessageId`, `Handler`, `ProcessedOnUtc` | The persisted dedupe row. |
| `InboxMessageConfiguration` | `Configure(EntityTypeBuilder<InboxMessage>)` | Maps the inbox table (applied by `ApplyResqInfrastructure`). |

### Model configuration — `ModelBuilderExtensions`

| Member | Signature | Description |
|--------|-----------|-------------|
| `ApplyResqInfrastructure` | `static (this ModelBuilder) -> ModelBuilder` | Maps the outbox and inbox tables. Call it in `OnModelCreating`. |
| `ApplyConfigurationsFrom` | `static (this ModelBuilder, Assembly) -> ModelBuilder` | Applies every `IEntityTypeConfiguration<T>` in the assembly. |

### Registration — `PersistenceServiceCollectionExtensions`

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddResqPersistence<TContext>` | `(this IServiceCollection, Action<IServiceProvider, DbContextOptionsBuilder> configure, Action<ResqPersistenceOptions>? setup = null) -> IServiceCollection` | Pools `TContext` (`AddDbContextPool`), relays a scoped `DbContext`, registers `EfUnitOfWork`/`EfIdempotencyStore` scoped and `AuditInterceptor` singleton, and — per the options — the outbox and command transaction behavior. |
| `AddResqRepositories` | `(this IServiceCollection, params Assembly[] assemblies) -> IServiceCollection` | Scans for concrete `Repository`/`ReadRepository` derivations and registers each as its implemented interfaces, scoped. Defaults to the calling assembly when no assemblies are passed. |

#### `ResqPersistenceOptions`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `UseOutbox` | `bool` | `false` | Register `EfOutbox` + `OutboxRelay`. |
| `UseTransactionBehavior` | `bool` | `true` | Register the open-generic `TransactionBehavior` so commands run inside a transaction. |
| `EnableAudit` | `bool` | `true` | Attach the `AuditInterceptor` so `IAuditable` entities get created/modified timestamps at save time. |

## Prerequisites

- **Target frameworks**: `net8.0`, `net9.0`
- **EF Core provider**: bring your own (Npgsql, SQLite, SQL Server, …); this package references only EF Core + Relational and selects nothing.
- **Application core**: an `IClock`, `IDomainEventDispatcher`, and `IMessageSerializer` implementation (supplied by `ResQ.BuildingBlocks.ServiceDefaults` or your own registrations).

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
