# Migrating a Connector to Multi-Version Targeting

This document tracks the migration of `CluedIn.Connector.Http` from a single-version build to the
multi-version targeting pattern, part of a larger effort migrating connector/enricher repos. See
`CluedIn.Connector.Dataverse.V2`, `CluedIn.Enricher.GoogleMaps`, and the other already-migrated
repos' own `docs/multi-version-targeting-migration.md` for the full prior art this follows.

Branch: `feature/multi-version-targeting` (off `develop`).

---

## Overview

Target matrix: `4.7.0`/net6.0, `4.8.0`/net6.0, `5.0.0-beta.*`/net10.0 (auto-detected by the shared
`crawler.build.jobs.yml` template, not hand-specified).

**4.6.0 excluded.** `HttpConnectorComponent.cs` already calls the async, `executionContext`-taking
stream APIs (`IStreamRepository.GetAllStreams(executionContext)`,
`SetupConnector(executionContext, ...)`, `GetStreamMappings(executionContext, id)`) that only exist
from CluedIn 4.7.0 onward — same reasoning as Dataverse.V2.

Note: `src/Connector.SqlServer/` is the actual project folder despite the repo/package being
`CluedIn.Connector.Http` — looks like this repo was originally forked from
`CluedIn.Connector.SqlServer` and the folder was never renamed. Left as-is; out of scope for this
migration.

---

## Changes

- **`azure-pipelines.yml`** — switched from the single-version `crawler.build.yml` to
  `crawler.build.jobs.yml`, added `multiVersionCluedInTargets` and `probeCluedInVersion`, switched
  pool from `windows-latest` to `ubuntu-22.04` (required by the shared template), added
  `useGitVersionDotNetTool: true` (a repo in this effort — `CluedIn.Enricher.Permid` — failed all
  three legs on its first CI run from omitting this: without it, the legacy marketplace
  `GitVersionTask@5` runs instead, which fails outright on the retired Node6 runtime).
- **`Directory.Build.props`** — honours `CluedInMultiVersionTargetFramework` (net10.0 local
  fallback), derives `CLUEDIN_V47`/`V48`/`V50` `DefineConstants`, pins `LangVersion` to `13.0`.
- **`Packages.props`** — guarded `_CluedIn`; split `Microsoft.EntityFrameworkCore`(`.InMemory`),
  `Microsoft.NET.Test.Sdk`, and xunit/AutoFixture generation by `$(_CluedIn)` version (net6.0 legs
  get EF Core 6.0.16/xunit v2/Test SDK 17.12.0; net10.0 gets EF Core 10.0.7/xunit v3/Test SDK
  18.3.0) — same EF-Core-by-TFM pattern `CluedIn.Connector.AzureDataLake` already uses.
- **`NuGet.config`** — renamed from `Nuget.config` (two-step `git mv`, Linux case-sensitivity).
- **`test/unit/.../Connector.Http.Unit.Tests.csproj`** — conditional `ItemGroup`s for xunit v2/v3 +
  AutoFixture.Xunit2/Xunit3 selection, gated on `CLUEDIN_V50`. No `GlobalUsings.cs` needed — the one
  test file uses a bare `using Xunit;` with no AutoFixture attributes or `ITestOutputHelper`, so no
  namespace divergence to guard.
- **`GitVersion.yml`** — no pre-existing `ignore:` block here (unlike several other repos in this
  effort), so added fresh: `next-version: 1.0`, `ignore.commits-before: 2025-05-24T00:00:00` (3 days
  past the highest tag, `4.5.0`/`v4.5.0` at `2025-05-21T15:37:26+01:00` — checked by real commit
  date via `git log -1 --format=%aI <tag>`, not tag-name sort order). Verified with the pipeline's
  actual pinned `GitVersion.Tool 5.9.0`: resolves to `MajorMinorPatch: "1.0.0"`.

## Verification

Built and tested for real (not just reasoned about) against all three legs:
- `4.7.0`/net6.0 — src builds 0 errors; `dotnet test` 6/6 pass
- `4.8.0`/net6.0 — src builds 0 errors
- `5.0.0-beta.*`/net10.0 (local-dev default) — src builds 0 errors; `dotnet test` 6/6 pass

No `#if CLUEDIN_Vxx` guards were needed in source — this repo has no RestSharp dependency (uses a
custom `IHttpClient`/`HttpPostClient` wrapper over `System.Net.Http.HttpClient`, which has no
version-sensitive break across the targeted CluedIn generations) and no transitive dependency
version break was found.

---

## Checklist

- [x] `azure-pipelines.yml` — switched to `crawler.build.jobs.yml` with `multiVersionCluedInTargets` (4.7.0, 4.8.0, 5.0.0-beta.*); pool switched to `ubuntu-22.04`; `useGitVersionDotNetTool: true` added
- [x] `Directory.Build.props` — honours `CluedInMultiVersionTargetFramework`; `DefineConstants` derived; `LangVersion` pinned to 13.0
- [x] `Packages.props` — `_CluedIn` guarded; EF Core/xunit/Test SDK split by TFM
- [x] `NuGet.config` — renamed from `Nuget.config`
- [x] Test csproj — conditional xunit v2/v3 + AutoFixture selection; real `dotnet test` passes on both TFMs
- [x] Source — audited; 0 errors on all three legs, no `#if` guards needed
- [x] `GitVersion.yml` — `next-version: 1.0`; `ignore.commits-before: 2025-05-24T00:00:00`; verified `MajorMinorPatch: "1.0.0"` with the pinned GitVersion.Tool 5.9.0
- [ ] Push branch and confirm the actual Azure DevOps pipeline run is green end-to-end
