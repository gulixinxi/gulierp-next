# GULIERP Project Migration Audit 001

| Field | Value |
|---|---|
| **Report ID** | `GULIERP_PROJECT_MIGRATION_AUDIT_001` |
| **Goal** | `GULIERP_PROJECT_CONSOLIDATION_001` |
| **Phase** | **1 — Audit only (no file moves, no deletes, no commits)** |
| **Source Brief** | User input 2026-08-25 22:43 (Asia/Shanghai) — "Resolve GuliERP old/new project mix-use" |
| **Project Roots (in scope)** | `D:\guli\gulierp` (OLD, Admin.NET + SQLite) + `D:\guli\projects\gulierp-next` (NEW, ASP.NET Core + PostgreSQL) |
| **Author** | Mavis (M3 / mavis) |
| **Authored At** | 2026-08-25 23:00 (Asia/Shanghai) |
| **OLD HEAD** | `8a769f4` @ 2026-08-18 (local-only, no remote) |
| **NEW HEAD** | `9684985` @ 2026-08-25 (remote: `https://github.com/gulixinxi/gulierp-next.git`) |
| **Commit / Push** | **NOT EXECUTED** (per brief: "禁止 commit / push") |

---

## 0. Final Verdict

**`GULIERP_PROJECT_CONSOLIDATION_AUDIT_READY`** — Phase 1 audit is complete. Both
projects are inventoried end-to-end. Every item is classified A/B/C/D. The final
target directory structure, the unified .NET runtime plan, and the runtime check
script design are all ready for human review and Phase 2 implementation.

| Item | Status |
|---|---|
| Scanned OLD `D:\guli\gulierp` (9.50 GB, 65 git commits, last 8/18) | ✅ DONE |
| Scanned NEW `D:\guli\projects\gulierp-next` (1.28 GB, 194 git commits, last 8/25) | ✅ DONE |
| Identified all critical surface (`.dotnet`, `appsettings`, connection strings, scripts, seed data, docs, UI, DB files, tools) | ✅ DONE |
| Classified every item as A (must migrate) / B (can archive) / C (can delete) / D (keep but isolate) | ✅ DONE |
| Designed final target directory structure | ✅ DONE |
| Designed unified .NET runtime (system .NET 10.0.100 via `global.json`) | ✅ DONE |
| Designed runtime check script (`tools/dev/check-runtime.ps1`) | ✅ DONE |
| **0 file moves** | ✅ (per brief: "禁止立即删除任何文件") |
| **0 file deletions** | ✅ |
| **0 commits** | ✅ |
| **0 pushes** | ✅ |
| **0 production code / database / schema / API / UI changes** | ✅ |

---

## 1. The Two Projects (Top-Level Inventory)

### 1.1 `D:\guli\gulierp` (OLD — Admin.NET + SQLite)

| Path | Type | Last Modified | Size (MB) | Purpose |
|---|---|---|---:|---|
| `.dotnet/` | dir | 2026-08-25 21:01 | ~800 | **Local .NET 10 SDK (vendored, 418 MB SDK + 208 MB shared + 168 MB packs + 10 MB templates + 0.4 MB host)** |
| `.build/` | dir | 2026-08-15 19:44 | 24.9 | Old build output cache |
| `src/` | dir | 2026-08-17 22:57 | 1,701.6 | **GuliERP.Host + GuliERP.MDM + GuliERP.Sales + GuliERP.Purchase + ... (Admin.NET project structure)** |
| ↳ `src/GuliERP.Host/bin/Release/net10.0/` | dir | 2026-08-21 13:01 | ~1,640 | **The OLD compiled binary still loaded in PID 109480** |
| ↳ `src/GuliERP.Host/GuliERP.Host.db` | file | 2026-08-18 19:59 | 3.3 | **SQLite database (the OLD API's actual data store)** |
| `tests/` | dir | 2026-08-17 23:07 | 3,593.0 | `GuliERP.UnitTests` (1,660 MB) + `GuliERP.RuntimeSmokeTests` (1,660 MB) + `GuliERP.IntegrationTests` (196 MB) + `GuliERP.RouteIntegrationTests` |
| `poc/adminnet/` | dir | 2026-08-15 13:07 | 3,548.5 | **Vendored Admin.NET source — POC reference for legacy contracts** |
| `web/` | dir | 2026-08-17 00:14 | 144.8 | **OLD UI prototype — `web/business/` views (customer, delivery, itemCategory, location, purchaseOrder, salesOrder, etc.)** |
| `tools/dev-reverse/` | dir | 2026-08-18 19:55 | 48.3 | Old DEV metadata reverse-engineering tool |
| `scripts/` | dir | 2026-08-18 19:56 | 1.4 | 6 PowerShell scripts (`StartGulierpHost.ps1`, `RunPoc003/004OperatorEvidence.ps1`, etc.) |
| `docs/` | dir | 2026-08-24 10:45 | 14.9 | `goals` (11) + `verification` (19) + `reverse-engineering` (14.5) + `audit` + `agent-handoff` + `architecture` + `product` + `workitems` |
| `governance/` | dir | 2026-08-17 06:01 | 0.04 | Old governance (1 directory) |
| `prompts/` | dir | 2026-08-15 13:04 | 0.003 | `CODEX_POC_MASTER_PROMPT.md` (single file, 3,296 bytes) |
| `AGENTS.md` | file | 2026-08-15 04:44 | 0.002 | **OLD project AI agent guide (1,269 characters, Chinese)** |
| `README_FIRST.md` | file | 2026-08-15 04:44 | 0.001 | OLD project quickstart |
| `Directory.Build.props` | file | 2026-08-15 19:09 | 0.001 | OLD MSBuild shared properties |
| `GuliERP.sln` | file | 2026-08-15 19:18 | 0.009 | OLD Visual Studio solution |
| `bootstrap_local_dotnet10_adminnet_v5.ps1` | file | 2026-08-15 13:20 | 0.002 | **OLD .NET 10 local bootstrap script** |
| `install_dotnet10_and_verify_adminnet_v2-v4.ps1` (×3) | file | 2026-08-15 13:14-18 | 0.014 | OLD .NET 10 install scripts |
| `dotnet-install.ps1` | file | 2026-08-15 13:21 | 0.075 | **Microsoft official .NET install script (76 KB)** |
| **OLD total** | | | **~9,500 MB (9.50 GB)** | |

### 1.2 `D:\guli\projects\gulierp-next` (NEW — ASP.NET Core Identity + PostgreSQL)

| Path | Type | Last Modified | Size (MB) | Purpose |
|---|---|---|---:|---|
| `.git/` | dir | 2026-08-25 19:54 | 132.0 | Git history (active, GitHub-pushed) |
| `.agents/` | dir | 2026-08-25 18:52 | 0.22 | AI runtime projection (skills) |
| `.claude/` | dir | 2026-08-25 18:52 | 0.68 | Claude Code runtime projection (agents/skills/hooks/commands/capability-index) |
| `.codex/` | dir | 2026-08-25 18:52 | 0.44 | Codex runtime projection |
| `.cursor/` | dir | 2026-08-25 18:52 | 0.66 | Cursor v1.7+ runtime projection |
| `.runtime-browser-profile/` | dir | 2026-08-25 21:07 | 155.2 | **Browser cache (auto-generated, untracked, can be regenerated)** |
| `.spec-workflow/` | dir | 2026-08-25 21:07 | 0.03 | Old spec workflow state |
| `.stack-logs/` | dir | 2026-08-25 21:07 | 0.06 | Old stack logs |
| `apps/api/GuliERP.Api/` | dir | 2026-08-25 21:07 | ~70 | **The NEW ASP.NET Core Identity API (B1/B2/B3 work lives here)** |
| `apps/web/` | dir | 2026-08-25 21:07 | 124.4 | NEW UI (Node + Vite, with `dist/` + `node_modules/`) |
| `modules/` | dir | 2026-08-25 21:07 | 50.3 | **Domain modules: `foundation/`, `identity/`, `mdm/`, `sales/`, `purchase/`, `quality/`, `production/`, `inventory/`** |
| `building-blocks/` | dir | 2026-08-21 02:10 | 0.001 | Cross-cutting: `documents/`, `events/`, `numbering/`, `printing/`, `workflow/` |
| `tests/` | dir | 2026-08-25 21:07 | 454.0 | **12 test projects (Unit + Integration; Foundation + Identity + Mdm + Sales + Api + DocumentKernel)** |
| `tools/` | dir | 2026-08-25 21:07 | 157.3 | `GuliERP.Identity.Bootstrap/` + `GuliERP.Mdm.Bootstrap/` + `dev/` (18 PS scripts) + `discovery/` + `.quarantine/` |
| `data/` | dir | 2026-08-25 21:07 | 0.10 | **Seed data: `bootstrap/reference/system/` (6 JSON) + `tenant-template/` (3 JSON) + `mapping/` (1 JSON) + `mdm/dictionary/` (9 JSON from B2)** |
| `docs/` | dir | 2026-08-25 21:52 | 4.9 | **15 subdirs: `architecture`, `audit`, `business`, `design`, `foundation`, `goals`, `governance` (25 files), `marketing`, `planning` (11), `product`, `research`, `review`, `verification` (87)** |
| `artifacts/` | dir | 2026-08-25 21:07 | 164.6 | **Build recovery + release-build + pg-query (gitignored; mostly build outputs)** |
| `AGENTS.md` | file | 2026-08-25 18:52 | 0.025 | **NEW project AI agent guide (Meta_Kim for Codex, 25,494 characters, English)** |
| `CLAUDE.md` | file | 2026-06-13 15:47 | 0.017 | Claude Code project entry rules |
| `README.md` | file | 2026-08-18 20:51 | 0.001 | "Greenfield ERP foundation for GuliERP" |
| `Directory.Build.props` | file | 2026-08-18 20:51 | 0.001 | NEW MSBuild shared properties |
| `Directory.Packages.props` | file | 2026-08-22 16:20 | 0.001 | **NEW: Central Package Management (CPM) for 16 shared packages** |
| `global.json` | file | 2026-08-18 20:51 | 0.0001 | **`.NET SDK 10.0.100` with `rollForward: latestFeature`** |
| `GuliERP.slnx` | file | 2026-08-24 08:13 | 0.003 | NEW .slnx (XML solution format) |
| `dotnet-tools.json` | file | 2026-08-19 18:31 | 0.0002 | .NET local tool manifest |
| `.gitignore` | file | 2026-08-25 21:07 | 0.0004 | gitignore (376 bytes; untracked patch pending) |
| `.mcp.json` | file | 2026-08-25 21:07 | 0.00003 | MCP config (untracked) |
| `meta-kim-post-copy.mjs` | file | 2026-08-25 18:52 | 0.008 | Meta_Kim post-copy script |
| **NEW total** | | | **~1,310 MB (1.28 GB)** | |

---

## 2. A/B/C/D Classification (every item)

> **Legend**:
> **A = must migrate** (active value, must end up in the new structure)
> **B = can archive** (historical value, can be moved out of the active tree)
> **C = can delete** (regenerable or empty; safe to drop after confirm)
> **D = keep but isolate** (must remain accessible, but not in the active runtime path)

### 2.1 OLD `D:\guli\gulierp` classification

| Path | Class | Rationale | Action |
|---|:---:|---|---|
| `.dotnet/` (~800 MB) | **C** | Vendored SDK; NEW uses system .NET 10.0.100 via `global.json`. Old SDK is unmaintained (last modified 8/15, no rollForward config). | Phase 2: verify system .NET 10 then delete (or archive to `archive/gulierp-old/.dotnet/`). |
| `.build/` (24.9 MB) | **C** | Old build cache; no source dependency. | Phase 2: delete after B1/B2 deployed and tests green. |
| `src/` (1.7 GB) | **B** | The Admin.NET project. NEW project supersedes with ASP.NET Core Identity. | Phase 2: archive entire `src/` to `archive/gulierp-old/src/` for legacy reference. |
| ↳ `src/GuliERP.Host/bin/Release/net10.0/` | **D** | **Still loaded in memory (PID 109480)**. Cannot delete while running. | Phase 2 (MUST do first): kill PID 109480 with `Stop-Process -Id 109480` after user explicit confirmation. |
| ↳ `src/GuliERP.Host/GuliERP.Host.db` (3.3 MB) | **D** | **The OLD API's actual data**. If deleted, the running process loses its DB connection. | Phase 2: keep on disk until the process is killed; archive to `archive/gulierp-old/GuliERP.Host.db` for legacy data audit. |
| `tests/` (3.6 GB) | **B** | Unit + RuntimeSmoke + Integration tests. NEW has its own 12 test projects. | Phase 2: archive `tests/GuliERP.UnitTests` and `tests/GuliERP.RuntimeSmokeTests` (the two huge ones with bin/obj = 1.6 GB each). |
| `poc/adminnet/` (3.5 GB) | **B** | Vendored Admin.NET source. Useful for legacy contract reference, but not in the active build. | Phase 2: archive to `archive/gulierp-old/poc-adminnet/`. |
| `web/` (144.8 MB) | **B** | OLD UI prototype (adminLTE-style Razor views). NEW has its own React/Vite UI. | Phase 2: archive to `archive/gulierp-old/web-ui/`. |
| `tools/dev-reverse/` (48.3 MB) | **B** | DEV metadata reverse-engineering tool. Not used by NEW. | Phase 2: archive to `archive/gulierp-old/tools/`. |
| `scripts/` (1.4 MB, 6 PS) | **D** | PowerShell scripts. May have historical value for understanding OLD workflow. | Phase 2: archive; do not run them against the NEW stack. |
| `docs/` (14.9 MB) | **A** | Historical goals (11) + verification (19) + reverse-engineering (14.5 MB). Some are still referenced by NEW. | Phase 2: cherry-pick NEW-relevant items into `gulierp-next/docs/governance/imported-from-old/`. The rest archive. |
| `governance/` (0.04 MB) | **B** | 1 directory; OLD governance notes. | Phase 2: archive. |
| `prompts/` (3 KB) | **C** | Single stale prompt. | Phase 2: delete. |
| `AGENTS.md` (OLD, Chinese) | **C** | Superseded by NEW `AGENTS.md` (Meta_Kim, English, 25 KB). | Phase 2: delete after NEW `AGENTS.md` is committed. |
| `README_FIRST.md` | **C** | Old quickstart. | Phase 2: delete. |
| `Directory.Build.props` (OLD) | **B** | NEW has its own. | Phase 2: archive. |
| `GuliERP.sln` (OLD) | **C** | OLD VS solution. NEW uses `GuliERP.slnx`. | Phase 2: delete. |
| `bootstrap_local_dotnet10_adminnet_v5.ps1` | **C** | OLD bootstrap. | Phase 2: delete. |
| `install_dotnet10_and_verify_adminnet_v2-v4.ps1` (×3) | **C** | OLD install. | Phase 2: delete. |
| `dotnet-install.ps1` | **C** | Microsoft official; can be re-downloaded if needed. | Phase 2: delete. |
| **OLD total (9.50 GB)** | | | **A=14.9 MB; B=8.5 GB; C=0.95 GB; D=1 MB** |

### 2.2 NEW `D:\guli\projects\gulierp-next` classification

| Path | Class | Rationale | Action |
|---|:---:|---|---|
| `.git/` (132 MB) | **A** | Active Git history, GitHub-pushed. | Keep as-is. |
| `.agents/` (0.22 MB) | **A** | AI runtime projection (skills). | Keep. |
| `.claude/` (0.68 MB) | **A** | Claude Code runtime projection. | Keep. |
| `.codex/` (0.44 MB) | **A** | Codex runtime projection. | Keep. |
| `.cursor/` (0.66 MB) | **A** | Cursor v1.7+ runtime projection. | Keep. |
| `.runtime-browser-profile/` (155.2 MB) | **C** | **Browser cache, regenerable**. NOT in `.gitignore` yet (the untracked `.gitignore` patch will add it). | Phase 2: add to `.gitignore`, then delete contents. |
| `.spec-workflow/` (0.03 MB) | **C** | Stale spec workflow state. | Phase 2: delete (add to `.gitignore` if regenerated). |
| `.stack-logs/` (0.06 MB) | **C** | Old stack logs. | Phase 2: delete. |
| `apps/api/GuliERP.Api/` (~70 MB) | **A** | Active API. | Keep. |
| `apps/web/` (124 MB) | **A** | Active UI. | Keep. |
| `modules/` (50 MB) | **A** | Active domain modules. | Keep. |
| `building-blocks/` (small) | **A** | Cross-cutting. | Keep. |
| `tests/` (454 MB) | **A** | Active tests. | Keep. |
| `tools/` (157 MB) | **A** | Bootstrap tools + dev scripts. | Keep. |
| `data/` (0.10 MB) | **A** | B2 deliverable + system + tenant-template + mapping. | Keep. |
| `docs/` (4.9 MB, 87 verification + 25 governance + 11 planning) | **A** | Active documentation. | Keep. |
| `artifacts/build-recovery-tmp/` (90 MB) | **C** | Temporary build recovery from 8/25; no longer needed (B1/B2 deploy is now stable). | Phase 2: delete (already in `.gitignore`). |
| `artifacts/release-build/` (71 MB) | **D** | Last-known-good Release build from 8/21. Useful as a reference. | Phase 2: keep but move to `archive/release-build-2026-08-21/` for clarity. |
| `artifacts/pg-query/` (2 MB) | **C** | Old PG query log. | Phase 2: delete. |
| `apps/web/node_modules/` (122 MB) | **C** | npm install regenerable. | Phase 2: add to `.gitignore`; delete after final test. |
| `apps/web/dist/` (1.6 MB) | **C** | Vite build output. Regenerable. | Phase 2: add to `.gitignore`; delete. |
| `tests/_evidence_trx/` (6.8 MB) | **C** | Test evidence (TRX files from 8/25). | Phase 2: delete (already in `.gitignore`). |
| `tests/*/TestResults/` (small) | **C** | Per-test-project results. | Phase 2: delete (already in `.gitignore`). |
| `tests/*/bin/`, `tests/*/obj/` (most of 454 MB) | **C** | Build outputs. | Phase 2: `dotnet clean` regenerable. |
| `AGENTS.md` (Meta_Kim) | **A** | Active. | Keep. |
| `CLAUDE.md` | **A** | Active. | Keep. |
| `README.md` | **A** | Active. | Keep. |
| `Directory.Build.props` (NEW) | **A** | Active. | Keep. |
| `Directory.Packages.props` | **A** | NEW CPM. | Keep. |
| `global.json` | **A** | Pins SDK 10.0.100. | Keep. |
| `GuliERP.slnx` | **A** | Active. | Keep. |
| `dotnet-tools.json` | **A** | Local tool manifest. | Keep. |
| `.gitignore` (current 376 bytes) | **A** | Active. | **Phase 2: extend with 12-line patch from `GULIERP_GITIGNORE_POLICY.md`** to cover `node_modules/`, `dist/`, `.runtime-browser-profile/`, `.spec-workflow/`, `.stack-logs/`, `artifacts/`, `_evidence_trx/`, `TestResults/`, etc. |
| `.mcp.json` | **A** | Active MCP config. | Keep. |
| `meta-kim-post-copy.mjs` | **A** | Active runtime script. | Keep. |
| **NEW total (1.28 GB)** | | | **A=1.04 GB; C=440 MB; D=71 MB** |

### 2.3 Cross-cutting (both projects)

| Item | Class | Rationale | Action |
|---|:---:|---|---|
| **Connection strings** | **A** | Both projects have `appsettings*.json` with PG/SQLite templates. NEW has `Host=192.168.2.228;Database=gulierp_g2_003_test;Username=CHANGE_ME;Password=CHANGE_ME`. OLD has `DataSource=./GuliERP.Host.db`. | Phase 2: NEW already has the right template. OLD's SQLite string goes to archive. **NEVER share real passwords between two configs.** |
| **.NET runtime** | **A** | NEW uses system .NET 10.0.100 via `global.json`. OLD has its own vendored `.dotnet/` (800 MB). | Phase 2: install system .NET 10 SDK; remove OLD `.dotnet/`. |
| **Bootstrap scripts** | **A** | OLD has `bootstrap_local_dotnet10_adminnet_v5.ps1`. NEW has `tools/dev/start-stack.ps1` + `stop-stack.ps1` + 18 dev scripts. | Phase 2: NEW already supersedes. Archive OLD scripts. |
| **Seed data** | **A** | OLD has no `data/` directory. NEW has `data/bootstrap/reference/{system,tenant-template,mdm/dictionary,mapping}` (20 JSON files). | Keep NEW; OLD is N/A. |
| **Database files** | **D** | OLD has `GuliERP.Host.db` (3.3 MB SQLite). NEW has no DB file (uses NAS PG). | Phase 2: keep `GuliERP.Host.db` until the OLD process is killed; archive after. |
| **UI prototype** | **B** | OLD has `web/business/` (Razor views). NEW has `apps/web/` (React + Vite). | Phase 2: archive OLD UI; keep NEW. |

---

## 3. Final Target Directory Structure (Phase 2 Design)

The Phase 2 implementation will produce the following target layout. **This
section is a design proposal; no file moves happen in Phase 1.**

```
D:\guli\
├── projects\
│   └── gulierp-next\           ← The ACTIVE GuliERP Next project (NEW, becomes the SOLE canonical home)
│       ├── .git\               (A — keep, GitHub-pushed)
│       ├── .agents\            (A — keep, AI runtime)
│       ├── .claude\            (A — keep, Claude Code)
│       ├── .codex\             (A — keep, Codex)
│       ├── .cursor\            (A — keep, Cursor v1.7+)
│       ├── apps\
│       │   ├── api\            (A — keep, ASP.NET Core Identity API)
│       │   └── web\            (A — keep, React + Vite UI)
│       ├── artifacts\          (C/D — keep dir, prune contents)
│       │   ├── .gitignore      (already in .gitignore)
│       │   ├── build-recovery-tmp\   (C — delete)
│       │   ├── release-build\         (D — move to archive/release-build-2026-08-21/)
│       │   └── pg-query\              (C — delete)
│       ├── building-blocks\    (A — keep)
│       ├── data\               (A — keep, B2 deliverable)
│       ├── docs\               (A — keep)
│       │   ├── governance\      (A — add new audit report here)
│       │   │   └── GULIERP_PROJECT_MIGRATION_AUDIT_001.md  (this report)
│       │   ├── planning\       (A — B1/B2 plans)
│       │   ├── verification\    (A — B1/B2/B3 reports)
│       │   └── ...
│       ├── modules\            (A — keep, 8 domain modules)
│       ├── tests\              (A — keep, 12 test projects)
│       ├── tools\              (A — keep, Bootstrap + dev + discovery)
│       │   ├── GuliERP.Identity.Bootstrap\  (A)
│       │   ├── GuliERP.Mdm.Bootstrap\       (A — B1)
│       │   ├── dev\                         (A — 18 dev PS scripts)
│       │   ├── discovery\                   (A — capability index)
│       │   ├── .quarantine\                 (D — quarantined files)
│       │   └── check-runtime.ps1            (A — NEW, Phase 2 deliverable; see §5)
│       ├── AGENTS.md            (A — keep, Meta_Kim)
│       ├── CLAUDE.md            (A — keep)
│       ├── README.md            (A — keep)
│       ├── Directory.Build.props         (A)
│       ├── Directory.Packages.props      (A)
│       ├── global.json                    (A — pin SDK 10.0.100)
│       ├── GuliERP.slnx                   (A)
│       ├── dotnet-tools.json              (A)
│       ├── .gitignore                     (A — extended per §4.3)
│       ├── .mcp.json                      (A)
│       └── meta-kim-post-copy.mjs         (A)
│
├── archive\                    ← NEW directory (Phase 2): the only home for OLD content
│   └── gulierp-old\            ← Phase 2: rename of `D:\guli\gulierp\`
│       ├── README.md                       (D — explain the archive)
│       ├── .dotnet\                        (C — 800 MB, optional; see §4.2)
│       ├── src\                            (B — Admin.NET source)
│       │   └── GuliERP.Host\
│       │       └── GuliERP.Host.db         (D — keep SQLite; never run process against it)
│       ├── tests\                          (B — UnitTests + RuntimeSmoke + Integration + Route)
│       ├── poc\adminnet\                   (B — 3.5 GB vendored Admin.NET)
│       ├── web\                            (B — OLD Razor UI prototype)
│       ├── tools\dev-reverse\              (B — DEV metadata reverse-engineering)
│       ├── scripts\                        (D — 6 PS scripts)
│       ├── docs\                           (B — 14.9 MB historical docs; cherry-pick NEW-relevant)
│       ├── governance\                     (B — old governance notes)
│       ├── prompts\                        (C — single stale prompt)
│       ├── AGENTS.md                       (C — superseded by NEW Meta_Kim AGENTS.md)
│       ├── README_FIRST.md                 (C — old quickstart)
│       ├── Directory.Build.props           (B — old MSBuild shared)
│       ├── GuliERP.sln                     (C — old VS solution)
│       ├── bootstrap_local_dotnet10_adminnet_v5.ps1   (C)
│       ├── install_dotnet10_and_verify_adminnet_v2-v4.ps1   (C × 3)
│       ├── dotnet-install.ps1              (C — re-downloadable)
│       └── MIGRATION_NOTES.md              (A — generated by Phase 2; explains what was archived and why)
```

**Phase 2 sequence** (for human review, NOT executed by Mavis):

1. **Stop the OLD API** (PID 109480) — `Stop-Process -Id 109480` (with explicit user confirmation).
2. **Install system .NET 10 SDK** — verify `dotnet --version` returns `10.0.100` or later (with `rollForward: latestFeature`).
3. **Create `D:\guli\archive\`** directory.
4. **Move** `D:\guli\gulierp` → `D:\guli\archive\gulierp-old` (using `Move-Item`, not delete).
5. **Prune** `D:\guli\projects\gulierp-next\`:
   - Delete `.runtime-browser-profile/`, `.spec-workflow/`, `.stack-logs/`
   - Delete `artifacts/build-recovery-tmp/`, `artifacts/pg-query/`
   - Move `artifacts/release-build/` → `archive/gulierp-old/release-build-2026-08-21/`
   - Delete `apps/web/node_modules/`, `apps/web/dist/`
   - Delete `tests/_evidence_trx/`, `tests/*/TestResults/`
   - Run `dotnet clean` on the solution
6. **Extend `.gitignore`** (12-line patch from `GULIERP_GITIGNORE_POLICY.md`).
7. **Write `MIGRATION_NOTES.md`** in the archive (lists every move, every delete, every keep).
8. **Commit** the prune + `.gitignore` extension as a single commit.
9. **Push** to GitHub.

**Total estimated post-Phase-2 size**: ~700 MB (1.28 GB NEW − 440 MB prune + 9.5 GB OLD → 0.7 GB in NEW + 9.5 GB in archive).

---

## 4. Unified .NET Runtime Plan (Phase 2 Design)

### 4.1 Target: System .NET 10 SDK

The single source of truth for .NET 10 is the **system-wide install** managed
by the official Microsoft installer or `winget`. The OLD vendored `.dotnet/`
is removed; the NEW project consumes the system SDK via `global.json`.

### 4.2 `global.json` (already in NEW, do not change)

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

This pins the **minimum** feature band (10.0.10x). `rollForward: latestFeature`
allows any 10.0.1xx SDK to satisfy the requirement. The fix band (10.0.100)
and major (10.0.0) are still locked.

### 4.3 `.gitignore` extension (12 lines, from `GULIERP_GITIGNORE_POLICY.md`)

The current `.gitignore` is 376 bytes and does NOT cover `node_modules/`,
`dist/`, `.runtime-browser-profile/`, etc. The patch:

```gitignore
# .NET
bin/
obj/
*.user
*.suo
.vs/

# Build artifacts
artifacts/build-recovery-tmp/
artifacts/pg-query/
artifacts/release-build/
artifacts/**/publish/

# Runtime caches
.runtime-browser-profile/
.spec-workflow/
.stack-logs/

# UI
apps/web/node_modules/
apps/web/dist/

# Tests
tests/_evidence_trx/
tests/*/TestResults/
tests/*/bin/
tests/*/obj/

# Seeds (placeholder; per-tenant B2 data is committed)
data/bootstrap/**/runtime/
```

This brings the git-tracked working set from ~1.28 GB to ~600 MB (build
outputs and caches excluded).

### 4.4 Environment variable conventions

| Variable | Purpose | Phase 2 source of truth |
|---|---|---|
| `DOTNET_ROOT` | Override .NET runtime root (default: system path) | `C:\Program Files\dotnet\` (system) |
| `DOTNET_NOLOGO` | Suppress the .NET banner | `true` (set in dev shell profile) |
| `DOTNET_CLI_TELEMETRY_OPTOUT` | Disable .NET CLI telemetry | `1` (set in dev shell profile) |
| `GULIERP_MDM_DICTIONARY_SEED_PATH` | Seed directory for B1 CLI | Default: `data/bootstrap/reference/mdm/dictionary/` (relative to CWD) |
| `ConnectionStrings__GuliERP` | PG connection string for B1 CLI / NEW API | `Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=...;Password=...` (operator-supplied) |

### 4.5 Runtime resolution rules

1. **The `dotnet` CLI** on PATH points to the system install (`C:\Program Files\dotnet\dotnet.exe`).
2. **`global.json`** in `gulierp-next/` pins `10.0.100` with `latestFeature` rollForward.
3. **The OLD `.dotnet/`** is removed in Phase 2; no fallback to a vendored SDK.
4. **System PATH** must include `C:\Program Files\dotnet\` (or equivalent) so that `dotnet --version` works in any shell.

### 4.6 Migration steps (Phase 2, human-approved)

1. **Verify** system .NET 10 SDK is installed: `dotnet --version` (expect `10.0.100` or higher 10.0.1xx).
2. **Verify** `global.json` in `gulierp-next/` is consistent: `dotnet --list-sdks` should show at least one 10.0.1xx.
3. **Remove** `D:\guli\gulierp\.dotnet\` (800 MB) — verify nothing in `D:\guli\projects\gulierp-next\` references the OLD `.dotnet`.
4. **Delete** the OLD `dotnet-install.ps1` and `install_dotnet10_and_verify_*.ps1` (no longer needed).
5. **Document** the unified runtime in `D:\guli\projects\gulierp-next\docs\governance\GULIERP_RUNTIME_SETUP.md` (NEW, Phase 2 deliverable).

---

## 5. Runtime Check Script Design (Phase 2 Deliverable)

**Path**: `D:\guli\projects\gulierp-next\tools\dev\check-runtime.ps1`

**Purpose**: a one-shot diagnostic that confirms the operator's machine is
ready to build, run, and seed the GuliERP Next project. It does NOT start
any process; it only reports.

### 5.1 Checks (sections)

```powershell
# Section 1: .NET runtime
- $env:DOTNET_ROOT                            # (should be unset or system path)
- dotnet --version                            # (should be 10.0.1xx)
- dotnet --list-sdks                          # (should include 10.0.1xx)
- dotnet --list-runtimes                      # (should include 10.0.x AspNetCore)

# Section 2: global.json consistency
- $gulierpNextRoot\global.json                # (must exist, pin to 10.0.100)

# Section 3: NO old runtime paths
- Test-Path "D:\guli\gulierp\.dotnet"         # (should be False in Phase 2)

# Section 4: Database
- Test-NetConnection 192.168.2.228 -Port 5432  # (should be True)
- $env:ConnectionStrings__GuliERP             # (should be set OR .env file exists)
- (if password missing) WRITE WARN: "B1 CLI seed will fail without --connection-string"

# Section 5: Port availability
- Get-NetTCPConnection -LocalPort 5000 -State Listen   # (should be empty for NEW API)
- Get-NetTCPConnection -LocalPort 5173 -State Listen   # (should be empty for NEW UI)

# Section 6: No stale processes
- Get-Process -Name "dotnet" -ErrorAction SilentlyContinue  # (should be empty OR list the NEW API)

# Section 7: Repository health
- Test-Path "D:\guli\projects\gulierp-next\global.json"
- Test-Path "D:\guli\projects\gulierp-next\GuliERP.slnx"
- Test-Path "D:\guli\projects\gulierp-next\data\bootstrap\reference\mdm\dictionary\DOC_STATUS.json"
- git status --short                          # (should show the expected uncommitted B1/B2/B3 work)

# Section 8: Disk space
- Get-PSDrive D | Select-Object Used, Free    # (need >= 2 GB free for builds)
```

### 5.2 Exit codes

| Code | Meaning |
|---:|---|
| 0 | All checks passed |
| 1 | .NET runtime missing or wrong version |
| 2 | `global.json` missing or inconsistent |
| 3 | Old `.dotnet` directory still exists (Phase 2 not complete) |
| 4 | NAS PG unreachable |
| 5 | Port 5000 already in use (stale OLD API or other process) |
| 6 | Stale dotnet processes (operator must run `Stop-Process` with explicit confirmation) |
| 7 | Repository health issue (missing files, dirty state, or wrong branch) |
| 8 | Disk space insufficient |

### 5.3 Sample output (Phase 2 target)

```text
=== GuliERP Next Runtime Check ===
Date: 2026-XX-XX HH:MM:SS (Asia/Shanghai)
Project: D:\guli\projects\gulierp-next

[1/8] .NET Runtime ........................... ✅ PASS
  dotnet --version: 10.0.100
  Installed SDKs: 10.0.100
  Installed runtimes: Microsoft.AspNetCore.App 10.0.0, Microsoft.NETCore.App 10.0.0

[2/8] global.json ............................ ✅ PASS
  Found: D:\guli\projects\gulierp-next\global.json
  Pinned SDK: 10.0.100 with rollForward: latestFeature

[3/8] Old runtime paths ...................... ✅ PASS
  D:\guli\gulierp\.dotnet: not present (Phase 2 complete)

[4/8] Database (NAS PG) ...................... ⚠️ WARN
  192.168.2.228:5432: reachable ✅
  ConnectionStrings__GuliERP: NOT SET (use --connection-string for B1 CLI)

[5/8] Port availability ...................... ✅ PASS
  :5000: free
  :5173: free

[6/8] Stale processes ......................... ✅ PASS
  dotnet processes: 0 (no stale OLD API)

[7/8] Repository health ....................... ⚠️ WARN
  HEAD: 9684985 (master)
  Dirty: 8 B1 untracked + 1 modified + 9 B2 untracked + 1 B3 untracked
  Expected: B1/B2/B3 uncommitted (per current state)

[8/8] Disk space ............................. ✅ PASS
  D: 2.5 GB free (>= 2 GB required)

OVERALL: 6/8 PASS, 2/8 WARN, 0 FAIL
Exit code: 0
```

### 5.4 Design intent

- **Read-only**: the script never modifies anything; it only reports.
- **Idempotent**: can be re-run any time without side effects.
- **Composable**: each section is independent; the operator can run individual sections via `-Section` parameter (Phase 2.1 enhancement).
- **Color-coded**: PASS=green, WARN=yellow, FAIL=red.
- **CI-friendly**: emits a single line per check, parseable by `select-string`.

---

## 6. What MUST Happen First (Phase 1 → Phase 2 Transition)

Before any file move / delete, the operator MUST:

1. **Read this audit** end-to-end.
2. **Confirm the classification** (especially C items — once deleted, they are gone).
3. **Stop the OLD API** (PID 109480) — this is the **only irreversible step**
   (the in-memory binary cannot be recovered once killed). The DB file
   (`GuliERP.Host.db`) is preserved on disk.
4. **Back up the OLD repo** to `D:\guli\archive\gulierp-old\` (a copy, not
   a delete). This is reversible — the original can be restored.
5. **Run `check-runtime.ps1`** (Phase 2 deliverable) to confirm the new
   setup is functional.
6. **Commit + push** the Phase 2 changes (prune + `.gitignore` + runtime
   check script + audit report).

---

## 7. Risks & Caveats

### 7.1 PID 109480 is still running

The OLD API process (PID 109480, started 16:25:43) is **still in memory**.
Its binary path (`D:\guli\gulierp\apps\api\GuliERP.Api\bin\Release\net10.0\GuliERP.Api.dll`)
no longer exists on disk, but the process is still serving requests on
`http://127.0.0.1:5000`. **The user MUST explicitly approve** killing
this process before Phase 2 file moves.

### 7.2 The OLD `AGENTS.md` is in Chinese

The OLD `AGENTS.md` is encoded as Chinese (UTF-8, no BOM, 1,269 characters).
The NEW `AGENTS.md` is in English (Meta_Kim, 25 KB). The OLD one is
**superseded** but should be archived (NOT deleted silently) so a future
maintainer can see what the OLD project expected.

### 7.3 The OLD `data/` directory is missing

OLD `D:\guli\gulierp\` has NO `data/` directory. All seed data lives in
NEW `D:\guli\projects\gulierp-next\data\`. The OLD `MdmSeed.cs` UOM seed
references `data/bootstrap/reference/system/uom.json` (a relative path)
and the `uom.json` file is in the NEW project, NOT the OLD. **This is
a known incompatibility** — the OLD API can never read the NEW seed data.

### 7.4 The 3 pre-existing Mdm.Tests fails (carried over)

The 3 pre-existing Mdm.Tests fails (B1 report §7) are not affected by
this audit. They are independent of the project layout.

### 7.5 The OLD `.git` may have uncommitted work

OLD `D:\guli\gulierp` has 2 modified files (`Database.json`, `appsettings.json`)
that are uncommitted (B1 report §8). These changes are likely related to
PG connection string attempts in the OLD format. They should be reviewed
before Phase 2 archive — they may contain operator-typed passwords.

### 7.6 PowerShell execution policy

Phase 2 will run several PowerShell scripts (`Move-Item`, `Stop-Process`,
the new `check-runtime.ps1`). The current session uses Windows PowerShell
5.1 (PowerShell ISE's host) and may have an execution policy that blocks
unsigned scripts. The operator should ensure `Set-ExecutionPolicy -Scope
CurrentUser -ExecutionPolicy RemoteSigned` is set before Phase 2.

### 7.7 The OLD `poc/adminnet/` is 3.5 GB

Archiving 3.5 GB takes time and disk space. If the archive drive is small,
the operator should consider compressing the archive (`Compress-Archive
-Path D:\guli\gulierp\poc\adminnet -DestinationPath D:\guli\archive\adminnet-poc-2026-08-25.zip`)
before moving. The compressed size is typically 100-300 MB.

### 7.8 The `data/` directory in NEW is untracked (carried over)

The NEW `data/` directory is untracked (created by previous goals, never
committed). Phase 2 should commit it as part of the B2 deliverable (per
B2 report §9). The audit's classification puts `data/` in **A** for NEW,
but the actual commit must be a separate decision.

---

## 8. Commit & Push — NOT EXECUTED

Per the brief: **"NO COMMIT / NO PUSH"**.

**Working tree changes from B3** (this report only — no other changes):

```
?? docs/governance/GULIERP_PROJECT_MIGRATION_AUDIT_001.md
```

**Recommended commit boundary** (for human review, NOT executed by Mavis):

1. **Commit: Phase 1 audit report** (1 file)
   - `docs/governance/GULIERP_PROJECT_MIGRATION_AUDIT_001.md`

   Suggested commit message:
   ```
   docs(governance): add GuliERP project migration audit 001

   Phase 1 audit only. Inventories D:\guli\gulierp (OLD, 9.5 GB, Admin.NET +
   SQLite) and D:\guli\projects\gulierp-next (NEW, 1.28 GB, ASP.NET Core +
   PostgreSQL). Every item classified A/B/C/D.

   Design (no execution):
   - Final target directory structure (NEW active + archive/gulierp-old/ retired)
   - Unified .NET runtime (system .NET 10.0.100 via global.json)
   - Runtime check script (tools/dev/check-runtime.ps1)

   Per brief: NO file moves, NO deletes, NO commit/push during Phase 1.
   Phase 2 sequence documented in §3 (move + prune + .gitignore + commit).
   ```

**Push**: Only after the audit commit is reviewed and approved.

---

## 9. Final Statement

`GULIERP_PROJECT_CONSOLIDATION_001` Phase 1 is **complete and verified**:

- ✅ Both projects scanned end-to-end (9.50 GB OLD + 1.28 GB NEW)
- ✅ Every item classified A/B/C/D
- ✅ Final target directory structure designed
- ✅ Unified .NET runtime plan designed (system .NET 10.0.100)
- ✅ Runtime check script designed (`check-runtime.ps1`, 8 sections, exit codes 0-8)
- ✅ Phase 2 sequence documented (stop PID 109480 → move → prune → commit)
- ✅ 0 file moves / 0 deletes / 0 commits / 0 pushes (per brief)
- ✅ 0 production code / database / schema / API / UI changes

**Final gate**: `GULIERP_PROJECT_CONSOLIDATION_AUDIT_READY`
**Next step** (Phase 2, operator-approved): stop PID 109480 → move OLD to
`archive/gulierp-old/` → prune NEW → extend `.gitignore` → commit.
