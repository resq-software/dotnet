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

# ResQ.BuildingBlocks.Adapters.Web

[![NuGet](https://img.shields.io/nuget/v/ResQ.BuildingBlocks.Adapters.Web?style=flat-square)](https://www.nuget.org/packages/ResQ.BuildingBlocks.Adapters.Web)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue?style=flat-square)](https://github.com/resq-software/dotnet/blob/main/LICENSE)

> The HTTP driving adapter (inbound ring) — a minimal-API surface that maps the domain `Result`/`Error` model to RFC 7807 `ProblemDetails`, with endpoint discovery, FluentValidation filters, correlation, versioning, OpenAPI/Scalar, and a config-bound CORS allowlist behind one ordered wiring.

## Installation

```bash
dotnet add package ResQ.BuildingBlocks.Adapters.Web
```

Depends on `ResQ.BuildingBlocks.Application` and `ResQ.BuildingBlocks.Domain`, and references the `Microsoft.AspNetCore.App` framework plus `Microsoft.AspNetCore.OpenApi`, `Asp.Versioning.Http`, `FluentValidation`, and `Scalar.AspNetCore`. Single-targets **net9.0** — the built-in `AddOpenApi`/`MapOpenApi` surface ships net9 assets only.

## Quick Start

```csharp
using ResQ.BuildingBlocks.Adapters.Web;

var builder = WebApplication.CreateBuilder(args);

// Registers problem details + exception handler, CORS, versioning, OpenAPI,
// correlation, pagination/correlation options, and scans the entry assembly for IEndpoint modules.
builder.Services.AddResqWeb(builder.Configuration, configure: o =>
{
    o.DocsBaseUri = "https://docs.example.com/errors";
    o.Pagination.MaxPageSize = 200;
});

var app = builder.Build();

// Wires middleware in a fixed order: exception handling → CORS → correlation →
// authentication → authorization → endpoints → OpenAPI.
app.UseResqWeb();
app.Run();

// An endpoint module — discovered by assembly scan and mapped at startup.
public sealed class WidgetEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var set = app.CreateResqVersionSet(1.0);
        var group = app.MapVersionedGroup("/api", set).MapGroup("/widgets");

        group.MapPost("", async (CreateWidgetRequest body, ISender sender) =>
                (await sender.Send(body.ToCommand()))
                    .ToHttpResult(id => TypedResults.Created($"/api/v1/widgets/{id}", id)))
            .WithValidation<CreateWidgetRequest>();

        group.MapGet("", async ([AsParameters] PageRequest page, ISender sender) =>
            (await sender.Send(new ListWidgets(page).ToQuery())).ToHttpResult());
    }
}
```

## API Reference

### Composition — `WebServiceCollectionExtensions`

The single, ordered composition root. Register services with `AddResqWeb`, then wire middleware with `UseResqWeb`.

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddResqWeb` | `(this IServiceCollection, IConfiguration, Action<ResqWebOptions>? configure = null, params Assembly[] endpointAssemblies) → IServiceCollection` | Registers problem details + `ProblemDetailsExceptionHandler`, CORS, versioning, OpenAPI, scoped correlation middleware, options, and endpoint discovery. Pass the API assembly (`typeof(Program).Assembly`) when hosting under `WebApplicationFactory`; otherwise the entry assembly is scanned. |
| `UseResqWeb` | `(this WebApplication) → WebApplication` | Wires middleware in the canonical order: exception handling → CORS → correlation → authentication → authorization → endpoints → OpenAPI. |

### Options — `ResqWebOptions`

Aggregate options applied by `AddResqWeb`.

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `EnableOpenApiUi` | `bool` | `true` | Whether the Scalar UI is mapped in Development. |
| `Pagination` | `PaginationOptions` | `new()` | Bounds bound into `PaginationOptions`. |
| `Correlation` | `CorrelationOptions` | `new()` | Settings bound into `CorrelationOptions`. |
| `StatusOverrides` | `IReadOnlyDictionary<ErrorType, int>?` | `null` | Per-`ErrorType` HTTP status overrides passed to `ProblemDetailsMapper`. |
| `DocsBaseUri` | `string?` | `null` | Documentation base used to synthesize the ProblemDetails `type` URI from an error code. |

### Result → HTTP — `ResultExtensions`

Translates the domain `Result`/`Result<T>` model into minimal-API `IResult` values via `TypedResults`, so the happy path never throws and failures flow to RFC 7807 problem responses.

| Member | Signature | Description |
|--------|-----------|-------------|
| `ToHttpResult` | `(this Result, int successStatus = 204) → IResult` | Status-code result on success, problem result on failure. |
| `ToHttpResult<T>` | `(this Result<T>, Func<T, IResult>? onSuccess = null) → IResult` | Projects the value (default `200 OK`) or a problem result. |
| `Match<T>` | `(this Result<T>, Func<T, IResult> onSuccess, Func<Error, IResult>? onFailure = null) → IResult` | Pattern-matches into success/failure branches. |
| `Problem` | `(this Error, HttpContext? context = null) → IResult` | Deferred problem result that resolves the ambient `ResqWebOptions` (overrides + docs URI) at execution time. |

### ProblemDetails mapping — `ProblemDetailsMapper`

Maps `Error`/`ErrorType` onto RFC 7807 `ProblemDetails`. The `type` URI is synthesized from `Error.Code` (`{docsBaseUri}/{code}`, or a `urn:resq:error:{code}` fallback); `Extensions["code"]` carries the stable machine code and `Extensions["traceId"]` the current activity/request id.

| Member | Signature | Description |
|--------|-----------|-------------|
| `ToStatusCode` | `(ErrorType type, IReadOnlyDictionary<ErrorType, int>? overrides = null) → int` | Defaults: Validation→400, NotFound→404, Conflict→409, Unauthorized→401, Forbidden→403, else→500. An override wins per type. |
| `ToProblemDetails` | `(Error error, HttpContext? context = null, IReadOnlyDictionary<ErrorType, int>? overrides = null, string? docsBaseUri = null) → ProblemDetails` | Builds the populated problem details. |

### Error handling

| Type | Member | Description |
|------|--------|-------------|
| `ProblemDetailsExceptionHandler` | `TryHandleAsync(HttpContext, Exception, CancellationToken) → ValueTask<bool>` | `IExceptionHandler` safety net behind the `Result` path; renders unhandled exceptions as problem responses. |
| `ProblemDetailsConfigurator` | `Customize : Action<ProblemDetailsContext>` | Shared `CustomizeProblemDetails` callback — stamps `traceId`/`requestId` into `Extensions` and sets `Instance` to the request path. |

### Validation

FluentValidation at the endpoint boundary; a failure short-circuits with a `ValidationProblem` (400).

| Type / Member | Signature | Description |
|---------------|-----------|-------------|
| `ValidationFilterExtensions.WithValidation<TRequest>` | `(this RouteHandlerBuilder) → RouteHandlerBuilder` / `(this RouteGroupBuilder) → RouteGroupBuilder` | Attaches the filter to a handler or a whole group. |
| `ValidationEndpointFilter<TRequest>` | `IEndpointFilter` | Runs every registered `IValidator<TRequest>` against the first `TRequest` argument. |

### Correlation

| Type | Member | Description |
|------|--------|-------------|
| `CorrelationMiddleware` | `IMiddleware` | W3C-aware: reads the inbound correlation header (falling back to the current `Activity` id / trace identifier), records it as activity baggage, and echoes it on the response. Registered scoped by `AddResqWeb`. |
| `CorrelationOptions` | `HeaderName : string` | The header carrying the correlation id (default `X-Correlation-ID`). |

### Endpoints

Assembly-scanned endpoint modules — implement `IEndpoint` and register + map with the extensions.

| Type / Member | Signature | Description |
|---------------|-----------|-------------|
| `IEndpoint.MapEndpoint` | `(IEndpointRouteBuilder app) → void` | Maps this module's routes. |
| `EndpointExtensions.AddResqEndpoints` | `(this IServiceCollection, params Assembly[]) → IServiceCollection` | Registers every concrete `IEndpoint` in the assemblies as transient. |
| `EndpointExtensions.MapResqEndpoints` | `(this IEndpointRouteBuilder) → IEndpointRouteBuilder` | Resolves and invokes `MapEndpoint` on each registered endpoint. |

### Versioning — `ApiVersioningExtensions`

URL-segment API versioning (Asp.Versioning), defaulting to v1.0.

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddResqApiVersioning` | `(this IServiceCollection, Action<ApiVersioningOptions>? configure = null) → IServiceCollection` | Registers versioning: default v1.0, assume-default-when-unspecified, report versions, read from URL segment. |
| `CreateResqVersionSet` | `(this IEndpointRouteBuilder, params double[] versions) → ApiVersionSet` | Builds a version set for the given versions. |
| `MapVersionedGroup` | `(this IEndpointRouteBuilder, string prefix, ApiVersionSet set) → RouteGroupBuilder` | Maps a group under `{prefix}/v{version:apiVersion}` bound to the set. |

### OpenAPI — `OpenApiExtensions`

Built-in net9 `AddOpenApi`/`MapOpenApi`, plus an optional Scalar UI in Development only (neither endpoint is exposed in Production).

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddResqOpenApi` | `(this IServiceCollection, string documentName = "v1") → IServiceCollection` | Adds a document with an operation transformer that advertises the shared `application/problem+json` error response. |
| `MapResqOpenApi` | `(this WebApplication, bool enableUi = true) → WebApplication` | Maps the OpenAPI JSON (Development) and, when enabled, the Scalar UI. |

### CORS — `CorsExtensions` / `ResqCorsOptions`

Binds an explicit allowlist from the `Cors` configuration section. A wildcard origin (`*`) combined with `AllowCredentials` throws `InvalidOperationException` rather than being silently allowed.

| Type / Member | Type | Default | Description |
|---------------|------|---------|-------------|
| `CorsExtensions.AddResqCors` | `(this IServiceCollection, IConfiguration) → IServiceCollection` | — | Binds `ResqCorsOptions` and registers the named policy. |
| `ResqCorsOptions.PolicyName` | `string` | `"resq"` | Registered/applied policy name. |
| `ResqCorsOptions.AllowedOrigins` | `string[]` | `[]` | Explicit origin allowlist. |
| `ResqCorsOptions.AllowCredentials` | `bool` | `false` | Whether credentials are allowed (incompatible with `*`). |

### Pagination

`[AsParameters]`-friendly query records plus bounds.

| Type / Member | Signature | Description |
|---------------|-----------|-------------|
| `PageRequest` | record with `Page : int` (default `1`), `PageSize : int` (default `20`) | Offset request bound from `?page=&pageSize=`. |
| `PageRequest.Normalize` | `(PaginationOptions) → PageRequest` | Clamps `Page ≥ 1` and `PageSize` to `[1, MaxPageSize]` (falling back to `DefaultPageSize`). |
| `PaginationOptions` | `DefaultPageSize : int` (`20`), `MaxPageSize : int` (`100`) | Normalization bounds. |
| `CursorRequest` | record with `Cursor : string?`, `PageSize : int` (default `20`) | Keyset request bound from `?cursor=&pageSize=` (experimental). |
| `CursorCodec.Encode` / `Decode` | `(string) → string` / `(string?) → string?` | Opaque base64url (no padding) cursor codec; `Decode` returns `null` for absent/malformed input. **Experimental** — no first-cut consumer. |

## Prerequisites

- **Target framework**: `net9.0`
- **ASP.NET Core**: references `Microsoft.AspNetCore.App` (net9) — the built-in OpenAPI surface is net9-only

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
