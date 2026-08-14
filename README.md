# ResQ Building Blocks

Reusable .NET building blocks for **Clean / Hexagonal (Ports & Adapters)** architecture — the *frame*, published to NuGet, so every ResQ .NET service is built the same way. This repo holds the **paradigm and primitives, not the domain**: base classes, port interfaces, and adapters carry zero business logic. The actual ResQ domain, use-cases, and moat stay private.

> Sibling to `dotnet-sdk` (the proto-generated interop client). This repo is *how we build services*; `dotnet-sdk` is *what we expose over the wire*.

## The hexagon → the packages

```
        Driving adapters                Core                 Driven adapters
   (Web / CLI / Events)  ─►  Application ─► Domain  ◄─  (Persistence / Messaging / External)
                               (ports defined here)      (ports implemented here)
```

| Package | Ring | Status | Contents |
|---|---|---|---|
| **`ResQ.BuildingBlocks.Domain`** | Domain (inner) | ✅ | `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `Result`/`Error`, `Guard` — dependency-free |
| **`ResQ.BuildingBlocks.Application`** | Application | ✅ | CQRS contracts, driven **ports** (`IUnitOfWork`/`IClock`/`IDomainEventDispatcher`), pipeline behaviors |
| `ResQ.BuildingBlocks.Adapters.Web` | Driving | 🚧 | REPR endpoints, `Result`→HTTP mapping, ProblemDetails, versioning, OpenAPI |
| `ResQ.BuildingBlocks.Adapters.Persistence` | Driven | 🚧 | EF Core repository base, Specification eval, `UnitOfWork`, Outbox, Idempotency |
| `ResQ.BuildingBlocks.Adapters.Messaging` | Driven | 🚧 | broker abstractions, consumer base |
| `ResQ.BuildingBlocks.ServiceDefaults` | Cross-cutting | 🚧 | OpenTelemetry, health checks, resilience, config validation |
| `ResQ.BuildingBlocks.Testing` | — | 🚧 | fixtures/harness for the paradigm |

Plus `templates/resq-service` (a `dotnet new` template) and `samples/Widgets` (a throwaway, non-moat reference service showing the whole hexagon). 🚧 = scaffolded next.

## Repo mechanics (inspired by `dotnet/extensions`)

- **Central Package Management** — every version pinned once in `Directory.Packages.props`.
- **`Directory.Build.props`** (root: language/analysis/identity) + **`src/Directory.Build.props`** (packaging: multi-target `net8.0;net9.0`, SourceLink, `.snupkg` symbols, docs).
- **Git-tag versioning** via **MinVer** — tag `v1.2.3` → package `1.2.3`.
- **CI** (`.github/workflows/ci.yml`): restore → build → test → pack on every push; `dotnet nuget push` on a `v*` tag.

## Consume

```bash
dotnet add package ResQ.BuildingBlocks.Domain
dotnet add package ResQ.BuildingBlocks.Application
```

## Build locally

```bash
dotnet build -c Release
dotnet test  -c Release
dotnet pack  -c Release -o artifacts
```

## Publish

Push a tag: `git tag v0.1.0 && git push --tags`. CI packs and pushes to NuGet (needs the `NUGET_API_KEY` secret + a `nuget` environment).

---
Apache-2.0 · © ResQ Systems, Inc.
