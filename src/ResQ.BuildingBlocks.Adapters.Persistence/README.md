# ResQ.BuildingBlocks.Adapters.Persistence

The **EF Core 9 persistence adapter** (driven ring). Provider-agnostic — it references only EF Core and its Relational package, so the consumer picks the provider (Npgsql, SQLite, …) in the `configure` callback. Depends only on `ResQ.BuildingBlocks.Application` + `ResQ.BuildingBlocks.Domain` — never on the messaging adapter (the outbox/inbox use the messaging **ports** that live in the application core).

## The keystone: `EfUnitOfWork`

`EfUnitOfWork` is the scoped `IUnitOfWork`. Its `SaveChangesAsync` **dispatches domain events before** the underlying `DbContext.SaveChanges`, in a drain loop over `ChangeTracker.Entries<IHasDomainEvents>()`:

1. Collect tracked aggregates with pending events, snapshot and clear them.
2. `await IDomainEventDispatcher.DispatchAsync(events)` — a handler may enqueue an outbox row or raise more events.
3. Repeat until no aggregate has pending events, then call the underlying `SaveChangesAsync`.

Because dispatch happens **inside the same save**, outbox rows a handler enqueues commit atomically with the aggregate write. There is deliberately **no** domain-event-dispatch interceptor: EF's internal service provider does not contain application services. **Contract:** commits must flow through `IUnitOfWork.SaveChangesAsync` for domain events to dispatch.

## What's in the box

- **`Repository<TAggregate,TId>` / `ReadRepository<TAggregate,TId>`** — specification-driven aggregate repositories; the read side forces `AsNoTracking`.
- **`SpecificationEvaluator`** — bridges the EF-free `ISpecification<T>` (criteria, includes, ordering, paging, tracking, split-query, ignore-filters) onto an `IQueryable<T>`.
- **`TransactionBehavior<TRequest,TResponse>`** — wraps **commands** (detected through the CQRS marker interfaces, so `ICommand<T>` is caught too) in a transaction via EF's execution strategy. Queries pass through.
- **`AuditInterceptor` + `IAuditable`** — a singleton save-changes interceptor that stamps created/modified timestamps from `IClock`.
- **Transactional outbox** — `OutboxMessage` (bytes payload), `OutboxMessageConfiguration`, `IOutbox` / `EfOutbox` (enqueue in the same transaction), `OutboxRelay` (at-least-once background publisher), `OutboxOptions`.
- **Inbox idempotency** — `InboxMessage`, `InboxMessageConfiguration`, `EfIdempotencyStore` (dedupe by `(messageId, handler)`).
- **`ModelBuilderExtensions`** — `ApplyResqInfrastructure()` maps the outbox/inbox tables; `ApplyConfigurationsFrom(assembly)` applies your own configs.

## Wiring

```csharp
services.AddResqPersistence<AppDbContext>(
    (sp, options) => options.UseNpgsql(connectionString),
    setup =>
    {
        setup.UseOutbox = true;             // register EfOutbox + OutboxRelay
        setup.UseTransactionBehavior = true; // register TransactionBehavior last (innermost)
        setup.EnableAudit = true;            // attach AuditInterceptor
    });

services.AddResqRepositories(typeof(AppDbContext).Assembly);
```

`AddResqPersistence` pools the context (`AddDbContextPool`), relays a scoped `DbContext`, and registers `EfUnitOfWork`/`EfIdempotencyStore` scoped and `AuditInterceptor` singleton. In `OnModelCreating`, call `modelBuilder.ApplyResqInfrastructure().ApplyConfigurationsFrom(GetType().Assembly)`.

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
