# ResQ.BuildingBlocks.Application

The **application core** (outer ring) — orchestration contracts, driven **ports**, and cross-cutting behaviors. Depends only on `ResQ.BuildingBlocks.Domain`.

- **CQRS contracts:** `ICommand` / `ICommand<T>` / `IQuery<T>` + their handlers (own mediator abstraction — no third-party pipeline dependency, so no licensing surprises).
- **Driven ports (the Core owns these; adapters implement them):** `IUnitOfWork`, `IClock`, `IDomainEventDispatcher`.
- **Pipeline behaviors:** `IPipelineBehavior<TRequest,TResponse>` + `ValidationBehavior` (FluentValidation) and `LoggingBehavior`.

```csharp
public sealed record RegisterDrone(string CallSign) : ICommand<Guid>;

internal sealed class RegisterDroneHandler(IDroneRepository repo, IUnitOfWork uow)
    : ICommandHandler<RegisterDrone, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterDrone command, CancellationToken ct)
    {
        var drone = Drone.Register(command.CallSign);
        if (drone.IsFailure) return Result.Failure<Guid>(drone.Error);
        repo.Add(drone.Value);
        await uow.SaveChangesAsync(ct);
        return drone.Value.Id;
    }
}
```

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
