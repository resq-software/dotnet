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
public sealed class Widget : AggregateRoot<Guid>
{
    private Widget(Guid id, string sku) : base(id) => Sku = sku;
    public string Sku { get; }

    public static Result<Widget> Create(string sku) =>
        string.IsNullOrWhiteSpace(sku)
            ? Result.Failure<Widget>(Error.Validation("widget.sku_required", "SKU is required."))
            : new Widget(Guid.NewGuid(), sku);
}
```

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
