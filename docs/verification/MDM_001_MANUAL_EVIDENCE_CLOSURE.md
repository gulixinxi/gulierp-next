# MDM-001 Manual Evidence Adjudication & Closure

> **Scope**: Final closure of the MDM-001 Goal via human-supervised
> evidence adjudication. Closes the Goal with a positive gate
> (`MDM_001_REAL_MASTER_DATA_VERIFIED`) and registers a single
> non-blocking technical-debt item for the acceptance harness
> (`MDM_ACCEPTANCE_HARNESS_RESUME_BACKFILL_DEFERRED`).
>
> **Authoritative evidence inputs** (per R7 + R8 + R9 + Operator transcripts):
>
> | # | Evidence | Source class | Status |
> |---|---|---|---|
> | 1 | `dotnet build GuliERP.slnx -c Release` | Machine build | PASS (22 projects, 0 warnings, 0 errors) |
> | 2 | `dotnet test tests/GuliERP.Mdm.Tests` | Machine unit | **57 / 57 PASS** (R3 +7, R4 +12, R6 +6, R7 +6 Service Boundary + 2 Seed walk-up) |
> | 3 | `dotnet test tests/GuliERP.Identity.Tests` | Machine unit | **21 / 21 PASS** |
> | 4 | `dotnet test tests/GuliERP.Foundation.Tests` | Machine unit | **44 / 44 PASS** |
> | 5 | `dotnet test tests/GuliERP.Mdm.IntegrationTests` × 5 rounds | Machine integration (Operator-side PG) | **50 / 50 PASS** (Round 1 10/10, Round 2 10/10, Round 3 10/10, Round 4 10/10, Round 5 10/10) |
> | 6 | `dotnet ef database update` | Machine migration | PASS (already up to date, idempotent) |
> | 7 | API Runtime Round 1 host start | Operator transcript | PASS (PID 1620, listening on `http://127.0.0.1:5179`) |
> | 8 | `/health/live` | Machine endpoint probe | **200** |
> | 9 | `/` (root banner) | Machine endpoint probe | **200** |
> | 10 | `/health/ready` | Machine endpoint probe | **200** |
> | 11 | R7 → R8 → R9 git diff | Code change audit | 0 `.cs` change in `modules/`, `apps/api/`, `Migrations/`, `tests/GuliERP.*.cs`, `tools/GuliERP.*.cs`; only `tools/dev/Mdm001Acceptance.Harness.ps1`, `tools/dev/mdm-001-final-acceptance.ps1`, `tools/dev/mdm-001-final-acceptance-selftest.ps1`, and `docs/**` were modified |
> | 12 | Operator transcript 2026-08-21 15:18-15:59 +0800 | Operator console | `OPERATOR_TRANSCRIPT_REPORTED`; canonicalized in `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md` (R9) |
> | 13 | API host log `tests/_evidence_trx/api_host_round1_20260821155926.log` | Machine log | 6,788 bytes, SHA256 `C855F079C3AB366A8631BE85B8DC27A63FE607993BBBBF51F6C44AEF054FFDDA` |

## 1. Adjudication

**Adjudicator**: project owner (per direct instruction, 2026-08-21 17:51 +0800).

**Decision**: The accumulated evidence (1-13) is sufficient to certify
that the MDM-001 vertical slice (UOM + ItemCategory + Item) is
correctly implemented against real PostgreSQL, with the canonical
Service Boundary enforced and seed data loaded idempotently. The
Harness-side automation gaps described below do **not** invalidate
the business correctness of the MDM module.

| Decision axis | Verdict |
|---|---|
| Build (1) | ✅ Pass |
| Unit tests (2-4) | ✅ Pass (122/122) |
| Integration 5 rounds (5) | ✅ Pass (50/50) — Operator-side PG, real runs |
| Migration (6) | ✅ Pass — applied/already up to date, idempotent |
| API Host boot (7) | ✅ Pass — PID 1620, listening |
| Health endpoints (8-10) | ✅ Pass — all three 200 |
| R7→R8→R9 no business code change (11) | ✅ Pass — git diff zero `.cs` in business paths |
| Operator transcript (12) | ✅ Recorded + canonicalized (R9 Mode B) |
| API host log (13) | ✅ On disk, SHA256 verified |
| **Overall Gate** | **`MDM_001_REAL_MASTER_DATA_VERIFIED`** |

## 2. Non-blocking Technical Debt

**`MDM_ACCEPTANCE_HARNESS_RESUME_BACKFILL_DEFERRED`** — registered, **not** part of MDM-001 acceptance.

**Scope** (deferred to a future acceptance-harness consolidation Goal, NOT MDM-002/DocumentKernel/SalesOrder):

1. The API automatic Stop/Restart Round 2 did not produce final machine
   evidence — the R7 harness aborted on `Stop-ApiHost: The term
   'Stop-ApiHost' is not recognized` before Round 2 could start.
2. R7 already produced one real API start with all three 200 health
   endpoints, and yesterday (2026-08-20) produced an independent
   `GuliERP.Api.dll` start log — together these two facts substitute
   for the missing Round 2.
3. R8 + R9 added `-ResumeApiRuntime` switch and Operator Transcript
   Backfill to repair the harness, but neither has been re-executed
   on Operator's terminal in this session; the harness repair is
   therefore a **harness-tooling debt**, not a **MDM-001 business
   debt**.

**Why this is non-blocking**:
- The MDM business module (Domain / Application / Infrastructure /
  Host) is independently verified by:
  - 122/122 unit tests
  - 50/50 integration tests on real PG
  - idempotent migration apply
  - one real API start with three 200 health endpoints
  - zero R7→R8→R9 business code drift
- The 2nd API Runtime Round is a **harness retry safety check**, not
  a **business correctness check**. The 1st Round's three 200 plus
  the prior independent start log already prove the API can boot
  and serve health endpoints against the real PG.

**Future Goal** (not started, not scheduled):
- Goal: **Consolidate the MDM-001 acceptance harness into the
  cross-module acceptance framework** (unified operator evidence
  pipeline + canonical transcript backfill + multi-round API
  runtime + machine TRX writer for all `dotnet test` suites).
- This is the right place to fix Round 2 automation, the
  `--logger trx` gap, and the canonical transcript dual-mode
  verification — together, in one place, with all 3 modules
  (MDM / Identity / Foundation) on equal footing.

## 3. What is now closed vs. still open

| Item | Status under MDM-001 |
|---|---|
| Build | CLOSED — 22 projects, 0/0 |
| MDM unit | CLOSED — 57/57 |
| Identity unit | CLOSED — 21/21 |
| Foundation unit | CLOSED — 44/44 |
| MDM integration × 5 rounds | CLOSED — 50/50 |
| Migration | CLOSED — applied, idempotent |
| API Round 1 | CLOSED — PID 1620, three 200 |
| Service Boundary tenant isolation | CLOSED — `IMdmService` only, 6 [Fact] lock |
| Seed idempotency | CLOSED — `ResolveSeedFilePath` 3-tier resolution |
| API Round 2 (harness retry) | **DEFERRED** to harness consolidation Goal |
| `--logger trx` per-round TRX writer | **DEFERRED** to harness consolidation Goal |
| Dual-mode prior evidence verification (Mode A / Mode B) | R9 added; **deferred verification** to harness consolidation Goal |

## 4. Trust boundary

This manual closure **does not**:
- Re-run any test (Agent has no PG).
- Re-run any API.
- Re-run any harness.
- Modify any business code, migration, integration test, or frontend.
- Modify any PowerShell script (per instruction §本轮禁止).
- Start the next Goal (MDM-002 / DocumentKernel / SalesOrder).

This manual closure **does**:
- Upgrade the GOAL_REGISTRY Gate from
  `MDM_001_TRANSCRIPT_BACKFILL_VERIFIED` to
  `MDM_001_REAL_MASTER_DATA_VERIFIED`.
- Register `MDM_ACCEPTANCE_HARNESS_RESUME_BACKFILL_DEFERRED` as a
  non-blocking technical-debt item, pointing to this report.
- Make one atomic docs-only commit.

## 5. Report path

| Document | Purpose |
|---|---|
| `docs/verification/MDM_001_MANUAL_EVIDENCE_CLOSURE.md` (this file) | Manual adjudication + final closure |
| `docs/verification/MDM_001_FINAL_ACCEPTANCE_READINESS_REPORT.md` (R7) | 11-step harness readiness + 122/122 unit + 11-step procedure |
| `docs/verification/MDM_001_POSTGRES_INTEGRATION_STABILIZATION_REPORT.md` (R6) | Tenant-context parallel integration stabilization |
| `docs/verification/MDM_001_R7_OPERATOR_TRANSCRIPT_EVIDENCE.md` (R9) | Canonical R7 Operator transcript backfill |
| `docs/verification/MDM_001_R7_TRANSCRIPT_BACKFILL_RESUME_REPORT.md` (R9) | R9 transcript backfill + R8 report correction |
| `docs/verification/MDM_001_API_RUNTIME_RESUME_FIX_REPORT.md` (R8, R9-corrected) | API host lifecycle fix + R8 readiness |
| `docs/verification/MDM_001_OPERATOR_EVIDENCE_CLOSURE_REPORT.md` (R1) | R1 read-only audit + ENV_BLOCKED honest disclosure |
| `docs/governance/GOAL_REGISTRY.md` | Active Goal gate string + Status line |

## 6. Decision

**`MDM_001_REAL_MASTER_DATA_VERIFIED`** — MDM-001 is closed.

After this commit, MDM-001 is no longer the active Goal. The
**next** Goal (only on explicit user authorization) is
**MDM-002 — BusinessPartner / Warehouse / Location**.
