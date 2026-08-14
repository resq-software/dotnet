# ResQ.BuildingBlocks.Testing

Lightweight, in-memory **test doubles** for the ResQ building blocks. No Docker, no
`WebApplicationFactory`, no ASP.NET framework reference — safe to add to any unit-test project.
(The heavy Docker / `WebApplicationFactory` helpers live in
[`ResQ.BuildingBlocks.Testing.Integration`](https://www.nuget.org/packages/ResQ.BuildingBlocks.Testing.Integration).)

Targets `net8.0` and `net9.0`. Depends on `ResQ.BuildingBlocks.Application` +
`ResQ.BuildingBlocks.Domain`, and brings `xunit`, `FluentAssertions`, and `NSubstitute` for consumers.

## What's in the box

| Type | Replaces / role |
|------|-----------------|
| `FakeClock` | Deterministic `IClock` you `Set`/`Advance` by hand |
| `NoopUnitOfWork` | `IUnitOfWork` that does nothing, returns `0` |
| `FakeUnitOfWork` | `IUnitOfWork` that counts saves and returns `AffectedRows` |
| `ThrowingUnitOfWork` | `IUnitOfWork` whose `SaveChangesAsync` always fails |
| `RecordingDomainEventDispatcher` | Captures dispatched `IDomainEvent`s in `Dispatched` |
| `RecordingIntegrationEventPublisher` | Captures published `IntegrationEvent`s in `Published` |
| `Builder<T>` | Base class for fluent test-data builders (implicit `T` conversion) |
| `CapturingLoggerProvider` | Captures formatted log lines in `Entries` |
| `XUnitLoggerProvider` | Forwards logs to an `ITestOutputHelper` |
| `HexagonRules` | Dependency-rule data for your NetArchTest assertions |
| `AddTestDoubles()` | One call to swap the four driven ports for the doubles above |

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Testing;

var services = new ServiceCollection();
// ... register the system under test ...
services.AddTestDoubles();               // swaps IClock, IUnitOfWork, dispatcher, publisher

var provider = services.BuildServiceProvider();
var clock = (FakeClock)provider.GetRequiredService<IClock>();
clock.Advance(TimeSpan.FromHours(1));    // time is now deterministic

var publisher = (RecordingIntegrationEventPublisher)provider.GetRequiredService<IIntegrationEventPublisher>();
// ... exercise the SUT ...
publisher.Published.Should().ContainSingle();
```

### A fluent builder

```csharp
public sealed class WidgetBuilder : Builder<Widget>
{
    private string _name = "default";
    public WidgetBuilder Named(string name) { _name = name; return this; }
    public override Widget Build() => new(_name);
}

Widget widget = new WidgetBuilder().Named("gadget"); // implicit Build()
```

### Architecture rule data

```csharp
foreach (var (inner, mustNotDependOn) in HexagonRules.DependencyRule)
{
    // feed `inner` / `mustNotDependOn` into NetArchTest in your own arch-test project
}
```

## License

Apache-2.0. See [LICENSE](https://github.com/resq-software/dotnet/blob/main/LICENSE).
