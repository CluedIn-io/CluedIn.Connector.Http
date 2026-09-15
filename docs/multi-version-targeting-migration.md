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

## CI-only failure: platform-dependent test (found on real CI, two attempts to fully root-cause)

All three `Multi-version build+test` legs failed with 3/6 unit test failures on the first real CI
run (build 151996) — but this repo's `dotnet test` had passed clean locally beforehand.
`HttpConnectorTests.cs` (`VerifyStoreData`, `VerifyStoreEventData`, `VerifyStoreDataWithEdges`)
asserts the exact raw HTTP request text a `TcpListener` receives against a verbatim interpolated
string literal (`$@"POST / HTTP/1.1 ..."`) written directly in the source file.

**First fix attempt (incomplete):** assumed the only issue was the literal's own line-ending style —
`\r\n` on this Windows dev machine (git `core.autocrlf`), LF-only on the Linux CI agent (switched
from `windows-latest` as part of this migration, exposing the mismatch for the first time). Fixed by
normalizing both sides with `.Replace("\r\n", "\n")`. Reproduced by converting the file to LF-only
locally and confirming `dotnet test` failed the same way, then confirming the fix made it pass — but
that repro only exercised the *literal's* line endings; it could not exercise the second, deeper
cause below, since that one depends on the OS the test actually *runs* on, not the file's checked-out
line endings, and this machine is Windows either way. Pushed anyway, believing it fixed; **CI failed
again identically (build 152005)**, same 3 tests, same 3/3 pass/fail split every leg.

**Real root cause, found from the full CI log (not just the stack-trace tail):** the failure wasn't
just the outer HTTP line breaks — the `Content-Length` header value itself differed (e.g. expected
`377`, actual `366`, an 11-byte gap). `HttpPostClient.cs` serializes the body via
`JsonConvert.SerializeObject(data, Formatting.Indented, ...)`. Newtonsoft.Json's `Formatting.Indented`
writes its internal line breaks through `TextWriter.NewLine`, i.e. `Environment.NewLine` — so on
Linux the JSON body itself is genuinely fewer *bytes* (LF) than on Windows (CRLF), and
`Content-Length` correctly reflects that real difference. Normalizing line endings in the captured
text doesn't fix this: the `Content-Length` value is already-computed digits, not newline characters,
so a hardcoded `377` in the test literal will never match a genuinely-different real byte count on
another platform.

**Fix:** added a `NormalizeForComparison` helper that normalizes `\r\n`→`\n` *and* strips the numeric
`Content-Length` value (regex `Content-Length: \d+` → `Content-Length: X`) before comparing, so the
assertion verifies the header's presence/well-formedness and exact JSON body content, without being
tied to a platform-specific byte count. Verified the fix's actual logic directly (not just by
re-reasoning): a throwaway script fed the helper a synthetic Windows-CRLF string with
`Content-Length: 377` and a synthetic Linux-LF string with `Content-Length: 366` for the same
logical content, and confirmed both normalize to the same value. Re-ran `dotnet test` on Windows for
both net6.0 and net10.0 — still 6/6 passing after the change.

---

## Checklist

- [x] `azure-pipelines.yml` — switched to `crawler.build.jobs.yml` with `multiVersionCluedInTargets` (4.7.0, 4.8.0, 5.0.0-beta.*); pool switched to `ubuntu-22.04`; `useGitVersionDotNetTool: true` added
- [x] `Directory.Build.props` — honours `CluedInMultiVersionTargetFramework`; `DefineConstants` derived; `LangVersion` pinned to 13.0
- [x] `Packages.props` — `_CluedIn` guarded; EF Core/xunit/Test SDK split by TFM
- [x] `NuGet.config` — renamed from `Nuget.config`
- [x] Test csproj — conditional xunit v2/v3 + AutoFixture selection; real `dotnet test` passes on both TFMs
- [x] Source — audited; 0 errors on all three legs, no `#if` guards needed
- [x] Fixed a platform-dependent test (`HttpConnectorTests.cs`, 3 assertions) found only by real CI on the Linux agent — took two attempts to fully root-cause, see above
- [x] `GitVersion.yml` — `next-version: 1.0`; `ignore.commits-before: 2025-05-24T00:00:00`; verified `MajorMinorPatch: "1.0.0"` with the pinned GitVersion.Tool 5.9.0
- [x] Pushed branch and confirmed the Azure DevOps pipeline is green end-to-end — PR #41, build 152006: all three legs (4.7.0/4.8.0/5.0.0-beta.*) + `Multi-version: publish` passed

---

## Addendum — version baseline moved from 1.0.0 to 100.0.0

Status: **Done**

The CluedIn version is now carried entirely by the package suffix (`.470`/`.480`/`.500`), not by
this repo's own `next-version` number, so that number moved again, from `1.0` to `100.0`. Reason:
repos that were previously at 4.x/5.x under the old single-version-targeting scheme would appear to
"go backwards" if their next version showed as `1.0.0` — `100.0.0` is unambiguously higher than any
prior single-version release number this repo ever had.

Unlike the original `1.0` reset, no `commits-before`/`ignore` trick is needed this time:
`next-version` only needs help overriding an existing tag when the configured value is *lower* than
that tag, and `100.0` is already higher than every pre-existing tag here. Removed the
`ignore.commits-before` line entirely (this repo's `ignore:` block had no `sha`, so the whole block
was removed).

Verified with a real local `dotnet-gitversion` run: `MajorMinorPatch` resolves to `"100.0.0"`.
`docs/1.0.0-release-notes.md` renamed to `docs/100.0.0-release-notes.md`.
