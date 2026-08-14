# OrgNamePlaceholder Service

A production-shaped **Clean / Hexagonal (Ports & Adapters)** .NET service, scaffolded from the ResQ
`resq-service` template and wired to the `ResQ.BuildingBlocks.*` NuGet packages. The four rings and the
three test projects are here from minute one.

## The hexagon

```
        Driving adapters                Core                 Driven adapters
   (Web / Events)  ─►  Application ─► Domain  ◄─  (Persistence / Messaging)
                          (ports defined here)      (ports implemented here)
```

| Project | Ring | Contents |
|---|---|---|
| `src/ResQ.Service.Domain` | Domain (inner) | `Sample` aggregate, `SampleId`, domain events — dependency-free |
| `src/ResQ.Service.Application` | Application | CQRS commands/queries/handlers, ports, specifications |
| `src/ResQ.Service.Infrastructure` | Driven | EF Core `DbContext`, repository, DI composition root |
| `src/ResQ.Service.Api` | Driving | Minimal-API endpoints, `Result`→HTTP, versioning, OpenAPI |

Plus `tests/ResQ.Service.UnitTests`, `tests/ResQ.Service.ArchitectureTests` (NetArchTest enforces the
hexagon dependency rule), and `tests/ResQ.Service.IntegrationTests` (Testcontainers, Docker-gated).

Timestamps are stamped inside the application handlers from `IClock.UtcNow` (no `IAuditable` coupling),
so the architecture tests hold and the Domain takes no persistence dependency.

## Run

```bash
dotnet run --project src/ResQ.Service.Api
```

- `GET /health` and `GET /alive` health probes
- `POST /api/v1/samples` — validates via the endpoint filter, returns `201` or a ProblemDetails
- `GET /api/v1/samples/{id}` and `GET /api/v1/samples` (paged)
- OpenAPI + Scalar UI in Development; OpenTelemetry wired via `AddServiceDefaults`

## Test

```bash
# Fast tests (unit + architecture); integration tests are filtered out (no Docker needed):
dotnet test --filter Category!=Integration

# Integration tests (requires Docker for Testcontainers):
dotnet test --filter Category=Integration
```

## Template options

Regenerate with different options via `dotnet new resq-service`:

| Option | Default | Effect |
|---|---|---|
| `--OrgName` | `ResQ` | Organization name in metadata/README |
| `--DatabaseProvider` | `postgres` | `postgres` (Npgsql + snake_case) or `sqlite` |
| `--IncludeMessaging` | `true` | Include the integration-event messaging slice |
| `--IncludeOutbox` | `false` | Enable the transactional outbox + relay |

---
© OrgNamePlaceholder
