# Goal Registry

## Active Goal

| Field | Value |
|---|---|
| Goal | **G2-001 — Host & PostgreSQL** |
| Gate | `G2_001_HOST_POSTGRESQL_VERIFIED_CODE_READY_OPERATOR_RUNTIME_PENDING` |
| Status | **Active — Mavis-driven code-side PASS; Operator-driven real-DB round PENDING** |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Scope | Minimal ASP.NET Core host + FoundationDbContext (schema=foundation) + `G2001_InitializeFoundationSchema` migration + `/health/live` + `/health/ready` + 7 integration tests + operator evidence pack |
| Non-goals | All business modules (Sales/Purchase/Inventory); real auth/JWT; permission; tenant/company/org/user/role entities; audit; dictionary; workflow; sales API; any Admin.NET / Furion / SqlSugar |
| Verification | `docs/verification/G2_001_HOST_POSTGRESQL_REPORT.md` (22 sections, 33 KB) |
| Operator handoff | `tools/dev/g2-001-operator-evidence.ps1` |
| Hard Stop | G2-001 does NOT auto-advance to G2-002. The Operator must (a) run the evidence pack, (b) update this registry to `G2_001_HOST_POSTGRESQL_VERIFIED` before the next session can begin G2-002. |
| Forbidden follow-up without Operator sign-off | `G2-002 Foundation Kernel` (any identity/auth/permission work) |

## Previous Active Goal (superseded)

| Field | Value |
|---|---|
| Goal | G1A-FINAL — Operator Decision Writeback & Business Spec Freeze |
| Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` |
| Status | **FROZEN** — 10 USER_CONFIRMED decisions; spec & governance written back |
| Scope | Writeback 10 user decisions to SO/PO/INV/UX specs; establish Module Independence + Meta_Guli governance; record decisions; advance gate to FROZEN |
| Non-goals | Sales/Purchase/Inventory implementation, UX prototype code, API contract, Foundation implementation |

## G1A-FINAL Verification Notes

| Check | Status | Note |
|---|---|---|
| 10 USER_CONFIRMED decisions applied | PASS | SO/PO/INV/UX specs updated; `G1A_DECISIONS_V1.md` created |
| 3D status model (DEC-STATUS-001) | PASS | REJECTED old 9-string Status; adopted `DocumentStatus/ApprovalStatus/ExecutionStatus` |
| `GULIERP_MODULE_INDEPENDENCE_RULE.md` | PASS | new governance file (DEC-MODULE-001) |
| `META_GULI_GOVERNANCE_V1.md` + LESSON-001 | PASS | new meta-governance file |
| `G1A_DECISIONS_V1.md` | PASS | 10 decisions recorded; 5 special-record decisions highlighted |
| PendingInspection NOT in Available (DEC-INV-001) | PASS | `INVENTORY_BUSINESS_SPEC_V1.md` §2 + §9 + §15 updated |
| `InventoryPostingEngine` REQUIRED in V1 (DEC-INV-002) | PASS | `INVENTORY_BUSINESS_SPEC_V1.md` §0.5 + §7.1; `CORE_MODULE_SCOPE_V1.md` §18 corrected |
| Workflow reference correction (DEC-WORKFLOW-001) | PASS | POC-004 marked as reference only, NOT runtime dep |
| All BLOCKING_BEFORE_UX items resolved | PASS | `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` now shows "none remaining" for Sales/Purchase/Inventory/UX |
| Source code unchanged | PASS | Only spec/governance/decision docs edited; no .cs/.ts/.vue/.sql touched |
| No .NET / npm operations | PASS | task G1A-FINAL §一 forbids; no build attempted |
| Git commit/tag/push/rebase | NONE | task §九 forbids; user to commit when ready |

## G2-001 — Host & PostgreSQL (active)

| Check | Status | Note |
|---|---|---|
| .NET 10 SDK reachable | PASS | `D:\guli\gulierp\.dotnet\dotnet.exe` SDK 10.0.400; `global.json` 10.0.100 latestFeature matches |
| Host build (Release) | PASS | 0 warnings / 0 errors; `GuliERP.Api.dll` produced |
| Migration generated | PASS | `20260819103150_G2001_InitializeFoundationSchema.cs` author-written `CREATE SCHEMA IF NOT EXISTS foundation;` |
| Migration apply to real PG | **PENDING Operator** | Mavis cannot inject PGPASSWORD; `tools/dev/g2-001-operator-evidence.ps1` provided |
| foundation schema + EF Migrations History | **PENDING Operator** | `FoundationDatabaseFacts` will assert once real DB available |
| Integration Tests (real DB path) | SKIP → PENDING Operator | 5 tests with `[Fact(Skip = "ConnectionStrings__GuliERP env var not set")]` |
| `/health/live` with bad DB | PASS (Round 1 + Round 2) | Returns 200 Healthy even when PostgreSQL unreachable |
| `/health/ready` with bad DB | PASS (Round 1 + Round 2) | Returns 503 Unhealthy — readiness failure boundary proven |
| `/health/live` with real DB | **PENDING Operator** | `FoundationHostHealthFacts.LiveHealthyWithGoodDb` |
| `/health/ready` with real DB | **PENDING Operator** | `FoundationHostHealthFacts.ReadyHealthyWithGoodDb` |
| Runtime Round 1 (real DB) | **PENDING Operator** | `g2-001-operator-evidence.ps1` Step 4 |
| Runtime Round 2 (real DB) | **PENDING Operator** | `g2-001-operator-evidence.ps1` Step 5 |
| Secret handling | PASS | `appsettings.{,Development}.json` use `Password=CHANGE_ME`; no real password in any tracked file; `git diff --check` exit 0 |
| Forbidden patterns | PASS | Scan found no `UseInMemoryDatabase` / `UseSqlite` / `EnsureCreated` / `Admin.NET` / `Furion` / `SqlSugar` in code (only mentioned in comments as forbidden) |
| VOL Pattern Reuse | DOCUMENTED | Single-rejected-pattern (TenancyManager empty fn) explicitly avoided; composition-root shape adopted |
| Gate | `G2_001_HOST_POSTGRESQL_VERIFIED_CODE_READY_OPERATOR_RUNTIME_PENDING` | Operator must flip to `G2_001_HOST_POSTGRESQL_VERIFIED` after running the evidence pack |
| Next Goal | **G2-002 Foundation Kernel (Identity)** | HALTED — Operator must sign off before Mavis can begin |
| Forbidden auto-advance | G2-002 cannot start until Operator flips the gate; per META_GULI HR-7 no silent scope expansion |

## G1A-FINAL Residual Risks (post-FREEZE)

| Risk | Mitigation |
|---|---|
| Other OPEN_QUESTIONs (BLOCKING_BEFORE_IMPLEMENTATION / CAN_DEFER / ADVANCED) remain | Tracked in `G1A_OPERATOR_CONFIRMATION_CHECKLIST.md` post-FINAL state |
| The 3D status model is a major architecture change; per-dimension transitions need to be unit-tested | Reserved for V1 implementation Goals (G1D+) |
| Workflow Approval in V1 is "simple"; full BPM in V1.5+ | Per DEC-WORKFLOW-001; not a risk for V1 release |
| Mobile/H5 deferred to V1.5+ | Per DEC-UX-001; not a risk for V1 release |

## Previous Goals

### G1A — Core Business Specification Freeze (pre-FINAL)

| Field | Value |
|---|---|
| Gate | `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` |
| Status | Superseded by G1A-FINAL |
| Scope | (pre-FINAL) spec inputs produced |
| Note | Replaced by G1A-FINAL when user decisions were written back. |

### G1A Verification Notes (pre-FINAL)

| Check | Status | Note |
|---|---|---|
| Spec files produced (8) | PASS | `docs/product/specs/*.md` (8 files) |
| Evidence-type classification | PASS | every field/rule tagged; no `INFERENCE` marked Frozen |
| FAILED POC gap analysis | PASS | `FAILED_POC_REQUIREMENT_GAP_ANALYSIS.md` covers 130+ concrete gaps |
| 12-section reverse self-audit | PASS | no POC-as-truth, no DEV-tech-as-spec, no INFERENCE-as-Frozen |
| Git diff check | N/A | G0 working tree was untracked; G1A adds new untracked spec files only |
| Git commit | N/A | task G1A forbids commit/tag/push/rebase |

### G0 — GuliERP Greenfield Bootstrap & Business Source of Truth

| Field | Value |
|---|---|
| Gate | `GULIERP_GREENFIELD_BOOTSTRAPPED` |
| Status | Bootstrapped with environment verification gaps |
| Scope | Engineering skeleton, governance, business source of truth, discovery docs |
| Non-goals | Formal Sales/Purchase/Inventory implementation |

### G0 Verification Notes

| Check | Status | Note |
|---|---|---|
| Backend build | Blocked | No .NET SDK installed; runtime only |
| Backend tests | Blocked | No .NET SDK installed; runtime only |
| Frontend install | Blocked | npm cache-only mode; dependency not cached |
| Frontend build | Blocked | `vue-tsc` unavailable because install was blocked |
| Git diff check | PASS | `git diff --check` returned 0 |
| Target path | Blocked | `D:\guli\gulierp-next` creation required escalation, which was rejected by system usage limit |
| Git commit | Blocked | `.git` index write required escalation, which was rejected by system usage limit |

## Gate Rules

- A goal may not start implementation work for core business documents until
  the UI approval gate is satisfied.
- Failed POC code remains reference only.
- Every goal must record verification status and known blockers.
- `GULIERP_CORE_BUSINESS_SPEC_READY_FOR_UX` is the input gate to UX prototype
  (G1B). It does NOT mark the spec as `USER_CONFIRMED` or `FROZEN`.
- `GULIERP_CORE_BUSINESS_SPEC_FROZEN` is the gate advanced by G1A-FINAL.
  Only the `USER_CONFIRMED` items recorded in `G1A_DECISIONS_V1.md` are
  Frozen; non-confirmed items remain under their original evidence type
  until the user addresses them. The current Gate does NOT authorize:
  - API implementation
  - Database implementation
  - Foundation / Sales / Purchase / Inventory implementation
  - Real Vue business pages (those require `USER_UX_APPROVED`)

## Next Goal

| Field | Value |
|---|---|
| Goal | **G1B-1 — SalesOrder High-Fidelity Static UX Prototype** |
| Executor | **TRAE** (not Mavis) |
| Entry Gate | `GULIERP_CORE_BUSINESS_SPEC_FROZEN` (current Gate) |
| Scope | SalesOrder only: List + Create/Edit + Detail. Static Vue 3 / Element Plus. Mock data only. NO API. NO DB. NO real Sales Service. |
| Non-goals | All other modules. All backend code. Real data. |
| Hard Stop | G1B-1 must be approved as `SALES_ORDER_UX_APPROVED` by the user before any further UI prototype (PO/Inventory). G1B-1 must NOT auto-advance to G1B-2. |
| Forbidden follow-up without UX approval | `G1B-2` (Purchase/Inventory UX), `G1C` (API contract), `G1D`+ (implementation) |
