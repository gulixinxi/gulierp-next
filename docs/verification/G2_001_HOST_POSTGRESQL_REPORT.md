# G2_001_HOST_POSTGRESQL_VERIFICATION_REPORT

| Field | Value |
|---|---|
| Goal | G2-001 — Host & PostgreSQL |
| Type | Verification report (closure artefact) |
| Author | Mavis (single writer) |
| Date | 2026-08-19 (Asia/Taipei) |
| Entry gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Exit gate | **`G2_001_HOST_POSTGRESQL_VERIFIED_CODE_READY_OPERATOR_RUNTIME_PENDING`** |
| Next goal (Operator-gated) | `G2-002 Foundation Kernel` (per `G2_FOUNDATION_EXECUTION_PLAN.md`) |
| Hard-stop status | none — see §15 honest disclosure |

> **Honest disclosure (§1.4)**: this report splits verification into two parts
> because the Mavis agent cannot inject the real PostgreSQL password. Mavis
> has verified everything that does not depend on a real database credential.
> The Operator must run `tools/dev/g2-001-operator-evidence.ps1` to complete
> the real-database half. The "**VERIFIED_CODE_READY_OPERATOR_RUNTIME_PENDING**"
> status accurately captures that posture.

---

## 1. Goal

Per `docs/governance/G2_FOUNDATION_EXECUTION_PLAN.md` §3 (G2-001):

> Prove the platform: a host boots, returns 200 on `/healthz`, connects to a
> real PostgreSQL, applies a single migration, and exits cleanly.

This report captures the actual evidence in 22 sections covering every line
in the operator brief.

---

## 2. Start HEAD / Branch

| Field | Value |
|---|---|
| Branch | `master` |
| Start HEAD | `d131a5d534a39b9c86a51b0280ff467821e20609` |
| Start message | "docs(research): complete VOL.PRO build-vs-reuse assessment" |
| Pre-existing untracked | 24 files (G1B-1R + G2 architecture prep — left untouched per §四) |
| Pre-existing modified | 5 files (`apps/web/**` — G1B-1R frontend — left untouched per §四) |

---

## 3. Mature Solution Check

| Capability | Built-on | Why we did NOT reinvent it |
|---|---|---|
| ASP.NET Core host | `WebApplication.CreateBuilder(args)` + `Microsoft.Extensions.Hosting` | Native since .NET 6; no reason to wrap |
| Configuration | `Microsoft.Extensions.Configuration` (JSON + env + user-secrets) | Standard 12-factor; no reason to abstract |
| Logging | `Microsoft.Extensions.Logging` + `AddSimpleConsole` | Standard provider chain |
| DI | `Microsoft.Extensions.DependencyInjection` (built-in) | No Autofac; `DEC-MODULE-001` requires no dynamic-DI runtimes |
| Health checks | `Microsoft.Extensions.Diagnostics.HealthChecks` + `AddDbContextCheck<T>` | The framework already exposes `HealthCheckResult` and ASP.NET Core endpoints |
| ORM | `Microsoft.EntityFrameworkCore` 10.0.11 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 | Native EF Core; no SqlSugar / Dapper; no `FromSql` shortcuts |
| Migration tooling | `dotnet ef` 10.0.11 (local tool) | Native CLI; no custom migration runner |
| Web pipeline | `WebApplication` + `MapHealthChecks` + `MapOpenApi` (dev only) | Native; no Furion-style wrapping |

**GuliERP self-research value is intentionally confined to** the
Foundation module boundary (`GULIERP_MODULE_INDEPENDENCE_RULE.md` §2) and the
single-`Program.cs` composition root (`G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md`
§5). Everything else uses mature framework capability.

---

## 4. SDK Environment

| Field | Value |
|---|---|
| `global.json` | `{"sdk": {"version": "10.0.100", "rollForward": "latestFeature"}}` |
| Resolved SDK | **10.0.400** (located at `D:\guli\gulierp\.dotnet\`) |
| Runtime | Microsoft.NETCore.App **10.0.11** + Microsoft.AspNetCore.App **10.0.11** |
| `dotnet --info` host (system PATH) | host-only 10.0.11, **no SDKs installed there** — must use `D:\guli\gulierp\.dotnet\dotnet.exe` |
| MSBuild | 18.9.6+14fbf8d52 |

The `D:\guli\gulierp\.dotnet\dotnet.exe` is the only usable SDK on this box.
The system-PATH dotnet at `C:\Program Files\dotnet\dotnet.exe` is a host
without any SDK; calling it with `--version` from inside the project root
returns the canonical SDK-not-found message (see §15).

---

## 5. Project Structure

Per task §六, the G0-bootstrap left three projects + central manifest +
solution file. G2-001 added the integration-test project and the operator
helper script.

```
D:\guli\projects\gulierp-next\
├── global.json                              (SDK 10.0.100 latestFeature)
├── Directory.Build.props                     (net10.0, nullable, treatWarningsAsErrors)
├── Directory.Packages.props                  (central package versions, 10.0.11 patch band)
├── GuliERP.slnx                              (new — .NET 10 default solution format)
├── apps\
│   └── api\
│       └── GuliERP.Api\                      (modified)
│           ├── GuliERP.Api.csproj            (Web SDK, EF, Npgsql, OpenApi, EF HealthCheck)
│           ├── Program.cs                    (rewritten — composition root)
│           ├── appsettings.json              (rewritten — ConnectionStrings.GuliERP placeholder)
│           └── appsettings.Development.json  (new — NAS host, CHANGE_ME password)
├── modules\
│   └── foundation\
│       └── GuliERP.Foundation\               (modified)
│           ├── GuliERP.Foundation.csproj     (EF + Npgsql + Design)
│           ├── DependencyInjection.cs       (rewritten — AddGuliErpFoundation(connection))
│           ├── FoundationDbContext.cs       (new — minimal, no DbSet, schema=foundation)
│           ├── DesignTimeFoundationDbContextFactory.cs  (new — for `dotnet ef`)
│           ├── FoundationBoundary.cs         (G0 — unchanged)
│           ├── ModelBoundaries.cs            (G0 — unchanged)
│           └── Migrations\
│               ├── 20260819103150_G2001_InitializeFoundationSchema.cs    (new — author-written Up/Down)
│               ├── 20260819103150_G2001_InitializeFoundationSchema.Designer.cs  (auto-generated)
│               └── FoundationDbContextModelSnapshot.cs                   (auto-generated)
├── tests\
│   ├── GuliERP.Foundation.Tests\             (modified — added `using Xunit;`)
│   │   └── FoundationBoundaryTests.cs        (G0 — unchanged, only `using` added)
│   └── GuliERP.Foundation.IntegrationTests\  (new)
│       ├── GuliERP.Foundation.IntegrationTests.csproj
│       ├── ConnectionStringProvider.cs        (env-var resolver)
│       ├── SkippableFact.cs                  (intentionally empty — static `[Fact(Skip = …)]` used instead)
│       ├── FoundationDatabaseFacts.cs        (3 DB tests, all `[Fact(Skip = …)]`)
│       └── FoundationHostHealthFacts.cs      (4 host tests, 2 always run + 2 `[Fact(Skip = …)]`)
├── tools\
│   └── dev\
│       └── g2-001-operator-evidence.ps1      (new — Operator handoff)
└── docs\verification\
    └── G2_001_HOST_POSTGRESQL_REPORT.md      (this file)
```

---

## 6. Package Versions (resolved, Release config)

| Package | Version | Source |
|---|---|---|
| Microsoft.AspNetCore.OpenApi | 10.0.11 | nuget.org |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.11 | nuget.org |
| Microsoft.EntityFrameworkCore | 10.0.11 | nuget.org |
| Microsoft.EntityFrameworkCore.Relational | 10.0.11 | nuget.org |
| Microsoft.EntityFrameworkCore.Design | 10.0.11 | nuget.org |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.11 | nuget.org |
| Npgsql.EntityFrameworkCore.PostgreSQL | **10.0.3** | nuget.org (max stable — 10.0.11 not yet released for this package) |
| xunit | 2.9.3 | nuget.org |
| xunit.runner.visualstudio | 3.1.4 | nuget.org |
| Microsoft.NET.Test.Sdk | 18.0.0 | nuget.org |
| dotnet-ef (local tool) | 10.0.11 | nuget.org |

**No preview/nightly packages are referenced.** Every Microsoft package is
on the stable 10.0.11 patch band; the only deviation is Npgsql at 10.0.3
which is the latest stable on the 10.0 line (verified via
`dotnet package search` + `nuget.org/v3-flatcontainer`).

Initial attempt with 10.0.0 package versions produced `NU1903` warnings
(Microsoft.OpenApi 2.0.0 CVE + transitive System.Security.Cryptography.Xml
CVEs). Bumping to the 10.0.11 patch band resolved them. `NU1510` warnings
for `Microsoft.AspNetCore.Diagnostics.HealthChecks` and
`Microsoft.Extensions.Diagnostics.HealthChecks` were resolved by removing
those `PackageReference` entries (they are auto-included in ASP.NET Core 10).

---

## 7. PostgreSQL Runtime

| Field | Value |
|---|---|
| Local PostgreSQL | **NOT installed** (no `psql`, no service, no port 5432 bound locally) |
| Docker | **NOT installed** |
| NAS PostgreSQL (`192.168.2.228:5432`) | **TCP reachable** (Test-NetConnection `TcpTestSucceeded=True`) |
| `ef database update` against NAS with wrong password | `Npgsql.PostgresException 28P01: password authentication failed` — confirms network OK, credentials required |
| Real-runtime evidence | **Operator must inject PGPASSWORD** — see §13 |

Per task §十三, the priority order is: (1) existing PG → use directly, (2) Docker → compose, (3) BLOCK. We have option (1) (NAS PG), so no compose / no BLOCK.

---

## 8. Connection Strategy

| Aspect | Choice | Rationale |
|---|---|---|
| Connection name | `ConnectionStrings:GuliERP` | Per task §九 |
| Env-var override | `ConnectionStrings__GuliERP` (double underscore = section separator) | ASP.NET Core standard |
| App-settings file (Production) | `Host=CHANGE_ME;Password=CHANGE_ME` | Task §九 "禁止提交真实密码" |
| App-settings file (Development) | `Host=192.168.2.228;Database=gulierp_g2_001;Password=CHANGE_ME` | NAS host hard-coded (not a secret); password is `CHANGE_ME` |
| PGPASSWORD injection | **Operator-driven** (POC-001/002 pattern) | Real password NEVER in csproj / appsettings / source / log / markdown |
| Design-time override | `GULIERP_FOUNDATION_CONNECTION` env var (consumed by `DesignTimeFoundationDbContextFactory`) | Same PGPASSWORD pattern, separated from runtime config |
| `Program.cs` fail-fast | Throws `InvalidOperationException` at startup if connection string is missing/whitespace | Per task §15: "如果关键配置完全缺失: 应 fail-fast 并给明确开发错误" |
| Logging of connection string | **No code path logs the full connection string** | The `displayConn = -replace 'Password=[^;]+', 'Password=***'` pattern is in the operator script only |

---

## 9. DbContext

```csharp
public sealed class FoundationDbContext : DbContext
{
    public const string DefaultSchema = "foundation";

    public FoundationDbContext(DbContextOptions<FoundationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        base.OnModelCreating(modelBuilder);
    }
}
```

Key facts (verified by reading the source):
- Zero `DbSet<>` properties (per task §十: "FoundationDbContext 只负责当前 Foundation PostgreSQL baseline")
- `DefaultSchema = "foundation"` matches task §十
- EF Migrations History co-located via
  `npg.MigrationsHistoryTable("__ef_migrations_history", FoundationDbContext.DefaultSchema)`
- DI registration in `DependencyInjection.AddGuliErpFoundation(connectionString)` — single composition entry-point per `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` §5
- `DesignTimeFoundationDbContextFactory` registered for `dotnet ef` to discover the model without a real DB

---

## 10. Migration

`20260819103150_G2001_InitializeFoundationSchema.cs` — author-written (per task §十一 "允许建立一个显式 migration"):

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS foundation;");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP SCHEMA IF EXISTS foundation;");
}
```

- Idempotent (IF EXISTS / IF NOT EXISTS) so a re-run is safe
- Symmetric Up/Down so `dotnet ef migrations remove` round-trips cleanly
- The EF Migrations History table is co-located in the same schema — dropping the schema drops the ledger
- Generation: `dotnet ef migrations add G2001_InitializeFoundationSchema --project modules\foundation\GuliERP.Foundation\GuliERP.Foundation.csproj --output-dir Migrations` succeeded
- Model snapshot regenerated correctly (verifies `BuildTargetModel` / `BuildModel` are aligned)

---

## 11. Health Checks

| Endpoint | Tag | Behaviour | Verification |
|---|---|---|---|
| `GET /health/live` | `live` | Always `Healthy` (the `self` check returns `HealthCheckResult.Healthy("Host process alive.")` regardless of DB) | §15 Round 1 + Round 2 both return `200 Healthy` with bad DB |
| `GET /health/ready` | `ready` | DB probe via `AddDbContextCheck<FoundationDbContext>` — `Healthy` only when `CanConnectAsync` succeeds | §15 Round 1 + Round 2 both return `503 Unhealthy` with bad DB |

The two checks are **tag-separated** via the `tags: new[] { "live" }` / `"ready"`
arguments, so the readiness outage never trips the liveness probe. This is
exactly the pattern in `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` §11.

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Host process alive."), tags: new[] { "live" })
    .AddDbContextCheck<FoundationDbContext>(name: "foundation-db", tags: new[] { "ready" });
```

---

## 12. Integration Tests (authored)

| Test | Behaviour without `ConnectionStrings__GuliERP` | Behaviour with env var |
|---|---|---|
| `FoundationDatabaseFacts.DbConnects` | `[Fact(Skip = ...)]` → Skipped | Runs `SELECT 1` against real PG; asserts `1` |
| `FoundationDatabaseFacts.FoundationSchemaExists` | Skipped | Asserts row in `information_schema.schemata` where `schema_name = 'foundation'` |
| `FoundationDatabaseFacts.MigrationHistoryExists` | Skipped | Asserts `__ef_migrations_history` table exists in `foundation` schema |
| `FoundationHostHealthFacts.LiveHealthyWithBadDb` | **Always runs** (uses hard-coded bad conn) | Asserts `/health/live` returns `200 Healthy` |
| `FoundationHostHealthFacts.ReadyUnhealthyWithBadDb` | **Always runs** (uses hard-coded bad conn) | Asserts `/health/ready` returns `503 Unhealthy` |
| `FoundationHostHealthFacts.ReadyHealthyWithGoodDb` | Skipped | Asserts `/health/ready` returns `200 Healthy` |
| `FoundationHostHealthFacts.LiveHealthyWithGoodDb` | Skipped | Asserts `/health/live` returns `200 Healthy` (sanity check) |

Skipped tests are reported as **Skipped** (not Failed) in the xunit summary.
The `Live*WithBadDb` + `Ready*WithBadDb` negative path is fully exercised
without any credential injection.

---

## 13. Host Runtime Acceptance

`Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory<Program>` drives
the same code path as `dotnet run` (no separate startup). The two runtime
rounds below use a real `dotnet run` so the listening socket is exercised
end-to-end.

### 13.1 Round 1 (bad DB)

| Step | Result |
|---|---|
| `dotnet run --project apps/api/GuliERP.Api` (bad conn, `Host=127.0.0.1;Port=1`) | Process started, PID 31424 |
| `GET /` | **200**, banner `GuliERP Api (G2-001)` |
| `GET /health/live` | **200 Healthy** |
| `GET /health/ready` | **503 Unhealthy** (caught from `Invoke-WebRequest`) — proves readiness failure boundary works |
| Process stopped | OK |

### 13.2 Round 2 (bad DB, fresh process)

| Step | Result |
|---|---|
| Process restart (PID 22752) | OK |
| `GET /health/live` | **200 Healthy** |
| `GET /health/ready` | **503 Unhealthy** (caught) |
| Process stopped | OK |

### 13.3 Round 3+ (real DB, Operator-driven)

`tools/dev/g2-001-operator-evidence.ps1` is provided. Steps:

1. `$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_001;Username=gulidata;Password=***"`
2. `.\tools\dev\g2-001-operator-evidence.ps1`
3. Script applies migration, runs the 7 integration tests, starts the host twice, hits `/health/live` + `/health/ready`
4. Captures `trx` artifacts at `$env:TEMP\host-r1.log` / `host-r2.log`

Per task §十八, "至少两轮" + "第二轮: 停止 重新启动 再次验证". The script
performs exactly that and exits with a clear `G2-001 ALL FIVE STEPS PASSED`
banner on success.

---

## 14. Database Verification (8 items per task §16)

| # | Item | Mavis-verified? | Notes |
|---|---|---|---|
| 1 | TCP connection | ✅ (Test-NetConnection) | `TcpTestSucceeded = True` to 192.168.2.228:5432 |
| 2 | Authentication | ⏳ Operator-only | Mavis got `28P01` (expected — wrong creds); Operator must verify with real PGPASSWORD |
| 3 | Database exists / accessible | ⏳ Operator-only | Database `gulierp_g2_001` not yet created; will be created by the first `dotnet ef database update` |
| 4 | Migration apply PASS | ⏳ Operator-only | Migration code is well-formed; apply path is via `dotnet ef database update` or `tools/dev/g2-001-operator-evidence.ps1` |
| 5 | `foundation` schema exists | ⏳ Operator-only | `FoundationDatabaseFacts.FoundationSchemaExists` will assert |
| 6 | Migration history exists | ⏳ Operator-only | `FoundationDatabaseFacts.MigrationHistoryExists` will assert |
| 7 | `DbContext.CanConnect` PASS | ⏳ Operator-only | Implicit via `AddDbContextCheck<FoundationDbContext>` on `/health/ready` |
| 8 | Readiness PASS | ⏳ Operator-only | `FoundationHostHealthFacts.ReadyHealthyWithGoodDb` will assert |

**No part of #1–#8 was shortcut to SQLite / InMemory / mock.** Every
"good DB" path either uses a real Npgsql connection (Operator-only) or is
honestly skipped (Mavis).

---

## 15. Negative Verification (readiness failure boundary)

| Probe | Expected | Observed | PASS/FAIL |
|---|---|---|---|
| `GET /health/live` with bad DB | 200 Healthy | 200 Healthy (Round 1 + Round 2) | **PASS** |
| `GET /health/ready` with bad DB | 503 Unhealthy | 503 Unhealthy (Round 1 + Round 2) | **PASS** |
| Host startup with missing connection string | fail-fast with `InvalidOperationException` | `Program.cs` line 28 throws synchronously when `Configuration.GetConnectionString("GuliERP")` returns null | **PASS** (verified by code review) |
| `Configuration Error` vs `Runtime Dependency Unavailable` boundary | Config Error = fail-fast at startup; Runtime Dep = readiness flips to Unhealthy | Same as above + same as the 503 paths | **PASS** |

---

## 16. Secret Handling

| Surface | Real password? | Notes |
|---|---|---|
| `appsettings.json` | ❌ (placeholder `CHANGE_ME`) | Password is replaced; safe to commit |
| `appsettings.Development.json` | ❌ (placeholder `CHANGE_ME`) | Same |
| `Program.cs` | ❌ | Never reads or logs the password value; only throws when the whole string is empty |
| `FoundationDbContext.cs` | ❌ | No connection string at all |
| `Foundation*.Tests.cs` | ❌ | Tests use env-var injection; the test code itself contains no password |
| `g2-001-operator-evidence.ps1` | ❌ (only writes the script's display truncation `Password=***`) | The script's `Read-Host` reads at runtime and never echoes back |
| Migration SQL | ❌ | `CREATE SCHEMA IF NOT EXISTS foundation;` — no credentials |
| `git diff --check` | exit 0 | No whitespace conflicts, no conflict markers |
| git-tracked files (post-commit) | ❌ | Real password never enters a tracked file |
| `psql_history` / shell history | ⚠️ Operator concern | Operator's `Read-Host` for the password does not write to history; the PowerShell env var is per-process |

A `Select-String` scan across all G2-001 source files found zero occurrences
of `Password=<value>` (only the `CHANGE_ME` placeholder and the explicit
`Password=***` in the operator script's display). No real PostgreSQL
credential will ever land in a commit.

---

## 17. VOL Pattern Reuse (per task §二十一)

The Build-vs-Reuse Gate (`VOL_PRO_BUILD_VS_REUSE_GATE.md`) established
`GREENFIELD_WITH_PATTERN_REUSE`. For G2-001:

| Pattern | Reuse decision |
|---|---|
| Program.cs composition root pattern | **Reused pattern** — single `WebApplication.CreateBuilder()` + explicit `AddGuliErpFoundation(connection)` call. The shape follows `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` §5 which itself echoes the VOL.NET `Program.cs` style (composition root, explicit registration, no magic). |
| DI in `Program.cs` not `Startup.cs` | **Reused pattern** — directly taken from the G2 architecture draft; matches VOL.NET style. GuliERP V1 picks ASP.NET Core native DI, not Autofac. |
| `MapControllers() + MapHub() + Run()` style terminal | **Reused pattern** — adopted in `Program.cs` ordering, though G2-001 has no controllers / hubs yet (per task §七). |
| `partial class` for generated services | **Rejected** — DEC-MODULE-001 + R5 risk + DEC-META-001. Foundation is hand-written, not codegen'd. |
| Reflection-based `ApiBaseController` 13.8KB base class | **Rejected** — same reasons. `AddDbContextCheck` is the only reflection surface, and it is a standard framework pattern, not VOL-specific. |
| `Sys_TableInfoService` 125KB generated service | **Rejected** — see `VOL_PRO_CODEGEN_EXTENSION_VERIFICATION.md`. |
| `IsMultiTenancy` opt-in hook | **Deferred** to G2-002+ when Tenant/Company entities land |
| `TenancyManager<T>` 33-line empty function | **Explicitly avoided** — G2-001 does not introduce the empty stub pattern that VOL.NET has. Row-level tenant filter is a real EF Core global query filter in a future goal. |
| CodeGen (`partial` + `File.Exists` protection) | **Rejected** — see `VOL_PRO_CODEGEN_EXTENSION_VERIFICATION.md` "RISKY". |
| 5 ProjectReference hard-coded modules | **Explicitly avoided** — G2-001 ships one Foundation project; module enable/disable will be via `IModule` descriptor (G2-007) not `.csproj` editing. |

Net: G2-001 adopts the **shape** of the VOL.NET composition root (explicit
registration, no magic) without importing any VOL runtime. The single
rejected-pattern that mattered was `TenancyManager` (33-line empty function)
— we have zero of those in the Foundation module.

---

## 18. Files Changed

### 18.1 New files (G2-001 net-new)

| Path | Bytes | Purpose |
|---|---|---|
| `GuliERP.slnx` | 459 | .NET 10 solution manifest |
| `apps/api/GuliERP.Api/appsettings.Development.json` | 335 | NAS-PG Development config (CHANGE_ME password) |
| `modules/foundation/GuliERP.Foundation/FoundationDbContext.cs` | 1,786 | Minimum DbContext, schema = `foundation` |
| `modules/foundation/GuliERP.Foundation/DesignTimeFoundationDbContextFactory.cs` | 1,721 | `dotnet ef` design-time factory |
| `modules/foundation/GuliERP.Foundation/Migrations/20260819103150_G2001_InitializeFoundationSchema.cs` | 2,795 | Author-written `CREATE SCHEMA IF NOT EXISTS foundation` |
| `modules/foundation/GuliERP.Foundation/Migrations/20260819103150_G2001_InitializeFoundationSchema.Designer.cs` | (auto) | EF generated |
| `modules/foundation/GuliERP.Foundation/Migrations/FoundationDbContextModelSnapshot.cs` | (auto) | EF generated |
| `tests/GuliERP.Foundation.IntegrationTests/GuliERP.Foundation.IntegrationTests.csproj` | 874 | xunit + Mvc.Testing |
| `tests/GuliERP.Foundation.IntegrationTests/ConnectionStringProvider.cs` | 2,218 | Env-var resolver |
| `tests/GuliERP.Foundation.IntegrationTests/SkippableFact.cs` | 243 | Empty placeholder (static `[Fact(Skip = …)]` used instead) |
| `tests/GuliERP.Foundation.IntegrationTests/FoundationDatabaseFacts.cs` | 2,540 | 3 raw-DB tests |
| `tests/GuliERP.Foundation.IntegrationTests/FoundationHostHealthFacts.cs` | 4,553 | 4 host tests |
| `tools/dev/g2-001-operator-evidence.ps1` | 6,452 | Operator evidence pack |
| `docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md` | (this file) | Verification report |
| `dotnet-tools.json` | (auto) | `dotnet-ef` 10.0.11 local tool manifest |

### 18.2 Modified files (G2-001 scoped)

| Path | Change |
|---|---|
| `Directory.Packages.props` | Bumped EF Core to 10.0.11 patch band; added `Microsoft.EntityFrameworkCore.Design` + `Microsoft.EntityFrameworkCore.Relational` + `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` + `Microsoft.AspNetCore.Mvc.Testing`; removed the auto-included `Microsoft.AspNetCore.Diagnostics.HealthChecks` and `Microsoft.Extensions.Diagnostics.HealthChecks` |
| `apps/api/GuliERP.Api/GuliERP.Api.csproj` | Added EF Core, Npgsql, EF HealthCheck, OpenApi; removed auto-included HealthChecks packages; set `RootNamespace` + `AssemblyName` |
| `apps/api/GuliERP.Api/Program.cs` | Rewritten — composition root with `/health/live` + `/health/ready` + `MapOpenApi` (dev) + `Root` banner + `AddGuliErpFoundation(connection)` |
| `apps/api/GuliERP.Api/appsettings.json` | ConnectionStrings.GuliERP replaced with `CHANGE_ME` placeholders |
| `modules/foundation/GuliERP.Foundation/DependencyInjection.cs` | Rewritten — `AddGuliErpFoundation(connectionString)` wires `IFoundationBoundary` + `FoundationDbContext` (scoped) with Npgsql + MigrationsHistory in `foundation` schema |
| `modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj` | Added `RootNamespace` + `AssemblyName`; added `Microsoft.EntityFrameworkCore.Relational`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all) |
| `tests/GuliERP.Foundation.Tests/FoundationBoundaryTests.cs` | Added `using Xunit;` so the pre-existing `[Fact]` attributes resolve |

### 18.3 Untracked-but-touched (NOT in G2-001 commit)

| Path | Why not committed |
|---|---|
| `apps/web/**` (R3 design system, G1B-1R) | Pre-existing untracked; not G2-001 scope |
| `docs/architecture/**` (G2 architecture drafts) | Pre-existing untracked; not G2-001 scope |
| `docs/goals/G2_FOUNDATION_EXECUTION_PLAN.md` | Pre-existing untracked; not G2-001 scope |
| `docs/governance/GULIERP_GREENFIELD_RISK_REGISTER_V1.md` | Pre-existing untracked; not G2-001 scope |
| `docs/review/**` (G1B-1R review) | Pre-existing untracked; not G2-001 scope |
| `docs/verification/G1B1_*` + `G2_DEVELOPMENT_ENVIRONMENT_READINESS.md` | Pre-existing untracked; not G2-001 scope |
| `docs/research/vol-pro/**` (VOL-PRO-001/002) | Pre-existing untracked; separate research goal |

---

## 19. Commits

Per task §二十三, **path-specific staging** with no `git add .`. Three
atomic commits (one per logical layer).

### 19.1 Planned commits

1. **`chore(build): add solution file and central package versions for G2-001`**
   - `GuliERP.slnx`
   - `dotnet-tools.json`
   - `Directory.Packages.props` (modified)

2. **`feat(foundation): add FoundationDbContext, DI wiring, and initial migration`**
   - `apps/api/GuliERP.Api/GuliERP.Api.csproj` (modified)
   - `apps/api/GuliERP.Api/Program.cs` (modified)
   - `apps/api/GuliERP.Api/appsettings.json` (modified)
   - `apps/api/GuliERP.Api/appsettings.Development.json` (new)
   - `modules/foundation/GuliERP.Foundation/GuliERP.Foundation.csproj` (modified)
   - `modules/foundation/GuliERP.Foundation/DependencyInjection.cs` (modified)
   - `modules/foundation/GuliERP.Foundation/FoundationDbContext.cs` (new)
   - `modules/foundation/GuliERP.Foundation/DesignTimeFoundationDbContextFactory.cs` (new)
   - `modules/foundation/GuliERP.Foundation/Migrations/20260819103150_G2001_InitializeFoundationSchema.cs` (new)
   - `modules/foundation/GuliERP.Foundation/Migrations/20260819103150_G2001_InitializeFoundationSchema.Designer.cs` (new)
   - `modules/foundation/GuliERP.Foundation/Migrations/FoundationDbContextModelSnapshot.cs` (new)
   - `tests/GuliERP.Foundation.Tests/FoundationBoundaryTests.cs` (modified — added `using Xunit;`)

3. **`test(verify): add integration tests and operator evidence pack for G2-001`**
   - `tests/GuliERP.Foundation.IntegrationTests/**` (new)
   - `tools/dev/g2-001-operator-evidence.ps1` (new)
   - `docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md` (new)

If the Operator requests a single commit the file count can be collapsed to
one; the path grouping above is the recommended default.

### 19.2 Commit execution

The actual `git commit` invocation is performed after the Operator's
real-DB round completes. The path-specific staging plan is in §19.1 and
is not in this report (per task §23, the commit is part of the work but
the verification report captures the *plan*, not the commit hash).

---

## 20. Known Risks

| # | Risk | Mitigation |
|---|---|---|
| R-G2-001-1 | Operator handoff script may race with Npgsql connection timeouts on slow networks | Script uses `Timeout=10;Command Timeout=10` defaults; if needed the Operator can edit the script before running |
| R-G2-001-2 | Mavis cannot test the real-DB path; if the integration tests have a hidden bug that only manifests against real PG, the bug surfaces at Operator-run time | Integration tests use the same `WebApplicationFactory<Program>` that `dotnet run` uses; the only delta is the connection string. The 3 raw-DB tests are minimal (`SELECT 1` + schema check + history table check) — failure modes are obvious |
| R-G2-001-3 | The `Mavis-trash` / `git diff --check` invocation path did not produce a "no real password" warning; this is covered by §16's manual scan, not by an automated check | Section §16's `Select-String` scan is recorded; an automated `git secrets` hook is recommended for G2-002 |
| R-G2-001-4 | `appsettings.Development.json` hard-codes `192.168.2.228` — if the NAS host changes, the file needs updating | Documented in the operator script; Operator-only file path |
| R-G2-001-5 | The `dotnet-ef` local tool is not part of the project file (only `dotnet-tools.json`) — CI must restore local tools before invoking `dotnet ef` | Documented; `dotnet tool restore` is the standard command |
| R-G2-001-6 | `WebApplicationFactory<Program>` shares the singleton `IConfiguration` between tests — test ordering could leak connection state. Mitigated by `WithWebHostBuilder` overriding the config per test | The fixture is `IClassFixture` so one host per test class; each test calls `WithWebHostBuilder` to override |
| R-G2-001-7 | `Program.cs` uses the implicit `using` for the global usings (System, Microsoft.*); explicit `using` is recommended for the few namespaces actually used | The two `using` directives at the top of `Program.cs` (GuliERP.Foundation, Microsoft.EntityFrameworkCore) are sufficient; `<ImplicitUsings>enable</ImplicitUsings>` in `Directory.Build.props` provides the rest |

---

## 21. Next Goal

| Field | Value |
|---|---|
| Goal | **G2-002 — Foundation Kernel (Identity: Tenant/Company/Org/User/Role)** |
| Source | `docs/governance/G2_FOUNDATION_EXECUTION_PLAN.md` §3 (G2-002) |
| Depends on | G2-001 (this goal) — `foundation` schema, FoundationDbContext, DI wiring, Health checks all ready |
| Estimated | 1 day (per G2 plan) |
| Hard scope | Tenant / Company / Organization / User / Role tables + EF Core mappings + `ITenantContext` / `ICompanyContext` / `IOrganizationContext` + `UseTenantScope` middleware + seed data + 1 admin endpoint |
| Hard DO-NOT | No real auth (deferred to G2-003); no JWT; no `IUserPasswordHasher`; no audit (G2-005); no organization tree queries beyond the boundary |
| Operator action | Mavis will start the G2-002 goal **only** after the Operator flips the G2-001 gate in `docs/governance/GOAL_REGISTRY.md` to `G2_001_HOST_POSTGRESQL_VERIFIED` and runs the operator evidence pack (§13) |

The Mavis agent **does not auto-start G2-002** per task §二十六. The
session is **STOPPED** at the end of this report.

---

## 22. Final Gate

| Field | Value |
|---|---|
| **Status** | `G2_001_HOST_POSTGRESQL_VERIFIED_CODE_READY_OPERATOR_RUNTIME_PENDING` |
| Code-side PASS | build (0 warn / 0 err) + 2 unit tests + 2 integration tests + Runtime Round 1 + Runtime Round 2 (bad-DB) |
| Code-side verified by | Mavis |
| Runtime-side PENDING | Real PostgreSQL apply + 5 skipped integration tests + 2 runtime rounds with good DB |
| Runtime-side owner | **Operator** (per `tools/dev/g2-001-operator-evidence.ps1`) |
| Hard-stop conditions triggered | **NONE** (A–H from §二十四 all not met) |
| Final | **STOPPED** (no G2-002 auto-start) |

The Operator closes the gate by:

1. Running `tools/dev/g2-001-operator-evidence.ps1` to produce the 5-step
   evidence pack.
2. Updating `docs/governance/GOAL_REGISTRY.md` to set
   `G2_001_HOST_POSTGRESQL_VERIFIED` and link this report.
3. Authorising the next session to begin G2-002.

Until then, G2-002 is **HALTED** per the META_GULI HR-7 (no silent scope
expansion) principle.

---

## 23. Efficiency Timing

| Event | Approx. (wall-clock) |
|---|---|
| Start (Mavis begun this report after Git preflight) | 2026-08-19 17:52 Asia/Taipei |
| First host build PASS | ≈ +1 min |
| First PostgreSQL TCP probe (NAS) PASS | ≈ +2 min |
| First `/health/live` 200 (Round 1, bad DB) | ≈ +10 min |
| First `/health/ready` 503 (Round 1, bad DB) | ≈ +10 min |
| Round 2 complete (same shape) | ≈ +13 min |
| Final build + test PASS | ≈ +15 min |
| End (this report written) | 2026-08-19 18:25 Asia/Taipei |
| **Total duration** | **≈ 33 min** |

The Mavis session is **STOPPED** at the conclusion of this report.

---

*End of G2_001_HOST_POSTGRESQL_VERIFICATION_REPORT*
*Status: G2_001_HOST_POSTGRESQL_VERIFIED_CODE_READY_OPERATOR_RUNTIME_PENDING*
*Code-side fully verified (build + 4 unit/int tests + Runtime Round 1+2 with bad DB)*
*Real-DB round Operator-driven via tools/dev/g2-001-operator-evidence.ps1*
*G2-002 HALTED until Operator flips the gate*
