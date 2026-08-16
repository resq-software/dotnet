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

# ResQ.BuildingBlocks.ServiceDefaults

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.ServiceDefaults?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.ServiceDefaults)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> Aspire-style cross-cutting service defaults — one call wires OpenTelemetry, health checks, service discovery, standard HTTP resilience, and a testable clock, plus the CQRS observability behaviors, strongly-typed options, and a Polly pipeline keyed on the domain `ErrorType`.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.ServiceDefaults
```

Depends on `ResQ.BuildingBlocks.Application` and `ResQ.BuildingBlocks.Domain`, and pulls the OpenTelemetry hosting stack (OTLP exporter + ASP.NET Core / HTTP-client / runtime instrumentation), `Microsoft.Extensions.Http.Resilience`, and `Microsoft.Extensions.ServiceDiscovery`. It carries a `FrameworkReference` to `Microsoft.AspNetCore.App`, so it targets ASP.NET Core hosts.

## Quick Start

```csharp
using ResQ.BuildingBlocks.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();               // OTel / health / service discovery / resilience / IClock — NO behaviors

builder.Services
    .AddResqApplication(typeof(CreateWidget).Assembly)
    .AddValidatorsFrom(typeof(CreateWidget).Assembly)
    .AddLoggingPipeline()                    // outermost
    .AddValidationPipeline();

builder.Services.AddResqObservabilityBehaviors();   // Tracing then Metrics — AFTER validation

var app = builder.Build();
app.MapDefaultEndpoints();                   // /health + /alive (non-Production only)
app.Run();
```

Resulting pipeline order (first-registered = outermost): **Logging → Validation → Tracing → Metrics → Transaction → handler**.

`AddServiceDefaults` deliberately does **not** register the observability pipeline behaviors — that is the separate, explicit `AddResqObservabilityBehaviors` call, placed *after* logging and validation so tracing and metrics sit at the correct depth.

## API Reference

### `ServiceDefaultsExtensions`

Host composition-root extensions on `IHostApplicationBuilder` (and `WebApplication` for endpoint mapping).

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddServiceDefaults<TBuilder>` | `TBuilder AddServiceDefaults<TBuilder>(this TBuilder)` | Wires OpenTelemetry, the default liveness health check, service discovery, standard HTTP-client resilience, and a singleton `SystemClock` for `IClock`. Does **not** register pipeline behaviors. |
| `ConfigureOpenTelemetry<TBuilder>` | `TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder)` | Configures OTel logs (`IncludeScopes` + `IncludeFormattedMessage`), metrics (ASP.NET Core + HTTP-client + runtime instrumentation + the `ResqDiagnostics.MeterName` meter), and traces (ASP.NET Core + HTTP-client sources + the `ResqDiagnostics.ActivitySourceName` and app-name sources). Health-probe requests are filtered out of traces. The OTLP exporter turns on only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. |
| `AddDefaultHealthChecks<TBuilder>` | `TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder)` | Adds a `self` liveness check tagged `live`. |
| `MapDefaultEndpoints` | `WebApplication MapDefaultEndpoints(this WebApplication)` | Maps `/health` (all checks) and `/alive` (only `live`-tagged checks). Mapped **only outside Production**, since unauthenticated health endpoints leak readiness detail — guard or authorize them explicitly before exposing them in Production. |

`TBuilder` is constrained to `IHostApplicationBuilder`.

### `ResqDiagnostics`

Single source of truth for telemetry names — the activity-source name and meter name are intentionally identical, so a service allow-lists both with one string.

| Member | Type | Value / Description |
|--------|------|---------------------|
| `ActivitySourceName` | `const string` | `"ResQ.BuildingBlocks"` — registered with tracing via `AddSource`. |
| `MeterName` | `const string` | `"ResQ.BuildingBlocks"` — registered with metrics via `AddMeter` and passed to `IMeterFactory.Create`. |
| `ActivitySource` | `static ActivitySource` | The shared source the pipeline behaviors start activities on. |

There is intentionally no hand-newed static `Meter`: metrics instruments come from `IMeterFactory` using `MeterName`, so the emitting meter and the allow-listed name are always the same string.

### Observability pipeline behaviors

CQRS `IPipelineBehavior<TRequest, TResponse>` implementations registered by `AddResqObservabilityBehaviors`.

| Type | Registration | Behavior |
|------|--------------|----------|
| `TracingBehavior<TRequest, TResponse>` | first (outer) | Starts an `Activity` named for the request type on `ResqDiagnostics.ActivitySource`; sets OK/Error status (a failed `Result` or a thrown exception is Error). Tags **no** business data, so traces never leak payloads. |
| `MetricsBehavior<TRequest, TResponse>` | second (inner) | Records a duration histogram (`resq.cqrs.request.duration`, ms) and a failure counter (`resq.cqrs.request.failures`), each tagged by request name. Instruments come from the injected `IMeterFactory`; both a failed `Result` and a thrown exception count as a failure. |

| Member | Signature | Description |
|--------|-----------|-------------|
| `ObservabilityExtensions.AddResqObservabilityBehaviors` | `IServiceCollection AddResqObservabilityBehaviors(this IServiceCollection)` | Registers `TracingBehavior<,>` then `MetricsBehavior<,>` as open generics. Call exactly once, after the logging and validation behaviors. |
| `MetricsBehavior<,>(IMeterFactory)` | constructor | Injects the meter factory used to create instruments. Instruments are cached per `IMeterFactory`, so a second in-process host (e.g. a `WebApplicationFactory` test) gets its own instruments instead of recording into an orphaned meter. |

### `OptionsRegistrationExtensions`

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddResqOptions<TOptions>` | `OptionsBuilder<TOptions> AddResqOptions<TOptions>(this IServiceCollection, string sectionName)` | Binds `TOptions` to the config section `sectionName`, runs `ValidateDataAnnotations`, and `ValidateOnStart` — so a misconfigured service fails at boot rather than on first use. Returns the `OptionsBuilder<TOptions>` for further chaining. `TOptions : class`. |

```csharp
builder.Services.AddResqOptions<PaymentsOptions>("Payments");
```

### `ResiliencePipelineExtensions`

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddResqResiliencePipeline` | `IServiceCollection AddResqResiliencePipeline(this IServiceCollection, string key, Action<RetryStrategyOptions>? configure = null)` | Registers a named Polly pipeline of retry + circuit breaker + timeout under `key`. Optional `configure` tweaks the retry options. |

The retry/break rule is typed on the domain `Result` world rather than a plain retryable flag: it handles any exception, or a failed `Result` whose `Error.Type` is `ErrorType.Failure` (transient). It never retries `Validation`, `NotFound`, `Conflict`, or `Unauthorized` — those are not transient. Sharing the rule between retry and breaker ensures the breaker actually opens on a sustained stream of failed `Result`s instead of retrying them forever. Defaults: 3 attempts, 1s base delay (exponential + jitter), 30s timeout.

```csharp
builder.Services.AddResqResiliencePipeline(
    "orders-api",
    retry => retry.MaxRetryAttempts = 5);
```

### `SystemClock`

| Member | Signature | Description |
|--------|-----------|-------------|
| `SystemClock()` | constructor | The production `IClock`, registered as a singleton by `AddServiceDefaults`. |
| `UtcNow` | `DateTimeOffset UtcNow { get; }` | Reads real UTC wall-clock time through `TimeProvider.System`. Tests substitute a fake clock instead. |

## Telemetry names

| Instrument | Name | Unit | Kind |
|------------|------|------|------|
| Activity source | `ResQ.BuildingBlocks` | — | traces |
| Meter | `ResQ.BuildingBlocks` | — | metrics |
| Request duration | `resq.cqrs.request.duration` | `ms` | histogram |
| Request failures | `resq.cqrs.request.failures` | `{failure}` | counter |

## Known coupling (accepted)

`FrameworkReference Microsoft.AspNetCore.App` and `AddAspNetCoreInstrumentation` are unconditional, so a pure worker host that calls `AddServiceDefaults` still pulls the ASP.NET Core shared framework. This mirrors Aspire's own `ServiceDefaults`; a `ServiceDefaults.Worker` split is possible future work.

## Prerequisites

- **Target frameworks**: `net8.0`, `net9.0`
- **ASP.NET Core** host — the package references the `Microsoft.AspNetCore.App` shared framework
- An **OTLP endpoint** (`OTEL_EXPORTER_OTLP_ENDPOINT`) only if you want the OTLP exporter enabled

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
