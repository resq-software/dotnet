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

# ResQ.BuildingBlocks.Domain

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Domain?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Domain)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)
[![dependencies](https://img.shields.io/badge/dependencies-none-25c68a?style=flat-square)](https://github.com/resq-software/dotnet)

> Hexagonal/DDD domain primitives — `Entity`, `AggregateRoot`, `ValueObject`, domain events, `Result`/`Error`, and guard clauses. The inner core: zero dependencies, no infrastructure.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Domain
```

Zero dependencies. This is the innermost layer of the hexagon — it references nothing outward, no frameworks, no infrastructure. The assembly is marked `IsAotCompatible`, so it is publishable into a trimmed/AOT app.

## Quick Start

Model an aggregate whose factory returns a `Result<T>` instead of throwing, and raise a domain event on state change:

```csharp
using ResQ.BuildingBlocks.Domain;

public sealed record WidgetCreated(Guid WidgetId, DateTimeOffset OccurredOnUtc) : IDomainEvent;

public sealed class Widget : AggregateRoot<Guid>
{
    private Widget(Guid id, string sku) : base(id) => Sku = sku;

    public string Sku { get; }

    public static Result<Widget> Create(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Failure<Widget>(
                Error.Validation("widget.sku_required", "SKU is required."));
        }

        var widget = new Widget(Guid.NewGuid(), sku);
        widget.Raise(new WidgetCreated(widget.Id, DateTimeOffset.UtcNow));
        return widget; // implicit lift into a successful Result<Widget>
    }
}

// Consume the outcome without exceptions for expected failures:
Result<Widget> result = Widget.Create("SKU-123");

if (result.IsSuccess)
{
    Widget widget = result.Value;
    IReadOnlyCollection<IDomainEvent> events = widget.DomainEvents; // pending dispatch
}
else
{
    Error error = result.Error; // (code, message, type)
}
```

Model an identity-free value with equality-by-components:

```csharp
public sealed class Money : ValueObject
{
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = Guard.AgainstNullOrWhiteSpace(currency);
    }

    public decimal Amount { get; }
    public string Currency { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

## API Reference

Namespace: `ResQ.BuildingBlocks.Domain`.

### `Entity<TId>` / `AggregateRoot<TId>`

`Entity<TId>` is the base for domain objects with a stable identity; equality is by identity, not by attribute values (`TId` is constrained `notnull`). `AggregateRoot<TId>` derives from `Entity<TId>` and marks the consistency boundary — the only kind of object repositories load and save.

| Member | Signature | Description |
|--------|-----------|-------------|
| `Entity(TId id)` | `protected Entity(TId id)` | Constructs the entity with its identity. |
| `Id` | `TId { get; }` | The entity's stable identity. |
| `DomainEvents` | `IReadOnlyCollection<IDomainEvent> { get; }` | Domain events raised by this entity, awaiting dispatch. |
| `Raise(IDomainEvent)` | `protected void` | Records a domain event; dispatched by infrastructure after persistence. |
| `ClearDomainEvents()` | `void` | Clears pending domain events (called by the dispatcher once handled). |
| `Equals(Entity<TId>?)` | `bool` | Identity equality, additionally gated on exact runtime type. |
| `Equals(object?)` | `override bool` | Object override delegating to the typed comparison. |
| `GetHashCode()` | `override int` | Combines the runtime type and `Id`. |
| `AggregateRoot(TId id)` | `protected AggregateRoot(TId id)` | Constructs the aggregate root with its identity. |

### `IDomainEvent` / `IHasDomainEvents`

`IDomainEvent` marks something meaningful that happened in the domain. `IHasDomainEvents` is a non-generic seam for reading and clearing the events an aggregate has raised — it lets infrastructure enumerate heterogeneous aggregates in a change tracker without knowing their `TId`, since you cannot pattern-match on the open generic `Entity<>`. `Entity<TId>` implements it.

| Type | Member | Description |
|------|--------|-------------|
| `IDomainEvent` | `DateTimeOffset OccurredOnUtc { get; }` | When the event occurred (UTC). |
| `IHasDomainEvents` | `IReadOnlyCollection<IDomainEvent> DomainEvents { get; }` | Events raised by the aggregate, awaiting dispatch. |
| `IHasDomainEvents` | `void ClearDomainEvents()` | Clears pending domain events. |

### `ValueObject`

Base for immutable, identity-free values that are equal when their components are equal. Derive and override `GetEqualityComponents()`; equality and hashing are computed from that sequence (order-sensitive via `SequenceEqual`).

| Member | Signature | Description |
|--------|-----------|-------------|
| `GetEqualityComponents()` | `protected abstract IEnumerable<object?>` | The atomic components that define equality. |
| `Equals(ValueObject?)` | `bool` | Component equality, additionally gated on exact runtime type. |
| `Equals(object?)` | `override bool` | Object override delegating to the typed comparison. |
| `GetHashCode()` | `override int` | Aggregates a hash over the equality components. |

### `Result` / `Result<T>`

Explicit success/failure — no exceptions for expected failures. A successful `Result` carries `Error.None`; a failed one carries a non-`None` `Error`. The constructor enforces this invariant and throws `ArgumentException` if violated. `Result<T>.Value` throws `InvalidOperationException` when read on a failed result; a `T` lifts implicitly into a successful `Result<T>`.

| Member | Signature | Description |
|--------|-----------|-------------|
| `Result.Success()` | `static Result` | A successful result with no value. |
| `Result.Failure(Error)` | `static Result` | A failed result carrying the error. |
| `Result.Success<T>(T)` | `static Result<T>` | A successful result carrying a value. |
| `Result.Failure<T>(Error)` | `static Result<T>` | A failed result of type `T`. |
| `IsSuccess` | `bool { get; }` | True when the operation succeeded. |
| `IsFailure` | `bool { get; }` | `!IsSuccess`. |
| `Error` | `Error { get; }` | The error on failure, or `Error.None` on success. |
| `Result<T>.Value` | `T { get; }` | The value on success; throws `InvalidOperationException` on failure. |
| `implicit operator Result<T>(T)` | `static` | Lifts a value into a successful `Result<T>`. |

### `Error` / `ErrorType`

`Error` is a `sealed record` of a stable machine-readable `Code`, a human-readable `Message`, and an `ErrorType`. Record equality, `Deconstruct`, `==`/`!=`, and `ToString()` are provided. Static factories preset the classification.

| Member | Signature | Description |
|--------|-----------|-------------|
| `Error(string, string, ErrorType)` | ctor, `Type` defaults to `ErrorType.Failure` | Constructs an error from code, message, and type. |
| `Error.None` | `static readonly Error` | The absence of an error (empty code and message). |
| `Error.Validation(code, message)` | `static Error` | `ErrorType.Validation`. |
| `Error.NotFound(code, message)` | `static Error` | `ErrorType.NotFound`. |
| `Error.Conflict(code, message)` | `static Error` | `ErrorType.Conflict`. |
| `Error.Unauthorized(code, message)` | `static Error` | `ErrorType.Unauthorized` (unauthenticated; HTTP 401). |
| `Error.Forbidden(code, message)` | `static Error` | `ErrorType.Forbidden` (authenticated but not permitted; HTTP 403). |
| `Code` / `Message` / `Type` | `string` / `string` / `ErrorType` (init) | The error's components. |

`ErrorType` classifies an error so adapters can map it (e.g. to an HTTP status):

| Value | Numeric | Meaning |
|-------|---------|---------|
| `Failure` | `0` | A generic failure. |
| `Validation` | `1` | Input failed validation. |
| `NotFound` | `2` | The requested resource was not found. |
| `Conflict` | `3` | Conflicts with current state (e.g. a uniqueness violation). |
| `Unauthorized` | `4` | Caller is not authenticated (HTTP 401). |
| `Forbidden` | `5` | Caller is authenticated but not permitted (HTTP 403). |

### `Guard`

Static guard clauses for enforcing invariants at domain boundaries. Each returns the validated value (enabling inline assignment) or throws. The `name` argument is captured automatically from the call site via `[CallerArgumentExpression]`.

| Member | Signature | Throws when |
|--------|-----------|-------------|
| `AgainstNull<T>(T?, string?)` | `static T` | `value` is null → `ArgumentNullException`. |
| `AgainstNullOrWhiteSpace(string?, string?)` | `static string` | `value` is null, empty, or whitespace → `ArgumentException`. |
| `AgainstNonPositive(int, string?)` | `static int` | `value <= 0` → `ArgumentOutOfRangeException`. |

## Prerequisites

- **Target frameworks**: `net8.0`; `net9.0`
- **Dependencies**: none — the domain core is dependency-free and reflection-free, and is `IsAotCompatible` (safe to publish into a trimmed/AOT app).

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
