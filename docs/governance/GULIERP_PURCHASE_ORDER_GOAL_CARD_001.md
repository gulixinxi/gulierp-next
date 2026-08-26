# Goal Card: GULIERP_PURCHASE_ORDER_001

| Field | Value |
|---|---|
| **Goal ID** | `GULIERP_PURCHASE_ORDER_001` |
| **Title** | Develop Purchase Order (PO) module — first business development under V1 5-role protocol |
| **Status** | **PROPOSAL** (awaiting user ratification in TASK 4 simulation) |
| **Author** | Mavis (M3 / mavis), acting as AI Software Factory Architect |
| **Date** | 2026-08-25 (Asia/Shanghai) |
| **Phase** | 1 (Goal) — completed for this Goal Card |
| **Per Brief** | No code. No DB. No migration. Design only. |

> **This Goal Card is a SIMULATION**, not an active Goal. It is
> designed to demonstrate the V1 5-role protocol's Phase 1 (Goal)
> output for a realistic next business module. The user can adopt
> this Goal Card as the actual `GULIERP_PURCHASE_ORDER_001` Goal
> after the V1 protocol is ratified.

---

## 0. Goal Identity

| Field | Value |
|---|---|
| Goal ID | `GULIERP_PURCHASE_ORDER_001` |
| Title | Purchase Order (PO) module — first business vertical under V1 5-role protocol |
| Phase | 1 (Goal) → 2 (Architecture) → 3 (Execution) → 4 (Review) → 5 (Verification) → 6 (Evolution) |
| Lane | Procurement (new) |
| Triggered by | User intent: "开发采购订单模块" (develop the Purchase Order module) |
| Entry gate | `GULIERP_GITHUB_BASELINE_READY` (achieved at `9e4ec48`); `MDM_000_MASTER_DATA_CONVENTION_FROZEN` (V1 freeze); `G2_DOCNO_VERIFIED` (NumberingRule runtime proven) |
| Architecture predecessors | `G2_FOUNDATION_ARCHITECTURE_V1_DRAFT.md`, `G2_API_STANDARD_V1_DRAFT.md`, `G2_APPROVAL_WORKFLOW_BOUNDARY_V1_DRAFT.md`, `G2_POSTGRESQL_ENGINEERING_STANDARD_V1_DRAFT.md` |
| Business model | `GULIERP_CODE_PIPELINE_DESIGN_V1.md`, `GULIERP_FOUNDATION_CODE_PIPELINE_MODEL_V1.md` |
| Source of truth | `docs/architecture/MDM_000_MASTER_DATA_CONVENTION_V1.md` |

---

## 1. Owner / Status

| Field | Value |
|---|---|
| Phase 0 intent (Human) | "开发采购订单模块" (User message) |
| Phase 1 Goal (GPT) | This Goal Card |
| Status (current) | **PROPOSAL — awaiting user ratification in TASK 4 simulation** |
| Future status transitions | APPROVED → IN-FLIGHT (Phase 3) → VERIFIED (Phase 5) → COMMITTED (Phase 6) → EVOLVED |
| Estimated size | 2-3 weeks (1 commit per sub-module) |

---

## 2. Agent 分工 (5-role)

| Role | Phase 1 (Goal) | Phase 2 (Architecture) | Phase 3 (Execution) | Phase 4 (Review) | Phase 5 (Verification) | Phase 6 (Evolution) |
|---|---|---|---|---|---|---|
| **GPT** | Author this Goal Card | Author `*_ARCHITECTURE_DECISION.md` | — | Review architecture sign-off | Review verification sign-off | — |
| **Meta_Kim** | Librarian: surface prior Goals (G2_DOCNO, MDM-001/002, Employee) | Conductor: workflow blueprint; Sentinel: V1 freeze risk check | Artisagent: skill fit; Sentinel: bash/secret guardrails; Conductor: shard orchestration | Prism: drift detection | Librarian: cross-Goal memory | Chrysalis: writeback reusable pattern |
| **Codex** | — | — | Implement: `PurchaseOrder` domain + application + infrastructure + migration + tests | — | — | — |
| **MiniMax** | — | Architecture review | Real-time review of hot files | Full code/test review | Write `*_VERIFICATION_REPORT.md`; declare Gate | Writeback content (if pattern emerged) |
| **Human** | Ratify Goal Card | Ratify architecture | — | — | Ratify Gate | Commit + push authorization |

---

## 3. Scope (In-Scope)

The Purchase Order module V1 is defined as:

1. **PO domain entity**: `PurchaseOrder` (header) + `PurchaseOrderLine` (lines)
2. **PO application service**: `IPurchaseOrderService` with `CreateDraftAsync`, `SubmitAsync`, `ApproveAsync`, `RejectAsync`, `CloseAsync`
3. **PO infrastructure**: `PurchaseOrderDbContext` + `PurchaseOrderRepository` + `PurchaseOrderConfiguration` (EF Core)
4. **PO migration**: `20260830_PURCHASE_ORDER_001_CreateSchema` (1 migration: tables + indexes + FKs)
5. **PO API endpoints**: `POST /api/v1/purchase/orders` (draft), `POST /api/v1/purchase/orders/{id}/submit`, `POST /api/v1/purchase/orders/{id}/approve`, `POST /api/v1/purchase/orders/{id}/reject`, `POST /api/v1/purchase/orders/{id}/close`, `GET /api/v1/purchase/orders/{id}`
6. **PO tests**: `PurchaseOrderServiceFacts` (unit, ~15 tests), `PurchaseOrderArchitectureFacts` (service-boundary), `PurchaseOrderRepositoryFacts` (integration, ~5 tests)
7. **PO permission codes**: `purchase.order.read`, `purchase.order.manage`, `purchase.order.submit`, `purchase.order.approve`, `purchase.order.close` (5 new permissions in `Identity.Application/GuliErpPermissions.cs`)
8. **PO document number generation**: re-uses `IDocumentNumberService.GenerateAsync` (the engine fix from G2_DOCNO_002 is the production path)
9. **PO ASP.NET Core policy registration**: 5 policies in `GuliErpAuthorizationPolicies.cs`
10. **PO Vue 3 frontend (if requested)**: deferred to a follow-up Goal (`GULIERP_PURCHASE_ORDER_UI_001`) — NOT in this V1

---

## 4. Out-of-Scope (V1)

- ❌ Vendor/BusinessPartner integration (BusinessPartner is the customer-side; vendor is MDM-000D deferred)
- ❌ Goods receipt (GR) — deferred to `GULIERP_GOODS_RECEIPT_001`
- ❌ Vendor invoice / 3-way match — deferred to `GULIERP_VENDOR_INVOICE_001`
- ❌ PO report / print — deferred to `GULIERP_PURCHASE_REPORT_001`
- ❌ PO approval workflow UI — deferred to `GULIERP_APPROVAL_WORKFLOW_UI_001`
- ❌ Multi-warehouse / multi-plant PO — out of V1
- ❌ PO revision (cancel + re-create) — out of V1
- ❌ Cross-currency PO — out of V1 (use CNY only in V1)
- ❌ PO attachment (file upload) — out of V1

---

## 5. Forbidden Follow-Ups

Per `GULIERP_GOAL_LIFECYCLE.md` anti-patterns, this Goal MUST NOT:

1. **Modify any V1 frozen contract** without an explicit unfreeze sub-Goal:
   - `FormatValidator.cs` regex (V1 frozen in `GULIERP_CODE_RULE_STANDARD_V1.md`)
   - `IDocumentNumberService` interface (V1 frozen by G2_DOCNO_001)
   - `IEntity` / `ITenantEntity` / `ICompanyScoped` interfaces (V1 frozen)
   - `PermissionRequirement` / `AuthorizationMiddleware` (V1 frozen)
   - `MdmValidationException` (V1 frozen)
2. **Add tests marked `Skip`** to make a build pass.
3. **Add a second migration in the same Goal** — `20260830_PURCHASE_ORDER_001_CreateSchema` is the only migration.
4. **Use `git add .`** — every `git add` must use explicit paths.
5. **Push without the user's explicit authorization.**
6. **Touch any test fixture (e.g., `g2-005-bootstrap-operator-user.ps1`) outside the PO scope.**
7. **Modify the NumberingRule service** — it is the production path; if a PO-specific override is needed, it must be a separate Goal.

---

## 6. Success Criteria (Phase 5 Gate)

The Goal is **VERIFIED** when ALL of the following are true:

| # | Criterion | Measurable |
|---|---|---|
| 1 | Build | `dotnet build GuliERP.slnx -c Release --no-restore` exit 0; 0 errors, 0 warnings |
| 2 | Unit tests | `dotnet test tests/GuliERP.Purchase.Tests` ≥ 15/15 PASS |
| 3 | Architecture tests | `dotnet test tests/GuliERP.Purchase.ArchitectureTests` ≥ 3/3 PASS (service boundary, tenant isolation, permission boundary) |
| 4 | Integration tests | `dotnet test tests/GuliERP.Purchase.IntegrationTests` (Operator PG only) ≥ 5/5 PASS |
| 5 | No regression | Foundation 44/44 + Identity 84/84 + MDM 221/221 + Sales 9/9 + Bootstrap 64/64 all pass (unchanged from baseline) |
| 6 | API contract verification | `docs/verification/GULIERP_PURCHASE_ORDER_001_API_CONTRACT_REPORT.md` written, contracts consistent with `G2_API_STANDARD_V1_DRAFT.md` |
| 7 | Runtime smoke | 2 draft POs created, both 201, both with sequential document numbers (`PO-<date>-000001`, `PO-<date>-000002`); counter `LastValue=2`; 2 PO rows in DB |
| 8 | Permission enforcement | Each of 5 new permissions tested: anonymous request → 401, wrong-role request → 403, correct-role request → 200 |
| 9 | No V1 contract violation | `git diff` shows ZERO changes to `FormatValidator.cs`, `IDocumentNumberService`, `IEntity`, `PermissionRequirement` |
| 10 | Honest disclosure | Verification report includes: any deferred work, any contradictions, any anti-patterns observed |
| 11 | Goal Card archived | `GOAL_REGISTRY.md` updated: `GULIERP_PURCHASE_ORDER_001_VERIFIED` entry |
| 12 | Committed | `git commit` with conventional message `feat(purchase): establish V1 Purchase Order module` |
| 13 | (Optional) Evolution writeback | If a reusable pattern emerged (e.g., "PO service follows same pattern as SO service"), write to `docs/governance/EVOLUTION_WRITEBACK.md` |

**Final Gate**: `GULIERP_PURCHASE_ORDER_001_VERIFIED`

---

## 7. Execution Plan (Phase 3 — Codex)

### 7.1 Shards (parallel where disjoint)

| Shard | Files | Owner | Parallel? |
|---|---|---|---|
| **S1. Domain** | `modules/purchase/GuliERP.Purchase.Domain/Entities/PurchaseOrder.cs` + `PurchaseOrderLine.cs` + `enums/PurchaseOrderStatus.cs` + `enums/ApprovalStatus.cs` | Codex | YES (independent of S2-S5) |
| **S2. Application** | `modules/purchase/GuliERP.Purchase.Application/IPurchaseOrderService.cs` + `PurchaseOrderService.cs` + `PurchaseOrderDtos.cs` + `PurchaseOrderPermissions.cs` | Codex | YES (after S1) |
| **S3. Infrastructure** | `modules/purchase/GuliERP.Purchase.Infrastructure/Purchase/PurchaseOrderRepository.cs` + `PurchaseOrderConfiguration.cs` + `Migrations/20260830_PURCHASE_ORDER_001_CreateSchema.cs` + `.Designer.cs` | Codex | YES (after S1) |
| **S4. Identity permission registration** | `modules/identity/GuliERP.Identity.Application/Authorization/GuliErpPermissions.cs` (add 5) + `GuliErpAuthorizationPolicies.cs` (add 5 policies) | Codex | YES (independent) |
| **S5. API endpoints** | `apps/api/GuliERP.Api/Purchase/PurchaseOrderEndpoints.cs` + `apps/api/GuliERP.Api/Program.cs` (DI registration) | Codex | YES (after S2, S3) |
| **S6. Unit tests** | `tests/GuliERP.Purchase.Tests/PurchaseOrderServiceFacts.cs` + `PurchaseOrderArchitectureFacts.cs` | Codex | YES (after S1, S2) |
| **S7. Integration tests** | `tests/GuliERP.Purchase.IntegrationTests/PurchaseOrderRepositoryFacts.cs` | Codex | YES (after S3) |

**Shards dependency graph**:
```
S1 (Domain) ─┬─→ S2 (Application) ─┐
              ├─→ S3 (Infrastructure) ─┼─→ S5 (API) ─┐
              ├─→ S6 (Unit tests) ──────────────────────┤
              └─→ S7 (Integration tests) ───────────────┤
                                                       ├─→ S8 (Local build + test)
S4 (Permissions) ───────────────────────────────────────┘
```

**S8. Local build + test** (final shard, serial):
- `dotnet build GuliERP.slnx -c Release --no-restore`
- `dotnet test tests/GuliERP.Purchase.Tests`
- `dotnet test tests/GuliERP.Purchase.ArchitectureTests`
- (Integration tests are Operator PG, NOT in Codex local test)

### 7.2 Stop conditions during execution

- Build takes > 10 min without progress → kill, retry
- Test fails and the fix is not obvious → stop, surface to user
- V1 contract is being touched without explicit unfreeze → STOP, escalate
- Scope drift (> 50 lines outside declared scope) → stop, ask for re-scope

---

## 8. Review Plan (Phase 4 — MiniMax)

MiniMax reviews the Codex output with focus on:

| # | Focus | What to check |
|---|---|---|
| 1 | **V1 contract compliance** | Confirm `git diff` shows ZERO changes to `FormatValidator.cs`, `IDocumentNumberService`, `IEntity`, `PermissionRequirement`, `MdmValidationException` |
| 2 | **Domain modeling** | `PurchaseOrder` entity has `IMultiTenant` + `ICompanyScoped` per V1 freeze; `PurchaseOrderLine` has `Id`, `PurchaseOrderId`, `LineNo`, `ItemId`, `Quantity`, `UnitPrice`, `TaxRate`, `NetAmount`, `TaxAmount`, `TotalAmount` per `GULIERP_CODE_RULE_STANDARD_V1.md` |
| 3 | **Permission boundary** | 5 new permissions registered in `GuliErpPermissions.cs` with consistent naming (`purchase.order.*`); 5 policies registered in `GuliErpAuthorizationPolicies.cs` with `AuthorizationPolicyBuilder.RequireClaim` |
| 4 | **Migration safety** | `20260830_PURCHASE_ORDER_001_CreateSchema` is idempotent (uses `IF NOT EXISTS`); no data loss on re-run; FK references existing tables (`mdm.gulierp_business_partner` for vendor, `mdm.gulierp_item` for item) |
| 5 | **API contract** | POST endpoints return 201 + Location header; GET endpoints return 200 + JSON; error responses use RFC7807 ProblemDetails per `G2_API_STANDARD_V1_DRAFT.md` |
| 6 | **Test coverage** | Unit tests cover happy path + 4+ edge cases per public method; integration tests cover DB roundtrip + tenant isolation + concurrent draft creation |
| 7 | **NumberingRule integration** | PO service uses `IDocumentNumberService.GenerateAsync` correctly; per `G2_DOCNO_002` engine fix, the call pattern matches the proven 10-consecutive test pattern |
| 8 | **Honest disclosure** | The review explicitly notes: any TODO / FIXME / deferral / known limitation |

### 8.1 Review sign-off format

```
Review Sign-off: APPROVED
Reviewed: <date> by MiniMax
Findings: <N findings, all addressed>
V1 contract compliance: VERIFIED (no changes to frozen contracts)
Drift detection (meta-prism): NO DRIFT
Anti-patterns: NONE
```

or

```
Review Sign-off: BLOCKED
Reasons: <file:line:rule>
Required fixes: <list>
```

---

## 9. Verification Plan (Phase 5 — MiniMax)

MiniMax writes `docs/verification/GULIERP_PURCHASE_ORDER_001_VERIFICATION_REPORT.md`:

| Section | Content |
|---|---|
| Test results | "Unit 15/15 PASS, Architecture 3/3 PASS, Integration 5/5 PASS (Operator PG)" |
| Build | "0 errors / 0 warnings, 11/11 projects" |
| Regression | "Foundation 44/44 + Identity 84/84 + MDM 221/221 + Sales 9/9 + Bootstrap 64/64 unchanged from baseline" |
| Runtime evidence | "2 PO drafts created via curl: SO#1 = PO-20260825-000001, SO#2 = PO-20260825-000002; counter LastValue=2; 2 rows in sales.gulierp_purchase_order" |
| V1 compliance | "git diff shows ZERO changes to V1 frozen contracts" |
| Honest disclosures | "TODO: vendor integration deferred to GULIERP_VENDOR_001; multi-warehouse deferred; cross-currency deferred" |
| **Final Gate** | **`GULIERP_PURCHASE_ORDER_001_VERIFIED`** (or `_BLOCKED` with reasons) |
| Recommended next action | "Commit + push (Human); open GULIERP_VENDOR_001 as next Goal" |

### 9.1 Operator runtime evidence

Codex cannot run Operator PG tests (no PGPASSWORD in agent session).
Human runs Operator tests on `gulierp_g2_003_test`:

```bash
$env:PGPASSWORD = "<REDACTED-POSTGRES-PASSWORD-2026-08-26>"
$env:ConnectionStrings__GuliERP = "Host=192.168.2.228;Port=5432;Database=gulierp_g2_003_test;Username=gulidata;Password=<REDACTED-POSTGRES-PASSWORD-2026-08-26>;Include Error Detail=true;Pooling=false"

cd D:\guli\projects\gulierp-next
dotnet test tests/GuliERP.Purchase.IntegrationTests -c Release --no-build --logger "trx;LogFileName=GULIERP_PURCHASE_ORDER_001.trx"

# 5 integration tests should pass

# Then manual smoke: 2 PO Drafts via API
curl -X POST http://127.0.0.1:5000/api/v1/purchase/orders -H "..." -d "{...PO Draft 1...}"
curl -X POST http://127.0.0.1:5000/api/v1/purchase/orders -H "..." -d "{...PO Draft 2...}"

# Verify DB state
psql -c "SELECT * FROM doc_kernel.document_number_counter WHERE PeriodKey = '20260825';"
psql -c "SELECT \"OrderNo\", \"Status\" FROM sales.gulierp_purchase_order;"
```

---

## 10. Commit Plan (Phase 6)

| Commit | Message | Files |
|---|---|---|
| 1 | `feat(purchase): add PO domain + application layer` | S1 + S2 files (~6 files) |
| 2 | `feat(purchase): add PO infrastructure + migration` | S3 files (~4 files, including 1 new migration) |
| 3 | `feat(purchase): add 5 PO permissions + policies` | S4 files (~2 files modified) |
| 4 | `feat(purchase): add PO API endpoints` | S5 files (~2 files) |
| 5 | `test(purchase): add unit + architecture tests` | S6 files (~3 files) |
| 6 | `test(purchase): add integration tests` | S7 files (~2 files) |
| 7 | `docs(verification): add GULIERP_PURCHASE_ORDER_001 verification report` | 1 file |

7 atomic commits, each self-contained and reversible by `git revert`.

---

## 11. Cross-Goal Links

This Goal depends on:
- ✅ `MDM_000_MASTER_DATA_CONVENTION_FROZEN` (V1 contract; provides BusinessPartner / Item entities)
- ✅ `G2_DOCNO_002_ENGINE_FIX_VERIFIED` (NumberingRule runtime)
- ✅ `GULIERP_GITHUB_BASELINE_READY` (project baseline)
- ✅ `GULIERP_META_KIM_MULTI_AGENT_ACCEPTANCE_READY` (governance — this Goal is the FIRST under the V1 protocol, in PROBATION mode)

This Goal unblocks:
- `GULIERP_GOODS_RECEIPT_001` (depends on PO module)
- `GULIERP_VENDOR_INVOICE_001` (depends on PO + GoodsReceipt)
- `GULIERP_APPROVAL_WORKFLOW_UI_001` (depends on PO + SO approval workflow)
- `GULIERP_PURCHASE_ORDER_UI_001` (depends on PO module)

---

## 12. Sign-off

**Gate**: `GULIERP_PURCHASE_ORDER_001_GOAL_CARD_PROPOSAL`

- ✅ Goal ID, Owner, Status, Entry gate
- ✅ 5-role Agent 分工 (7 phases × 5 roles)
- ✅ 10 in-scope items, 10 out-of-scope items
- ✅ 7 Forbidden follow-ups
- ✅ 13 Success criteria (Phase 5 Gate)
- ✅ Execution Plan (7 shards + dependency graph)
- ✅ Review Plan (8 focus areas + sign-off format)
- ✅ Verification Plan (Operator PG evidence + 7 sections)
- ✅ Commit Plan (7 atomic commits)
- ✅ Cross-Goal links (4 dependencies, 4 unblocks)
- ✅ No code / DB / migration change (design only)
- ✅ Simulation only — NOT an active Goal

**Author**: Mavis (M3 / mavis)
**Date**: 2026-08-25 (Asia/Shanghai)
**Status**: **PROPOSAL** — User may ratify to make this the actual `GULIERP_PURCHASE_ORDER_001` Goal.
**Next deliverable**: `GULIERP_META_KIM_MULTI_AGENT_ACCEPTANCE_REPORT.md` (TASK 5, final)
