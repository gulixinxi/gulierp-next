# G2 — Development Environment Readiness

| Field | Value |
|---|---|
| Goal | G2 — Verify and document the developer-machine environment for GuliERP greenfield work |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Output Status | **DRAFT — for review only** |
| Author | Mavis (read-only investigation, single writer) |
| Probed machine | `WIN-...` (Operator's box, Windows 11 10.0.26100) |
| Probed at | 2026-08-19 (Asia/Taipei) |
| Scope | .NET SDK, PostgreSQL client, Node toolchain, Git, PowerShell |
| Out of scope | Docker host (not probed), VPN/NAS access (not probed), online NuGet feed (not probed) |
| Old project | **READ-ONLY** — `D:\guli\gulierp` is not modified by this investigation |

---

## 1. TL;DR — what the Operator needs to do tomorrow morning

In **one PowerShell session** (or a saved profile), before any G2 goal starts:

```powershell
# Step 1 — make the .NET 10.0.400 SDK the one that PowerShell sees
$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"

# Step 2 — verify
dotnet --version           # expect: 10.0.400
dotnet --list-sdks         # expect: 10.0.400

# Step 3 — go to project
cd D:\guli\projects\gulierp-next
dotnet --version           # expect: 10.0.400 (global.json is satisfied by rollForward=latestFeature)
```

For PostgreSQL (no `psql` is installed, see §3.4), use **docker-compose** with `postgres:16-alpine`. The CI / smoke tests use **Testcontainers.PostgreSql**, so no native PostgreSQL install is required for the G2-001 acceptance test.

**No large downloads. No system security changes. No global installs.**

---

## 2. The actual environment (as probed)

### 2.1 .NET — three places, one usable

There are **three** .NET installations on this box; only one has an SDK:

| Location | Type | Has SDK? | Has Runtime? | On PATH? |
|---|---|---|---|---|
| `C:\Program Files\dotnet\` (system) | Public install | **No** | Yes (6.0.36 + 10.0.11) | **Yes (first)** |
| `C:\Users\Administrator\.dotnet\` (user-level) | User-level cache | **No** (only sentinels + telemetry; no `sdk` subfolder) | No | No (only `\tools` is on PATH) |
| `D:\guli\gulierp\.dotnet\` (old project's private) | Private install for the old project | **Yes — 10.0.400** | Yes (10.0.11 only) | **No** |

**Why "no SDK" was reported earlier**: when PowerShell resolves `dotnet`, it finds `C:\Program Files\dotnet\dotnet.exe` first. That binary, when run from the project root, reads `global.json`, looks for an SDK, and finds **none in its own install** — it does not search `D:\guli\gulierp\.dotnet\` because that is a private install outside its own tree.

**The 10.0.400 SDK exists, is functional, is just not on PATH.** The old project (`D:\guli\gulierp`) is read-only, but we can use its SDK without modifying it.

### 2.2 .NET 10.0.400 SDK details

```
Version:           10.0.400
Commit:            14fbf8d527
Workload version:  10.0.400-manifests.330ea142
MSBuild version:   18.9.6+14fbf8d52
Base Path:         D:\guli\gulierp\.dotnet\sdk\10.0.400\
Runtimes:          Microsoft.AspNetCore.App 10.0.11
                   Microsoft.NETCore.App    10.0.11
                   Microsoft.WindowsDesktop.App 10.0.11
Workloads:         (none installed; not needed for G2)
```

**Is 10.0.400 compatible with the project's `global.json` (`10.0.100`)?** Yes. `rollForward: latestFeature` means the resolver accepts any SDK in the **same major.minor feature band** of 10.0.x with version ≥ 10.0.100. 10.0.400 satisfies that. (See `dotnet --list-sdks` semantics: `latestFeature` is `x.y.*`, so 10.0.100 with `latestFeature` accepts 10.0.400, 10.0.500, etc.)

### 2.3 Other toolchain

| Tool | Version | Path | Status |
|---|---|---|---|
| Node.js | v22.22.2 | `C:\Program Files\nodejs\node.exe` | ✅ Ready |
| npm | 10.9.7 | `C:\Program Files\nodejs\npm.ps1` | ✅ Ready |
| pnpm | 11.13.1 | `C:\Users\Administrator\AppData\Roaming\npm\pnpm.ps1` | ✅ Ready |
| yarn | — | — | ❌ Not installed (not needed; pnpm is the project's choice) |
| Git | 2.53.0.windows.2 | `C:\Program Files\Git\cmd\git.exe` | ✅ Ready |
| PowerShell | 7.6.0 | `pwsh` | ✅ Ready (we are in 7.6, not 5.1) |
| `psql` (PostgreSQL CLI) | — | — | ❌ Not installed |
| `pg_dump` | — | — | ❌ Not installed |
| `dotnet` (PATH-resolved) | 10.0.11 (no SDK) | `C:\Program Files\dotnet\dotnet.exe` | ⚠ Wrong dotnet on PATH |

### 2.4 What is **not** installed

- ❌ `psql`, `pg_dump`, `pg_dumpall` — PostgreSQL client tools.
- ❌ Docker Desktop (assumed; not probed — see §6).
- ❌ Redis, Kafka, RabbitMQ — out of V1 scope anyway.
- ❌ Visual Studio / Rider — **out of scope for this plan; .NET CLI + VS Code is sufficient**.
- ❌ SQL Server Management Studio — not needed; PostgreSQL is the V1 DB.

### 2.5 What is installed but **not** on PATH

- The old project's `.dotnet` SDK 10.0.400 (above).
- Any user-level `dotnet` tools that the Operator may have installed under `C:\Users\Administrator\.dotnet\tools` (which **is** on PATH, but the dir is currently empty of SDKs).

---

## 3. The four root causes of "no SDK" and the four fixes

### 3.1 Root cause 1: `global.json` pins a specific feature band

**File**: `D:\guli\projects\gulierp-next\global.json`
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

**Why this matters**: with `rollForward: latestFeature`, the SDK resolver will accept any 10.0.x SDK with version ≥ 10.0.100 — which 10.0.400 satisfies. So **the pin is not the problem**; the pin is correct *if* an SDK is reachable. The error message is misleading; the real issue is that no SDK is reachable.

**Option to change (NOT recommended unless §3.2 / §3.3 fail)**: set `version: 10.0.400` to be explicit. This is a one-character edit, easy to revert. **Acceptable** if the morning Operator prefers explicitness over roll-forward.

### 3.2 Root cause 2: PATH resolves to the system dotnet, which has no SDK

**Fix (recommended, smallest)**: prepend the old project's `.dotnet` to PATH for the current PowerShell session, or set `DOTNET_ROOT`:

```powershell
$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
```

To make this **persistent**, add it to `$PROFILE` (`$HOME\Documents\PowerShell\Microsoft.PowerShell_profile.ps1`):

```powershell
# Append to $PROFILE
$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
```

**Note**: this is **per-user**, not system-wide. It does not touch the system PATH. It does not require admin. It is reversible by removing the two lines from `$PROFILE`.

### 3.3 Root cause 3 (hypothetical): `DOTNET_ROOT` is wrong

`DOTNET_ROOT` tells the .NET host where to find the shared runtime. It should match where the SDK's `dotnet.exe` lives. If the morning Operator sees a confusing error like "The library hostfxr.dll could not be found", they should verify `$env:DOTNET_ROOT` matches `D:\guli\gulierp\.dotnet`.

**Fix**: re-run the two lines above and verify with `dotnet --info` that `Base Path: D:\guli\gulierp\.dotnet\sdk\10.0.400\` appears.

### 3.4 Root cause 4 (hypothetical, only for DB work): no `psql`

**Symptom**: any `psql ...` command fails with "command not found". EF Core migrations, Npgsql queries, and Testcontainers work **without** `psql` — the C# code talks to PostgreSQL via Npgsql. `psql` is only needed for ad-hoc DBA work.

**Fix options**:

| Option | Effort | Recommended? |
|---|---|---|
| A. Use `Testcontainers.PostgreSql` in tests; no native install | 0 (just use the lib) | **Yes** for G2-001 / G2-002 / G2-005 / G2-006 / G2-009 |
| B. `docker-compose up -d postgres` (postgres:16-alpine) | minimal (if Docker is available) | **Yes** for local dev; CI uses Testcontainers |
| C. Install PostgreSQL 16 natively (e.g. via `winget install PostgreSQL.PostgreSQL.16`) | ~500 MB download | **Only** if A and B both fail |
| D. Install just `psql` via `winget install PostgreSQL.PostgreSQL.16` and use a remote DB | ~500 MB download | Same as C; not preferred |

**The brief explicitly says**: "禁止下载大型无关工具. 禁止修改系统安全策略." → option C / D should be **avoided** unless A and B both fail. Testcontainers is the default.

---

## 4. The morning Operator's minimum command sequence (3 minutes)

```powershell
# === BLOCK 1: .NET SDK is now resolvable ===
$ErrorActionPreference = 'Stop'
$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"

# Sanity check
dotnet --version                              # expect: 10.0.400
dotnet --list-sdks                            # expect: 10.0.400

# === BLOCK 2: go to project, verify global.json is satisfied ===
Set-Location 'D:\guli\projects\gulierp-next'
dotnet --version                              # expect: 10.0.400 (rollForward=latestFeature)
dotnet build --no-restore 'src\GuliERP.Host\GuliERP.Host.csproj' `
            -c Release 2>$null                # (after src/ exists; see G2-001)

# === BLOCK 3: PostgreSQL via Testcontainers (no native install) ===
# (inside a test, automatically pulled)
# No CLI action needed. The first test run downloads `postgres:16-alpine` (~150 MB, one-time).

# === BLOCK 4: Node / pnpm sanity (for the R3 frontend) ===
node --version                                # expect: v22.22.2
pnpm --version                                # expect: 11.13.1

# === BLOCK 5: Git sanity ===
git --version                                 # expect: 2.53.0

# === BLOCK 6: (optional) persist for future sessions ===
Add-Content -Path $PROFILE -Value "`n# GuliERP env`n`$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'`n`$env:PATH = "`$env:DOTNET_ROOT;`$env:PATH`""
```

**If any command above produces a different output than expected, see §5 troubleshooting.**

---

## 5. Troubleshooting matrix

| Symptom | Likely cause | Fix |
|---|---|---|
| `dotnet --version` returns 10.0.11 (no SDK) | `$env:DOTNET_ROOT` / `$env:PATH` not set in this session | Re-run Block 1 |
| `dotnet --version` returns `10.0.100` not found | `global.json` resolution conflict; check `$env:PATH` does not contain two `dotnet.exe` paths | Use `where.exe dotnet` to list candidates; remove duplicates |
| `dotnet build` complains about MSBuild target not found | SDK present, but workloads not installed (e.g. for MAUI, WPF) | Not needed for V1 (we use ASP.NET Core only); no action |
| `npm install` hangs on network | No internet, or proxy not configured | Out of scope; check `winget` / `npm config get registry` |
| `pnpm install` fails with EACCES on `C:\Users\Administrator\AppData\Local\pnpm` | Permission issue | `pnpm config set store-dir D:\pnpm-store` (per-user) |
| `git push` / `git fetch` fails | No remote configured; or no internet | Per the brief, no `push` is allowed in V1 anyway |
| `psql: command not found` | Expected (see §3.4) | Use Testcontainers or `docker-compose` |
| `docker: command not found` | Docker not installed | Use Testcontainers (which pulls its own image); or install Docker Desktop |
| `dotnet ef` not found | `dotnet-ef` tool not installed | `dotnet tool install -g dotnet-ef` (per-user, in `$env:DOTNET_ROOT` or `$env:USERPROFILE\.dotnet\tools`) |
| After editing `global.json`, `dotnet --version` still wrong | Stale shell | Open a new PowerShell session |
| Want to verify `hostfxr.dll` is findable | The host helper | `$env:DOTNET_ROOT = 'D:\guli\gulierp\.dotnet'; dotnet --info` should show `Base Path: D:\guli\gulierp\.dotnet\sdk\10.0.400\` |

---

## 6. What is **not** verified in this document

The following are out of scope for this readiness document and must be probed **by the Operator** when relevant:

1. **Docker Desktop availability** — not probed. The brief allows Testcontainers as the default, which has its own image pull path. If Testcontainers also fails (no internet, no Docker), the morning Operator must investigate.
2. **NAS PostgreSQL reachability** (`192.168.2.228:5432`) — not probed. The new project is greenfield; the Foundation smoke uses Testcontainers by default. The NAS PostgreSQL is a *future* integration target, not a G2 dependency.
3. **Online NuGet feed** (`api.nuget.org`) — not probed. The first `dotnet restore` will tell us; if it fails, the morning Operator must investigate (proxy, offline feed mirror).
4. **Online npm registry** (`registry.npmjs.org`) — not probed. The same.
5. **VPN / corporate proxy** — not probed. If the Operator is behind a corporate proxy, the env vars `HTTP_PROXY` / `HTTPS_PROXY` must be set per corporate policy.
6. **GitHub remote** — per the brief, the project has no remote. `git push` is forbidden in V1.
7. **.NET 10.0.100 SDK availability on `aka.ms/dotnet`** — we are not downloading; we are using 10.0.400 already on disk.

If any of these are not green, the morning Operator surfaces it; we do not pre-emptively configure them.

---

## 7. The 6 environment contracts for G2 goals

A G2 goal is **not** ready to be marked green unless the environment satisfies these 6 contracts:

| # | Contract | How to verify | Fallback if not met |
|---|---|---|---|
| E1 | `dotnet --version` returns a 10.x SDK with version ≥ 10.0.100 | `dotnet --version` | §3.2 fix |
| E2 | `dotnet --list-sdks` returns at least one entry | `dotnet --list-sdks` | Same |
| E3 | `dotnet build` of `GuliERP.Host.csproj` succeeds (when src/ exists) | `dotnet build -c Release` | Same |
| E4 | A PostgreSQL instance is reachable for integration tests | Testcontainers or docker-compose or remote DB | §3.4 fix |
| E5 | Node + pnpm are on PATH for the frontend | `node --version` and `pnpm --version` | Install via winget or skip (TRAE handles frontend) |
| E6 | Git is on PATH and the working tree is clean | `git status` returns nothing to commit | Resolve uncommitted work before starting |

The G2-001 Gate is gated on E1, E2, E3, E4. The G2-008 Gate adds E5.

---

## 8. One-sentence summary

> The .NET 10.0.400 SDK is on disk but off-PATH; the morning Operator prepends `D:\guli\gulierp\.dotnet` to PATH (or sets `DOTNET_ROOT`) for one session, optionally persists it in `$PROFILE`, and uses Testcontainers.PostgreSql for the DB — no system changes, no new installs, no internet downloads required.

---

## 9. Operator self-check (10 questions, ~2 min)

Before the Operator starts G2-001, they should be able to answer **YES** to all 10:

1. ☐ `dotnet --version` shows 10.0.400 (not 10.0.11 or "command not found")
2. ☐ `dotnet --list-sdks` shows at least 10.0.400
3. ☐ `Set-Location 'D:\guli\projects\gulierp-next'; dotnet --version` still shows 10.0.400
4. ☐ `node --version` shows v22.x
5. ☐ `pnpm --version` shows 11.x
6. ☐ `git --version` shows 2.5x
7. ☐ `git status` shows nothing to commit (or only expected new files in `docs/`)
8. ☐ `Testcontainers.PostgreSql` is available (will be added as a NuGet ref in G2-001; not a separate install)
9. ☐ `appsettings.Development.json` (when written in G2-001) does NOT contain any real password
10. ☐ The Operator knows how to run the G2-001 smoke test and where to look at its output

If any answer is NO, the relevant section of this document has the fix.

---

## 10. Open questions for Operator

| # | Question | Default if unanswered |
|---|---|---|
| Q1 | Should `$PROFILE` be edited to persist the env vars, or only set in the current session? | Current session only (less invasive; profile is the Operator's choice) |
| Q2 | Should the morning Operator install Docker Desktop (so docker-compose is available) or rely on Testcontainers only? | Testcontainers only (no Docker Desktop install per brief) |
| Q3 | If the Operator prefers `winget install Postgresql.Postgresql.16` for a native psql, is that acceptable? | Only if Testcontainers fails; default is Testcontainers |
| Q4 | Should the morning Operator set up an offline NuGet feed mirror? | No (out of scope; first `dotnet restore` will tell us if it's needed) |
| Q5 | Should the morning Operator pre-pull the `postgres:16-alpine` Docker image? | Not needed; Testcontainers pulls on first test run |

---

## 11. Companion docs

- `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md` (TASK B) — what we are building
- `G2_FOUNDATION_EXECUTION_PLAN.md` (TASK H) — when we are building it
- This doc — the pre-flight check
- (Future) `G2_FOUNDATION_RUNTIME_ACCEPTANCE_REPORT.md` (after G2-010) — the post-flight verification

---

*End of G2 Development Environment Readiness — Status: DRAFT. The Operator's first action tomorrow is Block 1 of §4; the rest is verification.*
