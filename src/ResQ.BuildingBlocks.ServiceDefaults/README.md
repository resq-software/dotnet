# ResQ.BuildingBlocks.ServiceDefaults

Aspire-style **cross-cutting defaults** — one call from a host's composition root wires observability, health, resilience, and a testable clock. Depends only on `ResQ.BuildingBlocks.Application` + `ResQ.BuildingBlocks.Domain`.

## What `AddServiceDefaults` wires

- **OpenTelemetry** (`ConfigureOpenTelemetry`): logs (`IncludeScopes` + `IncludeFormattedMessage`), metrics (ASP.NET Core + HTTP-client + runtime instrumentation + the `ResqDiagnostics.MeterName` meter), and traces (ASP.NET Core + HTTP-client sources + the `ResqDiagnostics.ActivitySourceName` source). The **OTLP exporter** turns on only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- **Health checks** (`AddDefaultHealthChecks`): a `self` liveness check tagged `live`.
- **Service discovery** + **standard HTTP resilience**: applied to every `HttpClient` via `ConfigureHttpClientDefaults`.
- **`IClock`** → singleton `SystemClock` (backed by `TimeProvider.System`).

> It does **not** register the observability pipeline behaviors — that is an explicit, ordered call (below).

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();               // OTel / health / resilience / IClock — NO behaviors

builder.Services
    .AddResqApplication(typeof(CreateWidget).Assembly)
    .AddValidatorsFrom(typeof(CreateWidget).Assembly)
    .AddLoggingPipeline()                    // outermost
    .AddValidationPipeline();
builder.Services.AddResqObservabilityBehaviors();   // Tracing then Metrics — AFTER validation

var app = builder.Build();
app.MapDefaultEndpoints();                   // /health + /alive (non-Production)
app.Run();
```

Resulting behavior order: **Logging → Validation → Tracing → Metrics → Transaction → handler** (first-registered = outermost).

## Pieces

- **`ServiceDefaultsExtensions`** — `AddServiceDefaults`, `ConfigureOpenTelemetry`, `AddDefaultHealthChecks` (`IHostApplicationBuilder`), and `MapDefaultEndpoints` (`WebApplication`).
- **`ResqDiagnostics`** — the shared `ActivitySource` + the `ActivitySourceName`/`MeterName` constants. No hand-newed static `Meter`; metrics come from `IMeterFactory` so the emitting and allow-listed names always match.
- **`TracingBehavior<,>` / `MetricsBehavior<,>`** — CQRS pipeline behaviors. Tracing starts an activity named for the request and sets ok/error status (tags **no** business data); metrics record a duration histogram + failure counter, tagged by request name. `AddResqObservabilityBehaviors` registers both.
- **`AddResqOptions<T>(sectionName)`** — bind a config section, `ValidateDataAnnotations`, and `ValidateOnStart` (fail fast at boot).
- **`AddResqResiliencePipeline(key, configure?)`** — a Polly retry + circuit-breaker + timeout pipeline whose retry `ShouldHandle` treats a failed `Result` with `ErrorType.Failure` (or any exception) as retryable.

## Known coupling (accepted)

`FrameworkReference Microsoft.AspNetCore.App` and `AddAspNetCoreInstrumentation` are unconditional, so a pure worker host that calls `AddServiceDefaults` still pulls the ASP.NET shared framework. This mirrors Aspire's own `ServiceDefaults`; a `ServiceDefaults.Worker` split is possible future work.

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
