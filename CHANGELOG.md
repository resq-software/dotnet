# Changelog

All notable changes to the `ResQ.BuildingBlocks.*` packages are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-08-15

First public release on nuget.org. (`v0.1.0` was tagged but never published, so there are
no shipped consumers — pre-1.0 API-shaping changes below are safe.)

### Added
- `MessagingBuilder.AddKeyedMessageSource<TSource>(object sourceKey)` — bind distinct message
  sources to distinct consumers via keyed DI (a consumer selects its source with
  `[FromKeyedServices(sourceKey)]`).
- `AuditInterceptor.SavingChanges(...)` — the **synchronous** `SaveChanges` path now stamps
  `IAuditable` entities (previously async-only).
- `OutboxOptions` is validated on start (`PollingInterval > 0`, `BatchSize >= 1`, `MaxAttempts >= 1`).

### Changed
- **Breaking (pre-release):** `MessagingBuilder.AddConsumer<TSource>()` renamed to
  `AddMessageSource<TSource>()` — it registers an inbound source, not a consumer.
- CQRS `ValidationBehavior` returns a failed `Result` (`Error.Validation`) instead of throwing.
- Message-consumer retry classifies failures: deterministic poison (malformed JSON, validation,
  unknown/unregistered type) is dead-lettered immediately instead of burning the retry budget.
- Resilience retry no longer retries the circuit breaker's `BrokenCircuitException` or the
  timeout's `TimeoutRejectedException` (fails fast).
- `ReadRepository.GetByIdAsync` is now no-tracking, matching the class contract.
- The OpenAPI JSON document (`/openapi/v1.json`) is served only outside Production, matching
  health-endpoint gating.
- NuGet publishing uses GitHub OIDC trusted publishing — no long-lived API key.

### Fixed
- `TransactionBehavior` resets the EF `ChangeTracker` before a retriable execution-strategy
  replay (no duplicate inserts or lost domain events).
- Integration-event type identity is keyed on `FullName`, preventing silent cross-namespace
  collisions and wrong-handler dispatch.
- The message consumer supervises its drain loop — a transport fault is logged and retried with
  backoff instead of tearing down the host.
- Idempotency `MarkProcessed` absorbs duplicate-key races instead of dead-lettering a message
  whose handlers already succeeded.
- The outbox relay consumes retry attempts only for message-specific faults, so a transient
  broker outage no longer strands the whole backlog.
- Recording test doubles are thread-safe (`ConcurrentQueue` + snapshot reads).
- `EfUnitOfWork`'s domain-event drain loop is bounded and honors cancellation.
- `LoggingBehavior` logs a terminal error when a handler throws.
- Dead-letter records report the attempts actually made (never "0 attempt(s)").
- Web `Result`→HTTP mapping honors `StatusOverrides`/`DocsBaseUri`; CQRS metrics instruments are
  cached per `IMeterFactory`; fan-out retry skips already-succeeded handlers.
- Log forging prevented by sanitizing the request path before logging.

[0.2.0]: https://github.com/resq-software/dotnet/releases/tag/v0.2.0
