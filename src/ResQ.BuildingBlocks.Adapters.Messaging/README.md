# ResQ.BuildingBlocks.Adapters.Messaging

A **broker-agnostic messaging adapter** (driven ring) that implements the integration-event **ports** owned by `ResQ.BuildingBlocks.Application` and adds the transport, serialization, and consumer machinery. It ships **no broker client** — the default transport is an in-memory `System.Threading.Channels` loopback, so a service is publish/consume-capable from minute one and can swap in a real broker later without touching call sites.

Depends only on `ResQ.BuildingBlocks.Application` + `ResQ.BuildingBlocks.Domain`.

## What's in the box

- **Serialization:** `SystemTextJsonMessageSerializer` (`IMessageSerializer`) — UTF-8 JSON, web defaults, optional custom `JsonSerializerOptions`.
- **Transport contracts:** `MessageEnvelope` (the wire record) and `IMessageSource` (read / acknowledge / nack).
- **Default transport:** `ChannelMessageBroker` — an unbounded channel that is both `IIntegrationEventPublisher` (write) and `IMessageSource` (read). Nacks re-enqueue.
- **Typed dispatch:** `IIntegrationEventHandler<TEvent>` + `IntegrationEventDispatcher` — resolves the CLR type via `DictionaryIntegrationEventTypeRegistry`, deserializes, and fans out to every handler in the current scope (reflection cached per type).
- **Consumer:** `MessageConsumerService` (`BackgroundService`) — concurrency-bounded (`SemaphoreSlim`), idempotency short-circuit, a Polly `ResiliencePipeline` retry built from `RetryOptions`, dead-letter on exhaustion, and in-flight draining on shutdown. Configured via `ConsumerOptions`.
- **Reliability:** `RetryOptions`, `IDeadLetterSink` (+ `LoggingDeadLetterSink` default, `NullDeadLetterSink`).
- **Opt-outs:** `NullIdempotencyStore` (always "not processed"), `NullIntegrationEventPublisher` (discards).

## Wiring

```csharp
services.AddResqMessaging(
    b => b.UseInMemory(),          // publisher + source = ChannelMessageBroker
    typeof(SomethingHappened).Assembly);  // scans IntegrationEvent subtypes + handlers
```

`AddResqMessaging` registers the serializer, a registry populated from the scanned assemblies, the dispatcher, the discovered `IIntegrationEventHandler<>` implementations (scoped), and the default `NullIdempotencyStore` + `LoggingDeadLetterSink`. Swap components with `UsePublisher<T>()`, `AddConsumer<TSource>()`, and `UseDeadLetterSink<T>()`.

To run a consumer, subclass `MessageConsumerService` and register it as a hosted service; the subclass name is the idempotency handler key. A durable idempotency store (e.g. the EF inbox from `Adapters.Persistence`) or dead-letter sink registered elsewhere is honored automatically.

> The dead-letter / retry-with-jitter / idempotency stack is exercised through the in-memory transport until a real broker adapter (e.g. Kafka) lands.

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
