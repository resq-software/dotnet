# Contributing to ResQ Building Blocks

Thanks for helping build the frame. This page covers local setup, the reproducibility
guarantees, and the optional git hooks.

## Prerequisites

- **.NET SDK** matching `global.json` (`9.0.100`, `rollForward: latestMajor` — a newer major
  such as 10.x also satisfies it).
- Optional: **VS Code + Dev Containers** (or GitHub Codespaces). `.devcontainer/devcontainer.json`
  pins the SDK image by digest and runs `dotnet tool restore` + a solution restore on create, so a
  fresh container is build-ready with no extra steps.

## First-time setup

```bash
dotnet tool restore                       # local tools pinned in .config/dotnet-tools.json
dotnet restore ResQ.BuildingBlocks.slnx   # writes/refreshes packages.lock.json per project
```

## Everyday commands

```bash
dotnet build ResQ.BuildingBlocks.slnx -c Release
dotnet test  ResQ.BuildingBlocks.slnx -c Release
dotnet pack  ResQ.BuildingBlocks.slnx -c Release -o artifacts
```

> Running only a newer runtime than a test project targets (e.g. .NET 10 runtime, net9.0 tests)?
> Prefix test runs with `DOTNET_ROLL_FORWARD=LatestMajor` so the host rolls forward:
> `DOTNET_ROLL_FORWARD=LatestMajor dotnet test ...`.

## Reproducibility

- **Pinned dependency graph.** `RestorePackagesWithLockFile` is on, so every project keeps a
  committed `packages.lock.json` capturing the full transitive graph (including analyzers).
- **Locked restore in CI.** When `CI=true`, restore runs in locked mode and **fails** if the lock
  files no longer match the resolved graph — dependency drift is caught, never silently relocked.
- **Updating dependencies.** Change a version in `Directory.Packages.props`, then run
  `dotnet restore ResQ.BuildingBlocks.slnx` locally and commit the updated `packages.lock.json`
  files alongside the change.
- **Single source.** `NuGet.config` clears inherited feeds and maps every package to nuget.org, so
  restores are reproducible and immune to source-hijack / dependency-confusion.

## Git hooks (opt-in)

Hooks live in `.githooks/` and are **not** enabled automatically. Turn them on per clone:

```bash
git config core.hooksPath .githooks
```

| Hook | What it enforces |
|------|------------------|
| `pre-push` | Blocks a force-push (non-fast-forward) to `main`; verifies `dotnet format --verify-no-changes`; runs a clean `dotnet build` when any `*.cs` changed. |
| `commit-msg` | Requires [Conventional Commits](https://www.conventionalcommits.org/) subjects. |

**Escape hatch.** Bypass the hooks for a single command when you need to (e.g. an intentional
maintainer force-push):

```bash
GIT_HOOKS_SKIP=1 git push ...
GIT_HOOKS_SKIP=1 git commit ...
```

## Commit messages

Conventional Commits: `<type>[optional scope][!]: <description>`

Allowed types: `build chore ci docs feat fix perf refactor revert style test`.
Example: `feat(application): add ValidationBehavior`.

---
Apache-2.0 · © ResQ Systems, Inc.
