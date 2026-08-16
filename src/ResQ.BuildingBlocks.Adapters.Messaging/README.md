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

# ResQ.BuildingBlocks.Adapters.Messaging

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Adapters.Messaging?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Adapters.Messaging)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> Broker-agnostic integration-event transport — implements the application's messaging ports over an in-memory `System.Threading.Channels` loopback, with JSON serialization and a concurrency-bounded, idempotent, retrying `BackgroundService` consumer. Ships no broker client.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Adapters.Messaging
```

Depends on `ResQ.BuildingBlocks.Application` (whose integration-event ports it implements) and `ResQ.BuildingBlocks.Domain`. Runtime dependencies: the `Microsoft.Extensions.*` abstractions (DI, Logging, Hosting, Options), `Polly.Core` for the retry pipeline, and `Scrutor` for handler assembly scanning. There is **no broker client** — the default transport is an in-process channel.

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResQ.BuildingBlocks.Adapters.Messaging;
using ResQ.BuildingBlocks.Application; // IntegrationEvent, IIntegrationEventPublisher, IIntegrationEventHandler<>

// 1. An integration event (a fact from your application core).
public sealed record OrderPlaced(Guid OrderId, decimal Total) : IntegrationEvent;

// 2. A handler — one per (event, concern). Handlers are resolved per message, scoped.
public sealed class NotifyWarehouse : IIntegrationEventHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced @event, CancellationToken ct)
    {
        Console.WriteLine($"Order {@event.OrderId} placed for {@event.Total:C}.");
        return Task.CompletedTask;
    }
}

// 3. A consumer — subclass to bind a source. The subclass name is the idempotency handler key.
public sealed class OrderConsumer(
    IMessageSource source,
    IServiceScopeFactory scopes,
    IOptions<ConsumerOptions> options,
    ILogger<OrderConsumer> logger)
    : MessageConsumerService(source, scopes, options, logger);

// 4. Wire it up.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddResqMessaging(
    b => b.UseInMemory(),           // ChannelMessageBroker is both publisher and source
    typeof(OrderPlaced).Assembly);  // scans IntegrationEvent subtypes + their handlers

builder.Services.AddHostedService<OrderConsumer>();

var app = builder.Build();

// 5. Publish — the in-memory broker loops the event straight back to OrderConsumer.
var publisher = app.Services.GetRequiredService<IIntegrationEventPublisher>();
await publisher.PublishAsync(new OrderPlaced(Guid.NewGuid(), 42.00m));

await app.RunAsync();
```

Swap `UseInMemory()` for a real broker publisher/source later without touching a single publish or handler call site.

## API Reference

`IntegrationEvent`, `IIntegrationEventPublisher`, `IMessageSerializer`, `IIntegrationEventTypeRegistry`, and `IIdempotencyStore` are the **ports** owned by `ResQ.BuildingBlocks.Application`; this package supplies the adapters below.

### Composition

#### `MessagingServiceCollectionExtensions.AddResqMessaging`

```csharp
static IServiceCollection AddResqMessaging(
    this IServiceCollection services,
    Action<MessagingBuilder>? configure = null,
    params Assembly[] handlerAssemblies)
```

Registers the messaging core: the JSON serializer (`IMessageSerializer`), an event-type registry populated by scanning `handlerAssemblies` for `IntegrationEvent` subtypes, the `IntegrationEventDispatcher` (scoped), the discovered `IIntegrationEventHandler<>` implementations (scoped, via Scrutor), and the default `NullIdempotencyStore` + `LoggingDeadLetterSink`. The `configure` callback then picks the transport and reliability components. Registered components use `TryAdd`, so a durable idempotency store or dead-letter sink registered elsewhere (e.g. the EF inbox from `Adapters.Persistence`) wins.

#### `MessagingBuilder`

Fluent builder passed to the `configure` callback; every method mutates the `IServiceCollection` and returns the builder for chaining.

| Member | Signature | Description |
|--------|-----------|-------------|
| `UseInMemory` | `() -> MessagingBuilder` | Registers `ChannelMessageBroker` as **both** the `IIntegrationEventPublisher` and the (unkeyed) `IMessageSource`. |
| `UsePublisher<TPublisher>` | `() -> MessagingBuilder` | Replaces the outbound `IIntegrationEventPublisher` with `TPublisher`. |
| `AddMessageSource<TSource>` | `() -> MessagingBuilder` | Adds `TSource` as the default (unkeyed) inbound `IMessageSource` a single consumer drains. |
| `AddKeyedMessageSource<TSource>` | `(object sourceKey) -> MessagingBuilder` | Adds `TSource` as a **keyed** source, so distinct consumers each drain a distinct source. |
| `UseDeadLetterSink<TSink>` | `() -> MessagingBuilder` | Replaces the default `LoggingDeadLetterSink` with `TSink`. |
| `Services` | `IServiceCollection { get; }` | The service collection being configured, for advanced registration. |

### Transport

#### `MessageEnvelope`

The wire record — the immutable unit read from and acknowledged back to a source.

| Member | Type | Description |
|--------|------|-------------|
| `MessageId` | `string` | Stable per-message identifier; also the idempotency key. |
| `MessageType` | `string` | Logical event-type name used to resolve the CLR type (the event's `EventType`). |
| `OccurredOnUtc` | `DateTimeOffset` | When the underlying event occurred. |
| `Headers` | `IReadOnlyDictionary<string, string>` | Opaque transport/metadata headers. |
| `Body` | `ReadOnlyMemory<byte>` | The serialized event payload. |

#### `IMessageSource`

An inbound stream a consumer drains. `ChannelMessageBroker` is the built-in implementation.

| Member | Signature | Description |
|--------|-----------|-------------|
| `ReadAllAsync` | `(CancellationToken) -> IAsyncEnumerable<MessageEnvelope>` | Streams messages until cancelled or the stream completes. |
| `AcknowledgeAsync` | `(MessageEnvelope, CancellationToken) -> Task` | Marks a message as successfully processed. |
| `NackAsync` | `(MessageEnvelope, Exception, CancellationToken) -> Task` | Signals a processing failure; the in-memory broker re-enqueues. |

#### `ChannelMessageBroker`

An unbounded `System.Threading.Channels` loopback that is simultaneously an `IIntegrationEventPublisher` (`PublishAsync` writes) and an `IMessageSource` (`ReadAllAsync` reads). `NackAsync` re-enqueues the envelope; `AcknowledgeAsync` is a no-op. Constructed with an `IMessageSerializer` and an `IIntegrationEventTypeRegistry`.

### Serialization

#### `SystemTextJsonMessageSerializer`

The default `IMessageSerializer` — UTF-8 JSON on `System.Text.Json` web defaults, with an optional custom `JsonSerializerOptions`.

| Member | Signature | Description |
|--------|-----------|-------------|
| `SystemTextJsonMessageSerializer` | `(JsonSerializerOptions? options = null)` | Uses web defaults unless `options` is supplied. |
| `Serialize` | `(object value, Type type) -> byte[]` | Serializes an event to a UTF-8 JSON payload. |
| `Deserialize` | `(ReadOnlySpan<byte> data, Type type) -> object?` | Deserializes a payload back to the given CLR type. |
| `ContentType` | `string { get; }` | The MIME content type of the serialized payload. |

### Dispatch

#### `IIntegrationEventHandler<TEvent>`

Implement one per (event, concern); all handlers for a delivered event run in the message's scope.

```csharp
Task Handle(TEvent @event, CancellationToken ct);
```

#### `IntegrationEventDispatcher`

Resolves the CLR type from the registry, deserializes the body, and fans the typed event out to every `IIntegrationEventHandler<TEvent>` in the current scope. Per-type invokers are reflection-built once and cached. Within a single delivery it tracks which handlers have already completed, so a retry does not re-run a handler that already succeeded (see [Delivery semantics](#delivery-semantics)).

#### `DictionaryIntegrationEventTypeRegistry`

The default `IIntegrationEventTypeRegistry` — an in-memory map keyed by each event's namespace-qualified CLR type name (`Type.FullName`, the default `EventType`).

| Member | Signature | Description |
|--------|-----------|-------------|
| `Register` | `(Type eventType) -> void` | Registers an `IntegrationEvent` subtype. Re-registering the same type is a no-op; a **different** type under the same wire key throws, surfacing the collision at startup. |
| `TryResolve` | `(string eventType, out Type? type) -> bool` | Resolves the CLR type for a logical event-type name. |

### Consumer

#### `MessageConsumerService`

An abstract `BackgroundService` that drains an `IMessageSource` and dispatches each message. Subclass it and register the subclass as a hosted service; the **subclass name is the idempotency handler key**. Concurrency is bounded by a `SemaphoreSlim`, each dispatch is wrapped in the Polly retry pipeline, exhausted retries are dead-lettered, in-flight work is drained on shutdown, and a source-level fault (e.g. a dropped broker connection) is backed off and re-subscribed rather than tearing the host down.

Constructor: `(IMessageSource source, IServiceScopeFactory scopes, IOptions<ConsumerOptions> options, ILogger<MessageConsumerService> logger)`.

#### `ConsumerOptions`

Bind via the standard options pattern (`services.Configure<ConsumerOptions>(...)`).

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxConcurrency` | `int` | `1` | Maximum messages processed concurrently. |
| `Retry` | `RetryOptions` | *(defaults)* | Retry policy wrapped around each message's dispatch. |
| `EnableIdempotency` | `bool` | `true` | Skip messages this handler has already processed (via `IIdempotencyStore`). |

#### `RetryOptions`

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxAttempts` | `int` | `3` | Total processing attempts, including the first. |
| `BaseDelay` | `TimeSpan` | `1s` | Delay before the first retry, grown exponentially thereafter. |
| `BackoffMultiplier` | `double` | `2.0` | Factor by which the delay grows each attempt. |
| `UseJitter` | `bool` | `true` | Randomize the delay (±20%, cryptographically strong RNG) to avoid thundering herds. |

### Reliability & opt-outs

| Type | Role |
|------|------|
| `IDeadLetterSink` | Receives poison messages once the retry budget is exhausted: `SendAsync(MessageEnvelope, Exception, int attempts, CancellationToken)`. |
| `LoggingDeadLetterSink` | Default sink — logs the poison message at error level and drops it. |
| `NullDeadLetterSink` | Silently discards poison messages (no-op). |
| `NullIdempotencyStore` | Default `IIdempotencyStore` — always reports "not processed"; replace with a durable store for real dedup. |
| `NullIntegrationEventPublisher` | An `IIntegrationEventPublisher` that discards everything published — a null object for tests or disabled outbound paths. |

## Delivery semantics

- **At-least-once.** Handlers must be individually idempotent to tolerate redelivery. The message-level idempotency key is per `(message, consumer)`.
- **Per-handler fan-out dedup within a delivery.** The dispatcher is scoped per message, so a retry re-runs the fan-out on the same instance; handlers that already succeeded on an earlier attempt are skipped. This only spans the retries of a single delivery — a fresh redelivery starts clean.
- **Transient vs. permanent classification.** The retry pipeline retries only plausibly-transient faults. Deterministic failures — `JsonException`, `FluentValidation.ValidationException`, `ArgumentException`, `NotSupportedException`, and the dispatcher's `InvalidOperationException` (unregistered type / null-deserialized body) — are treated as permanent poison and dead-lettered on the first pass instead of burning the backoff budget.
- **Dead-letter, then acknowledge.** On exhaustion the message is sent to the `IDeadLetterSink` and then acknowledged so the transport drops it — it is never also nack-requeued (which would redeliver a poison message forever).
- **Supervision back-off.** A faulting source is logged, backed off (floored at 500ms, with the same jitter as the retry pipeline), and re-subscribed; only cancellation of the stopping token — or the source completing its stream — exits the drain loop.

## Multiple sources

A single `MessageConsumerService` subclass resolves the default (unkeyed) `IMessageSource`. To run several consumers each draining a distinct source, register each with `AddKeyedMessageSource<TSource>(sourceKey)` and annotate the subclass's base-constructor `source` parameter with `[FromKeyedServices(sourceKey)]`.

## Prerequisites

- **Target frameworks**: `net8.0`, `net9.0`
- **Hosting**: a `Microsoft.Extensions.Hosting` host to run `MessageConsumerService` as a `BackgroundService`.

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
