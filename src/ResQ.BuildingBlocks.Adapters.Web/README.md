# ResQ.BuildingBlocks.Adapters.Web

The **HTTP driving adapter** (inbound ring) for the ResQ building blocks — a minimal-API surface that
maps the domain `Result`/`Error` model to RFC 7807 `ProblemDetails`. Single-targets **net9.0** (the
built-in `AddOpenApi`/`MapOpenApi` surface is net9-only). Depends only on
`ResQ.BuildingBlocks.Application` + `ResQ.BuildingBlocks.Domain`.

- **Result → HTTP:** `ProblemDetailsMapper` (`ErrorType` → status, with an `overrides` hook and a
  `type` URI synthesized from `Error.Code` + `docsBaseUri`) and `ResultExtensions`
  (`ToHttpResult`/`Match`/`Problem`) built on `TypedResults`, so the happy path never throws.
- **Validation:** `ValidationEndpointFilter<TRequest>` + `WithValidation<TRequest>()` run
  FluentValidation at the endpoint and return a `ValidationProblem` on failure.
- **Error handling:** `ProblemDetailsExceptionHandler` (safety net behind the `Result` path) +
  `ProblemDetailsConfigurator.Customize` (trace/request id + `Instance`).
- **Correlation:** `CorrelationMiddleware` (`IMiddleware`, W3C-aware) + `CorrelationOptions`
  (registered scoped by `AddResqWeb`).
- **Pagination:** `PageRequest`/`CursorRequest` (`[AsParameters]`-friendly), `PaginationOptions`, and
  the experimental `CursorCodec` (base64url).
- **Endpoints:** `IEndpoint` + `AddResqEndpoints`/`MapResqEndpoints` for assembly-scanned endpoint
  modules.
- **Versioning:** `AddResqApiVersioning` + `CreateResqVersionSet`/`MapVersionedGroup` (Asp.Versioning,
  URL-segment).
- **OpenAPI:** `AddResqOpenApi`/`MapResqOpenApi` (`AddOpenApi` + optional Scalar UI in Development).
- **CORS:** `AddResqCors` binds an explicit allowlist and **rejects `*` with credentials**.
- **Composition:** `AddResqWeb` + `UseResqWeb` register everything and wire the middleware in one fixed
  order: exception handling → CORS → correlation → authentication → authorization → endpoints →
  OpenAPI.

```csharp
builder.Services.AddResqWeb(builder.Configuration);
var app = builder.Build();
app.UseResqWeb();

// An endpoint module:
public sealed class WidgetEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var set = app.CreateResqVersionSet(1.0);
        var group = app.MapVersionedGroup("/api", set).MapGroup("/widgets");
        group.MapPost("", async (CreateWidgetRequest body, ISender sender) =>
            (await sender.Send(body.ToCommand())).ToHttpResult(id => TypedResults.Created($"/api/v1/widgets/{id}", id)))
            .WithValidation<CreateWidgetRequest>();
    }
}
```

Apache-2.0 · part of [`resq-software/dotnet`](https://github.com/resq-software/dotnet).
