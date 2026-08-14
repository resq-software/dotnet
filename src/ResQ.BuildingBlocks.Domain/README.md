# ResQ.BuildingBlocks.Domain

The **inner core** of the hexagon — dependency-free DDD primitives. No infrastructure, no frameworks.

| Type | Purpose |
|---|---|
| `Entity<TId>` | Identity-based equality; raises/holds `IDomainEvent`s |
| `AggregateRoot<TId>` | The consistency boundary; what repositories load/save |
| `ValueObject` | Immutable, equal-by-components |
| `IDomainEvent` | Something meaningful that happened in the domain |
| `Result` / `Result<T>` / `Error` | Explicit success/failure — no exceptions for expected failures |
| `Guard` | Invariant enforcement at domain boundaries |

```csharp
public sealed class Drone : AggregateRoot<Guid>
{
    private Drone(Guid id, string callSign) : base(id) => CallSign = callSign;
    public string CallSign { get; }

    public static Result<Drone> Register(string callSign) =>
        string.IsNullOrWhiteSpace(callSign)
            ? Result.Failure<Drone>(Error.Validation("drone.callsign_required", "Call sign is required."))
            : new Drone(Guid.NewGuid(), callSign);
}
```

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
