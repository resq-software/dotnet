# ResQ.BuildingBlocks.Application

The **application core** (outer ring) — orchestration contracts, driven **ports**, and cross-cutting behaviors. Depends only on `ResQ.BuildingBlocks.Domain`.

- **CQRS contracts:** `ICommand` / `ICommand<T>` / `IQuery<T>` + their handlers (own mediator abstraction — no third-party pipeline dependency, so no licensing surprises).
- **Driven ports (the Core owns these; adapters implement them):** `IUnitOfWork`, `IClock`, `IDomainEventDispatcher`.
- **Pipeline behaviors:** `IPipelineBehavior<TRequest,TResponse>` + `ValidationBehavior` (FluentValidation) and `LoggingBehavior`.
- **Dispatch:** `ISender` / `Sender` route each request to its single handler through the ordered behavior chain; `IDomainEventHandler<TEvent>` + `DomainEventDispatcher` fan domain events out to their handlers by runtime type.
- **Persistence ports:** `IRepository<TAggregate,TId>` / `IReadRepository<TAggregate,TId>` and an EF-free `ISpecification<T>` / `Specification<T>` (criteria, includes, ordering, paging, tracking) — adapters translate the specification onto their query engine.
- **Pagination:** `OffsetPage<T>` (page/size/total) and the experimental `CursorPage<T>` (keyset).
- **Integration ports** (shared by the messaging and persistence adapters, so neither references the other): `IntegrationEvent`, `IIntegrationEventPublisher`, `IMessageSerializer`, `IIntegrationEventTypeRegistry`, `IIdempotencyStore`.

```csharp
public sealed record CreateWidget(string Sku) : ICommand<Guid>;

internal sealed class CreateWidgetHandler(IWidgetRepository repo, IUnitOfWork uow)
    : ICommandHandler<CreateWidget, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWidget command, CancellationToken ct)
    {
        var widget = Widget.Create(command.Sku);
        if (widget.IsFailure) return Result.Failure<Guid>(widget.Error);
        repo.Add(widget.Value);
        await uow.SaveChangesAsync(ct);
        return widget.Value.Id;
    }
}
```

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
